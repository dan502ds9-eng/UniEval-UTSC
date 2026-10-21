using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace UniEvalUtsc.Api.Auth;

public sealed class UsuarioSimuladoHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Usuario-Simulado", out var raw) || !int.TryParse(raw, out var id) || id is not (1 or 2))
            return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Name, id == 1 ? "Alexis Alumno Demo" : "Sam Alumno Demo"), new Claim(ClaimTypes.Email, id == 1 ? "alumno@utsc.edu.mx" : "alumno2@utsc.edu.mx"), new Claim(ClaimTypes.Role, "Alumno") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
