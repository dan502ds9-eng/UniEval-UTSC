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

    public Task<IReadOnlyList<PeriodoAdministrativo>> ObtenerPeriodosAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<PeriodoAdministrativo>>(_periodos);
    }

    public Task<PeriodoAdministrativo> ObtenerActualAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(
            _periodos.FirstOrDefault(periodo => periodo.Estado == EstadoPeriodoGestion.Abierto)
            ?? _periodos.FirstOrDefault(periodo => periodo.Estado == EstadoPeriodoGestion.EnPreparacion)
            ?? _periodos.First());
    }

    public Task<PeriodoAdministrativo> CrearPeriodoAsync(
        string nombre,
        DateOnly inicio,
        DateOnly cierre,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
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
        return Task.FromResult(periodo);
    }

    public Task EditarFechasAsync(
        string periodoId,
        DateOnly inicioCaptura,
        DateOnly finCaptura,
        DateOnly inicioEncuestas,
        DateOnly cierreEncuestas,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
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
        return Task.CompletedTask;
    }

    public async Task<int> AbrirAsync(string periodoId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var periodo = Obtener(periodoId);
        if (periodo.Estado != EstadoPeriodoGestion.EnPreparacion)
        {
            throw new InvalidOperationException("Solo se puede abrir un periodo en preparación.");
        }

        if (_periodos.Any(otro => otro.Estado == EstadoPeriodoGestion.Abierto))
        {
            throw new InvalidOperationException("Ya hay otro periodo abierto. Cierra el periodo actual antes de abrir uno nuevo.");
        }

        var asignaciones = cargaAcademica is null
            ? []
            : await cargaAcademica.ObtenerAsignacionesAsync(ct);
        var encuestasDocentes = asignaciones.Count(asignacion => !asignacion.EsTutor) * 30;
        var encuestasTutorGrupo = asignaciones.Count(asignacion => asignacion.EsTutor) * 30;
        var encuestasEstadias = estadias is null
            ? 0
            : await ContarEncuestasEstadiasAsync(estadias, ct);
        var cantidadEncuestas = encuestasDocentes + encuestasTutorGrupo + encuestasEstadias;
        periodo.EncuestasGeneradas = cantidadEncuestas;
        periodo.Estado = EstadoPeriodoGestion.Abierto;
        return cantidadEncuestas;
    }

    public Task CerrarAsync(string periodoId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var periodo = Obtener(periodoId);
        if (periodo.Estado != EstadoPeriodoGestion.Abierto)
        {
            throw new InvalidOperationException("Solo se puede cerrar un periodo abierto.");
        }

        periodo.Estado = EstadoPeriodoGestion.Cerrado;
        return Task.CompletedTask;
    }

    private static async Task<int> ContarEncuestasEstadiasAsync(IEstadiasService estadias, CancellationToken ct)
    {
        var grupos = await estadias.ObtenerGruposAsync(ct);
        var cantidad = 0;
        foreach (var grupo in grupos)
        {
            var asignaciones = await estadias.ObtenerAsignacionesAsync(grupo.Id, ct);
            cantidad += asignaciones.Count(asignacion => !string.IsNullOrWhiteSpace(asignacion.TutorId));
        }

        return cantidad;
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

    public Task<IReadOnlyList<AsignacionCarga>> ObtenerAsignacionesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<AsignacionCarga>>(_asignaciones);
    }

    public Task<IReadOnlyList<string>> ObtenerDocentesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Docentes);
    }

    public Task CambiarDocenteAsync(string asignacionId, string docente, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
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
        return Task.CompletedTask;
    }

    public Task<ResultadoImportacion> ImportarAsync(
        IReadOnlyList<IReadOnlyList<string>> filas,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var errores = new List<RegistroImportacion>();
        if (filas.Count < 2)
        {
            return Task.FromResult(new ResultadoImportacion(0, 0, [new RegistroImportacion(1, "archivo", "El archivo no contiene renglones de carga.")]));
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

        return Task.FromResult(new ResultadoImportacion(filas.Count - 1, errores.Count == 0 ? nuevas.Count : 0, errores));
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

    public Task<IReadOnlyList<GrupoEstadias>> ObtenerGruposAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Grupos);
    }

    public Task<IReadOnlyList<AsignacionTutorEstadias>> ObtenerAsignacionesAsync(
        string grupoId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<AsignacionTutorEstadias>>(ObtenerAlumnos(grupoId));
    }

    public Task AsignarTutorATodoElGrupoAsync(string grupoId, string tutorId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidarTutor(tutorId);
        foreach (var alumno in ObtenerAlumnos(grupoId))
        {
            alumno.TutorId = tutorId;
        }

        return Task.CompletedTask;
    }

    public Task CambiarTutorAsync(string alumnoId, string? tutorId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (tutorId is not null)
        {
            ValidarTutor(tutorId);
        }

        var alumno = _alumnos.Values.SelectMany(alumnos => alumnos)
            .SingleOrDefault(alumno => alumno.AlumnoId == alumnoId)
            ?? throw new ArgumentException("No se encontró el alumno de estadías.", nameof(alumnoId));
        alumno.TutorId = tutorId;
        return Task.CompletedTask;
    }

    public Task GuardarAsignacionesAsync(string grupoId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _ = ObtenerAlumnos(grupoId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ObtenerTutoresAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Tutores);
    }

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

    public Task<IReadOnlyList<ElementoCatalogo>> ObtenerAsync(string tipo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<ElementoCatalogo>>(Catalogo(tipo));
    }

    public Task AgregarAsync(string tipo, string nombre, string contexto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        var catalogo = Catalogo(tipo);
        if (catalogo.Any(elemento => string.Equals(elemento.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Ya existe un registro con ese nombre.", nameof(nombre));
        }

        catalogo.Add(new ElementoCatalogo(Guid.NewGuid().ToString("N"), nombre.Trim(), contexto.Trim()));
        return Task.CompletedTask;
    }

    public Task CambiarEstadoAsync(string tipo, string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var catalogo = Catalogo(tipo);
        var indice = catalogo.FindIndex(elemento => elemento.Id == id);
        if (indice < 0)
        {
            throw new ArgumentException("No se encontró el registro del catálogo.", nameof(id));
        }

        catalogo[indice] = catalogo[indice] with { Activo = !catalogo[indice].Activo };
        return Task.CompletedTask;
    }

    public Task<ResultadoImportacion> ImportarAsync(
        string tipo,
        IReadOnlyList<IReadOnlyList<string>> filas,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var errores = new List<RegistroImportacion>();
        if (filas.Count < 2)
        {
            return Task.FromResult(new ResultadoImportacion(0, 0, [new RegistroImportacion(1, "archivo", "El archivo no contiene datos.")]));
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

        return Task.FromResult(new ResultadoImportacion(filas.Count - 1, errores.Count == 0 ? nuevas.Count : 0, errores));
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

    public Task<IReadOnlyList<PlantillaAdministrativa>> ObtenerPlantillasAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<PlantillaAdministrativa>>(_plantillas);
    }

    public Task<PlantillaAdministrativa> ObtenerAsync(string plantillaId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ObtenerCore(plantillaId));
    }

    public Task<PlantillaAdministrativa> CrearBorradorAsync(string plantillaId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(CrearBorradorCore(plantillaId));
    }

    public Task ReemplazarAspectosAsync(
        string plantillaId,
        IReadOnlyList<AspectoPlantilla> aspectos,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ReemplazarAspectosCore(plantillaId, aspectos);
        return Task.CompletedTask;
    }

    public Task PublicarAsync(string plantillaId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        PublicarCore(plantillaId);
        return Task.CompletedTask;
    }

    public Task DescartarAsync(string plantillaId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        DescartarCore(plantillaId);
        return Task.CompletedTask;
    }

    private PlantillaAdministrativa ObtenerCore(string plantillaId) =>
        _plantillas.SingleOrDefault(plantilla => plantilla.Id == plantillaId)
        ?? throw new ArgumentException("No se encontró la plantilla solicitada.", nameof(plantillaId));

    private PlantillaAdministrativa CrearBorradorCore(string plantillaId)
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

    private void ReemplazarAspectosCore(string plantillaId, IReadOnlyList<AspectoPlantilla> aspectos)
    {
        ArgumentNullException.ThrowIfNull(aspectos);
        var plantilla = ObtenerCore(plantillaId);
        ValidarBorrador(plantilla);
        if (aspectos.Count == 0 || aspectos.Any(aspecto => string.IsNullOrWhiteSpace(aspecto.Texto)))
        {
            throw new ArgumentException("La plantilla debe conservar al menos un aspecto y todos deben tener texto.", nameof(aspectos));
        }

        plantilla.Aspectos = aspectos.Select((aspecto, indice) => new AspectoPlantilla(indice + 1, aspecto.Texto.Trim())).ToArray();
    }

    private void PublicarCore(string plantillaId)
    {
        var borrador = ObtenerCore(plantillaId);
        ValidarBorrador(borrador);
        var vigente = ObtenerPlantillaVigente(plantillaId[..plantillaId.LastIndexOf("-v", StringComparison.Ordinal)]);
        vigente.Vigente = false;
        borrador.Borrador = false;
        borrador.Vigente = true;
    }

    private void DescartarCore(string plantillaId)
    {
        var borrador = ObtenerCore(plantillaId);
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
