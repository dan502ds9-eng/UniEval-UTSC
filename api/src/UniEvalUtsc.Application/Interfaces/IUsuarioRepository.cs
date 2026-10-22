namespace UniEvalUtsc.Application.Interfaces;

public sealed record UsuarioSistema(
    string Id,
    string Nombre,
    string Correo,
    IReadOnlyCollection<string> Roles,
    string? CarreraId = null,
    IReadOnlyCollection<int>? CargasAcademicasId = null,
    IReadOnlyCollection<int>? GruposTutorId = null);

public interface IUsuarioRepository
{
    Task<UsuarioSistema?> ObtenerAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyCollection<UsuarioSistema>> ObtenerTodosAsync(CancellationToken ct = default);
}
