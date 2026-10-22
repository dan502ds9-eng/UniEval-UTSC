using UniEvalUtsc.Application.Interfaces;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioUsuarioInMemory : IUsuarioRepository
{
    private static readonly IReadOnlyDictionary<string, UsuarioSistema> Usuarios = new Dictionary<string, UsuarioSistema>
    {
        ["1"] = new("1", "[Nombre del alumno]", "alumno@utsc.edu.mx", ["Alumno"]),
        ["2"] = new("2", "Óscar Gabriel Chávez Gómez", "docente@utsc.edu.mx", ["Docente"], "dsm", [101, 1]),
        ["3"] = new("3", "Mónica Janeth Rodríguez Reyna", "tutora@utsc.edu.mx", ["Tutor"], "dsm", null, [1, 6]),
        ["4"] = new("4", "[Coordinación DSM]", "coordinacion@utsc.edu.mx", ["Coordinador"], "dsm"),
        ["5"] = new("5", "[Administrador del sistema]", "admin@utsc.edu.mx", ["Administrador"]),
        ["6"] = new("6", "Sem Tomás Hernández Martínez", "docente-tutor@utsc.edu.mx", ["Docente", "Tutor"], "dsm", [102], [6]),
        ["7"] = new("7", "[Nombre del alumno]", "alumno2@utsc.edu.mx", ["Alumno"]),
        ["8"] = new("8", "[Nombre del alumno]", "beatriz@utsc.edu.mx", ["Alumno"])
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
