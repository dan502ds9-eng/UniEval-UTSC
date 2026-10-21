using System.IO.Compression;
using System.Xml.Linq;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class CoordinacionInMemoryServiceTests
{
    private readonly CoordinacionInMemoryService _servicio = new();

    [Fact]
    public void ObtenerGrupos_OrdenaParticipacionSegunDatosFiltrados()
    {
        var grupos = _servicio.ObtenerGrupos("sep-dic-2026", "5", "dsm");

        Assert.Equal(2, grupos.Count);
        Assert.Equal("dsm05av-a", grupos[0].Id);
        Assert.Equal(48.4m, grupos[0].Porcentaje);
        Assert.Equal(16, grupos[0].Faltan);
    }

    [Fact]
    public void ObtenerResultados_EnEstadiasSoloIncluyeTutorEstadias()
    {
        var docentes = _servicio.ObtenerResultados("sep-dic-2026", "6", tutores: false, "dsm");
        var tutores = _servicio.ObtenerResultados("sep-dic-2026", "6", tutores: true, "dsm");

        Assert.Empty(docentes);
        var tutor = Assert.Single(tutores);
        Assert.True(tutor.EsTutorEstadias);
        Assert.Equal(6, tutor.Cuatrimestre);
    }

    [Fact]
    public void ObtenerResultados_UsaPeriodoSolicitadoYOrdenaDeMenorAMayor()
    {
        var resultados = _servicio.ObtenerResultados("ene-abr-2026", "5", tutores: false, "dsm");

        Assert.All(resultados, resultado => Assert.Equal("Ene–Abr 2026", resultado.Periodo));
        Assert.Equal(resultados.OrderBy(resultado => resultado.Promedio), resultados);
    }

    [Fact]
    public void ObtenerResumen_EntregaIndicadoresAgregadosDeLaCarrera()
    {
        var resumen = _servicio.ObtenerResumen("sep-dic-2026", "todos", "dsm");

        Assert.Equal(68.7m, resumen.Participacion);
        Assert.Equal(79, resumen.AlumnosRespondieron);
        Assert.Equal(115, resumen.TotalAlumnos);
        Assert.Equal(8.2m, resumen.PromedioDocentes);
        Assert.Equal(2, resumen.DocentesPorDebajoDeOcho);
    }

    [Fact]
    public void ExportadorExcel_CreaLibroOpenXmlConContenidoDescargable()
    {
        var datos = _servicio.ObtenerDatosReporte("participacion", "sep-dic-2026", "5", "dsm");
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
    public void ObtenerDatos_ExcluyeOtraCarrera()
    {
        Assert.Empty(_servicio.ObtenerGrupos("sep-dic-2026", "todos", "otra-carrera"));
        Assert.Empty(_servicio.ObtenerResultados("sep-dic-2026", "todos", tutores: false, "otra-carrera"));
        Assert.Empty(_servicio.ObtenerAlumnosPendientes("dsm05av-a", "otra-carrera"));
        Assert.Empty(_servicio.ObtenerAlumnosPendientes("mti05-a", "dsm"));
    }
}
