using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace UniEvalUtsc.Web.Services;

public static class ImportadorTabular
{
    private static readonly XNamespace Spreadsheet =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    public static IReadOnlyList<IReadOnlyList<string>> Leer(Stream contenido, string nombreArchivo)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreArchivo);
        return Path.GetExtension(nombreArchivo).ToLowerInvariant() switch
        {
            ".csv" => LeerCsv(contenido),
            ".xlsx" => LeerXlsx(contenido),
            _ => throw new ArgumentException("El archivo debe tener formato CSV o XLSX.", nameof(nombreArchivo))
        };
    }

    private static IReadOnlyList<IReadOnlyList<string>> LeerCsv(Stream contenido)
    {
        using var lector = new StreamReader(contenido, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var filas = new List<IReadOnlyList<string>>();
        while (lector.ReadLine() is { } linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            filas.Add(SepararCsv(linea));
        }

        return filas;
    }

    private static IReadOnlyList<string> SepararCsv(string linea)
    {
        var separador = ContarSeparadores(linea, ';') > ContarSeparadores(linea, ',') ? ';' : ',';
        var valores = new List<string>();
        var valor = new StringBuilder();
        var entrecomillado = false;
        for (var indice = 0; indice < linea.Length; indice++)
        {
            var caracter = linea[indice];
            if (caracter == '"' && entrecomillado && indice + 1 < linea.Length && linea[indice + 1] == '"')
            {
                valor.Append('"');
                indice++;
            }
            else if (caracter == '"')
            {
                entrecomillado = !entrecomillado;
            }
            else if (caracter == separador && !entrecomillado)
            {
                valores.Add(valor.ToString().Trim());
                valor.Clear();
            }
            else
            {
                valor.Append(caracter);
            }
        }

        if (entrecomillado)
        {
            throw new FormatException("El CSV contiene un campo sin comillas de cierre.");
        }

        valores.Add(valor.ToString().Trim());
        return valores;
    }

    private static int ContarSeparadores(string linea, char separador)
    {
        var entrecomillado = false;
        var cantidad = 0;
        for (var indice = 0; indice < linea.Length; indice++)
        {
            if (linea[indice] == '"' && entrecomillado && indice + 1 < linea.Length && linea[indice + 1] == '"')
            {
                indice++;
            }
            else if (linea[indice] == '"')
            {
                entrecomillado = !entrecomillado;
            }
            else if (linea[indice] == separador && !entrecomillado)
            {
                cantidad++;
            }
        }

        return cantidad;
    }

    private static IReadOnlyList<IReadOnlyList<string>> LeerXlsx(Stream contenido)
    {
        using var archivo = new ZipArchive(contenido, ZipArchiveMode.Read, leaveOpen: true);
        var cadenas = LeerCadenasCompartidas(archivo);
        var libroEntrada = archivo.GetEntry("xl/workbook.xml")
            ?? throw new FormatException("El XLSX no contiene xl/workbook.xml.");
        var relacionesEntrada = archivo.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new FormatException("El XLSX no contiene las relaciones del libro.");
        var libro = CargarXml(libroEntrada);
        var relaciones = CargarXml(relacionesEntrada);
        var hojaId = libro.Descendants(Spreadsheet + "sheet").FirstOrDefault()
            ?.Attribute(Relationships + "id")?.Value
            ?? throw new FormatException("El XLSX no contiene hojas de cálculo.");
        var destino = relaciones.Descendants(PackageRelationships + "Relationship")
            .SingleOrDefault(relacion => relacion.Attribute("Id")?.Value == hojaId)
            ?.Attribute("Target")?.Value
            ?? throw new FormatException("El XLSX tiene una relación de hoja no válida.");
        var rutaHoja = destino.StartsWith("/", StringComparison.Ordinal)
            ? destino.TrimStart('/')
            : $"xl/{destino.TrimStart('/')}";
        var hojaEntrada = archivo.GetEntry(rutaHoja)
            ?? throw new FormatException($"No se encontró la hoja {rutaHoja} dentro del XLSX.");
        var hoja = CargarXml(hojaEntrada);

        return hoja.Descendants(Spreadsheet + "row")
            .Select(fila => (IReadOnlyList<string>)LeerFila(fila, cadenas))
            .Where(fila => fila.Any(valor => !string.IsNullOrWhiteSpace(valor)))
            .ToArray();
    }

    private static string[] LeerFila(XElement fila, IReadOnlyList<string> cadenas)
    {
        var celdas = new SortedDictionary<int, string>();
        var siguienteColumna = 0;
        foreach (var celda in fila.Elements(Spreadsheet + "c"))
        {
            var referencia = celda.Attribute("r")?.Value;
            var columna = referencia is null ? siguienteColumna : ObtenerIndiceColumna(referencia);
            celdas[columna] = LeerCelda(celda, cadenas);
            siguienteColumna = columna + 1;
        }

        if (celdas.Count == 0)
        {
            return [];
        }

        return Enumerable.Range(0, celdas.Keys.Max() + 1)
            .Select(indice => celdas.GetValueOrDefault(indice, string.Empty))
            .ToArray();
    }

    private static int ObtenerIndiceColumna(string referencia)
    {
        var indice = 0;
        var longitud = 0;
        foreach (var caracter in referencia)
        {
            if (!char.IsLetter(caracter))
            {
                break;
            }

            indice = checked(indice * 26 + char.ToUpperInvariant(caracter) - 'A' + 1);
            longitud++;
        }

        return longitud == 0
            ? throw new FormatException($"La referencia de celda '{referencia}' no contiene columna.")
            : indice - 1;
    }

    private static IReadOnlyList<string> LeerCadenasCompartidas(ZipArchive archivo)
    {
        var entrada = archivo.GetEntry("xl/sharedStrings.xml");
        return entrada is null
            ? []
            : CargarXml(entrada).Descendants(Spreadsheet + "si")
                .Select(cadena => string.Concat(cadena.Descendants(Spreadsheet + "t").Select(texto => texto.Value)))
                .ToArray();
    }

    private static string LeerCelda(XElement celda, IReadOnlyList<string> cadenas)
    {
        var valor = celda.Element(Spreadsheet + "v")?.Value;
        if (celda.Attribute("t")?.Value == "s" && int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var indice))
        {
            return indice >= 0 && indice < cadenas.Count
                ? cadenas[indice]
                : throw new FormatException("El XLSX hace referencia a una cadena compartida inexistente.");
        }

        return celda.Attribute("t")?.Value == "inlineStr"
            ? string.Concat(celda.Descendants(Spreadsheet + "t").Select(texto => texto.Value))
            : valor ?? string.Empty;
    }

    private static XDocument CargarXml(ZipArchiveEntry entrada)
    {
        using var flujo = entrada.Open();
        return XDocument.Load(flujo);
    }
}
