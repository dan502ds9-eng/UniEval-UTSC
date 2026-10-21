namespace UniEvalUtsc.Web.Services;

public sealed class ExportadorLocal(UnievalJsInterop jsInterop) : IExportadorReportes
{
    public async Task ExportarExcelAsync(string nombre, IReadOnlyList<IReadOnlyList<string>> filas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentNullException.ThrowIfNull(filas);

        var contenido = ExportadorExcel.Crear(nombre, filas);
        await jsInterop.DescargarArchivoAsync(
            $"{nombre}.xlsx",
            Convert.ToBase64String(contenido),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    public async Task ExportarPdfAsync(string nombre, IReadOnlyList<IReadOnlyList<string>> filas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentNullException.ThrowIfNull(filas);

        var resultado = await jsInterop.ImprimirReporteAsync(nombre, filas);
        if (!resultado)
        {
            throw new InvalidOperationException("El navegador bloqueó la ventana para generar el PDF.");
        }
    }
}
