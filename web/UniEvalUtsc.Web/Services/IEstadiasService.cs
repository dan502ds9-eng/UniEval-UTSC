namespace UniEvalUtsc.Web.Services;

public interface IEstadiasService
{
    Task<IReadOnlyList<GrupoEstadias>> ObtenerGruposAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AsignacionTutorEstadias>> ObtenerAsignacionesAsync(string grupoId, CancellationToken ct = default);

    Task AsignarTutorATodoElGrupoAsync(string grupoId, string tutorId, CancellationToken ct = default);

    Task CambiarTutorAsync(string alumnoId, string? tutorId, CancellationToken ct = default);

    Task GuardarAsignacionesAsync(string grupoId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ObtenerTutoresAsync(CancellationToken ct = default);
}
