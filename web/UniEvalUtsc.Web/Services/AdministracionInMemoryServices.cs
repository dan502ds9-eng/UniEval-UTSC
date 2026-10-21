using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public sealed class PeriodosInMemoryService(
    ICargaAcademicaService? cargaAcademica = null,
    IEstadiasService? estadias = null) : IPeriodosService
{
    private readonly List<PeriodoAdministrativo> _periodos =
    [
        new(
            "sep-dic-2026",
            "Sep–Dic 2026",
            EstadoPeriodoGestion.Abierto,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 12, 18),
            new DateOnly(2026, 9, 15),
            new DateOnly(2026, 12, 10)),
        new(
            "may-ago-2026",
            "May–Ago 2026",
            EstadoPeriodoGestion.Cerrado,
            new DateOnly(2026, 5, 4),
            new DateOnly(2026, 8, 28),
            new DateOnly(2026, 5, 18),
            new DateOnly(2026, 8, 20)),
        new(
            "ene-abr-2026",
            "Ene–Abr 2026",
            EstadoPeriodoGestion.Cerrado,
            new DateOnly(2026, 1, 5),
            new DateOnly(2026, 4, 24),
            new DateOnly(2026, 1, 19),
            new DateOnly(2026, 4, 16))
    ];

    public IReadOnlyList<PeriodoAdministrativo> ObtenerPeriodos() => _periodos;

    public PeriodoAdministrativo ObtenerActual() =>
        _periodos.FirstOrDefault(periodo => periodo.Estado == EstadoPeriodoGestion.Abierto)
        ?? _periodos.FirstOrDefault(periodo => periodo.Estado == EstadoPeriodoGestion.EnPreparacion)
        ?? _periodos.First();

    public PeriodoAdministrativo CrearPeriodo(string nombre, DateOnly inicio, DateOnly cierre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        if (inicio >= cierre)
        {
            throw new ArgumentException("La fecha de inicio debe ser anterior a la fecha de cierre.", nameof(cierre));
        }

        var periodo = new PeriodoAdministrativo(
            $"periodo-{Guid.NewGuid():N}",
            nombre.Trim(),
            EstadoPeriodoGestion.EnPreparacion,
            inicio,
            cierre,
            inicio,
            cierre);
        _periodos.Insert(0, periodo);
        return periodo;
    }

    public void EditarFechas(
        string periodoId,
        DateOnly inicioCaptura,
        DateOnly finCaptura,
        DateOnly inicioEncuestas,
        DateOnly cierreEncuestas)
    {
        var periodo = Obtener(periodoId);
        if (periodo.Estado != EstadoPeriodoGestion.EnPreparacion)
        {
            throw new InvalidOperationException("Las fechas solo se pueden editar mientras el periodo está en preparación.");
        }

        if (inicioCaptura >= finCaptura || inicioEncuestas >= cierreEncuestas
            || inicioEncuestas < inicioCaptura || cierreEncuestas > finCaptura)
        {
            throw new ArgumentException("Las fechas deben estar en orden y dentro del periodo de captura.");
        }

        periodo.InicioCaptura = inicioCaptura;
        periodo.FinCaptura = finCaptura;
        periodo.InicioEncuestas = inicioEncuestas;
        periodo.CierreEncuestas = cierreEncuestas;
    }

    public int Abrir(string periodoId)
    {
        var periodo = Obtener(periodoId);
        if (periodo.Estado != EstadoPeriodoGestion.EnPreparacion)
        {
            throw new InvalidOperationException("Solo se puede abrir un periodo en preparación.");
        }

        if (_periodos.Any(otro => otro.Estado == EstadoPeriodoGestion.Abierto))
        {
            throw new InvalidOperationException("Ya hay otro periodo abierto. Cierra el periodo actual antes de abrir uno nuevo.");
        }

        var asignaciones = cargaAcademica?.ObtenerAsignaciones() ?? [];
        var encuestasDocentes = asignaciones.Count(asignacion => !asignacion.EsTutor) * 30;
        var encuestasTutorGrupo = asignaciones.Count(asignacion => asignacion.EsTutor) * 30;
        var encuestasEstadias = estadias is null
            ? 0
            : estadias.ObtenerGrupos()
                .SelectMany(grupo => estadias.ObtenerAsignaciones(grupo.Id))
                .Count(asignacion => !string.IsNullOrWhiteSpace(asignacion.TutorId));
        var cantidadEncuestas = encuestasDocentes + encuestasTutorGrupo + encuestasEstadias;
        periodo.EncuestasGeneradas = cantidadEncuestas;
        periodo.Estado = EstadoPeriodoGestion.Abierto;
        return cantidadEncuestas;
    }

    public void Cerrar(string periodoId)
    {
        var periodo = Obtener(periodoId);
        if (periodo.Estado != EstadoPeriodoGestion.Abierto)
        {
            throw new InvalidOperationException("Solo se puede cerrar un periodo abierto.");
        }

        periodo.Estado = EstadoPeriodoGestion.Cerrado;
    }

    private PeriodoAdministrativo Obtener(string periodoId) =>
        _periodos.SingleOrDefault(periodo => periodo.Id == periodoId)
        ?? throw new ArgumentException("No se encontró el periodo solicitado.", nameof(periodoId));
}

public sealed class CargaAcademicaInMemoryService : ICargaAcademicaService
{
    private readonly List<AsignacionCarga> _asignaciones =
    [
        new("carga-web", "dsm05av", "DSM05AV · Grupo A", "Aplicaciones Web Orientada a Servicios", "Óscar Gabriel Chávez Gómez", false),
        new("carga-bd", "dsm05av", "DSM05AV · Grupo A", "Bases de Datos Avanzadas", "Sem Tomás Hernández Martínez", false),
        new("carga-ecuaciones", "dsm05av", "DSM05AV · Grupo A", "Ecuaciones Diferenciales", "José Manuel Chávez García", false),
        new("carga-ingles", "dsm05av", "DSM05AV · Grupo A", "Inglés V", "Viridiana Nataly Pintor Jiménez", false),
        new("carga-tutor", "dsm05av", "DSM05AV · Grupo A", "Tutor del grupo", "Mónica Janeth Rodríguez Reyna", true)
    ];

    private static readonly IReadOnlyList<string> Docentes =
    [
        "Óscar Gabriel Chávez Gómez",
        "Sem Tomás Hernández Martínez",
        "José Manuel Chávez García",
        "Viridiana Nataly Pintor Jiménez",
        "Mónica Janeth Rodríguez Reyna",
        "Humberto Hernández Ochoa",
        "Juan Pablo Vega Ramírez",
        "Martha Laura Esparza Robles"
    ];

    public IReadOnlyList<AsignacionCarga> ObtenerAsignaciones() => _asignaciones;

    public IReadOnlyList<string> ObtenerDocentes() => Docentes;

    public void CambiarDocente(string asignacionId, string docente)
    {
        if (!Docentes.Contains(docente, StringComparer.Ordinal))
        {
            throw new ArgumentException("Selecciona un docente del catálogo.", nameof(docente));
        }

        var indice = _asignaciones.FindIndex(asignacion => asignacion.Id == asignacionId);
        if (indice < 0)
        {
            throw new ArgumentException("No se encontró la asignación académica.", nameof(asignacionId));
        }

        _asignaciones[indice] = _asignaciones[indice] with { Docente = docente };
    }

    public ResultadoImportacion Importar(IReadOnlyList<IReadOnlyList<string>> filas)
    {
        var errores = new List<RegistroImportacion>();
        if (filas.Count < 2)
        {
            return new ResultadoImportacion(0, 0, [new RegistroImportacion(1, "archivo", "El archivo no contiene renglones de carga.")]);
        }

        var nuevas = new List<AsignacionCarga>();
        for (var indice = 1; indice < filas.Count; indice++)
        {
            var fila = filas[indice];
            if (fila.Count < 4)
            {
                errores.Add(new RegistroImportacion(indice + 1, "renglón", "Se requieren grupo, materia, docente y tipo."));
                continue;
            }

            var grupo = fila[0].Trim();
            var materia = fila[1].Trim();
            var docente = fila[2].Trim();
            var esTutor = string.Equals(fila[3].Trim(), "tutor", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(grupo))
            {
                errores.Add(new RegistroImportacion(indice + 1, "grupo", "El grupo es obligatorio."));
            }

            if (string.IsNullOrWhiteSpace(materia))
            {
                errores.Add(new RegistroImportacion(indice + 1, "materia", "La materia es obligatoria."));
            }

            if (!Docentes.Contains(docente, StringComparer.Ordinal))
            {
                errores.Add(new RegistroImportacion(indice + 1, "docente", "El docente no existe en el catálogo."));
            }

            if (errores.All(error => error.Renglon != indice + 1))
            {
                nuevas.Add(new AsignacionCarga(
                    $"carga-{Guid.NewGuid():N}",
                    grupo.ToLowerInvariant(),
                    grupo,
                    materia,
                    docente,
                    esTutor));
            }
        }

        if (errores.Count == 0)
        {
            _asignaciones.AddRange(nuevas);
        }

        return new ResultadoImportacion(filas.Count - 1, errores.Count == 0 ? nuevas.Count : 0, errores);
    }
}

public sealed class EstadiasInMemoryService : IEstadiasService
{
    private static readonly IReadOnlyList<GrupoEstadias> Grupos =
    [
        new("estadia-6-a", "DSM06AV · Grupo A", 6),
        new("estadia-10-a", "DSM10AV · Grupo A", 10)
    ];

    private static readonly IReadOnlyList<string> Tutores =
    [
        "Mónica Janeth Rodríguez Reyna",
        "Humberto Hernández Ochoa",
        "Juan Pablo Vega Ramírez"
    ];

    private readonly Dictionary<string, List<AsignacionTutorEstadias>> _alumnos = new(StringComparer.Ordinal)
    {
        ["estadia-6-a"] =
        [
            new("alumno-est-1", "Andrea Hernández Cantú", "a***@utsc.edu.mx", "Grupo Industrial Regio"),
            new("alumno-est-2", "Brandon Martínez Ríos", "b***@utsc.edu.mx", "Tecnología y Manufactura"),
            new("alumno-est-3", "Camila Hernández Ruiz", "c***@utsc.edu.mx", "Grupo Industrial Regio")
        ],
        ["estadia-10-a"] =
        [
            new("alumno-est-4", "Diego López Sánchez", "d***@utsc.edu.mx", "Soluciones Digitales"),
            new("alumno-est-5", "Fernanda Torres Medina", "f***@utsc.edu.mx", "Tecnología y Manufactura"),
            new("alumno-est-6", "Jorge Ramírez Silva", "j***@utsc.edu.mx", "Soluciones Digitales")
        ]
    };

    public IReadOnlyList<GrupoEstadias> ObtenerGrupos() => Grupos;

    public IReadOnlyList<AsignacionTutorEstadias> ObtenerAsignaciones(string grupoId) =>
        ObtenerAlumnos(grupoId);

    public void AsignarTutorATodoElGrupo(string grupoId, string tutorId)
    {
        ValidarTutor(tutorId);
        foreach (var alumno in ObtenerAlumnos(grupoId))
        {
            alumno.TutorId = tutorId;
        }
    }

    public void CambiarTutor(string alumnoId, string? tutorId)
    {
        if (tutorId is not null)
        {
            ValidarTutor(tutorId);
        }

        var alumno = _alumnos.Values.SelectMany(alumnos => alumnos)
            .SingleOrDefault(alumno => alumno.AlumnoId == alumnoId)
            ?? throw new ArgumentException("No se encontró el alumno de estadías.", nameof(alumnoId));
        alumno.TutorId = tutorId;
    }

    public void GuardarAsignaciones(string grupoId)
    {
        _ = ObtenerAlumnos(grupoId);
    }

    public IReadOnlyList<string> ObtenerTutores() => Tutores;

    private List<AsignacionTutorEstadias> ObtenerAlumnos(string grupoId) =>
        _alumnos.TryGetValue(grupoId, out var alumnos)
            ? alumnos
            : throw new ArgumentException("No se encontró el grupo de estadías.", nameof(grupoId));

    private static void ValidarTutor(string tutorId)
    {
        if (!Tutores.Contains(tutorId, StringComparer.Ordinal))
        {
            throw new ArgumentException("Selecciona un tutor de estadías del catálogo.", nameof(tutorId));
        }
    }
}

public sealed class CatalogosInMemoryService : ICatalogosService
{
    private readonly Dictionary<string, List<ElementoCatalogo>> _catalogos = new(StringComparer.Ordinal)
    {
        ["carreras"] = [new("dsm", "Desarrollo de Software Multiplataforma", "Carrera"), new("mti", "Mantenimiento Industrial", "Carrera")],
        ["grupos"] = [new("dsm05av", "DSM05AV", "5to · DSM"), new("dsm06av", "DSM06AV", "6to · estadías"), new("dsm10av", "DSM10AV", "10mo · estadías")],
        ["materias"] = [new("materia-web", "Aplicaciones Web Orientada a Servicios", "DSM05AV")],
        ["docentes"] = [new("docente-1", "Óscar Gabriel Chávez Gómez", "Docente")],
        ["alumnos"] = [new("alumno-1", "Alumno de ejemplo", "a***@utsc.edu.mx")]
    };

    public IReadOnlyList<ElementoCatalogo> Obtener(string tipo) => Catalogo(tipo);

    public void Agregar(string tipo, string nombre, string contexto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        var catalogo = Catalogo(tipo);
        if (catalogo.Any(elemento => string.Equals(elemento.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Ya existe un registro con ese nombre.", nameof(nombre));
        }

        catalogo.Add(new ElementoCatalogo(Guid.NewGuid().ToString("N"), nombre.Trim(), contexto.Trim()));
    }

    public void CambiarEstado(string tipo, string id)
    {
        var catalogo = Catalogo(tipo);
        var indice = catalogo.FindIndex(elemento => elemento.Id == id);
        if (indice < 0)
        {
            throw new ArgumentException("No se encontró el registro del catálogo.", nameof(id));
        }

        catalogo[indice] = catalogo[indice] with { Activo = !catalogo[indice].Activo };
    }

    public ResultadoImportacion Importar(string tipo, IReadOnlyList<IReadOnlyList<string>> filas)
    {
        var errores = new List<RegistroImportacion>();
        if (filas.Count < 2)
        {
            return new ResultadoImportacion(0, 0, [new RegistroImportacion(1, "archivo", "El archivo no contiene datos.")]);
        }

        var nuevas = new List<ElementoCatalogo>();
        for (var indice = 1; indice < filas.Count; indice++)
        {
            var fila = filas[indice];
            var nombre = fila.Count > 0 ? fila[0].Trim() : string.Empty;
            var contexto = fila.Count > 1 ? fila[1].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(nombre))
            {
                errores.Add(new RegistroImportacion(indice + 1, "nombre", "El nombre es obligatorio."));
            }
            else if (Catalogo(tipo).Any(elemento => string.Equals(elemento.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
            {
                errores.Add(new RegistroImportacion(indice + 1, "nombre", "El registro ya existe en el catálogo."));
            }
            else
            {
                nuevas.Add(new ElementoCatalogo(Guid.NewGuid().ToString("N"), nombre, contexto));
            }
        }

        if (errores.Count == 0)
        {
            Catalogo(tipo).AddRange(nuevas);
        }

        return new ResultadoImportacion(filas.Count - 1, errores.Count == 0 ? nuevas.Count : 0, errores);
    }

    private List<ElementoCatalogo> Catalogo(string tipo) =>
        _catalogos.TryGetValue(tipo, out var catalogo)
            ? catalogo
            : throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "El catálogo seleccionado no existe.");
}

public sealed class PlantillasInMemoryService : IPlantillasService
{
    private readonly List<PlantillaAdministrativa> _plantillas =
    [
        Crear("form-ac16", "FORM-AC16 · Desempeño docente", 1, true, 10),
        Crear("form-ac64", "FORM-AC64 · Desempeño del tutor", 1, true, 7)
    ];

    public IReadOnlyList<PlantillaAdministrativa> ObtenerPlantillas() => _plantillas;

    public PlantillaAdministrativa Obtener(string plantillaId) =>
        _plantillas.SingleOrDefault(plantilla => plantilla.Id == plantillaId)
        ?? throw new ArgumentException("No se encontró la plantilla solicitada.", nameof(plantillaId));

    public PlantillaAdministrativa CrearBorrador(string plantillaId)
    {
        var vigente = ObtenerPlantillaVigente(plantillaId);
        var indiceVersion = plantillaId.IndexOf("-v", StringComparison.Ordinal);
        var idBase = indiceVersion < 0 ? plantillaId : plantillaId[..indiceVersion];
        var existente = _plantillas.SingleOrDefault(plantilla => plantilla.Id == $"{idBase}-v{vigente.Version + 1}");
        if (existente is not null)
        {
            return existente;
        }

        var borrador = new PlantillaAdministrativa(
            $"{idBase}-v{vigente.Version + 1}",
            vigente.Nombre,
            vigente.Version + 1,
            false,
            vigente.Aspectos.ToArray())
        {
            Borrador = true
        };
        _plantillas.Add(borrador);
        return borrador;
    }

    public void ReemplazarAspectos(string plantillaId, IReadOnlyList<AspectoPlantilla> aspectos)
    {
        ArgumentNullException.ThrowIfNull(aspectos);
        var plantilla = Obtener(plantillaId);
        ValidarBorrador(plantilla);
        if (aspectos.Count == 0 || aspectos.Any(aspecto => string.IsNullOrWhiteSpace(aspecto.Texto)))
        {
            throw new ArgumentException("La plantilla debe conservar al menos un aspecto y todos deben tener texto.", nameof(aspectos));
        }

        plantilla.Aspectos = aspectos.Select((aspecto, indice) => new AspectoPlantilla(indice + 1, aspecto.Texto.Trim())).ToArray();
    }

    public void Publicar(string plantillaId)
    {
        var borrador = Obtener(plantillaId);
        ValidarBorrador(borrador);
        var vigente = ObtenerPlantillaVigente(plantillaId[..plantillaId.LastIndexOf("-v", StringComparison.Ordinal)]);
        vigente.Vigente = false;
        borrador.Borrador = false;
        borrador.Vigente = true;
    }

    public void Descartar(string plantillaId)
    {
        var borrador = Obtener(plantillaId);
        ValidarBorrador(borrador);
        _plantillas.Remove(borrador);
    }

    private PlantillaAdministrativa ObtenerPlantillaVigente(string plantillaId) =>
        _plantillas.Where(plantilla =>
                (plantilla.Id == plantillaId || plantilla.Id.StartsWith($"{plantillaId}-v", StringComparison.Ordinal))
                && plantilla.Vigente)
            .SingleOrDefault()
        ?? throw new ArgumentException("No se encontró la versión vigente de la plantilla.", nameof(plantillaId));

    private static void ValidarBorrador(PlantillaAdministrativa plantilla)
    {
        if (!plantilla.Borrador)
        {
            throw new InvalidOperationException("La operación solo está permitida en una versión borrador.");
        }
    }

    private static PlantillaAdministrativa Crear(string id, string nombre, int version, bool vigente, int cantidad)
    {
        var aspectos = Enumerable.Range(1, cantidad)
            .Select(indice => new AspectoPlantilla(indice, TextoAspecto(indice)))
            .ToArray();
        return new PlantillaAdministrativa(id, nombre, version, vigente, aspectos);
    }

    private static string TextoAspecto(int indice) => indice switch
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
