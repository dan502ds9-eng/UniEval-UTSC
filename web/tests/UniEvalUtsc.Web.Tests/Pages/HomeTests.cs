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
    public void Home_MuestraUsuariosUnicosYPermiteElegirRolDespues()
    {
        using var contexto = new BunitContext();
        contexto.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Autenticacion:Modo"] = "Simulado"
        }).Build());
        contexto.Services.AddScoped<SimulatedAuthenticationStateProvider>();
        contexto.Services.AddSingleton<ILogger<AuthService>>(NullLogger<AuthService>.Instance);
        contexto.Services.AddScoped<AuthService>();

        var pagina = contexto.Render<Home>();

        Assert.Equal("Tu opinión mejora la UTSC", pagina.Find("h1").TextContent);
        Assert.Equal("Usuario", pagina.Find("label[for='usuario-prueba']").TextContent);
        Assert.Equal(7, pagina.FindAll("#usuario-prueba option").Count);
        Assert.DoesNotContain("Demo", pagina.Markup);
        Assert.DoesNotContain("Simulado", pagina.Markup);
        Assert.Equal("Entrar", pagina.Find("button").TextContent);
        pagina.Find("#usuario-prueba").Change("6");
        Assert.Contains("Mónica Janeth Rodríguez Reyna", pagina.Markup);
        Assert.Equal(2, pagina.FindAll("#rol-prueba option").Count);
        Assert.DoesNotContain("type=\"password\"", pagina.Markup);
    }

    [Fact]
    public void Home_EntraSoloMuestraBotonMicrosoft()
    {
        using var contexto = new BunitContext();
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Autenticacion:Modo"] = "Entra"
        }).Build();
        contexto.Services.AddSingleton<IConfiguration>(configuracion);
        contexto.Services.AddScoped<SimulatedAuthenticationStateProvider>();
        contexto.Services.AddSingleton<ILogger<AuthService>>(NullLogger<AuthService>.Instance);
        contexto.Services.AddScoped<AuthService>();

        var pagina = contexto.Render<Home>();

        Assert.Equal("Iniciar sesión con Microsoft 365", pagina.Find("button").TextContent);
        Assert.True(pagina.Find("button").HasAttribute("disabled"));
        Assert.Equal("true", pagina.Find("button").GetAttribute("aria-disabled"));
        Assert.Contains("Microsoft 365 aún no está configurado", pagina.Find("#ayuda-entra").TextContent);
        Assert.Empty(pagina.FindAll("input, select"));
        Assert.DoesNotContain("Simulado", pagina.Markup);
    }
}
