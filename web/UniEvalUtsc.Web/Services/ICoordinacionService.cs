namespace UniEvalUtsc.Web.Services;

public interface ICoordinacionService
{
    ResumenCoordinacion ObtenerResumen(string periodoId, string cuatrimestre, string carreraId);

    IReadOnlyList<GrupoParticipacion> ObtenerGrupos(string periodoId, string cuatrimestre, string carreraId);

    IReadOnlyList<string> ObtenerAlumnosPendientes(string grupoId, string carreraId);

    IReadOnlyList<ResultadoCoordinacion> ObtenerResultados(
        string periodoId,
        string cuatrimestre,
        bool tutores,
        string carreraId);

    IReadOnlyList<IReadOnlyList<string>> ObtenerDatosReporte(
        string tipo,
        string periodoId,
        string cuatrimestre,
        string carreraId);
}
