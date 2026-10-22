using UniEvalUtsc.Application.Exceptions;
using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Contracts.Dtos;
using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;

namespace UniEvalUtsc.Application.Services;

public sealed class ConsultarEncuestasAlumnoService(
    IEncuestaAsignadaRepository encuestas,
    IPlantillaRepository plantillas,
    IPeriodoRepository periodos,
    ICatalogoConsultaRepository catalogos,
    IReloj reloj)
{
    public async Task<AvanceEncuestasDto> ObtenerMisEncuestasAsync(int alumnoId, CancellationToken ct = default)
    {
        var visibles = new List<(EncuestaAsignada Encuesta, Periodo Periodo)>();
        foreach (var encuesta in await encuestas.ObtenerTodasAsync(alumnoId, ct))
        {
            var periodo = await ObtenerPeriodoAsync(encuesta, ct);
            if (periodo?.Estado == EstadoPeriodo.Abierto && periodo.AceptaRespuestas(reloj.Ahora))
                visibles.Add((encuesta, periodo));
        }

        var items = new List<EncuestaAsignadaDto>();
        foreach (var (encuesta, _) in visibles)
        {
            var evaluado = await catalogos.ObtenerEvaluadoAsync(encuesta, ct);
            items.Add(new EncuestaAsignadaDto(encuesta.Id, evaluado.Grupo, evaluado.Materia, evaluado.PersonaEvaluada,
                (Contracts.Enums.TipoEncuesta)(int)encuesta.Tipo, (Contracts.Enums.EstadoEncuesta)(int)encuesta.Estado));
        }
        return new AvanceEncuestasDto(items.Count(i => i.Estado == Contracts.Enums.EstadoEncuesta.Contestada), items.Count, items);
    }

    public async Task<EvaluacionUnificadaDto> ObtenerEvaluacionAsync(int alumnoId, CancellationToken ct = default)
    {
        var filas = new List<(EncuestaAsignada Encuesta, Plantilla Plantilla, DatosEvaluadoAlumno Evaluado, Periodo Periodo)>();
        foreach (var encuesta in await encuestas.ObtenerPendientesAsync(alumnoId, ct))
        {
            var periodo = await ObtenerPeriodoAsync(encuesta, ct);
            if (periodo?.Estado != EstadoPeriodo.Abierto || !periodo.AceptaRespuestas(reloj.Ahora)) continue;
            var plantilla = await plantillas.ObtenerConAspectosAsync(encuesta.PlantillaId, ct)
                ?? throw new NoEncontradoException("La plantilla de la encuesta no existe.");
            filas.Add((encuesta, plantilla, await catalogos.ObtenerEvaluadoAsync(encuesta, ct), periodo));
        }

        var primera = filas.FirstOrDefault();
        if (primera.Plantilla is null) return new EvaluacionUnificadaDto("", []);
        var preguntas = primera.Plantilla.Aspectos.OrderBy(a => a.Numero).Select(aspecto =>
            new PreguntaEvaluacionDto(aspecto.Id, aspecto.Numero, aspecto.Texto,
                filas.Select(f => new EvaluadoDto(f.Encuesta.Id, f.Evaluado.PersonaEvaluada, f.Evaluado.Materia,
                    (Contracts.Enums.TipoEncuesta)(int)f.Encuesta.Tipo)).ToArray())).ToArray();
        return new EvaluacionUnificadaDto(primera.Periodo.Nombre, preguntas);
    }

    public async Task<ComprobanteDto> ObtenerComprobanteAsync(int alumnoId, string nombre, CancellationToken ct = default)
    {
        var asignaciones = await encuestas.ObtenerTodasAsync(alumnoId, ct);
        if (asignaciones.Count == 0) throw new NoEncontradoException("Aún no hay encuestas contestadas para el periodo actual.");
        var candidatas = new List<(EncuestaAsignada Encuesta, Periodo Periodo)>();
        foreach (var encuesta in asignaciones)
        {
            var periodo = await ObtenerPeriodoAsync(encuesta, ct);
            if (periodo is not null) candidatas.Add((encuesta, periodo));
        }
        var periodoElegido = candidatas.Select(x => x.Periodo).OrderByDescending(p => p.Estado == EstadoPeriodo.Abierto)
            .ThenByDescending(p => p.FechaFin).FirstOrDefault();
        if (periodoElegido is null) throw new NoEncontradoException("No hay un periodo disponible para el comprobante.");
        var delPeriodo = candidatas.Where(x => x.Periodo.Id == periodoElegido.Id).Select(x => x.Encuesta).ToArray();
        if (delPeriodo.Length == 0 || delPeriodo.Any(e => e.Estado != EstadoEncuesta.Contestada))
            throw new NoEncontradoException("Aún tienes encuestas pendientes.");
        var fecha = delPeriodo.Max(e => e.FechaContestada ?? reloj.Hoy);
        return new ComprobanteDto(nombre, periodoElegido.Nombre, delPeriodo.Length, delPeriodo.Length, fecha);
    }

    private Task<Periodo?> ObtenerPeriodoAsync(EncuestaAsignada encuesta, CancellationToken ct) =>
        encuesta.PeriodoId > 0
            ? periodos.ObtenerPorIdAsync(encuesta.PeriodoId, ct)
            : periodos.ObtenerPorGrupoAsync(encuesta.GrupoId, ct);
}
