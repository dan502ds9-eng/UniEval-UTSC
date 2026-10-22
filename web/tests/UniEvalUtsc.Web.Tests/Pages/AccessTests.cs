using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UniEvalUtsc.Web.Pages.Acceso;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Pages;

public sealed class AccessTests
{
    [Fact]
    public void AccesoSimulado_MuestraUsuarioDePruebaSinCamposDeCredenciales()
    {
        using var contexto = CrearContexto("Simulado");

        var pagina = contexto.Render<Access>(parameters => parameters.Add(componente => componente.RutaRol, "Administrador"));

        Assert.Equal("Administrador", pagina.Find("h1").TextContent);
        Assert.Equal("Usuario", pagina.Find("label[for='usuario-prueba']").TextContent);
        Assert.Equal("Entrar", pagina.Find("button").TextContent);
        Assert.DoesNotContain("Simulado", pagina.Markup);
        Assert.Single(pagina.FindAll("#usuario-prueba option"));
        Assert.Empty(pagina.FindAll("input"));
        Assert.Empty(pagina.FindAll("[type='password'], #codigo-verificacion"));
    }

    [Fact]
    public void AccesoEntra_SoloMuestraBotonDeMicrosoft()
    {
        using var contexto = CrearContexto("Entra");

        var pagina = contexto.Render<Access>(parameters => parameters.Add(componente => componente.RutaRol, "Administrador"));

        Assert.Equal("Iniciar sesión con Microsoft 365", pagina.Find("button").TextContent);
        Assert.Empty(pagina.FindAll("input, select, label"));
        Assert.Empty(pagina.FindAll("button:not([disabled])"));
        Assert.Equal("true", pagina.Find("button").GetAttribute("aria-disabled"));
        Assert.True(pagina.Find("button").HasAttribute("disabled"));
        Assert.Equal("Microsoft 365 aún no está configurado", pagina.Find("#ayuda-entra").TextContent);
        Assert.DoesNotContain(pagina.FindAll("button"), boton => boton.TextContent.Contains("modo demo", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Simulado", pagina.Markup);
    }

    private static BunitContext CrearContexto(string modo)
    {
        var contexto = new BunitContext();
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Autenticacion:Modo"] = modo
        }).Build();
        contexto.Services.AddSingleton<IConfiguration>(configuracion);
        contexto.Services.AddScoped<SimulatedAuthenticationStateProvider>();
        contexto.Services.AddSingleton<ILogger<AuthService>>(NullLogger<AuthService>.Instance);
        contexto.Services.AddScoped<AuthService>();
        return contexto;
    }
}
