using System;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Application.Interfaces;

public interface IPlantillaRepository
{
    Task<Plantilla?> ObtenerConAspectosAsync(int id, CancellationToken ct = default);
}
