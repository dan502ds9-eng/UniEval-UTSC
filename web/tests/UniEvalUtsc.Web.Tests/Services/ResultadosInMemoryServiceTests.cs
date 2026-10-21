using UniEvalUtsc.Contracts.Enums;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class ResultadosInMemoryServiceTests
{
    private readonly ResultadosInMemoryService _servicio = new();

    [Fact]
    public async Task ObtenerResultado_OcultaPeriodoAbiertoYResultadosConMenosDeCincoRespuestas()
    {
        Assert.Null(await _servicio.ObtenerResultadoAsync(Rol.Docente, "sep-dic-2026", "web-dsm05av"));
        Assert.Null(await _servicio.ObtenerResultadoAsync(Rol.Docente, "may-ago-2026", "ecuaciones-dsm05av"));
        Assert.NotNull(await _servicio.ObtenerResultadoAsync(Rol.Docente, "may-ago-2026", "web-dsm05av"));
    }

    [Fact]
    public async Task ObtenerHistorico_OcultaElementoConMenosDeCincoRespuestas()
    {
        var puntos = await _servicio.ObtenerHistoricoAsync(Rol.Docente, "ecuaciones-dsm05av");

        Assert.Empty(puntos);
    }

    [Fact]
    public async Task ObtenerReportes_OcultaElementosConMenosDeCincoRespuestas()
    {
        var reportes = await _servicio.ObtenerReportesAsync(Rol.Docente);

        Assert.DoesNotContain(reportes, reporte => reporte.ElementoId == "ecuaciones-dsm05av");
        Assert.All(reportes, reporte => Assert.NotEqual("sep-dic-2026", reporte.PeriodoId));
    }

    [Fact]
    public async Task ObtenerResultado_SeLiberaAlCerrarElPeriodoAdministrativo()
    {
        var periodos = new PeriodosInMemoryService(
            new CargaAcademicaInMemoryService(),
            new EstadiasInMemoryService());
        var resultados = new ResultadosInMemoryService(periodos);
        var nuevoPeriodo = await periodos.CrearPeriodoAsync(
            "Prueba",
            new DateOnly(2027, 1, 1),
            new DateOnly(2027, 4, 30));

        Assert.Null(await resultados.ObtenerResultadoAsync(Rol.Docente, nuevoPeriodo.Id, "web-dsm05av"));

        await periodos.CerrarAsync("sep-dic-2026");
        var encuestasGeneradas = await periodos.AbrirAsync(nuevoPeriodo.Id);
        Assert.True(encuestasGeneradas > 0);
        Assert.Equal(encuestasGeneradas, nuevoPeriodo.EncuestasGeneradas);
        Assert.Null(await resultados.ObtenerResultadoAsync(Rol.Docente, nuevoPeriodo.Id, "web-dsm05av"));

        await periodos.CerrarAsync(nuevoPeriodo.Id);
        Assert.NotNull(await resultados.ObtenerResultadoAsync(Rol.Docente, nuevoPeriodo.Id, "web-dsm05av"));
    }
}
