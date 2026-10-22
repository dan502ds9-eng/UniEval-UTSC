using UniEvalUtsc.Contracts;

namespace UniEvalUtsc.Tests.Unit;

public sealed class NivelesTests
{
    [Theory]
    [InlineData(8.05, 8.1)]
    [InlineData(8.04, 8.0)]
    [InlineData(7.95, 8.0)]
    public void Promedio_SeRedondeaAUnoDecimalAwayFromZero(decimal entrada, decimal esperado) =>
        Assert.Equal(esperado, Niveles.Redondear(entrada));
}
