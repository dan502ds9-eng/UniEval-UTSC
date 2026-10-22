using UniEvalUtsc.Application.Interfaces;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioUsuarioInMemory : IUsuarioRepository
{
    private static readonly IReadOnlyDictionary<string, UsuarioSistema> Usuarios = new Dictionary<string, UsuarioSistema>
    {
        ["1"] = new("1", "Alexis Alumno Demo", "alumno@utsc.edu.mx", ["Alumno"]),
        ["2"] = new("2", "Sam Docente Demo", "docente@utsc.edu.mx", ["Docente"], "dsm", [101, 1]),
        ["3"] = new("3", "Tania Tutora Demo", "tutora@utsc.edu.mx", ["Tutor"], "dsm", null, [1, 6]),
        ["4"] = new("4", "Cris Coordinador Demo", "coordinacion@utsc.edu.mx", ["Coordinador"], "dsm"),
        ["5"] = new("5", "Ari Administrador Demo", "admin@utsc.edu.mx", ["Administrador"]),
        ["6"] = new("6", "Dani Docente Tutor Demo", "docente-tutor@utsc.edu.mx", ["Docente", "Tutor"], "dsm", [102], [6]),
        ["7"] = new("7", "Alex Alumno Dos Demo", "alumno2@utsc.edu.mx", ["Alumno"]),
        ["8"] = new("8", "Beatriz Alumna Demo", "beatriz@utsc.edu.mx", ["Alumno"])
    };

    public Task<UsuarioSistema?> ObtenerAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Usuarios.TryGetValue(id, out var usuario);
        return Task.FromResult(usuario);
    }

    public Task<IReadOnlyCollection<UsuarioSistema>> ObtenerTodosAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<UsuarioSistema>>(Usuarios.Values.ToArray());
    }
}
