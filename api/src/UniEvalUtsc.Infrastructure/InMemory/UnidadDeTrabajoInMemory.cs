using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class UnidadDeTrabajoInMemory(RepositorioRespuestaInMemory respuestas, AlmacenEvaluacionInMemory almacen) : IUnitOfWork
{
    private readonly Dictionary<EncuestaAsignada, (EstadoEncuesta Estado, DateOnly? Fecha)> _estadosOriginales = almacen.ObtenerEncuestas()
        .ToDictionary(encuesta => encuesta, encuesta => (encuesta.Estado, encuesta.FechaContestada));
    public bool FallarAlGuardar { get; set; }

    public Task GuardarCambiosAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            if (FallarAlGuardar) throw new InvalidOperationException("Fallo de guardado simulado.");
            respuestas.Confirmar();
            return Task.CompletedTask;
        }
        catch
        {
            respuestas.Descartar();
            // En EF Core la transacción revierte; esto solo existe para InMemory.
            foreach (var (encuesta, estado) in _estadosOriginales)
                if (estado.Estado == EstadoEncuesta.Pendiente && encuesta.Estado == EstadoEncuesta.Contestada)
                    encuesta.RevertirContestacion();
            throw;
        }
    }
}
