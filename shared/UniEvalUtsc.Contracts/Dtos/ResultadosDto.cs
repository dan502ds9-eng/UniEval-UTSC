using UniEvalUtsc.Contracts.Enums;
using System.Text.Json.Serialization;

namespace UniEvalUtsc.Contracts.Dtos;

// DTOs que ya consume la interfaz de resultados.
public sealed record PeriodoResultado(string Id, string Nombre, bool Abierto, bool Cerrado = true);
public sealed record ElementoResultado(string Id, string Nombre, string Contexto, int AlumnosRespondieron, int TotalAlumnos);
public sealed record TarjetaResultado(ElementoResultado Elemento, ResultadoPersonal Resultado, string? MensajeNoDisponible);
public sealed record ResultadoPersonal(
    ElementoResultado Elemento, PeriodoResultado Periodo, decimal PromedioGeneral, int Respuestas,
    int TotalAlumnos, decimal PromedioCarrera, IReadOnlyList<AspectoResultado> Aspectos)
{
    public decimal DiferenciaCarrera => Math.Round(PromedioGeneral - PromedioCarrera, 1, MidpointRounding.AwayFromZero);
    public IReadOnlyList<AspectoResultado> AreasOportunidad => Aspectos.OrderBy(x => x.Promedio).ThenBy(x => x.Numero).Take(3).ToArray();
    public IReadOnlyList<AspectoResultado> Fortalezas => Aspectos.OrderByDescending(x => x.Promedio).ThenBy(x => x.Numero).Take(3).ToArray();
    public Nivel Nivel => Niveles.Nivel(PromedioGeneral);
}
public sealed record AspectoResultado(
    [property: JsonPropertyName("aspectoId")] int AspectoId,
    string Texto,
    string TextoCorto,
    decimal Promedio,
    decimal PromedioCarrera,
    string Nivel)
{
    public AspectoResultado(int numero, string texto, decimal promedio, decimal promedioCarrera, string nivel)
        : this(numero, texto, texto.Length > 48 ? $"{texto[..45]}…" : texto, promedio, promedioCarrera, nivel)
    {
    }

    [JsonIgnore]
    public int Numero => AspectoId;
}
public sealed record PuntoHistorico(string PeriodoId, string Periodo, decimal Promedio, string Nivel);
public sealed record FilaReportePersonal(string PeriodoId, string Periodo, string ElementoId, string ElementoNombre, string Contexto, decimal Promedio, string Nivel);

public sealed record OpcionCarreraResultado(string Id, string Nombre);
public sealed record ResumenCoordinacion(decimal Participacion, int AlumnosRespondieron, int TotalAlumnos, decimal PromedioDocentes, int DocentesPorDebajoDeOcho);
public sealed record GrupoParticipacion(string Id, string CarreraId, string Nombre, int Cuatrimestre, int Contestaron, int Total, bool EsEstadia)
{
    public decimal Porcentaje => Total == 0 ? 0 : Math.Round(Contestaron * 100m / Total, 1);
    public int Faltan => Total - Contestaron;
}
public sealed record ResultadoCoordinacion(
    string Id, string CarreraId, string Nombre, string Contexto, string Periodo, int Cuatrimestre,
    decimal Promedio, decimal PromedioCarrera, int Respuestas, IReadOnlyList<AspectoResultado> Aspectos,
    bool EsTutor, bool EsTutorEstadias)
{
    public decimal DiferenciaCarrera => Math.Round(Promedio - PromedioCarrera, 1, MidpointRounding.AwayFromZero);
    public IReadOnlyList<AspectoResultado> AreasOportunidad => Aspectos.OrderBy(x => x.Promedio).ThenBy(x => x.Numero).Take(3).ToArray();
    public IReadOnlyList<AspectoResultado> Fortalezas => Aspectos.OrderByDescending(x => x.Promedio).ThenBy(x => x.Numero).Take(3).ToArray();
}

public sealed record ResultadoOcultoDto(bool Oculto, int Respuestas);
public sealed record ResultadoNoDisponibleDto(bool Disponible);
public sealed record GrupoSeguimientoDto(
    int GrupoId, string Grupo, string CarreraId, int Cuatrimestre, int TotalAlumnos,
    int AlumnosRespondieron, decimal Participacion, IReadOnlyList<AlumnoPendienteDto> AlumnosPendientes);
public sealed record AlumnoPendienteDto(string Nombre, string Correo);
public sealed record MensajeDto(Guid Id, string Remitente, string Titulo, string Cuerpo, DateTime CreadoEl, DateTime? LeidoEl);
public sealed record RecordatorioCreadoDto(int Creados);
public sealed record RecordatorioRequestDto(int GrupoId);
