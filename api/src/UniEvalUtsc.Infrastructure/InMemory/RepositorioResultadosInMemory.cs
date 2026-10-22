using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Infrastructure.InMemory;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RepositorioResultadosInMemory(AlmacenEvaluacionInMemory almacen) : IResultadosRepository
{
    public Task<DatosResultados> ObtenerDatosAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var cargas = DatosSemillaDemo.Grupos
            .Where(g => g.Id == 1)
            .SelectMany(g => g.Materias.Select(m => new CargaResultadoDatos(m.CargaAcademicaId, g.Id, m.Materia, m.Docente)))
            .Concat([
                new(101, 6, "Aplicaciones Web", "Óscar Gabriel Chávez Gómez"),
                new(102, 6, "Bases de Datos", "Sem Tomás Hernández Martínez"),
                new(103, 6, "Proyecto Integrador II", "Juan Pablo Vega Ramírez"),
                new(108, 8, "Procesos Industriales", "Docente de Mantenimiento Industrial")
            ]).ToArray();
        var grupos = new[]
        {
            new GrupoResultadoDatos(1, "DSM05AV · Grupo A", "dsm", 5, false, 2, [1, 7], 1),
            new GrupoResultadoDatos(2, "DSM05AV · Grupo B", "dsm", 5, false, 31, Enumerable.Range(1, 31).ToArray(), 1),
            new GrupoResultadoDatos(3, "DSM06AV · Grupo A", "dsm", 6, true, 28, Enumerable.Range(1, 28).ToArray(), 503),
            new GrupoResultadoDatos(4, "DSM10AV · Grupo A", "dsm", 10, true, 25, Enumerable.Range(1, 25).ToArray(), 504),
            new GrupoResultadoDatos(6, "DSM05AV · Grupo A", "dsm", 5, false, 31, Enumerable.Range(1, 31).ToArray(), 501),
            new GrupoResultadoDatos(7, "DSM05AV · Grupo B", "dsm", 5, false, 31, Enumerable.Range(1, 31).ToArray(), 502),
            new GrupoResultadoDatos(8, "MTI05 · Grupo A", "mti", 5, false, 30, Enumerable.Range(40, 30).ToArray(), 505)
        };
        var datos = new DatosResultados(almacen.ObtenerPeriodos(), [.. DatosSemillaDemo.CrearPlantillas()],
            almacen.ObtenerEncuestas(), almacen.ObtenerRespuestas(), grupos, cargas);
        return Task.FromResult(datos);
    }
}
