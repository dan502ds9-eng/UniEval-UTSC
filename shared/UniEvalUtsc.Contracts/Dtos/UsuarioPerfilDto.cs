using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Contracts.Dtos;

public sealed record UsuarioPerfilDto(
    string Nombre,
    string CorreoInstitucional,
    IReadOnlyCollection<Rol> Roles);
