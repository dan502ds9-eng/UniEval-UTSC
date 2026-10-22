using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Domain.Entities;

public sealed class CargaAcademica
{
    public int Id { get; private set; }
    public int DocenteId { get; private set; }
    public int GrupoId { get; private set; }
    public int MateriaId { get; private set; }
    public int PeriodoId { get; private set; }

    private CargaAcademica() { }

    public static CargaAcademica Asignar(int id, int docenteId, int grupoId, int materiaId, int periodoId,
        IEnumerable<CargaAcademica> cargasExistentes)
    {
        if (id <= 0 || docenteId <= 0 || grupoId <= 0 || materiaId <= 0 || periodoId <= 0)
            throw new DomainException("La carga académica requiere identificadores mayores que cero.");
        if (cargasExistentes.Any(c => c.DocenteId == docenteId && c.GrupoId == grupoId
            && c.PeriodoId == periodoId && c.MateriaId != materiaId))
            throw new DomainException("Un docente no puede impartir dos materias distintas en el mismo grupo.");

        return new CargaAcademica { Id = id, DocenteId = docenteId, GrupoId = grupoId, MateriaId = materiaId, PeriodoId = periodoId };
    }
}
