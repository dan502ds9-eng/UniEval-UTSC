using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioPlantillaInMemory(AlmacenEvaluacionInMemory almacen) : IPlantillaRepository
{
    public Task<Plantilla?> ObtenerConAspectosAsync(int id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(almacen.ObtenerPlantilla(id));
    }
}
