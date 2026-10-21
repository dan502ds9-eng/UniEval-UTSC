namespace UniEvalUtsc.Web.Services;

public interface ICatalogosService
{
    Task<IReadOnlyList<ElementoCatalogo>> ObtenerAsync(string tipo, CancellationToken ct = default);

    Task AgregarAsync(string tipo, string nombre, string contexto, CancellationToken ct = default);

    Task CambiarEstadoAsync(string tipo, string id, CancellationToken ct = default);

    Task<ResultadoImportacion> ImportarAsync(string tipo, IReadOnlyList<IReadOnlyList<string>> filas, CancellationToken ct = default);
}
