using System;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Application.Interfaces;

public interface IPeriodoRepository
{
    Task<Periodo?> ObtenerPorGrupoAsync(int grupoId, CancellationToken ct = default);
}