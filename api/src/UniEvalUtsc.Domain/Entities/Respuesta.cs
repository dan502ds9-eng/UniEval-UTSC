using System;
using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Domain.Entities;

public record CalificacionAspecto(int AspectoId, int Calificacion);

public class Respuesta
{
    private readonly List<RespuestaDetalle> _detalles = [];

    public Guid Id { get; private set; }
    public int GrupoId { get; private set; }
    public int PlantillaId { get; private set; }
    public int? CargaAcademicaId { get; private set; }
    public int? TutorEvaluadoId { get; private set; }
    public TipoEncuesta Tipo { get; private set; }
    public IReadOnlyCollection<RespuestaDetalle> Detalles => _detalles.AsReadOnly();

    private Respuesta() { } // EF Core

    public static Respuesta Crear(EncuestaAsignada encuesta, Plantilla plantilla,
                                  IReadOnlyCollection<CalificacionAspecto> calificaciones)
    {
        if (plantilla.Id != encuesta.PlantillaId)
            throw new DomainException("La plantilla no corresponde a esta encuesta.");

        // RN-06 y RF-11: todos los aspectos, sin repetir, del 1 al 10
        var esperados = plantilla.Aspectos.Select(a => a.Id).ToHashSet();
        var recibidos = calificaciones.Select(c => c.AspectoId).ToList();

        if (recibidos.Count != recibidos.Distinct().Count())
            throw new ValidacionException("Hay aspectos calificados más de una vez.");
        if (!esperados.SetEquals(recibidos))
            throw new ValidacionException("Debes calificar todos los aspectos de la encuesta.");
        if (calificaciones.Any(c => c.Calificacion is < 1 or > 10))
            throw new ValidacionException("Cada calificación debe ser un número entero del 1 al 10.");

        var respuesta = new Respuesta
        {
            Id = Guid.NewGuid(), // UUID v4 aleatorio. NO usar Guid.CreateVersion7(): va ordenado por tiempo
            GrupoId = encuesta.GrupoId,
            PlantillaId = encuesta.PlantillaId,
            CargaAcademicaId = encuesta.CargaAcademicaId,
            TutorEvaluadoId = encuesta.TutorEvaluadoId,
            Tipo = encuesta.Tipo
            // RN-09: AlumnoId NO se copia. Aquí se corta el vínculo.
        };

        foreach (var c in calificaciones)
            respuesta._detalles.Add(new RespuestaDetalle(respuesta.Id, c.AspectoId, c.Calificacion));

        return respuesta;
    }
}