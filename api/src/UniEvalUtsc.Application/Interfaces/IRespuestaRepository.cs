using System;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Application.Interfaces;

public interface IRespuestaRepository
{
    void Agregar(Respuesta respuesta);
}
