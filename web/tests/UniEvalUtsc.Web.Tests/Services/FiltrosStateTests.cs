using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class FiltrosStateTests
{
    [Fact]
    public void CambiarCarrera_ActualizaLaCarreraParaAdministrador()
    {
        var filtros = new FiltrosState();

        filtros.CambiarCarrera("mti");

        Assert.Equal("mti", filtros.CarreraId);
    }

    [Fact]
    public void InicializarPeriodo_ConservaLaSeleccionMientrasSigaDisponible()
    {
        var filtros = new FiltrosState();
        var periodos =
            new[]
            {
                new PeriodoResultado("actual", "Periodo actual", true),
                new PeriodoResultado("anterior", "Periodo anterior", false)
            };
        filtros.InicializarPeriodo(periodos);
        filtros.CambiarPeriodo("anterior", periodos);

        filtros.InicializarPeriodo(periodos);

        Assert.Equal("anterior", filtros.PeriodoId);
    }
}
