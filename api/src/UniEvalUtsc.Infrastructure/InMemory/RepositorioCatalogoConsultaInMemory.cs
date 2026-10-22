using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioCatalogoConsultaInMemory : ICatalogoConsultaRepository
{
    public Task<DatosEvaluadoAlumno> ObtenerEvaluadoAsync(EncuestaAsignada encuesta, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var grupo = DatosSemillaDemo.Grupos.FirstOrDefault(g => g.Id == encuesta.GrupoId);
        var materia = encuesta.CargaAcademicaId is int carga
            ? grupo?.Materias.FirstOrDefault(m => m.CargaAcademicaId == carga)
            : null;
        var persona = encuesta.Tipo == TipoEncuesta.Tutor ? grupo?.Tutor : materia?.Docente;
        return Task.FromResult(new DatosEvaluadoAlumno(grupo?.Nombre ?? "", materia?.Materia, persona ?? ""));
    }
}
