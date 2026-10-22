using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Tests.Unit;

public sealed class CargaAcademicaTests
{
    [Fact]
    public void Asignar_PermiteVariasMateriasEnGruposDistintos()
    {
        var existente = CargaAcademica.Asignar(1, 10, 20, 30, 5, []);
        var nueva = CargaAcademica.Asignar(2, 10, 21, 31, 5, [existente]);

        Assert.Equal(21, nueva.GrupoId);
    }

    [Fact]
    public void Asignar_RechazaDosMateriasDelDocenteEnElMismoGrupo()
    {
        var existente = CargaAcademica.Asignar(1, 10, 20, 30, 5, []);

        Assert.Throws<DomainException>(() => CargaAcademica.Asignar(2, 10, 20, 31, 5, [existente]));
    }
}
