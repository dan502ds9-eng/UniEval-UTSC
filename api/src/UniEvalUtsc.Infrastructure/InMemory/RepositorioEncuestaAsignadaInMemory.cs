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

    public Task<IReadOnlyCollection<EncuestaAsignada>> ObtenerPendientesAsync(int alumnoId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyCollection<EncuestaAsignada>>(almacen.ObtenerEncuestas().Where(e => e.AlumnoId == alumnoId && e.Estado == UniEvalUtsc.Domain.Enums.EstadoEncuesta.Pendiente).ToArray());

    public Task<IReadOnlyCollection<EncuestaAsignada>> ObtenerTodasAsync(int alumnoId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyCollection<EncuestaAsignada>>(almacen.ObtenerEncuestas().Where(e => e.AlumnoId == alumnoId).ToArray());
}
