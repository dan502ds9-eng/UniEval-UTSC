using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using UniEvalUtsc.Contracts.Enums;
using UniEvalUtsc.Web.Components.Ui;
using UniEvalUtsc.Web.Services;

namespace UniEvalUtsc.Web.Tests.Components.Ui;

public sealed class AsistenteFaqTests
{
    [Fact]
    public async Task AsistenteFaq_AbrePreguntasFrecuentesYRespondeEnLaConversacion()
    {
        using var contexto = new BunitContext();
        contexto.Services.AddSingleton<IFaqService, FaqInMemoryService>();
        var proveedorAutenticacion = new SimulatedAuthenticationStateProvider();
        contexto.Services.AddSingleton<AuthenticationStateProvider>(proveedorAutenticacion);
        contexto.JSInterop.SetupVoid("unieval.trapFocus", _ => true).SetVoidResult();
        contexto.JSInterop.SetupVoid("unieval.focusById", _ => true).SetVoidResult();
        contexto.JSInterop.SetupVoid("unieval.scrollToEnd", _ => true).SetVoidResult();
        contexto.JSInterop.SetupVoid("unieval.releaseFocus", _ => true).SetVoidResult();
        var asistente = contexto.Render<AsistenteFaq>();

        var boton = asistente.Find("button.asistente-faq__lanzador");
        Assert.Equal("false", boton.GetAttribute("aria-expanded"));
        Assert.Equal("Abrir asistente Correcaminos IA", boton.GetAttribute("aria-label"));

        await boton.ClickAsync();
        asistente.WaitForAssertion(() =>
        {
            Assert.Equal("true", asistente.Find("button.asistente-faq__lanzador").GetAttribute("aria-expanded"));
            Assert.Equal("dialog", asistente.Find("[role='dialog']").GetAttribute("role"));
            Assert.Equal("log", asistente.Find("[role='log']").GetAttribute("role"));
            Assert.Contains(asistente.FindAll(".asistente-faq__preguntas button"),
                pregunta => pregunta.TextContent.Contains("¿Son anónimas?", StringComparison.Ordinal));
        });

        await asistente.FindAll(".asistente-faq__preguntas button")
            .Single(pregunta => pregunta.TextContent.Contains("¿Son anónimas?", StringComparison.Ordinal))
            .ClickAsync();

        asistente.WaitForAssertion(() =>
            Assert.Contains("tus calificaciones no quedan ligadas a tu matrícula",
                asistente.Find("[role='log']").TextContent));

        await asistente.Instance.CerrarPorEscape();

        asistente.WaitForAssertion(() =>
        {
            Assert.Equal("false", asistente.Find("button.asistente-faq__lanzador").GetAttribute("aria-expanded"));
            Assert.Contains(contexto.JSInterop.Invocations, llamada =>
                llamada.Identifier == "unieval.releaseFocus");
        });

        await proveedorAutenticacion.SetUserAsync(new AuthenticatedUser("Administración", Rol.Administrador));

        asistente.WaitForAssertion(() =>
        {
            Assert.Contains(asistente.FindAll(".asistente-faq__preguntas button"),
                pregunta => pregunta.TextContent.Contains("¿Cómo abro las encuestas?", StringComparison.Ordinal));
            Assert.DoesNotContain(asistente.FindAll(".asistente-faq__preguntas button"),
                pregunta => pregunta.TextContent.Contains("¿Son anónimas?", StringComparison.Ordinal));
        });
    }
}
