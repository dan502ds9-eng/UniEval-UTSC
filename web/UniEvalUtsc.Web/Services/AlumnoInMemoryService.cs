using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Services;

public sealed class AlumnoInMemoryService(
    IConfiguration configuration,
    UnievalJsInterop jsInterop,
    ILogger<AlumnoInMemoryService> logger,
    IPeriodosService? periodosService = null) : IAlumnoService
{
    private const string PeriodoId = "sep-dic-2026";
    private const string PeriodoNombre = "Sep–Dic 2026";
    private readonly List<MensajeAlumno> _mensajes =
    [
        new("mensaje-1", "Coordinación DSM", "Recordatorio de evaluación", "Te invitamos a completar tus evaluaciones pendientes del cuatrimestre.", DateOnly.FromDateTime(new DateTime(2026, 10, 20)), false),
        new("mensaje-2", "Servicios Escolares", "Evaluación disponible", "Ya puedes contestar la evaluación del cuatrimestre.", DateOnly.FromDateTime(new DateTime(2026, 10, 18)), false)
    ];
    private bool _enviado;
    private DateOnly? _fechaEnvio;

    public EvaluacionAlumno ObtenerEvaluacion()
    {
        var esEstadia = configuration.GetValue("Alumno:EsEstadia", false);
        var periodoActual = periodosService?.ObtenerActual();
        var periodoAbierto = periodoActual is null
            ? configuration.GetValue("Alumno:PeriodoAbierto", true)
            : periodoActual.Estado == EstadoPeriodoGestion.Abierto;
        IReadOnlyList<PreguntaAlumno> preguntas = esEstadia
            ? PreguntasEstadia
            : PreguntasDocente;
        IReadOnlyList<EvaluadoAlumno> evaluados = esEstadia
            ? EvaluadosEstadia
            : EvaluadosDocentes;

        return new EvaluacionAlumno(
            periodoActual?.Id ?? PeriodoId,
            periodoActual?.Nombre ?? PeriodoNombre,
            esEstadia ? "Grupo 10mo A · estadías" : "Grupo DSM05AV",
            periodoAbierto,
            esEstadia,
            preguntas,
            evaluados,
            _enviado,
            _fechaEnvio);
    }

    public async Task<Dictionary<string, int>> RecuperarBorradorAsync(string alumno)
    {
        var valor = await jsInterop.GetLocalStorageAsync(ClaveBorrador(alumno));
        if (string.IsNullOrWhiteSpace(valor))
        {
            return [];
        }

        Dictionary<string, int>? borrador;
        try
        {
            borrador = JsonSerializer.Deserialize<Dictionary<string, int>>(valor);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "El borrador de evaluación del alumno no tiene un formato válido.");
            return [];
        }

        if (borrador is null)
        {
            logger.LogError("El borrador de evaluación del alumno contiene un valor nulo.");
            return [];
        }

        var llavesValidas = ObtenerLlavesValidas();
        return borrador
            .Where(par => llavesValidas.Contains(par.Key) && par.Value is >= 1 and <= 10)
            .ToDictionary(par => par.Key, par => par.Value);
    }

    public async Task<bool> GuardarBorradorAsync(string alumno, IReadOnlyDictionary<string, int> calificaciones)
    {
        ValidarCalificaciones(calificaciones);
        var resultado = await jsInterop.SetLocalStorageAsync(
            ClaveBorrador(alumno),
            JsonSerializer.Serialize(calificaciones));
        if (!resultado)
        {
            logger.LogError("No fue posible guardar el borrador de evaluación en localStorage.");
        }

        return resultado;
    }

    public async Task<ResultadoEnvioAlumno> EnviarAsync(
        string alumno,
        IReadOnlyDictionary<string, int> calificaciones)
    {
        var evaluacion = ObtenerEvaluacion();
        if (_enviado)
        {
            return new ResultadoEnvioAlumno(false, "La evaluación ya fue enviada.");
        }

        if (!evaluacion.PeriodoAbierto)
        {
            return new ResultadoEnvioAlumno(false, "El periodo no está abierto para recibir respuestas.");
        }

        ValidarCalificaciones(calificaciones);
        var faltantes = ObtenerLlavesValidas().Except(calificaciones.Keys).Count();
        if (faltantes > 0)
        {
            return new ResultadoEnvioAlumno(false, $"Faltan {faltantes} respuestas por contestar.");
        }

        _enviado = true;
        _fechaEnvio = DateOnly.FromDateTime(DateTime.Today);
        var eliminado = await jsInterop.SetLocalStorageAsync(ClaveBorrador(alumno), string.Empty);
        if (!eliminado)
        {
            logger.LogError("No fue posible limpiar el borrador después de enviar la evaluación.");
        }

        return new ResultadoEnvioAlumno(true, string.Empty);
    }

    public ComprobanteAlumno? ObtenerComprobante()
    {
        if (!_enviado || _fechaEnvio is null)
        {
            return null;
        }

        var evaluacion = ObtenerEvaluacion();
        return new ComprobanteAlumno(
            PeriodoNombre,
            evaluacion.Evaluados.Count,
            evaluacion.Evaluados.Count,
            _fechaEnvio);
    }

    public IReadOnlyList<MensajeAlumno> ObtenerMensajes() => _mensajes;

    public int MensajesSinLeer => _mensajes.Count(mensaje => !mensaje.Leido);

    public void CrearRecordatorio(string grupoId, string nombreGrupo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(grupoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreGrupo);

        var grupoAsignado = configuration["Alumno:GrupoId"] ?? "dsm05av-a";
        if (!string.Equals(grupoAsignado, grupoId, StringComparison.Ordinal))
        {
            return;
        }

        _mensajes.Insert(0, new MensajeAlumno(
            $"recordatorio-{Guid.NewGuid():N}",
            "Coordinación DSM",
            $"Recordatorio de evaluación · {nombreGrupo}",
            "Te invitamos a completar tus evaluaciones pendientes del cuatrimestre.",
            DateOnly.FromDateTime(DateTime.Today),
            false));
    }

    public void MarcarMensajeComoLeido(string id)
    {
        var mensaje = _mensajes.SingleOrDefault(mensaje => mensaje.Id == id)
            ?? throw new ArgumentException("No se encontró el mensaje del alumno.", nameof(id));
        mensaje.Leido = true;
    }

    public void MarcarTodosComoLeidos()
    {
        foreach (var mensaje in _mensajes)
        {
            mensaje.Leido = true;
        }
    }

    private string ClaveBorrador(string alumno) =>
        $"unieval-borrador:{PeriodoId}:{alumno}";

    private HashSet<string> ObtenerLlavesValidas()
    {
        var evaluacion = ObtenerEvaluacion();
        return evaluacion.Preguntas
            .SelectMany(pregunta => evaluacion.Evaluados.Select(evaluado =>
                EvaluacionAlumno.CrearClaveCalificacion(pregunta.Id, evaluado.Id)))
            .ToHashSet(StringComparer.Ordinal);
    }

    private void ValidarCalificaciones(IReadOnlyDictionary<string, int> calificaciones)
    {
        var llavesValidas = ObtenerLlavesValidas();
        if (calificaciones.Any(par => !llavesValidas.Contains(par.Key) || par.Value is < 1 or > 10))
        {
            throw new ArgumentException("El borrador contiene una pregunta, evaluado o calificación no válida.", nameof(calificaciones));
        }
    }

    private static readonly IReadOnlyList<EvaluadoAlumno> EvaluadosDocentes =
    [
        new("docente-1", "Inglés V", "Viridiana Nataly Pintor Jiménez", false, "Docente"),
        new("docente-2", "Aplicaciones Web Orientada a Servicios", "Óscar Gabriel Chávez Gómez", false, "Docente"),
        new("docente-3", "Ecuaciones Diferenciales", "José Manuel Chávez García", false, "Docente"),
        new("docente-4", "Liderazgo de Equipos de Alto Desempeño", "Martha Laura Esparza Robles", false, "Docente"),
        new("docente-5", "Bases de Datos Avanzadas", "Sem Tomás Hernández Martínez", false, "Docente"),
        new("docente-6", "Estándares y Métricas para el Desarrollo de Software", "Humberto Hernández Ochoa", false, "Docente"),
        new("docente-7", "Proyecto Integrador II", "Juan Pablo Vega Ramírez", false, "Docente"),
        new("tutora", "Tutoría", "Mónica Janeth Rodríguez Reyna", true, "Tutora académica")
    ];

    private static readonly IReadOnlyList<EvaluadoAlumno> EvaluadosEstadia =
    [
        new("tutor-estadias", "Tutoría de estadías", "[Tutor de estadías asignado]", true, "Tutor de estadías")
    ];

    private static readonly IReadOnlyList<PreguntaAlumno> PreguntasDocente =
    [
        new(1, "Al inicio del curso dio a conocer los objetivos, temario y forma de evaluación."),
        new(2, "Las actividades de aprendizaje se apegaron a los temas cubiertos y a los objetivos del curso."),
        new(3, "Completó satisfactoriamente la totalidad del temario y el horario establecido."),
        new(4, "Mostró dominio y experiencia en los temas de su materia."),
        new(5, "Empleó estrategias de enseñanza como investigaciones, dinámicas de grupo, trabajos en equipo, etc., que facilitaron el dominio de la materia."),
        new(6, "Estimuló la creatividad y el análisis crítico de los contenidos, usando ejemplos prácticos."),
        new(7, "Revisó las actividades y resultados de aprendizaje, entregando retroalimentación."),
        new(8, "Las calificaciones se determinaron según la forma de evaluación establecida."),
        new(9, "Mantuvo el control y la atención de la clase."),
        new(10, "Mostró respeto y disposición a los alumnos, dentro y fuera del salón de clase.")
    ];

    private static readonly IReadOnlyList<PreguntaAlumno> PreguntasEstadia =
    [
        new(1, "El tutor estableció los objetivos de la tutoría y el plan de trabajo para las sesiones del cuatrimestre."),
        new(2, "El tutor asistió puntualmente a las sesiones."),
        new(3, "El tutor generó un clima propicio para la comunicación."),
        new(4, "El tutor mostró una actitud de respeto hacia los alumnos."),
        new(5, "El tutor mantuvo informado al grupo de las opciones de apoyo al estudiante, de tipo académico y de otro tipo que ofrece la Universidad."),
        new(6, "El tutor brindó la orientación oportuna con el fin de encontrar opciones para atender las dificultades académicas, personales y/o de salud."),
        new(7, "El tutor mostró interés en los aspectos personales y académicos que podían repercutir en el desempeño académico de los alumnos.")
    ];
}
