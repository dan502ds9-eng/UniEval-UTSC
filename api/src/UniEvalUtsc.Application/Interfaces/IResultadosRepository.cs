using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Application.Interfaces;

public sealed record GrupoResultadoDatos(
    int Id, string Nombre, string CarreraId, int Cuatrimestre, bool EsEstadia,
    int TotalAlumnos, IReadOnlyCollection<int> AlumnoIds, int? TutorEvaluadoId = null);

public sealed record CargaResultadoDatos(
    int Id, int GrupoId, string NombreMateria, string NombreDocente);

public sealed record DatosResultados(
    IReadOnlyCollection<Periodo> Periodos,
    IReadOnlyCollection<Plantilla> Plantillas,
    IReadOnlyCollection<EncuestaAsignada> Encuestas,
    IReadOnlyCollection<Respuesta> Respuestas,
    IReadOnlyCollection<GrupoResultadoDatos> Grupos,
    IReadOnlyCollection<CargaResultadoDatos> Cargas);

public interface IResultadosRepository
{
    Task<DatosResultados> ObtenerDatosAsync(CancellationToken ct = default);
}
