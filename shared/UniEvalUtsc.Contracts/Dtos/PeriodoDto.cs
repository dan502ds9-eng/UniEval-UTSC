using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Contracts.Dtos;

public sealed record PeriodoDto(
    int Id,
    string Nombre,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    EstadoPeriodo Estado);
