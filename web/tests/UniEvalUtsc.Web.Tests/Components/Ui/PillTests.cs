using Bunit;
using UniEvalUtsc.Web.Components.Ui;

namespace UniEvalUtsc.Web.Tests.Components.Ui;

public sealed class PillTests
{
    [Theory]
    [InlineData(4.9, "Bajo")]
    [InlineData(5, "Medio")]
    [InlineData(7.9, "Medio")]
    [InlineData(8, "Alto")]
    public void Pill_CalculaElNivelCorrecto(double valor, string nivelEsperado)
    {
        using var contexto = new BunitContext();

        var pill = contexto.Render<Pill>(parameters => parameters
            .Add(component => component.Value, (decimal)valor));

        Assert.EndsWith(nivelEsperado, pill.Find(".pill").GetAttribute("aria-label"));
    }
}
