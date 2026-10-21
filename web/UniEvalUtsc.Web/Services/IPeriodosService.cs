namespace UniEvalUtsc.Web.Services;

public interface IPeriodosService
{
    Task<IReadOnlyList<PeriodoAdministrativo>> ObtenerPeriodosAsync(CancellationToken ct = default);

    Task<PeriodoAdministrativo> ObtenerActualAsync(CancellationToken ct = default);

    Task<PeriodoAdministrativo> CrearPeriodoAsync(string nombre, DateOnly inicio, DateOnly cierre, CancellationToken ct = default);

    Task EditarFechasAsync(string periodoId, DateOnly inicioCaptura, DateOnly finCaptura, DateOnly inicioEncuestas, DateOnly cierreEncuestas, CancellationToken ct = default);

    Task<int> AbrirAsync(string periodoId, CancellationToken ct = default);

    Task CerrarAsync(string periodoId, CancellationToken ct = default);
}
