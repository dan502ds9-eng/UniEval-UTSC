namespace UniEvalUtsc.Web.Services;

public interface IEstadiasService
{
    IReadOnlyList<GrupoEstadias> ObtenerGrupos();

    IReadOnlyList<AsignacionTutorEstadias> ObtenerAsignaciones(string grupoId);

    void AsignarTutorATodoElGrupo(string grupoId, string tutorId);

    void CambiarTutor(string alumnoId, string? tutorId);

    void GuardarAsignaciones(string grupoId);

    IReadOnlyList<string> ObtenerTutores();
}
