using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Application.Interfaces;

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
    private readonly List<MensajeSistema> _mensajes = [];

    public AlmacenEvaluacionInMemory(bool sembrarResultados = false)
    {
        if (sembrarResultados)
        {
            // Semilla cerrada para comprobar RN-10 en resultados, separada del grupo activo del alumno.
            SembrarResultadosCerrados();
            var encuestaPendiente = EncuestaAsignada.ParaDocente(8, 1, 16, 1, id: 20, periodoId: 1);
            _encuestas.Add(encuestaPendiente.Id, encuestaPendiente);
        }
    }

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

    public Periodo? ObtenerPeriodo(int periodoId)
    {
        lock (_sincronizacion) return _periodos.GetValueOrDefault(periodoId);
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

    public IReadOnlyCollection<MensajeSistema> ObtenerMensajes()
    {
        lock (_sincronizacion) return _mensajes.ToArray();
    }

    public int CrearMensajes(IReadOnlyCollection<string> alumnoIds, string remitente, string titulo, string cuerpo, DateTime creadoEl)
    {
        lock (_sincronizacion)
        {
            foreach (var alumnoId in alumnoIds.Distinct(StringComparer.Ordinal))
                _mensajes.Add(new MensajeSistema(Guid.NewGuid(), alumnoId, remitente, titulo, cuerpo, creadoEl, null));
            return alumnoIds.Distinct(StringComparer.Ordinal).Count();
        }
    }

    public void MarcarMensajeLeido(Guid id, string alumnoId, DateTime leidoEl)
    {
        lock (_sincronizacion)
        {
            var index = _mensajes.FindIndex(x => x.Id == id && x.AlumnoId == alumnoId);
            if (index >= 0) _mensajes[index] = _mensajes[index] with { LeidoEl = leidoEl };
        }
    }

    public void MarcarMensajesLeidos(string alumnoId, DateTime leidoEl)
    {
        lock (_sincronizacion)
            for (var index = 0; index < _mensajes.Count; index++)
                if (_mensajes[index].AlumnoId == alumnoId && _mensajes[index].LeidoEl is null)
                    _mensajes[index] = _mensajes[index] with { LeidoEl = leidoEl };
    }

    private void SembrarResultadosCerrados()
    {
        var plantillaDocente = _plantillas[16];
        var plantillaTutor = _plantillas[64];
        var calificacionesDocente = plantillaDocente.Aspectos.Select(a => new CalificacionAspecto(a.Id, 8)).ToArray();
        var calificacionesTutor = plantillaTutor.Aspectos.Select(a => new CalificacionAspecto(a.Id, 7)).ToArray();

        for (var index = 0; index < 5; index++)
        {
            var alumnoId = index + 1;
            var asignada = EncuestaAsignada.ParaDocente(alumnoId, 6, 16, 101, periodoId: 2, id: 100 + index);
            asignada.MarcarContestada(new DateOnly(2026, 8, 20));
            _encuestas.Add(asignada.Id, asignada);
            _respuestas.Add(Respuesta.Crear(asignada, plantillaDocente, calificacionesDocente, periodoId: 2));

            var tutorAsignada = EncuestaAsignada.ParaTutor(alumnoId, 6, 64, 501, periodoId: 2, id: 200 + index);
            tutorAsignada.MarcarContestada(new DateOnly(2026, 8, 20));
            _encuestas.Add(tutorAsignada.Id, tutorAsignada);
            _respuestas.Add(Respuesta.Crear(tutorAsignada, plantillaTutor, calificacionesTutor, periodoId: 2));
        }

        for (var index = 0; index < 2; index++)
        {
            var asignada = EncuestaAsignada.ParaDocente(index + 6, 6, 16, 102, periodoId: 2, id: 300 + index);
            asignada.MarcarContestada(new DateOnly(2026, 8, 20));
            _encuestas.Add(asignada.Id, asignada);
            _respuestas.Add(Respuesta.Crear(asignada, plantillaDocente, calificacionesDocente, periodoId: 2));
        }

        var calificacionesOtroDocente = plantillaDocente.Aspectos
            .Select(aspecto => new CalificacionAspecto(aspecto.Id, 6)).ToArray();
        for (var index = 0; index < 5; index++)
        {
            var asignada = EncuestaAsignada.ParaDocente(index + 11, 6, 16, 103, periodoId: 2, id: 700 + index);
            asignada.MarcarContestada(new DateOnly(2026, 8, 20));
            _encuestas.Add(asignada.Id, asignada);
            _respuestas.Add(Respuesta.Crear(asignada, plantillaDocente, calificacionesOtroDocente, periodoId: 2));
        }

        for (var index = 0; index < 3; index++)
        {
            var asignada = EncuestaAsignada.ParaTutor(index + 6, 7, 64, 502, periodoId: 2, id: 400 + index);
            asignada.MarcarContestada(new DateOnly(2026, 8, 20));
            _encuestas.Add(asignada.Id, asignada);
            _respuestas.Add(Respuesta.Crear(asignada, plantillaTutor, calificacionesTutor, periodoId: 2));
        }

        for (var index = 0; index < 5; index++)
        {
            var asignada = EncuestaAsignada.ParaTutor(index + 1, 3, 64, 503, periodoId: 2, id: 500 + index);
            asignada.MarcarContestada(new DateOnly(2026, 8, 20));
            _encuestas.Add(asignada.Id, asignada);
            _respuestas.Add(Respuesta.Crear(asignada, plantillaTutor, calificacionesTutor, periodoId: 2));
        }

        for (var index = 0; index < 3; index++)
        {
            var asignada = EncuestaAsignada.ParaTutor(index + 6, 4, 64, 504, periodoId: 2, id: 600 + index);
            asignada.MarcarContestada(new DateOnly(2026, 8, 20));
            _encuestas.Add(asignada.Id, asignada);
            _respuestas.Add(Respuesta.Crear(asignada, plantillaTutor, calificacionesTutor, periodoId: 2));
        }
    }
}
