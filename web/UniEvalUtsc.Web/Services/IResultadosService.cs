using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public interface IResultadosService
{
    IReadOnlyList<PeriodoResultado> ObtenerPeriodos();

    IReadOnlyList<TarjetaResultado> ObtenerTarjetasInicio(Rol rol, string periodoId);

    IReadOnlyList<ElementoResultado> ObtenerElementos(Rol rol);

    ResultadoPersonal? ObtenerResultado(Rol rol, string periodoId, string elementoId);

    IReadOnlyList<PuntoHistorico> ObtenerHistorico(Rol rol, string elementoId);

    IReadOnlyList<FilaReportePersonal> ObtenerReportes(Rol rol);
}
