using System;
namespace UniEvalUtsc.Application.Interfaces;

public interface IReloj
{
    DateTime Ahora { get; }
    DateOnly Hoy { get; }
}