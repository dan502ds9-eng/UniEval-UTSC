using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        contexto.Services.AddSingleton<ILogger<AuthService>>(NullLogger<AuthService>.Instance);
        contexto.Services.AddScoped<AuthService>();

        var pagina = contexto.Render<Home>();

        Assert.Equal("Tu opinión mejora la UTSC", pagina.Find("h1").TextContent);
        Assert.Equal(5, pagina.FindAll("#rol-acceso option").Count);
        Assert.Equal("Iniciar sesión con Microsoft 365", pagina.Find("button").TextContent);
    }
}
