using UniEvalUtsc.Application.Interfaces;

namespace UniEvalUtsc.Infrastructure.InMemory;

public sealed class RelojSistema : IReloj
{
    public DateTime Ahora => DateTime.Now;
    public DateOnly Hoy => DateOnly.FromDateTime(Ahora);
}
