using System;
namespace SaiiutUtsc.Application.Interfaces;

public interface IReloj
{
    DateTime Ahora { get; }
    DateOnly Hoy { get; }
}