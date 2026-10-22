using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Text.Json;
using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public sealed class AuthService(
    IConfiguration configuration,
    SimulatedAuthenticationStateProvider authenticationStateProvider,
    IJSRuntime jsRuntime,
    ILogger<AuthService> logger)
{
    private const string SessionStorageKey = "unieval-sesion-simulada";

    public bool EntraConfigurado => string.Equals(
        configuration["Autenticacion:Modo"] ?? "Entra",
        "Entra",
        StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyCollection<UsuarioDePrueba> UsuariosDePrueba(Rol? rol = null) =>
        UsuariosSemilla.Where(usuario => rol is null || usuario.Rol == rol).ToArray();

    public static string NombreDePrueba(Rol rol) => UsuariosDePrueba(rol).First().Nombre;

    public AuthenticatedUser? CurrentUser => authenticationStateProvider.CurrentUser;

    public async Task InitializeAsync()
    {
        if (EntraConfigurado)
        {
            return;
        }

        string? storedUser;
        try
        {
            storedUser = await jsRuntime.InvokeAsync<string?>("unieval.getSessionStorage", SessionStorageKey);
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            logger.LogError(exception, "No fue posible restaurar la sesión simulada desde sessionStorage.");
            return;
        }

        if (string.IsNullOrWhiteSpace(storedUser))
        {
            return;
        }

        try
        {
            var user = JsonSerializer.Deserialize<AuthenticatedUser>(storedUser);
            if (user is not null && Enum.IsDefined(user.Role))
            {
                await authenticationStateProvider.SetUserAsync(user);
            }
            else
            {
                logger.LogError("La sesión simulada almacenada contiene un rol no válido.");
            }
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "La sesión simulada almacenada no tiene un formato válido.");
        }
    }

    public Task SignInSimuladoAsync(Rol rol) => SignInSimuladoAsync(rol, UsuariosDePrueba(rol).First().Id);

    public async Task SignInSimuladoAsync(Rol rol, string idUsuario)
    {
        if (EntraConfigurado)
        {
            // TODO: integrar MSAL y validar la sesión con Microsoft Entra ID.
            throw new NotSupportedException("La autenticación con Microsoft Entra ID está preparada, pero aún no implementada.");
        }

        var usuario = UsuariosDePrueba(rol).FirstOrDefault(usuario => usuario.Id == idUsuario)
            ?? throw new ArgumentException("El usuario de prueba no pertenece al rol seleccionado.", nameof(idUsuario));

        var user = new AuthenticatedUser(usuario.Nombre, rol);
        var session = JsonSerializer.Serialize(user);
        try
        {
            var persisted = await jsRuntime.InvokeAsync<bool>("unieval.setSessionStorage", SessionStorageKey, session);
            if (!persisted)
            {
                logger.LogError("No fue posible guardar la sesión simulada; la sesión permanecerá en memoria.");
            }
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            logger.LogError(exception, "No fue posible guardar la sesión simulada; la sesión permanecerá en memoria.");
        }

        await authenticationStateProvider.SetUserAsync(user);
    }

    public async Task SignOutAsync()
    {
        try
        {
            var removed = await jsRuntime.InvokeAsync<bool>("unieval.removeSessionStorage", SessionStorageKey);
            if (!removed)
            {
                logger.LogError("No fue posible eliminar la sesión simulada de sessionStorage; se cerrará en memoria.");
            }
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            logger.LogError(exception, "No fue posible eliminar la sesión simulada; se cerrará en memoria.");
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

    private static readonly UsuarioDePrueba[] UsuariosSemilla =
    [
        new("1", "Alexis Alumno Demo", Rol.Alumno),
        new("7", "Alex Alumno Dos Demo", Rol.Alumno),
        new("2", "Sam Docente Demo", Rol.Docente),
        new("6", "Dani Docente Tutor Demo", Rol.Docente),
        new("3", "Tania Tutora Demo", Rol.Tutor),
        new("6", "Dani Docente Tutor Demo", Rol.Tutor),
        new("4", "Cris Coordinador Demo", Rol.Coordinador),
        new("5", "Ari Administrador Demo", Rol.Administrador)
    ];

    private static readonly IReadOnlyDictionary<Rol, string> EtiquetasPorRol = new Dictionary<Rol, string>
    {
        [Rol.Alumno] = "Alumno",
        [Rol.Docente] = "Docente",
        [Rol.Tutor] = "Tutor",
        [Rol.Coordinador] = "Coordinador",
        [Rol.Administrador] = "Administrador"
    };
}

public sealed record UsuarioDePrueba(string Id, string Nombre, Rol Rol);

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
