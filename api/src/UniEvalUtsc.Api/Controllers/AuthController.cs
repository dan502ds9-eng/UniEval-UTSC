using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Contracts.Dtos;
using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController(IUsuarioRepository usuarios) : ControllerBase
{
    [HttpGet("perfil")]
    [ProducesResponseType<UsuarioPerfilDto>(200)]
    public async Task<ActionResult<UsuarioPerfilDto>> Perfil(CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        if (usuario is null) return Unauthorized();
        var roles = usuario.Roles.Select(rol => Enum.Parse<Rol>(rol)).ToArray();
        return Ok(new UsuarioPerfilDto(usuario.Nombre, usuario.Correo, roles));
    }
}
