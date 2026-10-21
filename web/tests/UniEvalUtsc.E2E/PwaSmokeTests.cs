using Microsoft.Playwright;
using System.Text.RegularExpressions;

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

        await pagina.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await pagina.GotoAsync(url);
        await Assertions.Expect(pagina).ToHaveTitleAsync("Tu opinión mejora la UTSC · UniEval UTSC");
        await Assertions.Expect(pagina.Locator("#app main")).ToBeVisibleAsync();
        await Assertions.Expect(pagina.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        await Assertions.Expect(pagina.Locator(".topbar__usuario")).ToHaveCountAsync(0);
        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");

        var menuTexto = pagina.GetByRole(AriaRole.Button, new() { Name = "Aa" });
        await menuTexto.PressAsync("Enter");
        var itemEscala100 = pagina.Locator("[role='menuitem']").Filter(new() { HasText = "100%" });
        await Assertions.Expect(itemEscala100).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menuTexto).ToBeFocusedAsync();
        await menuTexto.PressAsync("ArrowDown");
        var itemEscala125 = pagina.Locator("[role='menuitem']").Filter(new() { HasText = "125%" });
        var itemEscala150 = pagina.Locator("[role='menuitem']").Filter(new() { HasText = "150%" });
        var itemEscala200 = pagina.Locator("[role='menuitem']").Filter(new() { HasText = "200%" });
        await Assertions.Expect(itemEscala100).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("End");
        await Assertions.Expect(itemEscala200).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("Home");
        await Assertions.Expect(itemEscala100).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(itemEscala125).ToBeFocusedAsync();
        await itemEscala150.ClickAsync();
        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-ts", "150");
        Assert.Equal("150%", await pagina.EvaluateAsync<string>("() => document.documentElement.style.fontSize"));
        Assert.Equal("150", await pagina.EvaluateAsync<string>("() => localStorage.getItem('unieval-text-scale')"));

        await pagina.GetByRole(AriaRole.Button, new() { Name = "Cambiar a tema claro" }).ClickAsync();
        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-theme", "light");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Cambiar a tema oscuro" }).ClickAsync();
        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
        Assert.Equal("dark", await pagina.EvaluateAsync<string>("() => localStorage.getItem('unieval-theme')"));

        await pagina.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión" }).ClickAsync();
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/alumno$"));
        await Assertions.Expect(pagina.GetByRole(AriaRole.Button, new() { Name = "Notificaciones, 0 pendientes" })).ToBeVisibleAsync();
        await pagina.EvaluateAsync("() => { const enlace = document.createElement('a'); enlace.href = '/admin/periodos'; document.body.appendChild(enlace); enlace.click(); }");
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/alumno$"));
        var menuUsuario = pagina.Locator(".topbar__usuario");
        await menuUsuario.PressAsync("ArrowDown");
        var cerrarSesion = pagina.Locator("[role='menuitem']").Filter(new() { HasText = "Cerrar sesión" });
        await Assertions.Expect(cerrarSesion).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menuUsuario).ToBeFocusedAsync();
        await menuUsuario.ClickAsync();
        await cerrarSesion.ClickAsync();
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/$"));

        await pagina.GotoAsync(new Uri(new Uri(url), "/admin/periodos").ToString());
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/$"));
        await pagina.Locator("#rol-acceso").SelectOptionAsync("Administrador");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión" }).ClickAsync();
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/admin/inicio$"));
        await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Inicio" })).ToBeFocusedAsync();
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Periodos" }).ClickAsync();
        await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Periodos" })).ToBeFocusedAsync();
        await Assertions.Expect(pagina).ToHaveTitleAsync("Periodos · UniEval UTSC");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Aa" }).ClickAsync();
        await pagina.Locator("[role='menuitem']").Filter(new() { HasText = "100%" }).ClickAsync();
        Assert.Equal("sticky", await pagina.Locator(".staff-shell__sidebar").EvaluateAsync<string>("element => getComputedStyle(element).position"));
        await pagina.SetViewportSizeAsync(800, 900);
        Assert.Equal("static", await pagina.Locator(".staff-shell__sidebar").EvaluateAsync<string>("element => getComputedStyle(element).position"));
        Assert.Equal("auto", await pagina.Locator(".staff-shell__nav").EvaluateAsync<string>("element => getComputedStyle(element).overflowX"));
        await pagina.SetViewportSizeAsync(1280, 900);
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Volver al inicio" }).ClickAsync();
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/admin/inicio$"));

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
