namespace UniEvalUtsc.Contracts.Dtos;

public sealed record AvanceEncuestasDto(
    int Contestadas,
    int Total,
    IReadOnlyCollection<EncuestaAsignadaDto> Encuestas);
