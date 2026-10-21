using Bunit;
using UniEvalUtsc.Web.Components.Encuesta;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Components.Encuesta;

public sealed class PreguntaEvaluacionTests
{
    [Fact]
    public void PreguntaEvaluacion_MuestraCompletaCuandoTodosTienenCalificacion()
    {
        using var contexto = new BunitContext();
        var evaluados = new[]
        {
            new EvaluadoAlumno("docente-1", "Materia", "Docente", false, "Docente"),
            new EvaluadoAlumno("tutor-1", "Tutoría", "Tutor", true, "Tutor")
        };
        var calificaciones = new Dictionary<string, int>
        {
            [EvaluacionAlumno.CrearClaveCalificacion(1, "docente-1")] = 8,
            [EvaluacionAlumno.CrearClaveCalificacion(1, "tutor-1")] = 9
        };

        var pregunta = contexto.Render<PreguntaEvaluacion>(parametros => parametros
            .Add(componente => componente.Pregunta, new PreguntaAlumno(1, "Pregunta de prueba"))
            .Add(componente => componente.Evaluados, evaluados)
            .Add(componente => componente.Calificaciones, calificaciones));

        Assert.Contains("✓ Completa", pregunta.Markup);
    }
}
