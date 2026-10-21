using Bunit;
using UniEvalUtsc.Contracts;
using UniEvalUtsc.Web.Components.Ui;

namespace UniEvalUtsc.Web.Tests.Components.Ui;

public sealed class PillTests
{
    [Theory]
    [InlineData(4.9, "4.9", "Bajo")]
    [InlineData(5, "5.0", "Medio")]
    [InlineData(7.9, "7.9", "Medio")]
    [InlineData(4.95, "5.0", "Medio")]
    [InlineData(7.96, "8.0", "Alto")]
    [InlineData(7.95, "8.0", "Alto")]
    [InlineData(8, "8.0", "Alto")]
    public void Pill_RedondeaElValorAntesDeCalcularElNivel(double valor, string valorEsperado, string nivelEsperado)
    {
        using var contexto = new BunitContext();

        var pill = contexto.Render<Pill>(parameters => parameters
            .Add(component => component.Value, (decimal)valor));

        var partes = pill.FindAll(".pill > span");
        Assert.Equal(valorEsperado, partes[0].TextContent);
        Assert.Equal(nivelEsperado, partes[1].TextContent);
        Assert.Null(pill.Find(".pill").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(4.95, "Medio")]
    [InlineData(7.96, "Alto")]
    public void Niveles_RedondeaAntesDeDeterminarElNivel(double valor, string nivelEsperado)
    {
        Assert.Equal(nivelEsperado, Niveles.Nivel((decimal)valor).ToString());
    }
}
