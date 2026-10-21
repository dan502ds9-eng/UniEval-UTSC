using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace UniEvalUtsc.Web.Services;

public sealed class UnievalJsInterop(IJSRuntime jsRuntime)
{
    public ValueTask TrapFocusAsync<T>(ElementReference element, DotNetObjectReference<T> callback)
        where T : class =>
        jsRuntime.InvokeVoidAsync("unieval.trapFocus", element, callback);

    public ValueTask ReleaseFocusAsync(ElementReference element) =>
        jsRuntime.InvokeVoidAsync("unieval.releaseFocus", element);

    public ValueTask FocusElementAsync(ElementReference element) =>
        jsRuntime.InvokeVoidAsync("unieval.focusElement", element);

    public ValueTask FocusByIdAsync(string id) =>
        jsRuntime.InvokeVoidAsync("unieval.focusById", id);

    public ValueTask<bool> SpeakAsync(string text) =>
        jsRuntime.InvokeAsync<bool>("unieval.speak", text);

    public ValueTask CancelSpeechAsync() =>
        jsRuntime.InvokeVoidAsync("unieval.cancelSpeech");

    public ValueTask<string?> GetLocalStorageAsync(string key) =>
        jsRuntime.InvokeAsync<string?>("unieval.getLocalStorage", key);

    public ValueTask<bool> SetLocalStorageAsync(string key, string value) =>
        jsRuntime.InvokeAsync<bool>("unieval.setLocalStorage", key, value);

    public ValueTask PrintAsync() =>
        jsRuntime.InvokeVoidAsync("unieval.print");

    public ValueTask DescargarArchivoAsync(string nombre, string contenidoBase64, string tipo) =>
        jsRuntime.InvokeVoidAsync("unieval.downloadFile", nombre, contenidoBase64, tipo);
}
