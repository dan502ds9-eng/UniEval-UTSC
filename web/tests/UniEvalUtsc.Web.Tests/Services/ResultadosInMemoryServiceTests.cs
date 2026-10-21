using UniEvalUtsc.Contracts.Enums;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class ResultadosInMemoryServiceTests
{
    private readonly ResultadosInMemoryService _servicio = new();

    [Fact]
    public void ObtenerResultado_OcultaPeriodoAbiertoYResultadosConMenosDeCincoRespuestas()
    {
        Assert.Null(_servicio.ObtenerResultado(Rol.Docente, "sep-dic-2026", "web-dsm05av"));
        Assert.Null(_servicio.ObtenerResultado(Rol.Docente, "may-ago-2026", "ecuaciones-dsm05av"));
        Assert.NotNull(_servicio.ObtenerResultado(Rol.Docente, "may-ago-2026", "web-dsm05av"));
    }

    [Fact]
    public void ObtenerHistorico_OcultaElementoConMenosDeCincoRespuestas()
    {
        var puntos = _servicio.ObtenerHistorico(Rol.Docente, "ecuaciones-dsm05av");

        Assert.Empty(puntos);
    }

    [Fact]
    public void ObtenerReportes_OcultaElementosConMenosDeCincoRespuestas()
    {
        var reportes = _servicio.ObtenerReportes(Rol.Docente);

        Assert.DoesNotContain(reportes, reporte => reporte.ElementoId == "ecuaciones-dsm05av");
        Assert.All(reportes, reporte => Assert.NotEqual("sep-dic-2026", reporte.PeriodoId));
    }
}
