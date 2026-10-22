using UniEvalUtsc.Application.Interfaces;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioMensajeInMemory(AlmacenEvaluacionInMemory almacen) : IMensajeRepository
{
    public Task<int> CrearParaAlumnosAsync(IReadOnlyCollection<string> alumnoIds, string remitente,
        string titulo, string cuerpo, DateTime creadoEl, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(almacen.CrearMensajes(alumnoIds, remitente, titulo, cuerpo, creadoEl));
    }

    public Task<IReadOnlyCollection<MensajeSistema>> ObtenerPorAlumnoAsync(string alumnoId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<MensajeSistema>>(almacen.ObtenerMensajes()
            .Where(m => m.AlumnoId == alumnoId).OrderByDescending(m => m.CreadoEl).ToArray());
    }

    public Task<MensajeSistema?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(almacen.ObtenerMensajes().SingleOrDefault(m => m.Id == id));
    }

    public Task MarcarLeidoAsync(Guid id, string alumnoId, DateTime leidoEl, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        almacen.MarcarMensajeLeido(id, alumnoId, leidoEl);
        return Task.CompletedTask;
    }

    public Task MarcarTodosLeidosAsync(string alumnoId, DateTime leidoEl, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        almacen.MarcarMensajesLeidos(alumnoId, leidoEl);
        return Task.CompletedTask;
    }
}
