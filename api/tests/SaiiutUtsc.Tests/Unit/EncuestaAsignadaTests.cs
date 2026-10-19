using System;
using SaiiutUtsc.Domain.Entities;
using SaiiutUtsc.Domain.Enums;
using SaiiutUtsc.Domain.Exceptions;

namespace SaiiutUtsc.Tests.Unit;

public class EncuestaAsignadaTests
{
    [Fact]
    public void MarcarContestada_DosVeces_Lanza()
    {
        var e = EncuestaAsignada.ParaTutor(alumnoId: 50, grupoId: 7, plantillaId: 2, tutorId: 9);
        e.MarcarContestada(new DateOnly(2026, 12, 5));

        Assert.Equal(EstadoEncuesta.Contestada, e.Estado);
        Assert.Throws<DomainException>(() => e.MarcarContestada(new DateOnly(2026, 12, 6)));
    }

    [Fact]
    public void PerteneceA_OtroAlumno_EsFalso()
    {
        var e = EncuestaAsignada.ParaDocente(alumnoId: 50, grupoId: 7, plantillaId: 1, cargaAcademicaId: 30);
        Assert.True(e.PerteneceA(50));
        Assert.False(e.PerteneceA(51));
    }
}
