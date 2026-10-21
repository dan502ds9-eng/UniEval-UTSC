using System.Text;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Services;

public sealed class ImportadorTabularTests
{
    [Fact]
    public void Leer_CsvConPuntoYComaRespetaCamposEntrecomillados()
    {
        using var contenido = new MemoryStream(Encoding.UTF8.GetBytes(
            "Grupo;Materia;Docente;Tipo\r\nDSM05AV;\"Bases de Datos; Avanzadas\";Docente;docente"));

        var filas = ImportadorTabular.Leer(contenido, "carga.csv");

        Assert.Equal("Bases de Datos; Avanzadas", filas[1][1]);
        Assert.Equal(4, filas[1].Count);
    }

    [Fact]
    public void Leer_XlsxConservaColumnasVacias()
    {
        IReadOnlyList<IReadOnlyList<string>> filas =
        [
            ["Grupo", "Correo", "Tutor"],
            ["DSM06AV", "", "Tutor asignado"]
        ];
        using var contenido = new MemoryStream(ExportadorExcel.Crear("Estadías", filas));

        var resultado = ImportadorTabular.Leer(contenido, "estadias.xlsx");

        Assert.Equal("DSM06AV", resultado[1][0]);
        Assert.Equal(string.Empty, resultado[1][1]);
        Assert.Equal("Tutor asignado", resultado[1][2]);
    }
}
