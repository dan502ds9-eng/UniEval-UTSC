using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Contracts.Dtos;

public sealed record EnviarRespuestasRequest(
    IReadOnlyCollection<CalificacionAspectoDto> Respuestas);

public sealed record EnviarLoteRespuestasItemDto(int EncuestaAsignadaId, IReadOnlyCollection<CalificacionAspectoDto> Respuestas);
public sealed record EvaluadoDto(int EncuestaAsignadaId, string Nombre, string? Materia, TipoEncuesta Tipo);
public sealed record PreguntaEvaluacionDto(int AspectoId, int Numero, string Texto, IReadOnlyCollection<EvaluadoDto> Evaluados);
public sealed record EvaluacionUnificadaDto(string Periodo, IReadOnlyCollection<PreguntaEvaluacionDto> Preguntas);
public sealed record ComprobanteDto(string Nombre, string Periodo, int Contestadas, int Total, DateOnly FechaCompletado);
