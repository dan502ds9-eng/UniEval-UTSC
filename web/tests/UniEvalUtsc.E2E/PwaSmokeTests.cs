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

        await pagina.GotoAsync(new Uri(new Uri(url), "/dev/componentes").ToString());
        var abrirDialogo = pagina.GetByRole(AriaRole.Button, new() { Name = "Mostrar diálogo" });
        await abrirDialogo.ClickAsync();
        await Assertions.Expect(pagina.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await Assertions.Expect(pagina.Locator(".dialogo__cerrar")).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("Shift+Tab");
        await Assertions.Expect(pagina.Locator(".dialogo__pie button").Last).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("Tab");
        await Assertions.Expect(pagina.Locator(".dialogo__cerrar")).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("Escape");
        await Assertions.Expect(pagina.GetByRole(AriaRole.Dialog)).ToBeHiddenAsync();
        await Assertions.Expect(abrirDialogo).ToBeFocusedAsync();
    }
}
