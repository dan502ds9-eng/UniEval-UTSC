using System.IO.Compression;
using System.Xml.Linq;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class CoordinacionInMemoryServiceTests
{
    private readonly CoordinacionInMemoryService _servicio = new();

    [Fact]
    public async Task ObtenerGrupos_OrdenaParticipacionSegunDatosFiltrados()
    {
        var grupos = await _servicio.ObtenerGruposAsync("sep-dic-2026", "5", "dsm");

        Assert.Equal(2, grupos.Count);
        Assert.Equal("dsm05av-a", grupos[0].Id);
        Assert.Equal(48.4m, grupos[0].Porcentaje);
        Assert.Equal(16, grupos[0].Faltan);
    }

    [Fact]
    public async Task ObtenerResultados_EnEstadiasSoloIncluyeTutorEstadias()
    {
        var docentes = await _servicio.ObtenerResultadosAsync("sep-dic-2026", "6", tutores: false, "dsm");
        var tutores = await _servicio.ObtenerResultadosAsync("sep-dic-2026", "6", tutores: true, "dsm");

        Assert.Empty(docentes);
        var tutor = Assert.Single(tutores);
        Assert.True(tutor.EsTutorEstadias);
        Assert.Equal(6, tutor.Cuatrimestre);
    }

    [Fact]
    public async Task ObtenerResultados_UsaPeriodoSolicitadoYOrdenaDeMenorAMayor()
    {
        var resultados = await _servicio.ObtenerResultadosAsync("ene-abr-2026", "5", tutores: false, "dsm");

        Assert.All(resultados, resultado => Assert.Equal("Ene–Abr 2026", resultado.Periodo));
        Assert.Equal(resultados.OrderBy(resultado => resultado.Promedio), resultados);
    }

    [Fact]
    public async Task ObtenerResumen_EntregaIndicadoresAgregadosDeLaCarrera()
    {
        var resumen = await _servicio.ObtenerResumenAsync("sep-dic-2026", "todos", "dsm");

        Assert.Equal(68.7m, resumen.Participacion);
        Assert.Equal(79, resumen.AlumnosRespondieron);
        Assert.Equal(115, resumen.TotalAlumnos);
        Assert.Equal(8.2m, resumen.PromedioDocentes);
        Assert.Equal(2, resumen.DocentesPorDebajoDeOcho);
    }

    [Fact]
    public async Task ExportadorExcel_CreaLibroOpenXmlConContenidoDescargable()
    {
        var datos = await _servicio.ObtenerDatosReporteAsync("participacion", "sep-dic-2026", "5", "dsm");
        var bytes = ExportadorExcel.Crear("Participación", datos);
        using var memoria = new MemoryStream(bytes);
        using var archivo = new ZipArchive(memoria, ZipArchiveMode.Read);

        var contenido = archivo.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(contenido);
        using var lector = new StreamReader(contenido!.Open());
        var hoja = XDocument.Parse(lector.ReadToEnd());

        Assert.Contains(hoja.Descendants().Select(elemento => elemento.Value), valor => valor == "DSM05AV");
        Assert.NotNull(archivo.GetEntry("[Content_Types].xml"));
    }

    [Fact]
    public async Task ObtenerDatos_ExcluyeOtraCarrera()
    {
        Assert.Empty(await _servicio.ObtenerGruposAsync("sep-dic-2026", "todos", "otra-carrera"));
        Assert.Empty(await _servicio.ObtenerResultadosAsync("sep-dic-2026", "todos", tutores: false, "otra-carrera"));
        Assert.Empty(await _servicio.ObtenerAlumnosPendientesAsync("dsm05av-a", "otra-carrera"));
        Assert.Empty(await _servicio.ObtenerAlumnosPendientesAsync("mti05-a", "dsm"));
    }
}
