namespace UniEvalUtsc.Web.Services;

public interface IPeriodosService
{
    IReadOnlyList<PeriodoAdministrativo> ObtenerPeriodos();

    PeriodoAdministrativo ObtenerActual();

    PeriodoAdministrativo CrearPeriodo(string nombre, DateOnly inicio, DateOnly cierre);

    void EditarFechas(string periodoId, DateOnly inicioCaptura, DateOnly finCaptura, DateOnly inicioEncuestas, DateOnly cierreEncuestas);

    int Abrir(string periodoId);

    void Cerrar(string periodoId);
}
