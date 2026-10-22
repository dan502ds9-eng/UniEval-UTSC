namespace UniEvalUtsc.Application.Interfaces;

public sealed record MensajeSistema(
    Guid Id, string AlumnoId, string Remitente, string Titulo, string Cuerpo,
    DateTime CreadoEl, DateTime? LeidoEl);

public interface IMensajeRepository
{
    Task<int> CrearParaAlumnosAsync(
        IReadOnlyCollection<string> alumnoIds, string remitente, string titulo, string cuerpo,
        DateTime creadoEl, CancellationToken ct = default);
    Task<IReadOnlyCollection<MensajeSistema>> ObtenerPorAlumnoAsync(string alumnoId, CancellationToken ct = default);
    Task<MensajeSistema?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task MarcarLeidoAsync(Guid id, string alumnoId, DateTime leidoEl, CancellationToken ct = default);
    Task MarcarTodosLeidosAsync(string alumnoId, DateTime leidoEl, CancellationToken ct = default);
}
