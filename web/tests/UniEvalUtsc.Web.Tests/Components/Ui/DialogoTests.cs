using Bunit;
using UniEvalUtsc.Web.Tests.Components.Ui;

namespace UniEvalUtsc.Web.Tests.Components.Ui;

public sealed class DialogoTests
{
    [Fact]
    public void Dialogo_AlCerrarse_LiberaElFocoParaRestaurarloAlElementoAnterior()
    {
        using var contexto = new BunitContext();
        contexto.JSInterop.SetupVoid("unieval.trapFocus", _ => true);
        contexto.JSInterop.SetupVoid("unieval.releaseFocus", _ => true);
        var anfitrion = contexto.Render<DialogoHarness>();

        anfitrion.Find("button").Click();
        anfitrion.WaitForAssertion(() => Assert.Single(anfitrion.FindAll("[role='dialog']")));
        anfitrion.Find(".dialogo__cerrar").Click();

        anfitrion.WaitForAssertion(() => Assert.Empty(anfitrion.FindAll("[role='dialog']")));
        Assert.Contains(contexto.JSInterop.Invocations, llamada =>
            llamada.Identifier == "unieval.releaseFocus");
    }
}
