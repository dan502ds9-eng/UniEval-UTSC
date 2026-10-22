using UniEvalUtsc.Application.Exceptions;
using UniEvalUtsc.Application.Services;
using UniEvalUtsc.Domain.Exceptions;
using UniEvalUtsc.Infrastructure.InMemory;

namespace UniEvalUtsc.Tests.Unit;

public sealed class EnviarRespuestaServiceTests
{
    [Fact]
    public async Task EnviarLote_SiUnaEncuestaEsInvalida_NoGuardaNingunaNiMarcaContestadas()
    {
        var (servicio, almacen) = Crear();
        var calificaciones = Enumerable.Range(1, 10).Select(id => new UniEvalUtsc.Domain.Entities.CalificacionAspecto(id, 8)).ToArray();

        await Assert.ThrowsAsync<NoEncontradoException>(() => servicio.EnviarLoteAsync(1,
        [
            (1, calificaciones),
            (999, calificaciones)
        ]));

        Assert.Empty(almacen.ObtenerRespuestas());
        Assert.All(almacen.ObtenerEncuestas(), encuesta => Assert.Equal(UniEvalUtsc.Domain.Enums.EstadoEncuesta.Pendiente, encuesta.Estado));
    }

    [Fact]
    public async Task EnviarLote_EncuestaDeOtroAlumno_Lanza403AntesDeGuardar()
    {
        var (servicio, almacen) = Crear();
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => servicio.EnviarLoteAsync(2,
            [(1, Array.Empty<UniEvalUtsc.Domain.Entities.CalificacionAspecto>())]));
        Assert.Empty(almacen.ObtenerRespuestas());
    }

    [Fact]
    public async Task EnviarLote_FueraDelPeriodoAbierto_LanzaConflicto409()
    {
        var (servicio, almacen) = Crear(new DateTime(2027, 2, 1, 12, 0, 0));
        var calificaciones = Enumerable.Range(1, 10).Select(id => new UniEvalUtsc.Domain.Entities.CalificacionAspecto(id, 8)).ToArray();
        await Assert.ThrowsAsync<DomainException>(() => servicio.EnviarLoteAsync(1, [(1, calificaciones)]));
        Assert.Empty(almacen.ObtenerRespuestas());
    }

    [Fact]
    public async Task Enviar_SiGuardarFalla_ReversaEstadoYNoGuardaRespuesta()
    {
        var almacen = new AlmacenEvaluacionInMemory();
        var encuesta = almacen.ObtenerEncuesta(1)!;
        var repositorioRespuesta = new RepositorioRespuestaInMemory(almacen);
        var unidad = new UnidadDeTrabajoInMemory(repositorioRespuesta, almacen) { FallarAlGuardar = true };
        var servicio = new EnviarRespuestaService(new RepositorioEncuestaAsignadaInMemory(almacen),
            new RepositorioPlantillaInMemory(almacen), new RepositorioPeriodoInMemory(almacen), repositorioRespuesta,
            unidad, new RelojPrueba(new DateTime(2026, 10, 21, 12, 0, 0)));
        var calificaciones = Enumerable.Range(1, 10).Select(id => new UniEvalUtsc.Domain.Entities.CalificacionAspecto(id, 8)).ToArray();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EnviarAsync(1, 1, calificaciones));

        Assert.Equal(UniEvalUtsc.Domain.Enums.EstadoEncuesta.Pendiente, encuesta.Estado);
        Assert.Null(encuesta.FechaContestada);
        Assert.Empty(almacen.ObtenerRespuestas());
    }

    private static (EnviarRespuestaService Servicio, AlmacenEvaluacionInMemory Almacen) Crear(DateTime? ahora = null)
    {
        var almacen = new AlmacenEvaluacionInMemory();
        var repoRespuesta = new RepositorioRespuestaInMemory(almacen);
        var servicio = new EnviarRespuestaService(new RepositorioEncuestaAsignadaInMemory(almacen),
            new RepositorioPlantillaInMemory(almacen), new RepositorioPeriodoInMemory(almacen), repoRespuesta,
            new UnidadDeTrabajoInMemory(repoRespuesta, almacen), new RelojPrueba(ahora ?? new DateTime(2026, 10, 21, 12, 0, 0)));
        return (servicio, almacen);
    }

    private sealed class RelojPrueba(DateTime ahora) : UniEvalUtsc.Application.Interfaces.IReloj
    { public DateTime Ahora => ahora; public DateOnly Hoy => DateOnly.FromDateTime(Ahora); }
}
