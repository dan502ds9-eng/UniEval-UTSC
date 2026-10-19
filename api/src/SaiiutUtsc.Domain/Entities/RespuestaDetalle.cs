using System;
using System.Collections.Generic;
using System.Text;

namespace SaiiutUtsc.Domain.Entities;

public class RespuestaDetalle
{
    public Guid RespuestaId { get; private set; }
    public int AspectoId { get; private set; }
    public int Calificacion { get; private set; }

    private RespuestaDetalle() { } // EF Core

    internal RespuestaDetalle(Guid respuestaId, int aspectoId, int calificacion)
    {
        RespuestaId = respuestaId;
        AspectoId = aspectoId;
        Calificacion = calificacion;
    }
}