using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using UniEvalUtsc.Web.Components.Ui;

namespace UniEvalUtsc.Web.Tests.Components.Ui;

public sealed class DialogoHarness : ComponentBase
{
    private bool _abierto;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "type", "button");
        builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, Abrir));
        builder.AddContent(3, "Abrir diálogo");
        builder.CloseElement();

        builder.OpenComponent<Dialogo>(4);
        builder.AddAttribute(5, nameof(Dialogo.IsOpen), _abierto);
        builder.AddAttribute(6, nameof(Dialogo.IsOpenChanged),
            EventCallback.Factory.Create<bool>(this, CambiarEstado));
        builder.AddAttribute(7, nameof(Dialogo.Title), "Diálogo de prueba");
        builder.AddAttribute(8, nameof(Dialogo.ChildContent),
            (RenderFragment)(content => content.AddContent(0, "Contenido del diálogo")));
        builder.CloseComponent();
    }

    private void Abrir() => _abierto = true;

    private void CambiarEstado(bool abierto) => _abierto = abierto;
}
