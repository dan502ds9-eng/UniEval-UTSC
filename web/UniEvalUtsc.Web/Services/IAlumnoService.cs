namespace UniEvalUtsc.Web.Services;

public interface IAlumnoService
{
    EvaluacionAlumno ObtenerEvaluacion();

    Task<Dictionary<string, int>> RecuperarBorradorAsync(string alumno);

    Task<bool> GuardarBorradorAsync(string alumno, IReadOnlyDictionary<string, int> calificaciones);

    Task<ResultadoEnvioAlumno> EnviarAsync(string alumno, IReadOnlyDictionary<string, int> calificaciones);

    ComprobanteAlumno? ObtenerComprobante();

    IReadOnlyList<MensajeAlumno> ObtenerMensajes();

    int MensajesSinLeer { get; }

    void MarcarMensajeComoLeido(string id);

    void MarcarTodosComoLeidos();
}
