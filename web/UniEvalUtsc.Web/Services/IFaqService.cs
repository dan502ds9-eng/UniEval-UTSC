using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public interface IFaqService
{
    Task<IReadOnlyList<PreguntaFrecuente>> ObtenerPreguntasAsync(
        Rol rol,
        CancellationToken ct = default);

    Task<string> ResponderAsync(
        Rol rol,
        string pregunta,
        CancellationToken ct = default);
}

public sealed record PreguntaFrecuente(
    string Pregunta,
    string Respuesta,
    IReadOnlyList<string> PalabrasClave);
