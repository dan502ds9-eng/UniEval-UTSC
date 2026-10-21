using System;
using UniEvalUtsc.Application.Exceptions;
using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Application.Services;

public class EnviarRespuestaService(
    IEncuestaAsignadaRepository encuestas,
    IPlantillaRepository plantillas,
    IPeriodoRepository periodos,
    IRespuestaRepository respuestas,
    IUnitOfWork unidadDeTrabajo,
    IReloj reloj)
{
    public async Task EnviarAsync(int encuestaId, int alumnoId,
        IReadOnlyCollection<CalificacionAspecto> calificaciones, CancellationToken ct = default)
    {
        // 1. ¿Existe?  -> 404
        var encuesta = await encuestas.ObtenerAsync(encuestaId, ct)
            ?? throw new NoEncontradoException("La encuesta no existe.");

        // 2. ¿Es tuya? -> 403 (RNF-05)
        if (!encuesta.PerteneceA(alumnoId))
            throw new AccesoDenegadoException("Esta encuesta no te pertenece.");

        // 3. ¿Ya la contestaste? -> 409 (RN-07)
        if (encuesta.Estado == EstadoEncuesta.Contestada)
            throw new DomainException("Esta encuesta ya fue contestada.");

        // 4. ¿Periodo abierto y en fechas? -> 409 (RN-08)
        var periodo = await periodos.ObtenerPorGrupoAsync(encuesta.GrupoId, ct);
        if (periodo is null || !periodo.AceptaRespuestas(reloj.Ahora))
            throw new DomainException("El periodo de encuestas no está abierto.");

        // 5. Validar calificaciones y crear la respuesta anónima -> 400 si algo falla (RN-06, RN-09)
        var plantilla = await plantillas.ObtenerConAspectosAsync(encuesta.PlantillaId, ct)
            ?? throw new NoEncontradoException("La plantilla de la encuesta no existe.");
        var respuesta = Respuesta.Crear(encuesta, plantilla, calificaciones);

        // 6. Todo o nada (RNF-07)
        respuestas.Agregar(respuesta);
        encuesta.MarcarContestada(reloj.Hoy);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
    }
}