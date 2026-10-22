using System.Text.Json;
using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.E2E;

public sealed class AccesibilidadTests
{
    private static readonly IReadOnlyDictionary<Rol, IReadOnlyList<string>> RutasPorRol =
        new Dictionary<Rol, IReadOnlyList<string>>
        {
            [Rol.Alumno] = ["/alumno", "/alumno/encuesta"],
            [Rol.Docente] = ["/docente/inicio", "/docente/resultados", "/docente/historico", "/docente/reportes"],
            [Rol.Tutor] = ["/tutor/inicio", "/tutor/resultados", "/tutor/historico", "/tutor/reportes"],
            [Rol.Coordinador] =
            [
                "/coordinador/inicio",
                "/coordinador/seguimiento",
                "/coordinador/resultados-docentes",
                "/coordinador/resultados-tutores",
                "/coordinador/reportes",
                "/coordinador/detalle/docente/doc-1",
                "/coordinador/detalle/tutor/tutor-5"
            ],
            [Rol.Administrador] =
            [
                "/admin/inicio",
                "/admin/periodos",
                "/admin/carga-academica",
                "/admin/estadias",
                "/admin/catalogos",
                "/admin/plantillas",
                "/admin/seguimiento",
                "/admin/resultados-docentes",
                "/admin/resultados-tutores",
                "/admin/reportes",
                "/admin/detalle/docente/doc-1",
                "/admin/detalle/tutor/tutor-5"
            ]
        };

    private static readonly ColorScheme[] Temas = [ColorScheme.Light, ColorScheme.Dark];
    private static readonly int[] Anchos = [390, 1280];
    private static readonly string[] EtiquetasAxe = ["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"];

    [RequiresRunningWeb]
    public async Task TodasLasPantallas_DeCincoRoles_NoTienenViolacionesAxe()
    {
        var url = ObtenerUrl();
        using var playwright = await Playwright.CreateAsync();
        await using var navegador = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var fallas = new List<string>();

        foreach (var (rol, rutas) in RutasPorRol)
        {
            var contexto = await CrearContextoAsync(navegador, rol);
            var traza = await PlaywrightTrace.StartAsync(contexto, $"axe-{rol}");
            var pagina = await contexto.NewPageAsync();
            try
            {
                foreach (var ruta in rutas)
                {
                    await NavegarYPonerFocoEnH1Async(pagina, url, ruta);
                    foreach (var tema in Temas)
                    {
                        foreach (var ancho in Anchos)
                        {
                            await pagina.SetViewportSizeAsync(ancho, 900);
                            await AplicarTemaAsync(pagina, tema);
                            await DesactivarTransicionesAsync(pagina);
                            var resultado = await pagina.RunAxe(new AxeRunOptions
                            {
                                RunOnly = new RunOnlyOptions
                                {
                                    Type = "tag",
                                    Values = EtiquetasAxe.ToList()
                                }
                            });

                            foreach (var violacion in resultado.Violations ?? [])
                            {
                                var nodos = string.Join(
                                    "; ",
                                    violacion.Nodes.Select(nodo =>
                                        $"{string.Join(", ", nodo.Target)}: {nodo.Any?.FirstOrDefault()?.Message ?? nodo.Html}"));
                                fallas.Add(
                                    $"{ruta} ({rol}, {tema}, {ancho}px): {violacion.Id} — {violacion.Help}. {nodos}");
                            }
                        }
                    }
                }

                if (rol == Rol.Alumno)
                {
                    await CompletarEncuestaConTecladoAsync(pagina, url);
                    await EscanearRutaAsync(pagina, url, "/alumno/comprobante", fallas);
                }
            }
            finally
            {
                await PlaywrightTrace.StopAsync(contexto, traza);
                await contexto.CloseAsync();
            }
        }

        Assert.True(fallas.Count == 0, "Violaciones de axe:\n" + string.Join("\n", fallas));
    }

    [RequiresRunningWeb]
    public async Task TextoAlDoscientosPorCiento_NoTieneDesbordamientoNiTextoRecortado()
    {
        var url = ObtenerUrl();
        using var playwright = await Playwright.CreateAsync();
        await using var navegador = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var fallas = new List<string>();

        foreach (var (rol, rutas) in RutasPorRol)
        {
            var contexto = await CrearContextoAsync(navegador, rol);
            var traza = await PlaywrightTrace.StartAsync(contexto, $"zoom-{rol}");
            var pagina = await contexto.NewPageAsync();
            try
            {
                foreach (var ruta in rutas)
                {
                    await NavegarYPonerFocoEnH1Async(pagina, url, ruta);
                    await pagina.EvaluateAsync("""
                        () => {
                            document.documentElement.dataset.ts = "200";
                            document.documentElement.style.fontSize = "200%";
                        }
                        """);

                    foreach (var ancho in Anchos)
                    {
                        await pagina.SetViewportSizeAsync(ancho, 900);
                        var desbordamientos = await pagina.EvaluateAsync<string[]>("""
                            () => {
                                const anchoViewport = document.documentElement.clientWidth;
                                const elementos = Array.from(document.querySelectorAll("body *"));
                                const desbordados = elementos
                                    .map(elemento => ({ elemento, limites: elemento.getBoundingClientRect() }))
                                    .filter(({ elemento, limites }) =>
                                        limites.width > 0 &&
                                        limites.right > anchoViewport + 1 &&
                                        getComputedStyle(elemento).position !== "absolute")
                                    .sort((a, b) => b.limites.right - a.limites.right)
                                    .slice(0, 6)
                                    .map(({ elemento, limites }) =>
                                        `${elemento.tagName.toLowerCase()}#${elemento.id}.${String(elemento.className).replaceAll(" ", ".")} right=${Math.round(limites.right)} width=${Math.round(limites.width)}`);
                                const recortes = elementos
                                    .filter(elemento => {
                                        if (!elemento.textContent.trim() ||
                                            elemento.classList.contains("sr-only") ||
                                            !elemento.getClientRects().length) return false;
                                        const estilo = getComputedStyle(elemento);
                                        const recortaHorizontal =
                                            ["hidden", "clip"].includes(estilo.overflowX) &&
                                            elemento.scrollWidth > elemento.clientWidth + 1;
                                        const recortaVertical =
                                            ["hidden", "clip"].includes(estilo.overflowY) &&
                                            elemento.scrollHeight > elemento.clientHeight + 1;
                                        const elipsis =
                                            estilo.textOverflow === "ellipsis" &&
                                            elemento.scrollWidth > elemento.clientWidth + 1;
                                        return recortaHorizontal || recortaVertical || elipsis;
                                    })
                                    .map(elemento => `${elemento.tagName.toLowerCase()}.${String(elemento.className).replaceAll(" ", ".")}`);
                                return [
                                    ...(document.documentElement.scrollWidth > anchoViewport
                                        ? [`documento ${document.documentElement.scrollWidth}px > ${anchoViewport}px; elementos: ${desbordados.join(", ")}`]
                                        : []),
                                    ...recortes
                                ];
                            }
                            """);

                        fallas.AddRange(desbordamientos.Select(error => $"{ruta} ({rol}, {ancho}px): {error}"));
                    }
                }

                if (rol == Rol.Alumno)
                {
                    await CompletarEncuestaConTecladoAsync(pagina, url);
                    await pagina.Locator("#boton-enviar-evaluacion").ClickAsync();
                    await pagina.GetByRole(AriaRole.Dialog)
                        .GetByRole(AriaRole.Button, new() { Name = "Confirmar envío" })
                        .ClickAsync();
                    await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/alumno/comprobante$"));
                    await pagina.EvaluateAsync("""
                        () => {
                            document.documentElement.dataset.ts = "200";
                            document.documentElement.style.fontSize = "200%";
                        }
                        """);
                    foreach (var ancho in Anchos)
                    {
                        await pagina.SetViewportSizeAsync(ancho, 900);
                        var scrollWidth = await pagina.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
                        var clientWidth = await pagina.EvaluateAsync<int>("() => document.documentElement.clientWidth");
                        if (scrollWidth > clientWidth)
                        {
                            var elementos = await pagina.EvaluateAsync<string>("""
                                () => Array.from(document.querySelectorAll("body *"))
                                    .map(elemento => ({ elemento, limites: elemento.getBoundingClientRect() }))
                                    .filter(({ elemento, limites }) =>
                                        limites.width > 0 &&
                                        limites.right > document.documentElement.clientWidth + 1 &&
                                        getComputedStyle(elemento).position !== "absolute")
                                    .sort((a, b) => b.limites.right - a.limites.right)
                                    .slice(0, 6)
                                    .map(({ elemento, limites }) =>
                                        `${elemento.tagName.toLowerCase()}#${elemento.id}.${String(elemento.className).replaceAll(" ", ".")} right=${Math.round(limites.right)} width=${Math.round(limites.width)}`)
                                    .join(", ")
                                """);
                            fallas.Add($"/alumno/comprobante (Alumno, {ancho}px): documento {scrollWidth}px > {clientWidth}px; elementos: {elementos}");
                        }
                    }
                }
            }
            finally
            {
                await PlaywrightTrace.StopAsync(contexto, traza);
                await contexto.CloseAsync();
            }
        }

        Assert.True(fallas.Count == 0, "Desbordamientos o recortes:\n" + string.Join("\n", fallas));
    }

    [RequiresRunningWeb]
    public async Task FlujoDeAlumno_SoloConTeclado_ContestaEnvíaYCierraPanelesConEscape()
    {
        var url = ObtenerUrl();
        using var playwright = await Playwright.CreateAsync();
        await using var navegador = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var contexto = await navegador.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            ReducedMotion = ReducedMotion.Reduce
        });
        var traza = await PlaywrightTrace.StartAsync(contexto, "flujo-alumno-teclado");
        var pagina = await contexto.NewPageAsync();
        try
        {
            await pagina.GotoAsync(url);
            await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Tu opinión mejora la UTSC" })).ToBeFocusedAsync();

            var iniciarSesion = pagina.GetByRole(AriaRole.Button, new() { Name = "Entrar (modo demo)" });
            await EnfocarConTabAsync(pagina, iniciarSesion);
            await pagina.Keyboard.PressAsync("Enter");
            await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/alumno$"));
            await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Mis encuestas" })).ToBeFocusedAsync();

            var notificaciones = pagina.Locator(".topbar__notificaciones");
            await EnfocarConTabAsync(pagina, notificaciones);
            await pagina.Keyboard.PressAsync("Enter");
            await Assertions.Expect(pagina.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
            await pagina.Keyboard.PressAsync("Escape");
            await Assertions.Expect(pagina.GetByRole(AriaRole.Dialog)).ToBeHiddenAsync();
            await Assertions.Expect(notificaciones).ToBeFocusedAsync();

            var asistente = pagina.GetByRole(AriaRole.Button, new() { Name = "Abrir asistente Correcaminos IA" });
            await EnfocarConTabAsync(pagina, asistente);
            await pagina.Keyboard.PressAsync("Enter");
            await Assertions.Expect(pagina.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
            await pagina.Keyboard.PressAsync("Escape");
            await Assertions.Expect(pagina.GetByRole(AriaRole.Dialog)).ToBeHiddenAsync();
            await Assertions.Expect(asistente).ToBeFocusedAsync();

            var contestar = pagina.GetByRole(AriaRole.Button, new() { Name = "Contestar evaluación" });
            await EnfocarConTabAsync(pagina, contestar);
            await pagina.Keyboard.PressAsync("Enter");
            await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/alumno/encuesta$"));
            await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Evaluación del cuatrimestre" })).ToBeFocusedAsync();

            await CompletarEncuestaConTecladoAsync(pagina, url);
            var enviar = pagina.Locator("#boton-enviar-evaluacion");
            await Assertions.Expect(enviar).ToBeFocusedAsync();
            await pagina.Keyboard.PressAsync("Enter");

            var dialogo = pagina.GetByRole(AriaRole.Dialog);
            await Assertions.Expect(dialogo).ToBeVisibleAsync();
            var cerrarDialogo = dialogo.GetByRole(AriaRole.Button, new() { Name = "Cerrar diálogo" });
            await Assertions.Expect(cerrarDialogo).ToBeFocusedAsync();
            await pagina.Keyboard.PressAsync("Shift+Tab");
            await Assertions.Expect(dialogo.GetByRole(AriaRole.Button, new() { Name = "Confirmar envío" })).ToBeFocusedAsync();
            await pagina.Keyboard.PressAsync("Tab");
            await Assertions.Expect(cerrarDialogo).ToBeFocusedAsync();
            await pagina.Keyboard.PressAsync("Escape");
            await Assertions.Expect(dialogo).ToBeHiddenAsync();
            await Assertions.Expect(enviar).ToBeFocusedAsync();

            await pagina.Keyboard.PressAsync("Enter");
            await Assertions.Expect(cerrarDialogo).ToBeFocusedAsync();
            await pagina.Keyboard.PressAsync("Tab");
            await pagina.Keyboard.PressAsync("Tab");
            await Assertions.Expect(dialogo.GetByRole(AriaRole.Button, new() { Name = "Confirmar envío" })).ToBeFocusedAsync();
            await pagina.Keyboard.PressAsync("Enter");
            await Assertions.Expect(pagina).ToHaveURLAsync(new Regex(".*/alumno/comprobante$"));
            await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "¡Gracias!" })).ToBeFocusedAsync();
            await Assertions.Expect(pagina).ToHaveTitleAsync("¡Gracias! · UniEval UTSC");
        }
        finally
        {
            await PlaywrightTrace.StopAsync(contexto, traza);
            await contexto.CloseAsync();
        }
    }

    private static async Task CompletarEncuestaConTecladoAsync(IPage pagina, string url)
    {
        if (!pagina.Url.EndsWith("/alumno/encuesta", StringComparison.Ordinal))
        {
            await pagina.GotoAsync(new Uri(new Uri(url), "/alumno/encuesta").ToString());
        }

        var primeraCalificacion = pagina.Locator(".alumno-calificacion input[type='radio']").First;
        await EnfocarConTabAsync(pagina, primeraCalificacion, 100);

        for (var respuesta = 1; respuesta <= 80; respuesta++)
        {
            await pagina.Keyboard.PressAsync("8");
            await Assertions.Expect(pagina.Locator(".alumno-avance__encabezado"))
                .ToContainTextAsync($"{respuesta} de 80 respondidas");
        }
    }

    private static async Task<IPage> NavegarYPonerFocoEnH1Async(IPage pagina, string url, string ruta)
    {
        await pagina.GotoAsync(new Uri(new Uri(url), ruta).ToString());
        var encabezado = pagina.GetByRole(AriaRole.Heading, new() { Level = 1 });
        await Assertions.Expect(encabezado).ToBeVisibleAsync();
        await Assertions.Expect(encabezado).ToBeFocusedAsync();
        await Assertions.Expect(pagina).ToHaveTitleAsync(new Regex(" · UniEval UTSC$"));
        return pagina;
    }

    private static async Task EscanearRutaAsync(IPage pagina, string url, string ruta, ICollection<string> fallas)
    {
        await NavegarYPonerFocoEnH1Async(pagina, url, ruta);
        foreach (var tema in Temas)
        {
            foreach (var ancho in Anchos)
            {
                await pagina.SetViewportSizeAsync(ancho, 900);
                await AplicarTemaAsync(pagina, tema);
                await DesactivarTransicionesAsync(pagina);
                var resultado = await pagina.RunAxe(new AxeRunOptions
                {
                    RunOnly = new RunOnlyOptions { Type = "tag", Values = EtiquetasAxe.ToList() }
                });
                foreach (var violacion in resultado.Violations ?? [])
                {
                    fallas.Add($"{ruta} (Alumno, {tema}, {ancho}px): {violacion.Id} — {violacion.Help}");
                }
            }
        }
    }

    private static async Task<IBrowserContext> CrearContextoAsync(IBrowser navegador, Rol rol)
    {
        var contexto = await navegador.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            ReducedMotion = ReducedMotion.Reduce
        });
        var usuario = JsonSerializer.Serialize(new { Name = $"Prueba {rol}", Role = (int)rol });
        await contexto.AddInitScriptAsync(
            $"sessionStorage.setItem('unieval-sesion-simulada', {JsonSerializer.Serialize(usuario)});");
        return contexto;
    }

    private static async Task AplicarTemaAsync(IPage pagina, ColorScheme tema)
    {
        var valor = tema == ColorScheme.Dark ? "dark" : "light";
        await pagina.EvaluateAsync(
            "tema => { document.documentElement.dataset.theme = tema; localStorage.setItem('unieval-theme', tema); }",
            valor);
        await pagina.WaitForFunctionAsync("tema => document.documentElement.dataset.theme === tema", valor);
    }

    private static async Task DesactivarTransicionesAsync(IPage pagina)
    {
        await pagina.EvaluateAsync("""
            () => {
                if (document.getElementById("axe-disable-transitions")) return;
                const style = document.createElement("style");
                style.id = "axe-disable-transitions";
                style.textContent = "* { transition: none !important; animation: none !important; }";
                document.head.appendChild(style);
            }
            """);
        await pagina.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
    }

    private static async Task EnfocarConTabAsync(IPage pagina, ILocator elemento, int maximoTabs = 60)
    {
        var estados = new List<string>();
        for (var intento = 0; intento < maximoTabs; intento++)
        {
            if (await elemento.EvaluateAsync<bool>("elemento => elemento === document.activeElement"))
            {
                return;
            }

            await pagina.Keyboard.PressAsync("Tab");
            if (intento < 8)
            {
                estados.Add(await pagina.EvaluateAsync<string>(
                    "() => `${document.activeElement?.tagName}#${document.activeElement?.id}.${document.activeElement?.className}`"));
            }
        }

        Assert.True(
            await elemento.EvaluateAsync<bool>("elemento => elemento === document.activeElement"),
            $"No fue posible enfocar con Tab: {await elemento.GetAttributeAsync("aria-label") ?? await elemento.InnerTextAsync()}. " +
            $"Foco actual: {await pagina.EvaluateAsync<string>("() => `${document.activeElement?.tagName}#${document.activeElement?.id}.${document.activeElement?.className} ${document.activeElement?.getAttribute('aria-label')}`")}. " +
            $"Secuencia: {string.Join(" -> ", estados)}.");
    }

    private static string ObtenerUrl() =>
        Environment.GetEnvironmentVariable("UNIEVAL_WEB_URL")
        ?? throw new InvalidOperationException("Falta configurar UNIEVAL_WEB_URL.");
}
