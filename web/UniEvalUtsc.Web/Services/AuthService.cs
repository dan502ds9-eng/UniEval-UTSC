using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.Text.Json;
using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public sealed class AuthService(
    IConfiguration configuration,
    SimulatedAuthenticationStateProvider authenticationStateProvider,
    IJSRuntime jsRuntime)
{
    private const string SessionStorageKey = "unieval-sesion-simulada";

    public bool EntraConfigurado => string.Equals(
        configuration["Autenticacion:Modo"],
        "Entra",
        StringComparison.OrdinalIgnoreCase);

    public AuthenticatedUser? CurrentUser => authenticationStateProvider.CurrentUser;

    public async Task InitializeAsync()
    {
        if (EntraConfigurado)
        {
            return;
        }

        var storedUser = await jsRuntime.InvokeAsync<string?>("unieval.getSessionStorage", SessionStorageKey);
        if (string.IsNullOrWhiteSpace(storedUser))
        {
            return;
        }

        var user = JsonSerializer.Deserialize<AuthenticatedUser>(storedUser);
        if (user is not null && Enum.IsDefined(user.Role))
        {
            await authenticationStateProvider.SetUserAsync(user);
        }
    }

    public async Task SignInSimuladoAsync(Rol rol)
    {
        if (EntraConfigurado)
        {
            // TODO: integrar MSAL y validar la sesión con Microsoft Entra ID.
            throw new NotSupportedException("La autenticación con Microsoft Entra ID está preparada, pero aún no implementada.");
        }

        var user = new AuthenticatedUser(NombresPorRol[rol], rol);
        var session = JsonSerializer.Serialize(user);
        var persisted = await jsRuntime.InvokeAsync<bool>("unieval.setSessionStorage", SessionStorageKey, session);
        if (!persisted)
        {
            throw new InvalidOperationException("No fue posible guardar la sesión simulada en sessionStorage.");
        }

        await authenticationStateProvider.SetUserAsync(user);
    }

    public async Task SignOutAsync()
    {
        var removed = await jsRuntime.InvokeAsync<bool>("unieval.removeSessionStorage", SessionStorageKey);
        if (!removed)
        {
            throw new InvalidOperationException("No fue posible eliminar la sesión simulada de sessionStorage.");
        }

        await authenticationStateProvider.SetUserAsync(null);
    }

    public static string InicioPorRol(Rol rol) => rol switch
    {
        Rol.Alumno => "/alumno",
        Rol.Docente => "/docente/inicio",
        Rol.Tutor => "/tutor/inicio",
        Rol.Coordinador => "/coordinador/inicio",
        Rol.Administrador => "/admin/inicio",
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "El rol no está soportado.")
    };

    public static string Etiqueta(Rol rol) => EtiquetasPorRol.TryGetValue(rol, out var etiqueta)
        ? etiqueta
        : throw new ArgumentOutOfRangeException(nameof(rol), rol, "El rol no está soportado.");

    private static readonly IReadOnlyDictionary<Rol, string> NombresPorRol = new Dictionary<Rol, string>
    {
        [Rol.Alumno] = "[Nombre del alumno]",
        [Rol.Docente] = "Óscar Gabriel Chávez Gómez",
        [Rol.Tutor] = "Mónica Janeth Rodríguez Reyna",
        [Rol.Coordinador] = "[Coordinación DSM]",
        [Rol.Administrador] = "[Administrador del sistema]"
    };

    private static readonly IReadOnlyDictionary<Rol, string> EtiquetasPorRol = new Dictionary<Rol, string>
    {
        [Rol.Alumno] = "Alumno",
        [Rol.Docente] = "Docente",
        [Rol.Tutor] = "Tutor",
        [Rol.Coordinador] = "Coordinador",
        [Rol.Administrador] = "Administrador"
    };
}

public sealed record AuthenticatedUser(string Name, Rol Role)
{
    public string Initials => Role switch
    {
        Rol.Alumno => "AL",
        Rol.Docente => "ÓC",
        Rol.Tutor => "MR",
        Rol.Coordinador => "CO",
        Rol.Administrador => "AD",
        _ => throw new ArgumentOutOfRangeException(nameof(Role), Role, "El rol no está soportado.")
    };
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
