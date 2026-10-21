using NivelEvaluacion = UniEvalUtsc.Contracts.Enums.Nivel;

namespace UniEvalUtsc.Contracts;

public static class Niveles
{
    public static decimal Redondear(decimal valor) =>
        Math.Round(valor, 1, MidpointRounding.AwayFromZero);

    public static NivelEvaluacion Nivel(decimal valor)
    {
        var valorRedondeado = Redondear(valor);
        return valorRedondeado < 5
            ? NivelEvaluacion.Bajo
            : valorRedondeado < 8
                ? NivelEvaluacion.Medio
                : NivelEvaluacion.Alto;
    }
}
