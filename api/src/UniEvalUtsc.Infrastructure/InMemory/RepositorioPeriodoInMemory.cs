using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioPeriodoInMemory(AlmacenEvaluacionInMemory almacen) : IPeriodoRepository
{
    public Task<Periodo?> ObtenerPorGrupoAsync(int grupoId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(almacen.ObtenerPeriodoPorGrupo(grupoId));
    }

    public Task<Periodo?> ObtenerPorIdAsync(int periodoId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(almacen.ObtenerPeriodo(periodoId));
    }
}
