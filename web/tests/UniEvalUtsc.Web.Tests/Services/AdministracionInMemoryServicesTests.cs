using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class AdministracionInMemoryServicesTests
{
    [Fact]
    public void ObtenerActual_PriorizaPeriodoAbiertoAunqueSeCreeOtroEnPreparacion()
    {
        IPeriodosService servicio = new PeriodosInMemoryService();
        servicio.CrearPeriodo("Nuevo", new DateOnly(2027, 1, 1), new DateOnly(2027, 4, 30));

        Assert.Equal("sep-dic-2026", servicio.ObtenerActual().Id);
    }

    [Fact]
    public void Plantillas_PermitePublicarVersionSiguienteYCrearLaPosterior()
    {
        IPlantillasService servicio = new PlantillasInMemoryService();
        var borrador = servicio.CrearBorrador("form-ac16");
        var aspectos = borrador.Aspectos
            .Select(aspecto => aspecto with
            {
                Texto = aspecto.Id == 1 ? "Texto actualizado" : aspecto.Texto
            })
            .ToArray();

        servicio.ReemplazarAspectos(borrador.Id, aspectos);
        servicio.Publicar(borrador.Id);

        var vigente = servicio.Obtener("form-ac16-v2");
        Assert.True(vigente.Vigente);
        Assert.False(vigente.Borrador);
        Assert.Equal("Texto actualizado", vigente.Aspectos[0].Texto);
        Assert.Equal("form-ac16-v3", servicio.CrearBorrador(vigente.Id).Id);
    }
}
