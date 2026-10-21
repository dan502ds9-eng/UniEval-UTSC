using System;
using SaiiutUtsc.Domain.Entities;

namespace SaiiutUtsc.Application.Interfaces;

public interface IEncuestaAsignadaRepository
{
    Task<EncuestaAsignada?> ObtenerAsync(int id, CancellationToken ct = default);
}