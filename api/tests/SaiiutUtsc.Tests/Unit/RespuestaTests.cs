using System;
using SaiiutUtsc.Domain.Entities;
using SaiiutUtsc.Domain.Enums;
using SaiiutUtsc.Domain.Exceptions;

namespace SaiiutUtsc.Tests.Unit;

public class RespuestaTests
{
    // Plantilla de prueba con 3 aspectos (ids 101, 102, 103)
    private static Plantilla CrearPlantilla()
    {
        var p = new Plantilla(TipoEncuesta.Docente, "FORM-AC16", "1", "Califique a su profesor.", id: 1);
        p.AgregarAspecto(1, "Dio a conocer objetivos.", id: 101);
        p.AgregarAspecto(2, "Dominó los temas.", id: 102);
        p.AgregarAspecto(3, "Mostró respeto.", id: 103);
        return p;
    }

    // Encuesta del alumno 50, grupo 7, materia (carga) 30
    private static EncuestaAsignada CrearEncuesta() =>
        EncuestaAsignada.ParaDocente(alumnoId: 50, grupoId: 7, plantillaId: 1, cargaAcademicaId: 30);

    private static List<CalificacionAspecto> Completas() =>
        [new(101, 9), new(102, 10), new(103, 8)];

    [Fact]
    public void Crear_ConTodosLosAspectos_GuardaDetallesYDatosDelEvaluado()
    {
        var r = Respuesta.Crear(CrearEncuesta(), CrearPlantilla(), Completas());

        Assert.Equal(3, r.Detalles.Count);
        Assert.Equal(7, r.GrupoId);
        Assert.Equal(30, r.CargaAcademicaId);
        Assert.All(r.Detalles, d => Assert.Equal(r.Id, d.RespuestaId));
    }

    [Fact]
    public void Crear_FaltaUnAspecto_LanzaValidacion()
    {
        var incompletas = new List<CalificacionAspecto> { new(101, 9), new(102, 10) };
        Assert.Throws<ValidacionException>(() =>
            Respuesta.Crear(CrearEncuesta(), CrearPlantilla(), incompletas));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Crear_CalificacionFueraDeRango_LanzaValidacion(int mala)
    {
        var calif = new List<CalificacionAspecto> { new(101, mala), new(102, 10), new(103, 8) };
        Assert.Throws<ValidacionException>(() =>
            Respuesta.Crear(CrearEncuesta(), CrearPlantilla(), calif));
    }

    [Fact]
    public void Crear_AspectoRepetido_LanzaValidacion()
    {
        var repetidas = new List<CalificacionAspecto> { new(101, 9), new(101, 9), new(103, 8) };
        Assert.Throws<ValidacionException>(() =>
            Respuesta.Crear(CrearEncuesta(), CrearPlantilla(), repetidas));
    }

    // RN-09: la prueba de anonimato
    [Fact]
    public void Respuesta_NoTieneNingunCampoDelAlumno()
    {
        var props = typeof(Respuesta).GetProperties()
            .Concat(typeof(RespuestaDetalle).GetProperties());

        Assert.DoesNotContain(props, p => p.Name.Contains("Alumno"));
        Assert.DoesNotContain(props, p => p.Name.Contains("Fecha"));
    }

    // El id es UUID v4 aleatorio: no delata el orden de envío
    [Fact]
    public void Crear_GeneraIdAleatorioVersion4()
    {
        var r1 = Respuesta.Crear(CrearEncuesta(), CrearPlantilla(), Completas());
        var r2 = Respuesta.Crear(CrearEncuesta(), CrearPlantilla(), Completas());

        Assert.NotEqual(r1.Id, r2.Id);
        Assert.Equal(4, r1.Id.Version);
    }
}