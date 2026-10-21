namespace UniEvalUtsc.Web.Services;

public interface ICargaAcademicaService
{
    Task<IReadOnlyList<AsignacionCarga>> ObtenerAsignacionesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<string>> ObtenerDocentesAsync(CancellationToken ct = default);

    Task CambiarDocenteAsync(string asignacionId, string docente, CancellationToken ct = default);

    Task<ResultadoImportacion> ImportarAsync(IReadOnlyList<IReadOnlyList<string>> filas, CancellationToken ct = default);
}
