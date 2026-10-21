using Bunit;
using UniEvalUtsc.Web.Components.Resultados;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Components.Resultados;

public sealed class FiltrosResultadosTests
{
    [Fact]
    public void CambioDeCarrera_NotificaLaCarreraSeleccionada()
    {
        using var contexto = new BunitContext();
        var carreraNotificada = string.Empty;
        var componente = contexto.Render<FiltrosResultados>(parametros => parametros
            .Add(filtros => filtros.Periodos, [new PeriodoResultado("actual", "Periodo actual", true)])
            .Add(filtros => filtros.PeriodoId, "actual")
            .Add(filtros => filtros.CarreraId, "dsm")
            .Add(filtros => filtros.HabilitarCarrera, true)
            .Add(filtros => filtros.CarreraCambiada, carrera => carreraNotificada = carrera));

        componente.FindAll("select")[1].Change("todas");

        Assert.Equal("todas", carreraNotificada);
    }
}
