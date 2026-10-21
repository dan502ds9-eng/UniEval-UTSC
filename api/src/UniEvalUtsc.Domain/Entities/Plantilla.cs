using System;
using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Domain.Entities;

public class Plantilla
{
    private readonly List<Aspecto> _aspectos = [];

    public int Id { get; private set; }
    public TipoEncuesta Tipo { get; private set; }
    public string CodigoFormato { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public string Instrucciones { get; private set; } = string.Empty;
    public bool Vigente { get; private set; }
    public IReadOnlyCollection<Aspecto> Aspectos => _aspectos.AsReadOnly();

    private Plantilla() { } // EF Core

    public Plantilla(TipoEncuesta tipo, string codigoFormato, string version, string instrucciones, int id = 0)
    {
        if (string.IsNullOrWhiteSpace(codigoFormato) || string.IsNullOrWhiteSpace(version))
            throw new ValidacionException("La plantilla necesita código de formato y versión.");
        if (string.IsNullOrWhiteSpace(instrucciones))
            throw new ValidacionException("La plantilla necesita instrucciones.");

        Id = id;
        Tipo = tipo;
        CodigoFormato = codigoFormato.Trim();
        Version = version.Trim();
        Instrucciones = instrucciones.Trim();
    }

    public void AgregarAspecto(int numero, string texto, int id = 0)
    {
        // RN-12: una plantilla publicada no se toca; se crea versión nueva
        if (Vigente)
            throw new DomainException("No se pueden cambiar aspectos de una plantilla publicada. Crea una versión nueva.");
        if (numero <= 0)
            throw new ValidacionException("El número de aspecto debe ser mayor a 0.");
        if (string.IsNullOrWhiteSpace(texto))
            throw new ValidacionException("El aspecto necesita texto.");
        if (_aspectos.Any(a => a.Numero == numero))
            throw new ValidacionException($"Ya existe el aspecto número {numero}.");

        _aspectos.Add(new Aspecto(id, Id, numero, texto));
    }

    public void Publicar()
    {
        if (_aspectos.Count == 0)
            throw new DomainException("No se puede publicar una plantilla sin aspectos.");
        Vigente = true;
    }
}
