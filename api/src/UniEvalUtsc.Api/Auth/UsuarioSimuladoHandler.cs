using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using UniEvalUtsc.Application.Interfaces;

namespace UniEvalUtsc.Api.Auth;

public sealed class UsuarioSimuladoHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IUsuarioRepository usuarios)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Usuario-Simulado", out var raw)) return AuthenticateResult.NoResult();
        var usuario = await usuarios.ObtenerAsync(raw.ToString(), Context.RequestAborted);
        if (usuario is null) return AuthenticateResult.Fail("El usuario simulado no existe.");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id), new(ClaimTypes.Name, usuario.Nombre), new(ClaimTypes.Email, usuario.Correo)
        };
        claims.AddRange(usuario.Roles.Select(rol => new Claim(ClaimTypes.Role, rol)));
        if (usuario.CarreraId is not null) claims.Add(new Claim("carrera_id", usuario.CarreraId));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
