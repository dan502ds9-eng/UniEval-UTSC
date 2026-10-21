using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace UniEvalUtsc.Web.Services;

public static class ExportadorExcel
{
    private static readonly XNamespace HojaCalculo =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelacionesDocumento =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace RelacionesPaquete =
        "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace TiposContenido =
        "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Crear(string nombreHoja, IReadOnlyList<IReadOnlyList<string>> filas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreHoja);
        ArgumentNullException.ThrowIfNull(filas);

        using var memoria = new MemoryStream();
        using (var archivo = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            AgregarDocumento(archivo, "[Content_Types].xml", CrearTiposContenido());
            AgregarDocumento(archivo, "_rels/.rels", CrearRelacionesRaiz());
            AgregarDocumento(archivo, "xl/workbook.xml", CrearLibro(nombreHoja));
            AgregarDocumento(archivo, "xl/_rels/workbook.xml.rels", CrearRelacionesLibro());
            AgregarDocumento(archivo, "xl/worksheets/sheet1.xml", CrearHoja(filas));
        }

        return memoria.ToArray();
    }

    private static XDocument CrearTiposContenido() =>
        new(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(TiposContenido + "Types",
                new XElement(TiposContenido + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(TiposContenido + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(TiposContenido + "Override",
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(TiposContenido + "Override",
                    new XAttribute("PartName", "/xl/worksheets/sheet1.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));

    private static XDocument CrearRelacionesRaiz() =>
        new(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(RelacionesPaquete + "Relationships",
                new XElement(RelacionesPaquete + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))));

    private static XDocument CrearLibro(string nombreHoja) =>
        new(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(HojaCalculo + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", RelacionesDocumento),
                new XElement(HojaCalculo + "sheets",
                    new XElement(HojaCalculo + "sheet",
                        new XAttribute("name", NormalizarNombreHoja(nombreHoja)),
                        new XAttribute("sheetId", 1),
                        new XAttribute(RelacionesDocumento + "id", "rId1")))));

    private static XDocument CrearRelacionesLibro() =>
        new(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(RelacionesPaquete + "Relationships",
                new XElement(RelacionesPaquete + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", "worksheets/sheet1.xml"))));

    private static XDocument CrearHoja(IReadOnlyList<IReadOnlyList<string>> filas) =>
        new(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(HojaCalculo + "worksheet",
                new XElement(HojaCalculo + "sheetData",
                    filas.Select((fila, indiceFila) =>
                        new XElement(HojaCalculo + "row",
                            new XAttribute("r", indiceFila + 1),
                            fila.Select((valor, indiceColumna) =>
                                new XElement(HojaCalculo + "c",
                                    new XAttribute("r", $"{NombreColumna(indiceColumna + 1)}{indiceFila + 1}"),
                                    new XAttribute("t", "inlineStr"),
                                    new XElement(HojaCalculo + "is",
                                        new XElement(HojaCalculo + "t",
                                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                                            valor)))))))));

    private static void AgregarDocumento(ZipArchive archivo, string ruta, XDocument documento)
    {
        var entrada = archivo.CreateEntry(ruta);
        using var flujo = entrada.Open();
        using var escritor = XmlWriter.Create(flujo, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false
        });
        documento.Save(escritor);
    }

    private static string NormalizarNombreHoja(string nombre) =>
        string.Concat(nombre.Where(caracter => !"[]:*?/\\".Contains(caracter)))
            .Trim('\'')
            .Take(31)
            .ToArray() is { Length: > 0 } nombreNormalizado
                ? new string(nombreNormalizado)
                : "Reporte";

    private static string NombreColumna(int numero)
    {
        var nombre = string.Empty;
        while (numero > 0)
        {
            numero--;
            nombre = (char)('A' + numero % 26) + nombre;
            numero /= 26;
        }

        return nombre;
    }
}
