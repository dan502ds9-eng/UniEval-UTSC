namespace UniEvalUtsc.Web.Services;

public enum EstadoPeriodoGestion
{
    EnPreparacion,
    Abierto,
    Cerrado
}

public sealed class PeriodoAdministrativo(
    string id,
    string nombre,
    EstadoPeriodoGestion estado,
    DateOnly inicioCaptura,
    DateOnly finCaptura,
    DateOnly inicioEncuestas,
    DateOnly cierreEncuestas)
{
    public string Id { get; } = id;
    public string Nombre { get; set; } = nombre;
    public EstadoPeriodoGestion Estado { get; internal set; } = estado;
    public DateOnly InicioCaptura { get; internal set; } = inicioCaptura;
    public DateOnly FinCaptura { get; internal set; } = finCaptura;
    public DateOnly InicioEncuestas { get; internal set; } = inicioEncuestas;
    public DateOnly CierreEncuestas { get; internal set; } = cierreEncuestas;
    public int EncuestasGeneradas { get; internal set; }
}

public sealed record RegistroImportacion(int Renglon, string Campo, string Mensaje);

public sealed record ResultadoImportacion(int Renglones, int Importados, IReadOnlyList<RegistroImportacion> Errores);

public sealed record AsignacionCarga(
    string Id,
    string GrupoId,
    string Grupo,
    string Materia,
    string Docente,
    bool EsTutor);

public sealed record GrupoEstadias(string Id, string Nombre, int Cuatrimestre);

public sealed class AsignacionTutorEstadias(string alumnoId, string nombre, string correo, string empresa)
{
    public string AlumnoId { get; } = alumnoId;
    public string Nombre { get; } = nombre;
    public string Correo { get; } = correo;
    public string Empresa { get; } = empresa;
    public string? TutorId { get; set; }
}

public sealed record ElementoCatalogo(
    string Id,
    string Nombre,
    string Contexto,
    bool Activo = true);

public sealed record AspectoPlantilla(int Id, string Texto);

public sealed class PlantillaAdministrativa(
    string id,
    string nombre,
    int version,
    bool vigente,
    IReadOnlyList<AspectoPlantilla> aspectos)
{
    public string Id { get; } = id;
    public string Nombre { get; } = nombre;
    public int Version { get; } = version;
    public bool Vigente { get; internal set; } = vigente;
    public bool Borrador { get; internal set; }
    public IReadOnlyList<AspectoPlantilla> Aspectos { get; internal set; } = aspectos;
}
