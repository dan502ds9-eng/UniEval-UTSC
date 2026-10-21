namespace UniEvalUtsc.Web.Services;

public interface ICoordinacionService
{
    ResumenCoordinacion ObtenerResumen(string periodoId, string cuatrimestre);

    IReadOnlyList<GrupoParticipacion> ObtenerGrupos(string periodoId, string cuatrimestre);

    IReadOnlyList<string> ObtenerAlumnosPendientes(string grupoId);

    IReadOnlyList<ResultadoCoordinacion> ObtenerResultados(
        string periodoId,
        string cuatrimestre,
        bool tutores);

    IReadOnlyList<IReadOnlyList<string>> ObtenerDatosReporte(
        string tipo,
        string periodoId,
        string cuatrimestre);
}
