using System;
using SaiiutUtsc.Domain.Entities;

namespace SaiiutUtsc.Application.Interfaces;

public interface IPlantillaRepository
{
    Task<Plantilla?> ObtenerConAspectosAsync(int id, CancellationToken ct = default);
}
