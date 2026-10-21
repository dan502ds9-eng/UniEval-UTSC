using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class AdministracionInMemoryServicesTests
{
    [Fact]
    public async Task ObtenerActual_PriorizaPeriodoAbiertoAunqueSeCreeOtroEnPreparacion()
    {
        IPeriodosService servicio = new PeriodosInMemoryService();
        await servicio.CrearPeriodoAsync("Nuevo", new DateOnly(2027, 1, 1), new DateOnly(2027, 4, 30));

        Assert.Equal("sep-dic-2026", (await servicio.ObtenerActualAsync()).Id);
    }

    [Fact]
    public async Task Plantillas_PermitePublicarVersionSiguienteYCrearLaPosterior()
    {
        IPlantillasService servicio = new PlantillasInMemoryService();
        var borrador = await servicio.CrearBorradorAsync("form-ac16");
        var aspectos = borrador.Aspectos
            .Select(aspecto => aspecto with
            {
                Texto = aspecto.Id == 1 ? "Texto actualizado" : aspecto.Texto
            })
            .ToArray();

        await servicio.ReemplazarAspectosAsync(borrador.Id, aspectos);
        await servicio.PublicarAsync(borrador.Id);

        var vigente = await servicio.ObtenerAsync("form-ac16-v2");
        Assert.True(vigente.Vigente);
        Assert.False(vigente.Borrador);
        Assert.Equal("Texto actualizado", vigente.Aspectos[0].Texto);
        Assert.Equal("form-ac16-v3", (await servicio.CrearBorradorAsync(vigente.Id)).Id);
    }
}
