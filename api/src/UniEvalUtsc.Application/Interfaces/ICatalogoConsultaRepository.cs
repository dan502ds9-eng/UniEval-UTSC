using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Application.Interfaces;

public sealed record DatosEvaluadoAlumno(string Grupo, string? Materia, string PersonaEvaluada);

public interface ICatalogoConsultaRepository
{
    Task<DatosEvaluadoAlumno> ObtenerEvaluadoAsync(EncuestaAsignada encuesta, CancellationToken ct = default);
}
