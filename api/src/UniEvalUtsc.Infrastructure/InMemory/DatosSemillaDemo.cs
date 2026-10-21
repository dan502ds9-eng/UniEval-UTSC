using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed record AsignacionAcademicaDemo(
    string Materia,
    string Docente,
    int CargaAcademicaId);

public sealed record GrupoDemo(
    int Id,
    string Nombre,
    string Cuatrimestre,
    bool EsEstadias,
    IReadOnlyCollection<AsignacionAcademicaDemo> Materias,
    string? Tutor);

public static class DatosSemillaDemo
{
    public const string Tutora = "Mónica Janeth Rodríguez Reyna";

    public static IReadOnlyCollection<GrupoDemo> Grupos { get; } =
    [
        new GrupoDemo(1, "DSM05AV", "5to", false,
        [
            new("Inglés V", "Viridiana Nataly Pintor Jimenez", 1),
            new("Aplicaciones Web Orientada a Servicios", "Óscar Gabriel Chávez Gómez", 2),
            new("Ecuaciones Diferenciales", "José Manuel Chávez García", 3),
            new("Liderazgo de Equipos de Alto Desempeño", "Martha Laura Esparza Robles", 4),
            new("Bases de Datos Avanzadas", "Sem Tomás Hernández Martínez", 5),
            new("Estándares y Métricas para el Desarrollo de Software", "Humberto Hernández Ochoa", 6),
            new("Proyecto Integrador II", "Juan Pablo Vega Ramírez", 7)
        ], Tutora),
        new GrupoDemo(2, "[Grupo 5to B]", "5to", false, [], "[Tutor de grupo]"),
        new GrupoDemo(3, "[Grupo 6to A]", "6to", true, [], "[Tutor de estadías 1]"),
        new GrupoDemo(4, "[Grupo 10mo A]", "10mo", true, [], "[Tutor de estadías 2]")
    ];

    public static IReadOnlyCollection<Periodo> CrearPeriodos()
    {
        var periodoActual = new Periodo(
            "Sep–Dic 2026",
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 12, 31),
            new DateTime(2026, 10, 13),
            new DateTime(2026, 10, 31, 23, 59, 59),
            id: 1);
        periodoActual.Abrir();

        var periodosCerrados = new[]
        {
            CrearPeriodoCerrado(2, "May–Ago 2026", new DateOnly(2026, 5, 1), new DateOnly(2026, 8, 31)),
            CrearPeriodoCerrado(3, "Ene–Abr 2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)),
            CrearPeriodoCerrado(4, "Sep–Dic 2025", new DateOnly(2025, 9, 1), new DateOnly(2025, 12, 31))
        };

        return [periodoActual, .. periodosCerrados];
    }

    public static IReadOnlyCollection<Plantilla> CrearPlantillas()
    {
        var docente = new Plantilla(
            TipoEncuesta.Docente,
            "FORM-AC16",
            "1",
            "Por favor califique la actuación de cada uno de sus profesores en los siguientes aspectos a lo largo del presente cuatrimestre.",
            id: 16);
        var aspectosDocente = new[]
        {
            "Al inicio del curso dio a conocer los objetivos, temario y forma de evaluación.",
            "Las actividades de aprendizaje se apegaron a los temas cubiertos y a los objetivos del curso.",
            "Completó satisfactoriamente la totalidad del temario y el horario establecido.",
            "Mostró dominio y experiencia en los temas de su materia.",
            "Empleó estrategias de enseñanza como investigaciones, dinámicas de grupo, trabajos en equipo, etc., que facilitaron el dominio de la materia.",
            "Estimuló la creatividad y el análisis crítico de los contenidos, usando ejemplos prácticos.",
            "Revisó las actividades y resultados de aprendizaje, entregando retroalimentación.",
            "Las calificaciones se determinaron según la forma de evaluación establecida.",
            "Mantuvo el control y la atención de la clase.",
            "Mostró respeto y disposición a los alumnos, dentro y fuera del salón de clase."
        };

        for (var index = 0; index < aspectosDocente.Length; index++)
            docente.AgregarAspecto(index + 1, aspectosDocente[index], id: index + 1);
        docente.Publicar();

        var tutor = new Plantilla(
            TipoEncuesta.Tutor,
            "FORM-AC64",
            "1",
            "Por favor califique la actuación de su tutor en los siguientes aspectos a lo largo del presente cuatrimestre.",
            id: 64);
        var aspectosTutor = new[]
        {
            "El tutor estableció los objetivos de la tutoría y el plan de trabajo para las sesiones del cuatrimestre.",
            "El tutor asistió puntualmente a las sesiones.",
            "El tutor generó un clima propicio para la comunicación.",
            "El tutor mostró una actitud de respeto hacia los alumnos.",
            "El tutor mantuvo informado al grupo de las opciones de apoyo al estudiante, de tipo académico (cursos, conferencias, etc.) y de otro tipo (apoyo psicológico, atención médica, becas, seguro facultativo, etc.) que ofrece la Universidad.",
            "El tutor brindó la orientación oportuna con el fin de encontrar opciones para atender las dificultades académicas, personales y/o de salud.",
            "El tutor mostró interés en los aspectos personales y académicos que podían repercutir en el desempeño académico de los alumnos."
        };

        for (var index = 0; index < aspectosTutor.Length; index++)
            tutor.AgregarAspecto(index + 1, aspectosTutor[index], id: index + 11);
        tutor.Publicar();

        return [docente, tutor];
    }

    public static IReadOnlyCollection<EncuestaAsignada> CrearEncuestasAsignadas()
    {
        var grupo = Grupos.Single(g => g.Id == 1);
        var encuestasDocente = grupo.Materias.Select((materia, index) =>
            EncuestaAsignada.ParaDocente(
                alumnoId: 1,
                grupoId: grupo.Id,
                plantillaId: 16,
                cargaAcademicaId: materia.CargaAcademicaId,
                id: index + 1));

        return
        [
            .. encuestasDocente,
            EncuestaAsignada.ParaTutor(
                alumnoId: 1,
                grupoId: grupo.Id,
                plantillaId: 64,
                tutorId: 1,
                id: 8)
        ];
    }

    private static Periodo CrearPeriodoCerrado(int id, string nombre, DateOnly inicio, DateOnly fin)
    {
        var periodo = new Periodo(
            nombre,
            inicio,
            fin,
            inicio.ToDateTime(new TimeOnly(0, 0)),
            fin.ToDateTime(new TimeOnly(23, 59)),
            id);
        periodo.Abrir();
        periodo.Cerrar();
        return periodo;
    }
}
