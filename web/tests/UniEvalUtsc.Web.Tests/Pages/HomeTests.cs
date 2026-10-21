using Bunit;
using UniEvalUtsc.Web.Pages;

namespace UniEvalUtsc.Web.Tests.Pages;

public sealed class HomeTests
{
    [Fact]
    public void Home_RenderizaUnaSuperficieVacia()
    {
        using var contexto = new BunitContext();

        var pagina = contexto.Render<Home>();

        Assert.Empty(pagina.FindAll("h1"));
        Assert.Equal("UniEval UTSC", pagina.Find("main").GetAttribute("aria-label"));
    }
}
