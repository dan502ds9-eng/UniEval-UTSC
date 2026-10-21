using System;
using System.Collections.Generic;
using System.Text;

namespace SaiiutUtsc.Application.Interfaces;

// Guarda todo lo pendiente en UNA transacción: todo o nada (RNF-07)
public interface IUnitOfWork
{
    Task GuardarCambiosAsync(CancellationToken ct = default);
}