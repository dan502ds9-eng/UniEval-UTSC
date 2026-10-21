using System;
using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Domain.Entities;

public class EncuestaAsignada
{
    public int Id { get; private set; }
    public int AlumnoId { get; private set; }
    public int GrupoId { get; private set; }
    public int PlantillaId { get; private set; }
    public int? CargaAcademicaId { get; private set; }
    public int? TutorEvaluadoId { get; private set; }
    public TipoEncuesta Tipo { get; private set; }
    public EstadoEncuesta Estado { get; private set; }
    public DateOnly? FechaContestada { get; private set; }

    private EncuestaAsignada() { } // EF Core

    // RN-02: una encuesta por materia (carga académica)
    public static EncuestaAsignada ParaDocente(
        int alumnoId, int grupoId, int plantillaId, int cargaAcademicaId, int id = 0) =>
        new()
        {
            Id = id,
            AlumnoId = alumnoId,
            GrupoId = grupoId,
            PlantillaId = plantillaId,
            CargaAcademicaId = cargaAcademicaId,
            Tipo = TipoEncuesta.Docente,
            Estado = EstadoEncuesta.Pendiente
        };

    // RN-04: una sola encuesta de tutor por grupo
    public static EncuestaAsignada ParaTutor(
        int alumnoId, int grupoId, int plantillaId, int tutorId, int id = 0) =>
        new()
        {
            Id = id,
            AlumnoId = alumnoId,
            GrupoId = grupoId,
            PlantillaId = plantillaId,
            TutorEvaluadoId = tutorId,
            Tipo = TipoEncuesta.Tutor,
            Estado = EstadoEncuesta.Pendiente
        };

    public bool PerteneceA(int alumnoId) => AlumnoId == alumnoId;

    // RN-07: se contesta una sola vez
    public void MarcarContestada(DateOnly hoy)
    {
        if (Estado == EstadoEncuesta.Contestada)
            throw new DomainException("Esta encuesta ya fue contestada y no se puede modificar.");
        Estado = EstadoEncuesta.Contestada;
        FechaContestada = hoy;
    }

    public void RevertirContestacion()
    {
        Estado = EstadoEncuesta.Pendiente;
        FechaContestada = null;
    }
}
