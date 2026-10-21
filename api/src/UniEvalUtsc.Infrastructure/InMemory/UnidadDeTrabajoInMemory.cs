using UniEvalUtsc.Application.Interfaces;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class UnidadDeTrabajoInMemory(RepositorioRespuestaInMemory respuestas) : IUnitOfWork
{
    private readonly List<Action> _reversiones = [];
    public bool FallarAlGuardar { get; set; }

    public void RegistrarReversion(Action reversion) => _reversiones.Add(reversion);

    public Task GuardarCambiosAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            if (FallarAlGuardar) throw new InvalidOperationException("Fallo de guardado simulado.");
            respuestas.Confirmar();
            _reversiones.Clear();
            return Task.CompletedTask;
        }
        catch
        {
            respuestas.Descartar();
            foreach (var reversion in _reversiones.AsEnumerable().Reverse()) reversion();
            _reversiones.Clear();
            throw;
        }
    }
}
