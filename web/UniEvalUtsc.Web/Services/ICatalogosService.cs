namespace UniEvalUtsc.Web.Services;

public interface ICatalogosService
{
    IReadOnlyList<ElementoCatalogo> Obtener(string tipo);

    void Agregar(string tipo, string nombre, string contexto);

    void CambiarEstado(string tipo, string id);

    ResultadoImportacion Importar(string tipo, IReadOnlyList<IReadOnlyList<string>> filas);
}
