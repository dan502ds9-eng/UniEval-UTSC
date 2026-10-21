using UniEvalUtsc.Application.Interfaces;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class UnidadDeTrabajoInMemory(RepositorioRespuestaInMemory respuestas) : IUnitOfWork
{
    public Task GuardarCambiosAsync(CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            respuestas.Descartar();
            ct.ThrowIfCancellationRequested();
        }

        respuestas.Confirmar();
        return Task.CompletedTask;
    }
}
