namespace UniEvalUtsc.Web.Services;

public interface ICoordinacionService
{
    Task<ResumenCoordinacion> ObtenerResumenAsync(string periodoId, string cuatrimestre, string carreraId, CancellationToken ct = default);

    Task<IReadOnlyList<GrupoParticipacion>> ObtenerGruposAsync(string periodoId, string cuatrimestre, string carreraId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ObtenerAlumnosPendientesAsync(string grupoId, string carreraId, CancellationToken ct = default);

    Task<IReadOnlyList<ResultadoCoordinacion>> ObtenerResultadosAsync(
        string periodoId,
        string cuatrimestre,
        bool tutores,
        string carreraId,
        CancellationToken ct = default);

    Task<IReadOnlyList<IReadOnlyList<string>>> ObtenerDatosReporteAsync(
        string tipo,
        string periodoId,
        string cuatrimestre,
        string carreraId,
        CancellationToken ct = default);
}
