namespace UniEvalUtsc.Application.Interfaces;

public sealed record UsuarioSistema(string Id, string Nombre, string Correo, IReadOnlyCollection<string> Roles);

public interface IUsuarioRepository
{
    Task<UsuarioSistema?> ObtenerAsync(string id, CancellationToken ct = default);
}
