using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using UniEvalUtsc.Web;
using UniEvalUtsc.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("Falta configurar ApiBaseUrl en wwwroot/appsettings.json.");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) });
builder.Services.AddScoped<UnievalJsInterop>();
builder.Services.AddScoped<IAlumnoService, AlumnoInMemoryService>();
builder.Services.AddScoped<IResultadosService, ResultadosInMemoryService>();
builder.Services.AddScoped<ICoordinacionService, CoordinacionInMemoryService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<SimulatedAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(services =>
    services.GetRequiredService<SimulatedAuthenticationStateProvider>());
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
