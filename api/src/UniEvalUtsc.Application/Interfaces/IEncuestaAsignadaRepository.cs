using System;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Application.Interfaces;

public interface IEncuestaAsignadaRepository
{
    Task<EncuestaAsignada?> ObtenerAsync(int id, CancellationToken ct = default);
}