namespace UniEvalUtsc.Web.Components.Encuesta;

public sealed record CalificacionEvaluado(
    int PreguntaId,
    int IndiceEvaluado,
    string EvaluadoId,
    int Valor,
    bool AvanzarAlSiguiente = false);

public sealed record TeclaEvaluado(
    int PreguntaId,
    int IndiceEvaluado,
    string EvaluadoId,
    int Valor);
