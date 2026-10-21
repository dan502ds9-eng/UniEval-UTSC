using UniEvalUtsc.Contracts.Enums;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class FaqInMemoryServiceTests
{
    private readonly FaqInMemoryService _servicio = new();

    [Fact]
    public async Task ObtenerPreguntasAsync_DevuelveLasPreguntasDelRol()
    {
        var preguntas = await _servicio.ObtenerPreguntasAsync(Rol.Coordinador);

        Assert.Contains(preguntas, pregunta => pregunta.Pregunta == "¿Puedo ver quién falta?");
        Assert.Contains(preguntas, pregunta => pregunta.Pregunta == "¿Cómo exporto un reporte?");
        Assert.DoesNotContain(preguntas, pregunta => pregunta.Pregunta == "¿Cuándo veo mis resultados?");
    }

    [Fact]
    public async Task ResponderAsync_CoincidePorPalabrasClaveSinDistinguirAcentos()
    {
        var respuesta = await _servicio.ResponderAsync(Rol.Administrador, "¿Cómo importo en Catálogos?");

        Assert.Contains("Catálogos", respuesta);
        Assert.Contains("Excel/CSV", respuesta);
    }

    [Fact]
    public async Task ResponderAsync_CuandoNoEncuentraCoincidencia_DevuelveUnaRespuestaUtil()
    {
        var respuesta = await _servicio.ResponderAsync(Rol.Tutor, "¿Cuál es el clima de hoy?");

        Assert.Contains("No encontré una respuesta exacta", respuesta);
    }

    [Fact]
    public async Task ResponderAsync_ConTokenCancelado_LanzaCancelacion()
    {
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _servicio.ResponderAsync(Rol.Alumno, "¿Son anónimas?", cancelacion.Token));
    }
}
