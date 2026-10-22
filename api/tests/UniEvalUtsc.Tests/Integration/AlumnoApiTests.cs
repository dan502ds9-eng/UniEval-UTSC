using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniEvalUtsc.Application.Interfaces;

namespace UniEvalUtsc.Tests.Integration;

public sealed class AlumnoApiTests
{
    private static HttpClient Cliente(WebApplicationFactory<Program> api, string usuario = "1")
    {
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Usuario-Simulado", usuario);
        return cliente;
    }

    [Theory]
    [InlineData("/api/auth/perfil")]
    [InlineData("/api/mis-encuestas")]
    [InlineData("/api/mis-encuestas/evaluacion")]
    public async Task LecturasDelAlumno_Responden200(string ruta)
    {
        using var api = CrearApi();
        using var cliente = Cliente(api);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task Comprobante_ConPendientes_Responde404()
    {
        using var api = CrearApi();
        using var cliente = Cliente(api);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/mis-encuestas/comprobante")).StatusCode);
    }

    [Fact]
    public async Task Lote_Incompleto_Responde400()
    {
        using var api = CrearApi();
        using var cliente = Cliente(api);
        var body = new[] { new { encuestaAsignadaId = 1, respuestas = Array.Empty<object>() } };
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/mis-encuestas/lote/respuestas", body)).StatusCode);
    }

    [Fact]
    public async Task EnvioIndividual_Responde204()
    {
        using var api = CrearApi();
        using var cliente = Cliente(api);
        var respuestas = Enumerable.Range(1, 10).Select(id => new { aspectoId = id, calificacion = 8 }).ToArray();
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsJsonAsync("/api/mis-encuestas/1/respuestas", new { respuestas })).StatusCode);
    }

    [Fact]
    public async Task AsignacionDeOtroAlumno_Responde403()
    {
        using var api = CrearApi();
        using var cliente = Cliente(api, "7");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/mis-encuestas/1/respuestas", new { respuestas = Array.Empty<object>() })).StatusCode);
    }

    [Fact]
    public async Task UsuarioSinEncabezado_Responde401()
    {
        using var api = CrearApi();
        using var cliente = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/mis-encuestas")).StatusCode);
    }

    [Fact]
    public async Task Perfil_IncluyeTodosLosRolesDelUsuario()
    {
        using var api = CrearApi();
        using var cliente = Cliente(api, "6");
        var perfil = await cliente.GetFromJsonAsync<UniEvalUtsc.Contracts.Dtos.UsuarioPerfilDto>("/api/auth/perfil");
        Assert.NotNull(perfil);
        Assert.Contains(UniEvalUtsc.Contracts.Enums.Rol.Docente, perfil.Roles);
        Assert.Contains(UniEvalUtsc.Contracts.Enums.Rol.Tutor, perfil.Roles);
    }

    [Fact]
    public async Task OpenApi_DeclaraAutenticacionSimuladaYEjemplos()
    {
        using var api = CrearApi();
        using var cliente = api.CreateClient();
        var documento = await cliente.GetStringAsync("/openapi/v1.json");
        Assert.Contains("UsuarioSimulado", documento);
        Assert.Contains("apiKey", documento);
        Assert.Contains("Lote completo", documento);
        Assert.Contains("Evaluación unificada", documento);
    }

    [Fact]
    public async Task DocenteEnEndpointAlumno_Responde403()
    {
        using var api = CrearApi();
        using var cliente = Cliente(api, "2");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/mis-encuestas")).StatusCode);
    }

    [Fact]
    public async Task PeriodoCerrado_Responde409()
    {
        using var api = CrearApi(new RelojFalso(new DateTime(2026, 11, 1, 12, 0, 0)));
        using var cliente = Cliente(api);
        var respuestas = Enumerable.Range(1, 10).Select(id => new { aspectoId = id, calificacion = 8 }).ToArray();
        var lote = new[] { new { encuestaAsignadaId = 1, respuestas } };
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/mis-encuestas/lote/respuestas", lote)).StatusCode);
    }

    [Fact]
    public async Task EnvioCorrecto_Devuelve204_YSegundoEnvio409_YComprobante200()
    {
        using var api = CrearApi();
        using var cliente = Cliente(api);
        var respuestas = Enumerable.Range(1, 10).Select(id => new { aspectoId = id, calificacion = 8 }).ToArray();
        var lote = Enumerable.Range(1, 8).Select(id => new { encuestaAsignadaId = id, respuestas }).ToArray();
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsJsonAsync("/api/mis-encuestas/lote/respuestas", lote)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/mis-encuestas/lote/respuestas", lote)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/mis-encuestas/comprobante")).StatusCode);
    }

    private static WebApplicationFactory<Program> CrearApi(IReloj? reloj = null) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development").UseSetting("Auth:Modo", "Simulado").UseSetting("Datos:Proveedor", "InMemory");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        if (reloj is not null) builder.ConfigureTestServices(services => services.RemoveAll<IReloj>().AddSingleton(reloj));
    });

    private sealed class RelojFalso(DateTime ahora) : IReloj
    {
        public DateTime Ahora => ahora;
        public DateOnly Hoy => DateOnly.FromDateTime(ahora);
    }
}
