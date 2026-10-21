using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class AlmacenEvaluacionInMemory
{
    private readonly object _sincronizacion = new();
    private readonly Dictionary<int, Periodo> _periodos = DatosSemillaDemo.CrearPeriodos()
        .ToDictionary(periodo => periodo.Id);
    private readonly Dictionary<int, Plantilla> _plantillas = DatosSemillaDemo.CrearPlantillas()
        .ToDictionary(plantilla => plantilla.Id);
    private readonly Dictionary<int, EncuestaAsignada> _encuestas = DatosSemillaDemo.CrearEncuestasAsignadas()
        .ToDictionary(encuesta => encuesta.Id);
    private readonly List<Respuesta> _respuestas = [];

    public Periodo? ObtenerPeriodoPorGrupo(int grupoId)
    {
        lock (_sincronizacion)
        {
            var periodoActual = _periodos.Values.Where(periodo => periodo.Estado == EstadoPeriodo.Abierto)
                .OrderByDescending(periodo => periodo.FechaFin).FirstOrDefault()
                ?? _periodos.Values.OrderByDescending(periodo => periodo.FechaFin).FirstOrDefault();
            return periodoActual is not null && DatosSemillaDemo.Grupos.Any(grupo => grupo.Id == grupoId) ? periodoActual : null;
        }
    }

    public Plantilla? ObtenerPlantilla(int id)
    {
        lock (_sincronizacion)
            return _plantillas.GetValueOrDefault(id);
    }

    public EncuestaAsignada? ObtenerEncuesta(int id)
    {
        lock (_sincronizacion)
            return _encuestas.GetValueOrDefault(id);
    }

    public IReadOnlyCollection<Periodo> ObtenerPeriodos()
    {
        lock (_sincronizacion)
            return _periodos.Values.ToArray();
    }

    public IReadOnlyCollection<EncuestaAsignada> ObtenerEncuestas()
    {
        lock (_sincronizacion)
            return _encuestas.Values.ToArray();
    }

    public IReadOnlyCollection<Respuesta> ObtenerRespuestas()
    {
        lock (_sincronizacion)
            return _respuestas.ToArray();
    }

    public void GuardarRespuestas(IReadOnlyCollection<Respuesta> respuestas)
    {
        lock (_sincronizacion)
            _respuestas.AddRange(respuestas);
    }
}
