using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using UniEvalUtsc.Web.Components.Encuesta;

namespace UniEvalUtsc.Web.Tests.Components.Encuesta;

public sealed class SelectorCalificacionTests
{
    [Fact]
    public void SelectorCalificacion_SeleccionaElValorYExponeElNombreAccesible()
    {
        using var contexto = new BunitContext();
        var valorSeleccionado = 0;
        var selector = contexto.Render<SelectorCalificacion>(parametros => parametros
            .Add(componente => componente.Value, null)
            .Add(componente => componente.ValueChanged, EventCallback.Factory.Create<int>(this, valor => valorSeleccionado = valor))
            .Add(componente => componente.Etiqueta, "Calificación para la docente"));

        var opcion = selector.Find("input[value='8']");
        Assert.Equal("8 de 10, alto", opcion.GetAttribute("aria-label"));

        opcion.Change("8");

        Assert.Equal(8, valorSeleccionado);
        Assert.Equal(10, selector.FindAll("input[type='radio']").Count);
    }

    [Fact]
    public void SelectorCalificacion_AlPresionarCero_NotificaElEventoDeTecla()
    {
        using var contexto = new BunitContext();
        var valorRecibido = 0;
        var selector = contexto.Render<SelectorCalificacion>(parametros => parametros
            .Add(componente => componente.Etiqueta, "Calificación")
            .Add(componente => componente.OnTeclaNumero, EventCallback.Factory.Create<int>(this, valor => valorRecibido = valor)));

        selector.Find("input[value='1']").KeyDown(new KeyboardEventArgs { Key = "0" });

        Assert.Equal(10, valorRecibido);
    }
}
