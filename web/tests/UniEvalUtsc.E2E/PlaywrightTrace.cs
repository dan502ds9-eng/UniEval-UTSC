using Microsoft.Playwright;

namespace UniEvalUtsc.E2E;

internal static class PlaywrightTrace
{
    private static readonly string Directorio = Path.Combine(
        AppContext.BaseDirectory,
        "PlaywrightTraces");

    public static async Task<string> StartAsync(IBrowserContext contexto, string nombre)
    {
        Directory.CreateDirectory(Directorio);
        var ruta = Path.Combine(Directorio, $"{nombre}-{Guid.NewGuid():N}.zip");
        await contexto.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = false,
            Sources = false
        });
        return ruta;
    }

    public static Task StopAsync(IBrowserContext contexto, string ruta) =>
        contexto.Tracing.StopAsync(new TracingStopOptions { Path = ruta });
}
