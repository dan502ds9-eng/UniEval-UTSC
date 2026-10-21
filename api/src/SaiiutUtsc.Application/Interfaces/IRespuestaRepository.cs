using System;
using SaiiutUtsc.Domain.Entities;

namespace SaiiutUtsc.Application.Interfaces;

public interface IRespuestaRepository
{
    void Agregar(Respuesta respuesta);
}
