using System;
using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Tests.Unit;

public class PeriodoTests
{
    // Periodo de ejemplo: encuestas del 1 al 15 de diciembre
    private static Periodo CrearPeriodo() => new(
        "Sep-Dic 2026",
        new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 20),
        new DateTime(2026, 12, 1), new DateTime(2026, 12, 15));

    [Fact]
    public void Constructor_FechasInvertidas_Lanza()
    {
        Assert.Throws<DomainException>(() => new Periodo(
            "Malo",
            new DateOnly(2026, 12, 20), new DateOnly(2026, 9, 1),
            new DateTime(2026, 12, 1), new DateTime(2026, 12, 15)));
    }

    [Fact]
    public void AceptaRespuestas_SoloAbiertoYDentroDeFechas()
    {
        var periodo = CrearPeriodo();
        var dentro = new DateTime(2026, 12, 5);

        Assert.False(periodo.AceptaRespuestas(dentro));   // en preparación: no

        periodo.Abrir();
        Assert.True(periodo.AceptaRespuestas(dentro));    // abierto y en fecha: sí
        Assert.False(periodo.AceptaRespuestas(new DateTime(2026, 12, 16))); // ya pasó el cierre: no
    }

    [Fact]
    public void Cerrar_SinHaberAbierto_Lanza()
    {
        var periodo = CrearPeriodo();
        Assert.Throws<DomainException>(() => periodo.Cerrar());
    }
}
