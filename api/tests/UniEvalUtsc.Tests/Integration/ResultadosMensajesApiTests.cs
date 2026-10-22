using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using UniEvalUtsc.Contracts.Dtos;

namespace UniEvalUtsc.Tests.Integration;

public sealed class ResultadosMensajesApiTests
{
    [Fact]
    public async Task MisResultados_VisibleOcultoNoDisponibleYPropiedadDeCarga()
    {
        using var api = CrearApi();
        using var docente = Cliente(api, "2");
        using var tutorDocente = Cliente(api, "6");

        var visible = await docente.GetFromJsonAsync<JsonElement>("/api/mis-resultados?periodoId=2&cargaAcademicaId=101");
        Assert.True(visible.TryGetProperty("promedioGeneral", out _));
        Assert.Equal(5, visible.GetProperty("respuestas").GetInt32());
        Assert.Equal(8m, visible.GetProperty("promedioGeneral").GetDecimal());
        Assert.Contains(visible.GetProperty("aspectos").EnumerateArray(), x => x.TryGetProperty("textoCorto", out _));
        Assert.DoesNotContain("alumnoId", visible.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("correo", visible.ToString(), StringComparison.OrdinalIgnoreCase);

        var oculto = await tutorDocente.GetFromJsonAsync<JsonElement>("/api/mis-resultados?periodoId=2&cargaAcademicaId=102");
        Assert.Equal(new[] { "oculto", "respuestas" }, oculto.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.True(oculto.GetProperty("oculto").GetBoolean());
        Assert.Equal(2, oculto.GetProperty("respuestas").GetInt32());

        var noDisponible = await tutorDocente.GetFromJsonAsync<JsonElement>("/api/mis-resultados?periodoId=1&grupoId=6");
        Assert.Equal("False", noDisponible.GetProperty("disponible").ToString());

        Assert.Equal(HttpStatusCode.Forbidden,
            (await docente.GetAsync("/api/mis-resultados?periodoId=2&cargaAcademicaId=102")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Cliente(api, "3").GetAsync("/api/mis-resultados?periodoId=2&cargaAcademicaId=101")).StatusCode);
    }

    [Fact]
    public async Task ResumenEHistorico_UsanSoloPeriodosCerradosYConservanAmbosRoles()
    {
        using var api = CrearApi();
        using var ambos = Cliente(api, "6");
        var resumen = await ambos.GetFromJsonAsync<JsonElement>("/api/mis-resultados/resumen?periodoId=2");
        Assert.True(resumen.GetProperty("disponible").GetBoolean());
        var resultados = resumen.GetProperty("resultados").EnumerateArray().ToArray();
        Assert.Contains(resultados, x => x.TryGetProperty("promedioGeneral", out _));
        Assert.Contains(resultados, x => x.TryGetProperty("oculto", out _));

        var noDisponible = await ambos.GetFromJsonAsync<JsonElement>("/api/mis-resultados/resumen?periodoId=1");
        Assert.Equal(new[] { "disponible" }, noDisponible.EnumerateObject().Select(x => x.Name).ToArray());
        Assert.False(noDisponible.GetProperty("disponible").GetBoolean());

        var historico = await ambos.GetFromJsonAsync<JsonElement[]>("/api/mis-resultados/historico?cargaAcademicaId=102");
        Assert.NotNull(historico);
        Assert.All(historico, item => Assert.True(item.GetProperty("oculto").GetBoolean()));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Cliente(api, "3").GetAsync("/api/mis-resultados/historico?cargaAcademicaId=101")).StatusCode);
    }

    [Fact]
    public async Task ResultadosDeGestion_SeparanDocentesTutoresYAplicanFiltroCarrera()
    {
        using var api = CrearApi();
        using var admin = Cliente(api, "5");
        using var coordinador = Cliente(api, "4");

        var docentes = await admin.GetFromJsonAsync<JsonElement[]>("/api/resultados/docentes?periodoId=2&carreraId=dsm&cuatrimestre=5");
        Assert.NotNull(docentes);
        Assert.Contains(docentes, x => x.TryGetProperty("promedio", out _));
        Assert.Contains(docentes, x => x.TryGetProperty("oculto", out _));

        var tutores = await admin.GetFromJsonAsync<JsonElement[]>("/api/resultados/tutores?periodoId=2&carreraId=dsm&cuatrimestre=5");
        Assert.NotNull(tutores);
        Assert.Contains(tutores, x => x.TryGetProperty("promedio", out _));

        var coordinadorMti = await coordinador.GetFromJsonAsync<JsonElement[]>("/api/resultados/docentes?periodoId=2&carreraId=mti&cuatrimestre=5");
        Assert.NotNull(coordinadorMti);
        Assert.Contains(coordinadorMti, x => x.TryGetProperty("oculto", out _) || x.TryGetProperty("promedioGeneral", out _));
        Assert.DoesNotContain(coordinadorMti, x => x.TryGetProperty("carreraId", out var c) && c.GetString() == "mti");

        var docentesSexto = await admin.GetFromJsonAsync<JsonElement[]>("/api/resultados/docentes?periodoId=2&cuatrimestre=6");
        Assert.Empty(docentesSexto!);
        var tutoresSexto = await admin.GetFromJsonAsync<JsonElement[]>("/api/resultados/tutores?periodoId=2&cuatrimestre=6");
        Assert.NotEmpty(tutoresSexto!);
        var tutoresDecimo = await admin.GetFromJsonAsync<JsonElement[]>("/api/resultados/tutores?periodoId=2&cuatrimestre=10");
        Assert.Contains(tutoresDecimo!, x => x.TryGetProperty("oculto", out _));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await admin.GetAsync("/api/resultados/docentes/101?periodoId=2&carreraId=mti")).StatusCode);
    }

    [Fact]
    public async Task DetallesDocenteTutorYEstadias_AplicanUmbralYResultadoVisible()
    {
        using var api = CrearApi();
        using var admin = Cliente(api, "5");

        var docente = await admin.GetFromJsonAsync<JsonElement>("/api/resultados/docentes/101?periodoId=2");
        Assert.Equal(5, docente.GetProperty("respuestas").GetInt32());
        var docenteOculto = await admin.GetFromJsonAsync<JsonElement>("/api/resultados/docentes/102?periodoId=2");
        Assert.Equal(new[] { "oculto", "respuestas" }, docenteOculto.EnumerateObject().Select(x => x.Name).Order().ToArray());

        var tutor = await admin.GetFromJsonAsync<JsonElement>("/api/resultados/tutores/6?periodoId=2");
        Assert.Equal(5, tutor.GetProperty("respuestas").GetInt32());
        var tutorOculto = await admin.GetFromJsonAsync<JsonElement>("/api/resultados/tutores/7?periodoId=2");
        Assert.True(tutorOculto.GetProperty("oculto").GetBoolean());

        var estadias = await admin.GetFromJsonAsync<JsonElement>("/api/resultados/tutores/estadias/503?periodoId=2");
        Assert.Equal(5, estadias.GetProperty("respuestas").GetInt32());
        var estadiasOculto = await admin.GetFromJsonAsync<JsonElement>("/api/resultados/tutores/estadias/504?periodoId=2");
        Assert.Equal(new[] { "oculto", "respuestas" }, estadiasOculto.EnumerateObject().Select(x => x.Name).Order().ToArray());
    }

    [Fact]
    public async Task Seguimiento_MuestraParticipacionYPendientesEnmascaradosSegunRol()
    {
        using var api = CrearApi();
        using var coordinador = Cliente(api, "4");
        using var admin = Cliente(api, "5");

        var coordinacion = await coordinador.GetFromJsonAsync<GrupoSeguimientoDto[]>("/api/seguimiento?periodoId=1&carreraId=mti&grupoId=1");
        Assert.NotNull(coordinacion);
        Assert.Equal(2, coordinacion[0].AlumnosPendientes.Count);
        Assert.All(coordinacion[0].AlumnosPendientes, a => Assert.Equal("d***@utsc.edu.mx", a.Correo));
        Assert.Equal(0, coordinacion[0].AlumnosRespondieron);

        var administracion = await admin.GetFromJsonAsync<GrupoSeguimientoDto[]>("/api/seguimiento?periodoId=1&grupoId=1");
        Assert.Contains(administracion![0].AlumnosPendientes, a => a.Correo == "alumno@utsc.edu.mx");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await coordinador.GetAsync("/api/seguimiento?periodoId=1&grupoId=8")).StatusCode);
    }

    [Fact]
    public async Task Recordatorio_CreaSoloParaAlumnosPendientesYRechazaOtraCarrera()
    {
        using var api = CrearApi();
        using var coordinador = Cliente(api, "4");
        using var admin = Cliente(api, "5");

        var respuesta = await coordinador.PostAsJsonAsync("/api/recordatorios", new RecordatorioRequestDto(1));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(2, (await respuesta.Content.ReadFromJsonAsync<RecordatorioCreadoDto>())!.Creados);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await coordinador.PostAsJsonAsync("/api/recordatorios", new RecordatorioRequestDto(8))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PostAsJsonAsync("/api/recordatorios", new RecordatorioRequestDto(8))).StatusCode);
    }

    [Fact]
    public async Task Mensajes_SonPropiosYSePuedenMarcarLeidosIndividualmenteOTodos()
    {
        using var api = CrearApi();
        using var admin = Cliente(api, "5");
        using var alumno = Cliente(api, "1");
        using var otroAlumno = Cliente(api, "7");
        await admin.PostAsJsonAsync("/api/recordatorios", new RecordatorioRequestDto(1));

        var mensajes = await alumno.GetFromJsonAsync<MensajeDto[]>("/api/mis-mensajes");
        Assert.NotNull(mensajes);
        var mensaje = Assert.Single(mensajes);
        Assert.Equal(HttpStatusCode.NoContent, (await alumno.PutAsync($"/api/mis-mensajes/{mensaje.Id}/leido", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otroAlumno.PutAsync($"/api/mis-mensajes/{mensaje.Id}/leido", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await otroAlumno.PutAsync("/api/mis-mensajes/leidos", null)).StatusCode);
        var actualizados = await alumno.GetFromJsonAsync<MensajeDto[]>("/api/mis-mensajes");
        Assert.NotNull(actualizados![0].LeidoEl);
    }

    [Fact]
    public async Task OpenApi_PublicaEjemplosVisibleYOculto()
    {
        using var api = CrearApi();
        var texto = await api.CreateClient().GetStringAsync("/openapi/v1.json");
        using var documento = JsonDocument.Parse(texto);
        Assert.True(documento.RootElement.GetProperty("paths").TryGetProperty("/api/resultados/docentes/{cargaAcademicaId}", out _),
            string.Join(Environment.NewLine, documento.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name)));
        var ejemplos = documento.RootElement.GetProperty("paths")
            .GetProperty("/api/resultados/docentes/{cargaAcademicaId}").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("examples");
        Assert.True(ejemplos.GetProperty("Resultado oculto por RN-10").GetProperty("value").GetProperty("oculto").GetBoolean());
        Assert.Equal(2, ejemplos.GetProperty("Resultado oculto por RN-10").GetProperty("value").GetProperty("respuestas").GetInt32());
        Assert.True(ejemplos.GetProperty("Resultado visible").GetProperty("value").GetProperty("aspectos").GetArrayLength() > 0);
    }

    [Theory]
    [InlineData("GET", "/api/mis-resultados?periodoId=2&cargaAcademicaId=101")]
    [InlineData("GET", "/api/mis-resultados/resumen?periodoId=2")]
    [InlineData("GET", "/api/mis-resultados/historico?cargaAcademicaId=101")]
    [InlineData("GET", "/api/resultados/docentes?periodoId=2")]
    [InlineData("GET", "/api/resultados/tutores?periodoId=2")]
    [InlineData("GET", "/api/resultados/docentes/101?periodoId=2")]
    [InlineData("GET", "/api/resultados/tutores/6?periodoId=2")]
    [InlineData("GET", "/api/resultados/tutores/estadias/503?periodoId=2")]
    [InlineData("GET", "/api/seguimiento?periodoId=1")]
    [InlineData("POST", "/api/recordatorios")]
    [InlineData("GET", "/api/mis-mensajes")]
    [InlineData("PUT", "/api/mis-mensajes/00000000-0000-0000-0000-000000000001/leido")]
    [InlineData("PUT", "/api/mis-mensajes/leidos")]
    public async Task Endpoints_ExigenAutenticacionYRolCorrecto(string metodo, string ruta)
    {
        using var api = CrearApi();
        using var anonimo = api.CreateClient();
        using var alumno = Cliente(api, "1");
        using var docente = Cliente(api, "2");
        Assert.Equal(HttpStatusCode.Unauthorized, (await Solicitar(anonimo, metodo, ruta)).StatusCode);
        var usuarioIncorrecto = ruta.StartsWith("/api/mis-mensajes", StringComparison.Ordinal) ? docente : alumno;
        Assert.Equal(HttpStatusCode.Forbidden, (await Solicitar(usuarioIncorrecto, metodo, ruta)).StatusCode);
    }

    private static HttpClient Cliente(WebApplicationFactory<Program> api, string usuario)
    {
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Usuario-Simulado", usuario);
        return cliente;
    }

    private static Task<HttpResponseMessage> Solicitar(HttpClient cliente, string metodo, string ruta) =>
        cliente.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), ruta)
        {
            Content = metodo == "POST" ? JsonContent.Create(new RecordatorioRequestDto(1)) : null
        });

    private static WebApplicationFactory<Program> CrearApi() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development").UseSetting("Auth:Modo", "Simulado").UseSetting("Datos:Proveedor", "InMemory");
        builder.ConfigureLogging(logging => logging.ClearProviders());
    });
}
