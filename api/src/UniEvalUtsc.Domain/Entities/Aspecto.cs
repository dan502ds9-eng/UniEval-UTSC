using System;
using System.Collections.Generic;
using System.Text;

namespace UniEvalUtsc.Domain.Entities;

public class Aspecto
{
    public int Id { get; private set; }
    public int PlantillaId { get; private set; }
    public int Numero { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public string TextoCorto
    {
        get
        {
            if (Texto.Length <= 48) return Texto;
            var corte = Texto.LastIndexOf(' ', 46, 47);
            if (corte <= 0) corte = 47;
            return $"{Texto[..corte].TrimEnd()}…";
        }
    }

    private Aspecto() { } // EF Core

    internal Aspecto(int id, int plantillaId, int numero, string texto)
    {
        Id = id;
        PlantillaId = plantillaId;
        Numero = numero;
        Texto = texto.Trim();
    }
}
