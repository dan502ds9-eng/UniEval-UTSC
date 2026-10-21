namespace UniEvalUtsc.Web.Services;

public sealed class FiltrosState
{
    public const string CarreraCoordinadorId = "dsm";

    public string PeriodoId { get; private set; } = string.Empty;

    public string Cuatrimestre { get; private set; } = "todos";

    public string CarreraId { get; private set; } = CarreraCoordinadorId;

    public void InicializarPeriodo(IEnumerable<PeriodoResultado> periodos)
    {
        ArgumentNullException.ThrowIfNull(periodos);
        var listaPeriodos = periodos.ToArray();
        if (listaPeriodos.Length == 0)
        {
            throw new InvalidOperationException("No hay periodos disponibles para mostrar.");
        }

        if (listaPeriodos.All(periodo => periodo.Id != PeriodoId))
        {
            PeriodoId = listaPeriodos.FirstOrDefault(periodo => periodo.Abierto)?.Id
                ?? listaPeriodos[0].Id;
        }
    }

    public void CambiarPeriodo(string periodoId, IEnumerable<PeriodoResultado> periodos)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(periodoId);
        if (!periodos.Any(periodo => periodo.Id == periodoId))
        {
            throw new InvalidOperationException("El periodo seleccionado ya no está disponible.");
        }

        PeriodoId = periodoId;
    }

    public void CambiarCuatrimestre(string cuatrimestre)
    {
        if (cuatrimestre is not ("todos" or "5" or "6" or "10"))
        {
            throw new ArgumentOutOfRangeException(nameof(cuatrimestre), cuatrimestre, "El cuatrimestre no es válido.");
        }

        Cuatrimestre = cuatrimestre;
    }

    public void CambiarCarrera(string carreraId)
    {
        if (carreraId is not ("todas" or "dsm" or "mti"))
        {
            throw new ArgumentOutOfRangeException(nameof(carreraId), carreraId, "La carrera no es válida.");
        }

        CarreraId = carreraId;
    }
}
