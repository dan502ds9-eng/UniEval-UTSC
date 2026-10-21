using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class AlumnoInMemoryServiceTests
{
    [Fact]
    public async Task EvaluacionRegular_CalculaPreguntasPorDocentesMasTutor()
    {
        var servicio = CrearServicio();

        var evaluacion = await servicio.ObtenerEvaluacionAsync();

        Assert.Equal(10, evaluacion.Preguntas.Count);
        Assert.Equal(8, evaluacion.Evaluados.Count);
        Assert.Equal(80, evaluacion.TotalRespuestas);
        Assert.True(evaluacion.Evaluados[^1].EsTutor);
    }

    [Fact]
    public async Task EvaluacionDeEstadias_UsaAC64YAsignaSoloAlTutor()
    {
        var servicio = CrearServicio(esEstadia: true);

        var evaluacion = await servicio.ObtenerEvaluacionAsync();

        Assert.Equal("FORM-AC64", evaluacion.Formato);
        Assert.Equal(7, evaluacion.Preguntas.Count);
        Assert.Single(evaluacion.Evaluados);
        Assert.True(evaluacion.Evaluados[0].EsTutor);
        Assert.Equal(7, evaluacion.TotalRespuestas);
    }

    [Fact]
    public async Task EnviarAsync_RechazaEvaluacionIncompleta()
    {
        var servicio = CrearServicio();

        var resultado = await servicio.EnviarAsync("alumno-1", new Dictionary<string, int>());

        Assert.False(resultado.Exitoso);
        Assert.Contains("80", resultado.Mensaje);
        Assert.False((await servicio.ObtenerEvaluacionAsync()).Enviado);
    }

    [Fact]
    public async Task RecuperarBorradorAsync_DescartaRespuestasFueraDeLaEvaluacion()
    {
        var runtime = new RuntimeFalso
        {
            Borrador = JsonSerializer.Serialize(new Dictionary<string, int>
            {
                ["1:docente-1"] = 8,
                ["99:docente-1"] = 6,
                ["1:docente-2"] = 11
            })
        };
        var servicio = CrearServicio(runtime);

        var borrador = await servicio.RecuperarBorradorAsync("alumno-1");

        Assert.Equal(new Dictionary<string, int> { ["1:docente-1"] = 8 }, borrador);
        Assert.Equal("unieval-borrador:sep-dic-2026:alumno-1", runtime.UltimaClave);
    }

    [Fact]
    public async Task EnviarAsync_AceptaSoloTodasLasRespuestasValidas()
    {
        var servicio = CrearServicio();
        var evaluacion = await servicio.ObtenerEvaluacionAsync();
        var respuestas = evaluacion.Preguntas
            .SelectMany(pregunta => evaluacion.Evaluados.Select(evaluado =>
                new KeyValuePair<string, int>(
                    EvaluacionAlumno.CrearClaveCalificacion(pregunta.Id, evaluado.Id),
                    8)))
            .ToDictionary(par => par.Key, par => par.Value);

        var resultado = await servicio.EnviarAsync("alumno-1", respuestas);

        Assert.True(resultado.Exitoso);
        var evaluacionEnviada = await servicio.ObtenerEvaluacionAsync();
        Assert.True(evaluacionEnviada.Enviado);
        Assert.NotNull(evaluacionEnviada.FechaEnvio);
    }

    [Fact]
    public async Task CrearRecordatorio_AgregaMensajeVisibleParaElAlumno()
    {
        var servicio = CrearServicio();

        await servicio.CrearRecordatorioAsync("dsm05av-a", "DSM05AV · Grupo A");

        var mensajes = await servicio.ObtenerMensajesAsync();
        var mensaje = Assert.Single(mensajes, item =>
            item.Titulo.Contains("DSM05AV · Grupo A", StringComparison.Ordinal));
        Assert.Equal("Te invitamos a completar tus evaluaciones pendientes del cuatrimestre.", mensaje.Cuerpo);
        Assert.Equal(3, await servicio.ObtenerMensajesSinLeerAsync());
    }

    private static AlumnoInMemoryService CrearServicio(RuntimeFalso? runtime = null, bool esEstadia = false)
    {
        var valores = new Dictionary<string, string?>
        {
            ["Alumno:EsEstadia"] = esEstadia.ToString(),
            ["Alumno:PeriodoAbierto"] = "true"
        };
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new AlumnoInMemoryService(
            configuracion,
            new UnievalJsInterop(runtime ?? new RuntimeFalso()),
            NullLogger<AlumnoInMemoryService>.Instance);
    }

    private sealed class RuntimeFalso : IJSRuntime
    {
        public string? Borrador { get; init; }
        public string? UltimaClave { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            UltimaClave = args?.FirstOrDefault() as string;
            object? resultado = identifier switch
            {
                "unieval.getLocalStorage" => Borrador,
                "unieval.setLocalStorage" => true,
                _ => throw new InvalidOperationException($"Interoperabilidad JavaScript inesperada: {identifier}")
            };

            return ValueTask.FromResult((TValue)resultado!);
        }
    }
}
