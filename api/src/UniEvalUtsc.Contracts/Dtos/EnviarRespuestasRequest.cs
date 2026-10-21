namespace UniEvalUtsc.Contracts.Dtos;

public sealed record EnviarRespuestasRequest(
    IReadOnlyCollection<CalificacionAspectoDto> Respuestas);
