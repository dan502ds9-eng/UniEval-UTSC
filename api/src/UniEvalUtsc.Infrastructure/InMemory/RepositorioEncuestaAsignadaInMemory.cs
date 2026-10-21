using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioEncuestaAsignadaInMemory(AlmacenEvaluacionInMemory almacen)
    : IEncuestaAsignadaRepository
{
    public Task<EncuestaAsignada?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(almacen.ObtenerEncuesta(id));
    }
}
