using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public interface IResultadosService
{
    Task<IReadOnlyList<PeriodoResultado>> ObtenerPeriodosAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TarjetaResultado>> ObtenerTarjetasInicioAsync(Rol rol, string periodoId, CancellationToken ct = default);

    Task<IReadOnlyList<ElementoResultado>> ObtenerElementosAsync(Rol rol, CancellationToken ct = default);

    Task<ResultadoPersonal?> ObtenerResultadoAsync(Rol rol, string periodoId, string elementoId, CancellationToken ct = default);

    Task<IReadOnlyList<PuntoHistorico>> ObtenerHistoricoAsync(Rol rol, string elementoId, CancellationToken ct = default);

    Task<IReadOnlyList<FilaReportePersonal>> ObtenerReportesAsync(Rol rol, CancellationToken ct = default);
}
