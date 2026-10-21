using System;
using System.Collections.Generic;
using System.Text;

namespace UniEvalUtsc.Application.Interfaces;

// Guarda todo lo pendiente en UNA transacción: todo o nada (RNF-07)
public interface IUnitOfWork
{
    void RegistrarReversion(Action reversion);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
