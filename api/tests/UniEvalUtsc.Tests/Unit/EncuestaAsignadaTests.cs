using System;
using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Tests.Unit;

public class EncuestaAsignadaTests
{
    [Fact]
    public void Fabrica_RechazaPeriodoInvalido()
    {
        Assert.Throws<DomainException>(() => EncuestaAsignada.ParaDocente(50, 7, 1, 30, periodoId: 0));
        Assert.Throws<DomainException>(() => EncuestaAsignada.ParaTutor(50, 7, 2, 9, periodoId: -1));
    }

    [Fact]
    public void MarcarContestada_DosVeces_Lanza()
    {
        var e = EncuestaAsignada.ParaTutor(alumnoId: 50, grupoId: 7, plantillaId: 2, tutorId: 9, periodoId: 1);
        e.MarcarContestada(new DateOnly(2026, 12, 5));

        Assert.Equal(EstadoEncuesta.Contestada, e.Estado);
        Assert.Throws<DomainException>(() => e.MarcarContestada(new DateOnly(2026, 12, 6)));
    }

    [Fact]
    public void PerteneceA_OtroAlumno_EsFalso()
    {
        var e = EncuestaAsignada.ParaDocente(alumnoId: 50, grupoId: 7, plantillaId: 1, cargaAcademicaId: 30, periodoId: 1);
        Assert.True(e.PerteneceA(50));
        Assert.False(e.PerteneceA(51));
    }
}
