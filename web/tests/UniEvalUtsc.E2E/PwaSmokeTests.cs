using Microsoft.Playwright;

namespace UniEvalUtsc.E2E;

public sealed class PwaSmokeTests
{
    [RequiresRunningWeb]
    public async Task Inicio_MuestraLaAplicacionUniEval()
    {
        var url = Environment.GetEnvironmentVariable("UNIEVAL_WEB_URL")
            ?? throw new InvalidOperationException("Falta configurar UNIEVAL_WEB_URL.");
        using var playwright = await Playwright.CreateAsync();
        await using var navegador = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var pagina = await navegador.NewPageAsync();

        await pagina.GotoAsync(url);
        await Assertions.Expect(pagina).ToHaveTitleAsync("UniEval UTSC");
        await Assertions.Expect(pagina.Locator("#app main")).ToBeVisibleAsync();
        await Assertions.Expect(pagina.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
