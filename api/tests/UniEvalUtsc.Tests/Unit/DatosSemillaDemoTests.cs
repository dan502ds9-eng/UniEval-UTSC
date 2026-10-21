using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Infrastructure.InMemory;

namespace UniEvalUtsc.Tests.Unit;

public sealed class DatosSemillaDemoTests
{
    [Fact]
    public void SemillaIncluyePeriodosAbiertoYCerrados()
    {
        var periodos = DatosSemillaDemo.CrearPeriodos();

        Assert.Single(periodos, periodo => periodo.Estado == EstadoPeriodo.Abierto);
        Assert.Equal(3, periodos.Count(periodo => periodo.Estado == EstadoPeriodo.Cerrado));
        Assert.Equal("Sep–Dic 2026", periodos.Single(periodo => periodo.Estado == EstadoPeriodo.Abierto).Nombre);
    }

    [Fact]
    public void SemillaIncluyeMateriasTutorYPlantillas()
    {
        var grupo = Assert.Single(DatosSemillaDemo.Grupos, grupo => grupo.Nombre == "DSM05AV");
        var plantillas = DatosSemillaDemo.CrearPlantillas().ToDictionary(plantilla => plantilla.CodigoFormato);

        Assert.Equal(7, grupo.Materias.Count);
        Assert.Equal(DatosSemillaDemo.Tutora, grupo.Tutor);
        Assert.Equal(10, plantillas["FORM-AC16"].Aspectos.Count);
        Assert.Equal(7, plantillas["FORM-AC64"].Aspectos.Count);
        Assert.Equal(8, DatosSemillaDemo.CrearEncuestasAsignadas().Count);
    }

    [Fact]
    public void SemillaIncluyeGruposDeDemoYEstadias()
    {
        Assert.Contains(DatosSemillaDemo.Grupos, grupo => grupo.Nombre == "[Grupo 5to B]");
        Assert.Contains(DatosSemillaDemo.Grupos, grupo => grupo.Nombre == "[Grupo 6to A]" && grupo.EsEstadias);
        Assert.Contains(DatosSemillaDemo.Grupos, grupo => grupo.Nombre == "[Grupo 10mo A]" && grupo.EsEstadias);
    }
}
