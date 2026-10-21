using UniEvalUtsc.Contracts;
using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public sealed class ResultadosInMemoryService(IPeriodosService? periodosService = null) : IResultadosService
{
    private static readonly IReadOnlyList<PeriodoResultado> PeriodosPredeterminados =
    [
        new("sep-dic-2026", "Sep–Dic 2026", true, false),
        new("may-ago-2026", "May–Ago 2026", false),
        new("ene-abr-2026", "Ene–Abr 2026", false)
    ];

    private static readonly IReadOnlyList<ElementoResultado> MateriasDocente =
    [
        new("web-dsm05av", "Aplicaciones Web Orientada a Servicios", "DSM05AV · Grupo A", 26, 30),
        new("bd-dsm05av", "Bases de Datos Avanzadas", "DSM05AV · Grupo A", 23, 30),
        new("ecuaciones-dsm05av", "Ecuaciones Diferenciales", "DSM05AV · Grupo A", 4, 30)
    ];

    private static readonly IReadOnlyList<ElementoResultado> GruposTutor =
    [
        new("tutoria-dsm05av", "Grupo de tutoría", "DSM05AV · Grupo A", 22, 30)
    ];

    public IReadOnlyList<PeriodoResultado> ObtenerPeriodos() => periodosService is null
        ? PeriodosPredeterminados
        : periodosService.ObtenerPeriodos()
            .Select(periodo => new PeriodoResultado(
                periodo.Id,
                periodo.Nombre,
                periodo.Estado == EstadoPeriodoGestion.Abierto,
                periodo.Estado == EstadoPeriodoGestion.Cerrado))
            .ToArray();

    public IReadOnlyList<ElementoResultado> ObtenerElementos(Rol rol) => rol switch
    {
        Rol.Docente => MateriasDocente,
        Rol.Tutor => GruposTutor,
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "El rol debe ser Docente o Tutor.")
    };

    public IReadOnlyList<TarjetaResultado> ObtenerTarjetasInicio(Rol rol, string periodoId)
    {
        var periodo = ObtenerPeriodo(periodoId);
        return ObtenerElementos(rol)
            .Select(elemento =>
            {
                var resultado = CrearResultado(rol, periodo, elemento);
                var mensaje = !periodo.Cerrado
                    ? "Disponible al cierre del periodo."
                    : resultado.Respuestas < 5
                        ? "No se muestran resultados con menos de 5 respuestas (RN-10)."
                        : null;
                return new TarjetaResultado(elemento, resultado, mensaje);
            })
            .ToArray();
    }

    public ResultadoPersonal? ObtenerResultado(Rol rol, string periodoId, string elementoId)
    {
        var periodo = ObtenerPeriodo(periodoId);
        var elemento = ObtenerElementos(rol).SingleOrDefault(item => item.Id == elementoId)
            ?? throw new ArgumentException("No se encontró la materia o el grupo indicado.", nameof(elementoId));
        var resultado = CrearResultado(rol, periodo, elemento);
        return !periodo.Cerrado || resultado.Respuestas < 5 ? null : resultado;
    }

    public IReadOnlyList<PuntoHistorico> ObtenerHistorico(Rol rol, string elementoId)
    {
        var elemento = ObtenerElementos(rol).SingleOrDefault(item => item.Id == elementoId)
            ?? throw new ArgumentException("No se encontró la materia o el grupo indicado.", nameof(elementoId));

        return ObtenerPeriodos()
            .Where(periodo => periodo.Cerrado && elemento.AlumnosRespondieron >= 5)
            .Select((periodo, indice) =>
            {
                var promedio = PromedioBase(rol, elemento, indice + 1);
                return new PuntoHistorico(periodo.Id, periodo.Nombre, promedio, Niveles.Nivel(promedio).ToString());
            })
            .Reverse()
            .ToArray();
    }

    public IReadOnlyList<FilaReportePersonal> ObtenerReportes(Rol rol) =>
        ObtenerElementos(rol)
            .Where(elemento => elemento.AlumnosRespondieron >= 5)
            .SelectMany(elemento => ObtenerPeriodos()
                .Where(periodo => periodo.Cerrado)
                .Select((periodo, indice) =>
                {
                    var promedio = PromedioBase(rol, elemento, indice + 1);
                    return new FilaReportePersonal(
                        periodo.Id,
                        periodo.Nombre,
                        elemento.Id,
                        elemento.Nombre,
                        elemento.Contexto,
                        promedio,
                        Niveles.Nivel(promedio).ToString());
                }))
            .OrderByDescending(reporte => reporte.PeriodoId, StringComparer.Ordinal)
            .ThenBy(reporte => reporte.ElementoNombre, StringComparer.Ordinal)
            .ToArray();

    private PeriodoResultado ObtenerPeriodo(string periodoId) =>
        ObtenerPeriodos().SingleOrDefault(periodo => periodo.Id == periodoId)
        ?? throw new ArgumentException("No se encontró el periodo solicitado.", nameof(periodoId));

    private static ResultadoPersonal CrearResultado(
        Rol rol,
        PeriodoResultado periodo,
        ElementoResultado elemento)
    {
        var semilla = Semilla(rol.ToString(), periodo.Id, elemento.Id);
        var cantidad = rol == Rol.Tutor ? 7 : 10;
        var aspectos = Enumerable.Range(1, cantidad)
            .Select(indice =>
            {
                var variacion = ((semilla / (indice + 3) + indice * 17) % 31 - 15) / 10m;
                var promedio = Math.Clamp(7.3m + variacion + (periodo.Cerrado ? 0.4m : 0), 1m, 10m);
                var promedioCarrera = Math.Clamp(promedio + ((indice % 3) - 1) * 0.4m, 1m, 10m);
                return new AspectoResultado(
                    indice,
                    TextoAspecto(rol, indice),
                    promedio,
                    promedioCarrera,
                    Niveles.Nivel(promedio).ToString());
            })
            .ToArray();
        var general = Niveles.Redondear(aspectos.Average(aspecto => aspecto.Promedio));
        var respuestas = periodo.Cerrado ? elemento.AlumnosRespondieron : Math.Max(0, elemento.AlumnosRespondieron - 3);

        return new ResultadoPersonal(
            elemento,
            periodo,
            general,
            respuestas,
            elemento.TotalAlumnos,
            Niveles.Redondear(aspectos.Average(aspecto => aspecto.PromedioCarrera)),
            aspectos);
    }

    private static decimal PromedioBase(Rol rol, ElementoResultado elemento, int indicePeriodo)
    {
        var semilla = Semilla(rol.ToString(), elemento.Id);
        var promedio = 6.4m + ((semilla / (indicePeriodo + 7) + indicePeriodo * 13) % 29) / 10m;
        return Niveles.Redondear(Math.Clamp(promedio, 1m, 10m));
    }

    private static long Semilla(params string[] valores)
    {
        var hash = 2166136261u;
        foreach (var character in string.Join('|', valores))
        {
            hash = unchecked((hash ^ character) * 16777619u);
        }

        return hash;
    }

    private static string TextoAspecto(Rol rol, int indice)
    {
        if (rol == Rol.Tutor)
        {
            return indice switch
            {
                1 => "Estableció los objetivos de la tutoría y el plan de trabajo.",
                2 => "Asistió puntualmente a las sesiones.",
                3 => "Generó un clima propicio para la comunicación.",
                4 => "Mostró una actitud de respeto hacia los alumnos.",
                5 => "Mantuvo informado al grupo de las opciones de apoyo.",
                6 => "Brindó orientación oportuna ante dificultades.",
                _ => "Mostró interés en los aspectos personales y académicos."
            };
        }

        return indice switch
        {
            1 => "Comunicó objetivos, temario y forma de evaluación.",
            2 => "Alineó las actividades con los temas y objetivos.",
            3 => "Completó el temario y el horario establecido.",
            4 => "Mostró dominio y experiencia en los temas.",
            5 => "Empleó estrategias de enseñanza efectivas.",
            6 => "Estimuló la creatividad y el análisis crítico.",
            7 => "Revisó actividades y entregó retroalimentación.",
            8 => "Determinó calificaciones según la evaluación establecida.",
            9 => "Mantuvo el control y la atención de la clase.",
            _ => "Mostró respeto y disposición a los alumnos."
        };
    }
}

public sealed record PeriodoResultado(string Id, string Nombre, bool Abierto, bool Cerrado = true);

public sealed record ElementoResultado(
    string Id,
    string Nombre,
    string Contexto,
    int AlumnosRespondieron,
    int TotalAlumnos);

public sealed record TarjetaResultado(
    ElementoResultado Elemento,
    ResultadoPersonal Resultado,
    string? MensajeNoDisponible);

public sealed record ResultadoPersonal(
    ElementoResultado Elemento,
    PeriodoResultado Periodo,
    decimal PromedioGeneral,
    int Respuestas,
    int TotalAlumnos,
    decimal PromedioCarrera,
    IReadOnlyList<AspectoResultado> Aspectos)
{
    public decimal DiferenciaCarrera => Math.Round(PromedioGeneral - PromedioCarrera, 1, MidpointRounding.AwayFromZero);

    public IReadOnlyList<AspectoResultado> AreasOportunidad =>
        Aspectos.OrderBy(aspecto => aspecto.Promedio).ThenBy(aspecto => aspecto.Numero).Take(3).ToArray();

    public IReadOnlyList<AspectoResultado> Fortalezas =>
        Aspectos.OrderByDescending(aspecto => aspecto.Promedio).ThenBy(aspecto => aspecto.Numero).Take(3).ToArray();
}

public sealed record AspectoResultado(
    int Numero,
    string Texto,
    decimal Promedio,
    decimal PromedioCarrera,
    string Nivel);

public sealed record PuntoHistorico(string PeriodoId, string Periodo, decimal Promedio, string Nivel);

public sealed record FilaReportePersonal(
    string PeriodoId,
    string Periodo,
    string ElementoId,
    string ElementoNombre,
    string Contexto,
    decimal Promedio,
    string Nivel);
