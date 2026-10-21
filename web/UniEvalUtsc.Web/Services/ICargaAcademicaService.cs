namespace UniEvalUtsc.Web.Services;

public interface ICargaAcademicaService
{
    IReadOnlyList<AsignacionCarga> ObtenerAsignaciones();

    IReadOnlyList<string> ObtenerDocentes();

    void CambiarDocente(string asignacionId, string docente);

    ResultadoImportacion Importar(IReadOnlyList<IReadOnlyList<string>> filas);
}
