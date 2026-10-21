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
        await VerificarContrasteAcceso(pagina);

        var menuTexto = pagina.GetByRole(AriaRole.Button, new() { Name = "Aa" });
        await menuTexto.PressAsync("Enter");
        var itemEscala100 = pagina.Locator("[role='menuitemradio']").Filter(new() { HasText = "Normal 100%" });
        await Assertions.Expect(itemEscala100).ToBeFocusedAsync();
        await pagina.Keyboard.PressAsync("Escape");
        await Assertions.Expect(menuTexto).ToBeFocusedAsync();
        await menuTexto.PressAsync("ArrowDown");
        var itemEscala125 = pagina.Locator("[role='menuitemradio']").Filter(new() { HasText = "Mediano 125%" });
        var itemEscala150 = pagina.Locator("[role='menuitemradio']").Filter(new() { HasText = "Grande 150%" });
        var itemEscala200 = pagina.Locator("[role='menuitemradio']").Filter(new() { HasText = "Muy grande 200%" });
        await Assertions.Expect(itemEscala150).ToHaveAttributeAsync("aria-checked", "false");
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

        await pagina.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión con Microsoft 365" }).ClickAsync();
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/alumno$"));
        await Assertions.Expect(pagina.GetByRole(AriaRole.Button, new() { Name = "Notificaciones, 2 mensajes sin leer" })).ToBeVisibleAsync();
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
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión con Microsoft 365" }).ClickAsync();
        await Assertions.Expect(pagina.Locator(".acceso")).ToHaveCountAsync(0);
        await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Administrador" })).ToBeVisibleAsync();
        await Assertions.Expect(pagina.GetByRole(AriaRole.Link, new() { Name = "← Regresar al inicio de sesión" })).ToBeVisibleAsync();
        await Assertions.Expect(pagina.Locator(".topbar")).Not.ToHaveClassAsync(new Regex("topbar--acceso"));
        Assert.Contains("fondo-iconos", await pagina.Locator("body").EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage"));
        await pagina.GetByLabel("Correo institucional").FillAsync("admin@utsc.edu.mx");
        await pagina.GetByLabel("Correo institucional").PressAsync("Enter");
        await Assertions.Expect(pagina.GetByLabel("Contraseña")).ToBeFocusedAsync();
        await pagina.GetByLabel("Contraseña").FillAsync("simulada");
        await pagina.GetByLabel("Contraseña").PressAsync("Enter");
        await Assertions.Expect(pagina.GetByLabel("Código de verificación")).ToBeFocusedAsync();
        await pagina.GetByLabel("Código de verificación").FillAsync("123");
        await pagina.GetByLabel("Código de verificación").PressAsync("Enter");
        await Assertions.Expect(pagina.GetByRole(AriaRole.Alert)).ToHaveTextAsync("El código debe contener seis dígitos.");
        await pagina.GetByLabel("Código de verificación").FillAsync("123456");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "← Regresar" }).ClickAsync();
        await Assertions.Expect(pagina.GetByLabel("Correo institucional")).ToBeVisibleAsync();
        await pagina.GetByLabel("Contraseña").PressAsync("Enter");
        await pagina.GetByLabel("Código de verificación").FillAsync("123456");
        await pagina.GetByLabel("Código de verificación").PressAsync("Enter");
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/admin/inicio$"));
        await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Inicio" })).ToBeFocusedAsync();
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Periodos", Exact = true }).ClickAsync();
        await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Periodos", Exact = true })).ToBeFocusedAsync();
        await Assertions.Expect(pagina).ToHaveTitleAsync("Periodos · UniEval UTSC");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Aa" }).ClickAsync();
        await pagina.Locator("[role='menuitemradio']").Filter(new() { HasText = "Normal 100%" }).ClickAsync();
        Assert.Equal("sticky", await pagina.Locator(".staff-shell__sidebar").EvaluateAsync<string>("element => getComputedStyle(element).position"));
        await pagina.SetViewportSizeAsync(800, 900);
        Assert.Equal("static", await pagina.Locator(".staff-shell__sidebar").EvaluateAsync<string>("element => getComputedStyle(element).position"));
        Assert.Equal("auto", await pagina.Locator(".staff-shell__nav").EvaluateAsync<string>("element => getComputedStyle(element).overflowX"));
        await pagina.SetViewportSizeAsync(1280, 900);
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Volver al inicio" }).ClickAsync();
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/admin/inicio$"));
        await Assertions.Expect(pagina.GetByRole(AriaRole.Link, new() { Name = "Resultados de docentes" })).ToHaveAttributeAsync("href", "/admin/resultados-docentes");
        await Assertions.Expect(pagina.GetByRole(AriaRole.Link, new() { Name = "Resultados de tutores" })).ToHaveAttributeAsync("href", "/admin/resultados-tutores");

        await menuUsuario.PressAsync("ArrowDown");
        await pagina.Locator("[role='menuitem']").Filter(new() { HasText = "Cerrar sesión" }).ClickAsync();
        await pagina.Locator("#rol-acceso").SelectOptionAsync("Docente");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión con Microsoft 365" }).ClickAsync();
        await pagina.GetByLabel("Correo institucional").FillAsync("docente@utsc.edu.mx");
        await pagina.GetByLabel("Contraseña").FillAsync("simulada");
        await pagina.GetByLabel("Contraseña").PressAsync("Enter");
        await pagina.GetByLabel("Código de verificación").FillAsync("123456");
        await pagina.GetByLabel("Código de verificación").PressAsync("Enter");
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/docente/inicio$"));
        await Assertions.Expect(pagina.Locator(".staff-shell__nombre")).ToHaveTextAsync("Óscar Gabriel Chávez Gómez");
        Assert.NotEmpty(await pagina.EvaluateAsync<string>("() => sessionStorage.getItem('unieval-sesion-simulada')"));
        await pagina.ReloadAsync();
        await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/docente/inicio$"));
        await Assertions.Expect(pagina.Locator(".staff-shell__nombre")).ToHaveTextAsync("Óscar Gabriel Chávez Gómez");

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

    private static async Task VerificarContrasteAcceso(IPage pagina)
    {
        foreach (var esquema in new[] { ColorScheme.Light, ColorScheme.Dark })
        {
            await pagina.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = esquema });
            await pagina.EvaluateAsync("() => localStorage.removeItem('unieval-theme')");
            await pagina.ReloadAsync();
            await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync(
                "data-theme",
                esquema == ColorScheme.Dark ? "dark" : "light");

            foreach (var ancho in new[] { 390, 768, 1280, 1920 })
            {
                await pagina.SetViewportSizeAsync(ancho, 900);
                var contraste = await pagina.EvaluateAsync<double[]>("""
                    () => {
                        const main = document.querySelector(".acceso");
                        const contenido = main.querySelector(".acceso__contenido");
                        const style = getComputedStyle(main);
                        const oscuro = document.documentElement.dataset.theme === "dark";
                        const rgb = style.getPropertyValue("--acceso-overlay-rgb").trim().split(/\s+/).map(Number);
                        const right = (contenido.getBoundingClientRect().right / window.innerWidth);
                        const stops = [
                            [0, parseFloat(style.getPropertyValue("--acceso-overlay-0"))],
                            [0.48, parseFloat(style.getPropertyValue("--acceso-overlay-48"))],
                            [0.82, parseFloat(style.getPropertyValue("--acceso-overlay-82"))],
                            [1, parseFloat(style.getPropertyValue("--acceso-overlay-100"))]
                        ];
                        let alpha = stops[stops.length - 1][1];
                        for (let index = 1; index < stops.length; index++) {
                            if (right <= stops[index][0]) {
                                const [start, startAlpha] = stops[index - 1];
                                const [end, endAlpha] = stops[index];
                                alpha = startAlpha + (endAlpha - startAlpha) * (right - start) / (end - start);
                                break;
                            }
                        }
                        const channels = rgb.map(channel => channel * alpha + 255 * (1 - alpha));
                        const linear = channels.map(channel => {
                            const value = channel / 255;
                            return value <= 0.04045 ? value / 12.92 : Math.pow((value + 0.055) / 1.055, 2.4);
                        });
                        const luminance = 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
                        const ratio = 1.05 / (luminance + 0.05);
                        const textoBlanco = getComputedStyle(main).color === "rgb(255, 255, 255)";
                        const tituloBlanco = getComputedStyle(main.querySelector("h1")).color === "rgb(255, 255, 255)";
                        return [
                            ratio,
                            ratio,
                            style.backgroundImage.includes("campus.jpg") ? 1 : 0,
                            parseFloat(style.getPropertyValue("--acceso-overlay-100")),
                            textoBlanco ? 1 : 0,
                            tituloBlanco ? 1 : 0
                        ];
                    }
                    """);
                Assert.Equal(1, contraste[2]);
                if (ancho > 900)
                {
                    Assert.True(contraste[3] < 0.5, $"La foto debe conservarse visible a la derecha en {ancho}px, {esquema}.");
                }
                Assert.Equal(1, contraste[4]);
                Assert.Equal(1, contraste[5]);
                Assert.True(contraste[0] >= 4.5, $"Contraste del texto normal menor a 4.5:1 en {ancho}px, {esquema}: {contraste[0]:F2}:1.");
                Assert.True(contraste[1] >= 3, $"Contraste del título menor a 3:1 en {ancho}px, {esquema}.");
            }
        }

        await pagina.SetViewportSizeAsync(1280, 900);
    }
}
