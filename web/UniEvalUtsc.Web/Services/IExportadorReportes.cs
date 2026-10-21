namespace UniEvalUtsc.Web.Services;

public interface IExportadorReportes
{
    Task ExportarExcelAsync(string nombre, IReadOnlyList<IReadOnlyList<string>> filas);

    Task ExportarPdfAsync(string nombre, IReadOnlyList<IReadOnlyList<string>> filas);
}
