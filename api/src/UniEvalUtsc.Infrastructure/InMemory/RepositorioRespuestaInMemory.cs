using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioRespuestaInMemory(AlmacenEvaluacionInMemory almacen) : IRespuestaRepository
{
    private readonly List<Respuesta> _pendientes = [];

    public void Agregar(Respuesta respuesta) => _pendientes.Add(respuesta);

    internal void Confirmar()
    {
        almacen.GuardarRespuestas(_pendientes);
        _pendientes.Clear();
    }

    internal void Descartar() => _pendientes.Clear();
}
