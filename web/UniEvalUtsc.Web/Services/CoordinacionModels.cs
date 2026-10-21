namespace UniEvalUtsc.Web.Services;

public sealed record OpcionCarreraResultado(string Id, string Nombre);

public sealed record ResumenCoordinacion(
    decimal Participacion,
    int AlumnosRespondieron,
    int TotalAlumnos,
    decimal PromedioDocentes,
    int DocentesPorDebajoDeOcho);

public sealed record GrupoParticipacion(
    string Id,
    string Nombre,
    int Cuatrimestre,
    int Contestaron,
    int Total,
    bool EsEstadia)
{
    public decimal Porcentaje => Total == 0 ? 0 : Math.Round(Contestaron * 100m / Total, 1);

    public int Faltan => Total - Contestaron;
}

public sealed record ResultadoCoordinacion(
    string Id,
    string Nombre,
    string Contexto,
    string Periodo,
    int Cuatrimestre,
    decimal Promedio,
    decimal PromedioCarrera,
    int Respuestas,
    IReadOnlyList<AspectoResultado> Aspectos,
    bool EsTutor,
    bool EsTutorEstadias)
{
    public decimal DiferenciaCarrera =>
        Math.Round(Promedio - PromedioCarrera, 1, MidpointRounding.AwayFromZero);

    public IReadOnlyList<AspectoResultado> AreasOportunidad =>
        Aspectos.OrderBy(aspecto => aspecto.Promedio).ThenBy(aspecto => aspecto.Numero).Take(3).ToArray();

    public IReadOnlyList<AspectoResultado> Fortalezas =>
        Aspectos.OrderByDescending(aspecto => aspecto.Promedio).ThenBy(aspecto => aspecto.Numero).Take(3).ToArray();
}
