using System;
using SaiiutUtsc.Domain.Entities;

namespace SaiiutUtsc.Application.Interfaces;

public interface IPeriodoRepository
{
    Task<Periodo?> ObtenerPorGrupoAsync(int grupoId, CancellationToken ct = default);
}