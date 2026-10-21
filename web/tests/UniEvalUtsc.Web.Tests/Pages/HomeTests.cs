using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniEvalUtsc.Web.Services;
using UniEvalUtsc.Web.Pages;

namespace UniEvalUtsc.Web.Tests.Pages;

public sealed class HomeTests
{
    [Fact]
    public void Home_MuestraSelectorDeRolEnModoSimulado()
    {
        using var contexto = new BunitContext();
        contexto.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        contexto.Services.AddScoped<SimulatedAuthenticationStateProvider>();
        contexto.Services.AddScoped<AuthService>();

        var pagina = contexto.Render<Home>();

        Assert.Equal("Tu opinión mejora la UTSC", pagina.Find("h1").TextContent);
        Assert.Equal(5, pagina.FindAll("#rol-acceso option").Count);
        Assert.Contains("Iniciar sesión", pagina.Find("button").TextContent);
    }
}
