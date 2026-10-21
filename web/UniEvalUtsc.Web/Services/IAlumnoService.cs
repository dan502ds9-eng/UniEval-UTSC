namespace UniEvalUtsc.Web.Services;

public interface IAlumnoService
{
    Task<EvaluacionAlumno> ObtenerEvaluacionAsync(CancellationToken ct = default);

    Task<Dictionary<string, int>> RecuperarBorradorAsync(string alumno, CancellationToken ct = default);

    Task<bool> GuardarBorradorAsync(string alumno, IReadOnlyDictionary<string, int> calificaciones, CancellationToken ct = default);

    Task<ResultadoEnvioAlumno> EnviarAsync(string alumno, IReadOnlyDictionary<string, int> calificaciones, CancellationToken ct = default);

    Task<ComprobanteAlumno?> ObtenerComprobanteAsync(CancellationToken ct = default);

    Task<IReadOnlyList<MensajeAlumno>> ObtenerMensajesAsync(CancellationToken ct = default);

    Task<int> ObtenerMensajesSinLeerAsync(CancellationToken ct = default);

    Task CrearRecordatorioAsync(string grupoId, string nombreGrupo, CancellationToken ct = default);

    Task MarcarMensajeComoLeidoAsync(string id, CancellationToken ct = default);

    Task MarcarTodosComoLeidosAsync(CancellationToken ct = default);
}
