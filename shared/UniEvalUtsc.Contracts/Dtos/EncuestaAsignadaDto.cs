using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Contracts.Dtos;

public sealed record EncuestaAsignadaDto(
    int Id,
    string Grupo,
    string? Materia,
    string PersonaEvaluada,
    TipoEncuesta Tipo,
    EstadoEncuesta Estado);
