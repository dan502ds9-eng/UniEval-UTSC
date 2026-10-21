using UniEvalUtsc.Contracts;
using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public sealed class CoordinacionInMemoryService : ICoordinacionService
{
    private static readonly IReadOnlyList<GrupoParticipacion> Grupos =
    [
        new("dsm05av-a", "DSM05AV", 5, 15, 31, false),
        new("dsm05av-b", "DSM05AV · Grupo B", 5, 17, 31, false),
        new("estadia-6-a", "DSM06AV · Grupo A", 6, 27, 28, true),
        new("estadia-10-a", "DSM10AV · Grupo A", 10, 20, 25, true)
    ];

    private static readonly IReadOnlyList<ResultadoCoordinacion> ResultadosDocentes =
    [
        CrearResultado("doc-1", "Sem Tomás Hernández Martínez", "Bases de Datos Avanzadas · DSM05AV", 5, 7.1m, 15, false, false),
        CrearResultado("doc-2", "José Manuel Chávez García", "Ecuaciones Diferenciales · DSM05AV", 5, 7.8m, 15, false, false),
        CrearResultado("doc-3", "Humberto Hernández Ochoa", "Estándares y Métricas para el Desarrollo de Software · DSM05AV", 5, 8.2m, 15, false, false),
        CrearResultado("doc-4", "Viridiana Nataly Pintor Jiménez", "Inglés V · DSM05AV", 5, 8.3m, 15, false, false),
        CrearResultado("doc-5", "Juan Pablo Vega Ramírez", "Proyecto Integrador II · DSM05AV", 5, 8.5m, 15, false, false),
        CrearResultado("doc-6", "Óscar Gabriel Chávez Gómez", "Aplicaciones Web Orientada a Servicios · DSM05AV", 5, 8.7m, 15, false, false),
        CrearResultado("doc-7", "Martha Laura Esparza Robles", "Liderazgo de Equipos de Alto Desempeño · DSM05AV", 5, 8.7m, 15, false, false),
        CrearResultado("tutor-5", "Mónica Janeth Rodríguez Reyna", "Tutoría · DSM05AV", 5, 8.1m, 15, true, false),
        CrearResultado("tutor-6", "Tutor de estadías asignado", "DSM06AV · Grupo A", 6, 8.1m, 27, true, true),
        CrearResultado("tutor-10", "Tutor de estadías asignado", "DSM10AV · Grupo A", 10, 8.1m, 20, true, true)
    ];

    private static readonly IReadOnlyList<string> PendientesGrupoA =
    [
        "Alexis García Martínez",
        "Camila Hernández Ruiz",
        "Diego López Sánchez",
        "Fernanda Torres Medina",
        "Jorge Ramírez Silva",
        "Mariana Cruz Flores",
        "Ricardo Pérez Gómez",
        "Sofía Morales Reyes",
        "Andrea Hernández Cantú",
        "Brandon Martínez Ríos",
        "Daniela Garza Castillo",
        "Eduardo Vázquez Lara",
        "Fátima Cárdenas Ortiz",
        "Gabriel Rangel Soto",
        "Héctor Salinas Díaz",
        "Isabel Flores Lozano"
    ];

    public ResumenCoordinacion ObtenerResumen(string periodoId, string cuatrimestre, string carreraId)
    {
        var grupos = ObtenerGrupos(periodoId, cuatrimestre, carreraId);
        var total = grupos.Sum(grupo => grupo.Total);
        var contestaron = grupos.Sum(grupo => grupo.Contestaron);
        var docentes = ObtenerResultados(periodoId, cuatrimestre, tutores: false, carreraId);
        var tutores = ObtenerResultados(periodoId, cuatrimestre, tutores: true, carreraId);
        var promedio = docentes.Count == 0 ? 0 : docentes.Average(resultado => resultado.Promedio);

        return new ResumenCoordinacion(
            total == 0 ? 0 : Math.Round(contestaron * 100m / total, 1),
            contestaron,
            total,
            Niveles.Redondear(promedio),
            docentes.Concat(tutores).Count(resultado => resultado.Promedio < 8));
    }

    public IReadOnlyList<GrupoParticipacion> ObtenerGrupos(string periodoId, string cuatrimestre, string carreraId)
    {
        ValidarPeriodo(periodoId);
        if (!EsCarreraDisponible(carreraId))
        {
            return [];
        }

        var cuatrimestres = ObtenerCuatrimestres(cuatrimestre);
        return Grupos
            .Where(grupo => cuatrimestres is null || cuatrimestres.Contains(grupo.Cuatrimestre))
            .OrderBy(grupo => grupo.Porcentaje)
            .ToArray();
    }

    public IReadOnlyList<string> ObtenerAlumnosPendientes(string grupoId, string carreraId)
    {
        if (!EsCarreraDisponible(carreraId))
        {
            return [];
        }

        var grupo = Grupos.SingleOrDefault(item => item.Id == grupoId)
            ?? throw new ArgumentException("No se encontró el grupo solicitado.", nameof(grupoId));

        return grupo.Id == "dsm05av-a"
            ? PendientesGrupoA.Take(grupo.Faltan).ToArray()
            : Enumerable.Range(1, grupo.Faltan).Select(indice => $"Alumno pendiente {indice}").ToArray();
    }

    public IReadOnlyList<ResultadoCoordinacion> ObtenerResultados(
        string periodoId,
        string cuatrimestre,
        bool tutores,
        string carreraId)
    {
        ValidarPeriodo(periodoId);
        if (!EsCarreraDisponible(carreraId))
        {
            return [];
        }

        var cuatrimestres = ObtenerCuatrimestres(cuatrimestre);
        if (!tutores && cuatrimestre is ("6" or "10"))
        {
            return [];
        }

        var resultados = ResultadosDocentes.Where(resultado =>
            (cuatrimestres is null || cuatrimestres.Contains(resultado.Cuatrimestre))
            && (tutores ? resultado.EsTutor : !resultado.EsTutor)
            && (cuatrimestre is not ("6" or "10") || resultado.EsTutorEstadias)
            && (cuatrimestre != "5" || !resultado.EsTutorEstadias))
            .Select(resultado => resultado with { Periodo = NombrePeriodo(periodoId) });

        return resultados
            .Where(resultado => resultado.Respuestas >= 5)
            .OrderBy(resultado => resultado.Promedio)
            .ThenBy(resultado => resultado.Nombre, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<IReadOnlyList<string>> ObtenerDatosReporte(
        string tipo,
        string periodoId,
        string cuatrimestre,
        string carreraId)
    {
        return tipo switch
        {
            "docente" => FilasResultados(ObtenerResultados(periodoId, cuatrimestre, tutores: false, carreraId)),
            "tutor" => FilasResultados(ObtenerResultados(periodoId, cuatrimestre, tutores: true, carreraId)),
            "concentrado" => FilasResultados(
                ObtenerResultados(periodoId, cuatrimestre, tutores: false, carreraId)
                    .Concat(ObtenerResultados(periodoId, cuatrimestre, tutores: true, carreraId))
                    .OrderBy(resultado => resultado.Promedio)),
            "participacion" => FilasParticipacion(ObtenerGrupos(periodoId, cuatrimestre, carreraId)),
            "historico" => FilasHistorico(ObtenerResultados(periodoId, cuatrimestre, tutores: false, carreraId)),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "El tipo de reporte no es válido.")
        };
    }

    private static IReadOnlyList<IReadOnlyList<string>> FilasResultados(
        IEnumerable<ResultadoCoordinacion> resultados) =>
        [
            ["Nombre", "Contexto", "Periodo", "Promedio", "Nivel", "Respuestas"],
            .. resultados.Select(resultado => (IReadOnlyList<string>)
            [
                resultado.Nombre,
                resultado.Contexto,
                resultado.Periodo,
                resultado.Promedio.ToString("0.0"),
                Niveles.Nivel(resultado.Promedio).ToString(),
                resultado.Respuestas.ToString()
            ])
        ];

    private static IReadOnlyList<IReadOnlyList<string>> FilasParticipacion(
        IEnumerable<GrupoParticipacion> grupos) =>
        [
            ["Grupo", "Cuatrimestre", "Participación", "Contestaron", "Total", "Faltan"],
            .. grupos.Select(grupo => (IReadOnlyList<string>)
            [
                grupo.Nombre,
                grupo.Cuatrimestre.ToString(),
                $"{grupo.Porcentaje:0.0}%",
                grupo.Contestaron.ToString(),
                grupo.Total.ToString(),
                grupo.Faltan.ToString()
            ])
        ];

    private static IReadOnlyList<IReadOnlyList<string>> FilasHistorico(
        IEnumerable<ResultadoCoordinacion> resultados) =>
        [
            ["Periodo", "Evaluado", "Contexto", "Promedio", "Nivel"],
            .. resultados.SelectMany(resultado => new[]
            {
                CrearFilaHistorico("Ene–Abr 2026", resultado, -0.4m),
                CrearFilaHistorico("May–Ago 2026", resultado, -0.2m),
                CrearFilaHistorico("Sep–Dic 2026", resultado, 0m)
            })
        ];

    private static ResultadoCoordinacion CrearResultado(
        string id,
        string nombre,
        string contexto,
        int cuatrimestre,
        decimal promedio,
        int respuestas,
        bool tutor,
        bool tutorEstadias)
    {
        var cantidad = tutorEstadias ? 7 : 10;
        var aspectos = Enumerable.Range(1, cantidad)
            .Select(numero =>
            {
                var variacion = (numero % 5 - 2) / 10m;
                var aspecto = Math.Clamp(promedio + variacion, 1m, 10m);
                var promedioCarrera = Math.Clamp(aspecto + (numero % 3 - 1) * 0.3m, 1m, 10m);
                return new AspectoResultado(
                    numero,
                    tutorEstadias ? TextoAspectoTutor(numero) : TextoAspectoDocente(numero),
                    aspecto,
                    promedioCarrera,
                    Niveles.Nivel(aspecto).ToString());
            })
            .ToArray();

        return new ResultadoCoordinacion(
            id,
            nombre,
            contexto,
            "May–Ago 2026",
            cuatrimestre,
            Niveles.Redondear(aspectos.Average(aspecto => aspecto.Promedio)),
            Niveles.Redondear(aspectos.Average(aspecto => aspecto.PromedioCarrera)),
            respuestas,
            aspectos,
            tutor,
            tutorEstadias);
    }

    private static int[]? ObtenerCuatrimestres(string cuatrimestre) =>
        cuatrimestre switch
        {
            "todos" => null,
            "5" => [5],
            "6" => [6],
            "10" => [10],
            _ => throw new ArgumentOutOfRangeException(nameof(cuatrimestre), cuatrimestre, "El cuatrimestre no es válido.")
        };

    private static void ValidarPeriodo(string periodoId)
    {
        if (periodoId is not ("sep-dic-2026" or "may-ago-2026" or "ene-abr-2026"))
        {
            throw new ArgumentException("No se encontró el periodo solicitado.", nameof(periodoId));
        }
    }

    private static bool EsCarreraDisponible(string carreraId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carreraId);
        return string.Equals(carreraId, FiltrosState.CarreraCoordinadorId, StringComparison.Ordinal)
            || string.Equals(carreraId, "todas", StringComparison.Ordinal);
    }

    private static string NombrePeriodo(string periodoId) => periodoId switch
    {
        "sep-dic-2026" => "Sep–Dic 2026",
        "may-ago-2026" => "May–Ago 2026",
        "ene-abr-2026" => "Ene–Abr 2026",
        _ => throw new ArgumentException("No se encontró el periodo solicitado.", nameof(periodoId))
    };

    private static IReadOnlyList<string> CrearFilaHistorico(
        string periodo,
        ResultadoCoordinacion resultado,
        decimal variacion)
    {
        var promedio = Niveles.Redondear(Math.Clamp(resultado.Promedio + variacion, 1m, 10m));
        return
        [
            periodo,
            resultado.Nombre,
            resultado.Contexto,
            promedio.ToString("0.0"),
            Niveles.Nivel(promedio).ToString()
        ];
    }

    private static string TextoAspectoTutor(int numero) => numero switch
    {
        1 => "Estableció los objetivos de la tutoría y el plan de trabajo.",
        2 => "Asistió puntualmente a las sesiones.",
        3 => "Generó un clima propicio para la comunicación.",
        4 => "Mostró una actitud de respeto hacia los alumnos.",
        5 => "Mantuvo informado al grupo de las opciones de apoyo.",
        6 => "Brindó orientación oportuna ante dificultades.",
        _ => "Mostró interés en los aspectos personales y académicos."
    };

    private static string TextoAspectoDocente(int numero) => numero switch
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
