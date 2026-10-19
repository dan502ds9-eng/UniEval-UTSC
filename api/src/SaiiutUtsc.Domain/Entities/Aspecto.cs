using System;
using System.Collections.Generic;
using System.Text;

namespace SaiiutUtsc.Domain.Entities;

public class Aspecto
{
    public int Id { get; private set; }
    public int PlantillaId { get; private set; }
    public int Numero { get; private set; }
    public string Texto { get; private set; } = string.Empty;

    private Aspecto() { } // EF Core

    internal Aspecto(int id, int plantillaId, int numero, string texto)
    {
        Id = id;
        PlantillaId = plantillaId;
        Numero = numero;
        Texto = texto.Trim();
    }
}
