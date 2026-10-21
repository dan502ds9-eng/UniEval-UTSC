namespace UniEvalUtsc.Web.Services;

public sealed record EvaluacionAlumno(
    string PeriodoId,
    string Periodo,
    string Grupo,
    bool PeriodoAbierto,
    bool EsEstadia,
    IReadOnlyList<PreguntaAlumno> Preguntas,
    IReadOnlyList<EvaluadoAlumno> Evaluados,
    bool Enviado,
    DateOnly? FechaEnvio)
{
    public string Formato => EsEstadia ? "FORM-AC64" : "FORM-AC16";
    public int TotalRespuestas => Preguntas.Count * Evaluados.Count;

    public static string CrearClaveCalificacion(int preguntaId, string evaluadoId) =>
        $"{preguntaId}:{evaluadoId}";
}

public sealed record PreguntaAlumno(int Id, string Texto);

public sealed record EvaluadoAlumno(
    string Id,
    string Materia,
    string Nombre,
    bool EsTutor,
    string Etiqueta);

public sealed record ResultadoEnvioAlumno(bool Exitoso, string Mensaje);

public sealed record ComprobanteAlumno(
    string Periodo,
    int EncuestasContestadas,
    int TotalEncuestas,
    DateOnly? FechaEnvio);

public sealed class MensajeAlumno(
    string id,
    string remitente,
    string titulo,
    string cuerpo,
    DateOnly fecha,
    bool leido)
{
    public string Id { get; } = id;
    public string Remitente { get; } = remitente;
    public string Titulo { get; } = titulo;
    public string Cuerpo { get; } = cuerpo;
    public DateOnly Fecha { get; } = fecha;
    public bool Leido { get; set; } = leido;
}
