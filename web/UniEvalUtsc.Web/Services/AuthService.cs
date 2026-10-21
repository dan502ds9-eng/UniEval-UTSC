using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public sealed class AuthService(
    IConfiguration configuration,
    SimulatedAuthenticationStateProvider authenticationStateProvider)
{
    public bool EntraConfigurado => string.Equals(
        configuration["Autenticacion:Modo"],
        "Entra",
        StringComparison.OrdinalIgnoreCase);

    public AuthenticatedUser? CurrentUser => authenticationStateProvider.CurrentUser;

    public async Task SignInSimuladoAsync(Rol rol)
    {
        if (EntraConfigurado)
        {
            // TODO: integrar MSAL y validar la sesión con Microsoft Entra ID.
            throw new NotSupportedException("La autenticación con Microsoft Entra ID está preparada, pero aún no implementada.");
        }

        var user = new AuthenticatedUser($"Usuario {rol}", rol);
        await authenticationStateProvider.SetUserAsync(user);
    }

    public Task SignOutAsync() => authenticationStateProvider.SetUserAsync(null);

    public static string InicioPorRol(Rol rol) => rol switch
    {
        Rol.Alumno => "/alumno",
        Rol.Docente => "/docente/inicio",
        Rol.Tutor => "/tutor/inicio",
        Rol.Coordinador => "/coordinador/inicio",
        Rol.Administrador => "/admin/inicio",
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "El rol no está soportado.")
    };

    public static string Etiqueta(Rol rol) => rol.ToString();
}

public sealed record AuthenticatedUser(string Name, Rol Role)
{
    public string Initials => string.Join(
        string.Empty,
        Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0])));
}

public sealed class SimulatedAuthenticationStateProvider : AuthenticationStateProvider
{
    private AuthenticatedUser? _currentUser;

    public AuthenticatedUser? CurrentUser => _currentUser;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(CreateAuthenticationState(_currentUser));

    public Task SetUserAsync(AuthenticatedUser? user)
    {
        _currentUser = user;
        NotifyAuthenticationStateChanged(Task.FromResult(CreateAuthenticationState(user)));
        return Task.CompletedTask;
    }

    private static AuthenticationState CreateAuthenticationState(AuthenticatedUser? user)
    {
        if (user is null)
        {
            return new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity()));
        }

        var identity = new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
        ],
        authenticationType: "Simulado");

        return new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(identity));
    }
}
