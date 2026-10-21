using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Application.Services;
using UniEvalUtsc.Contracts.Dtos;
using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Infrastructure.InMemory;

namespace UniEvalUtsc.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = "Alumno")]
public sealed class AlumnoController(IEncuestaAsignadaRepository encuestas, IPlantillaRepository plantillas, IPeriodoRepository periodos,
    EnviarRespuestaService enviar, IReloj reloj) : ControllerBase
{
    private int AlumnoId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("auth/perfil")]
    [ProducesResponseType<UsuarioPerfilDto>(200)]
    public ActionResult<UsuarioPerfilDto> Perfil([FromHeader(Name = "X-Usuario-Simulado")] string? usuarioSimulado = null) => Ok(new UsuarioPerfilDto(User.Identity?.Name ?? "Alumno", User.FindFirstValue(ClaimTypes.Email) ?? "", [UniEvalUtsc.Contracts.Enums.Rol.Alumno]));

    [HttpGet("mis-encuestas")]
    [ProducesResponseType<AvanceEncuestasDto>(200)]
    public async Task<ActionResult<AvanceEncuestasDto>> MisEncuestas([FromHeader(Name = "X-Usuario-Simulado")] string? usuarioSimulado = null, CancellationToken ct = default)
    {
        var todas = await encuestas.ObtenerTodasAsync(AlumnoId, ct);
        var abiertas = new List<EncuestaAsignada>();
        foreach (var e in todas) { var p = await periodos.ObtenerPorGrupoAsync(e.GrupoId, ct); if (p?.AceptaRespuestas(reloj.Ahora) == true) abiertas.Add(e); }
        var items = abiertas.Select(e => new EncuestaAsignadaDto(e.Id, DatosSemillaDemo.Grupos.FirstOrDefault(g => g.Id == e.GrupoId)?.Nombre ?? "", e.CargaAcademicaId is int carga ? DatosSemillaDemo.Grupos.SelectMany(g => g.Materias).FirstOrDefault(m => m.CargaAcademicaId == carga)?.Materia : null, e.Tipo == TipoEncuesta.Tutor ? DatosSemillaDemo.Tutora : DatosSemillaDemo.Grupos.SelectMany(g => g.Materias).FirstOrDefault(m => m.CargaAcademicaId == e.CargaAcademicaId)?.Docente ?? "", (UniEvalUtsc.Contracts.Enums.TipoEncuesta)(int)e.Tipo, (UniEvalUtsc.Contracts.Enums.EstadoEncuesta)(int)e.Estado)).ToArray();
        return Ok(new AvanceEncuestasDto(items.Count(x => x.Estado == UniEvalUtsc.Contracts.Enums.EstadoEncuesta.Contestada), items.Length, items));
    }

    [HttpGet("mis-encuestas/evaluacion")]
    [ProducesResponseType<EvaluacionUnificadaDto>(200)]
    public async Task<ActionResult<EvaluacionUnificadaDto>> Evaluacion([FromHeader(Name = "X-Usuario-Simulado")] string? usuarioSimulado = null, CancellationToken ct = default)
    {
        var pendientes = await encuestas.ObtenerPendientesAsync(AlumnoId, ct);
        var evaluados = new List<(EncuestaAsignada Encuesta, UniEvalUtsc.Domain.Entities.Plantilla Plantilla)>();
        foreach (var e in pendientes)
        {
            var periodoAsignacion = await periodos.ObtenerPorGrupoAsync(e.GrupoId, ct);
            if (periodoAsignacion?.AceptaRespuestas(reloj.Ahora) != true) continue;
            var p = await plantillas.ObtenerConAspectosAsync(e.PlantillaId, ct);
            if (p is not null) evaluados.Add((e, p));
        }
        var plantilla = evaluados.FirstOrDefault().Plantilla;
        if (plantilla is null) return Ok(new EvaluacionUnificadaDto("", []));
        var preguntas = plantilla.Aspectos.OrderBy(a => a.Numero).Select(a => new PreguntaEvaluacionDto(a.Id, a.Numero, a.Texto, evaluados.Select(x => new EvaluadoDto(x.Encuesta.Id,
            x.Encuesta.Tipo == TipoEncuesta.Tutor ? DatosSemillaDemo.Tutora : DatosSemillaDemo.Grupos.SelectMany(g => g.Materias).FirstOrDefault(m => m.CargaAcademicaId == x.Encuesta.CargaAcademicaId)?.Docente ?? "", x.Encuesta.CargaAcademicaId is int id ? DatosSemillaDemo.Grupos.SelectMany(g => g.Materias).FirstOrDefault(m => m.CargaAcademicaId == id)?.Materia : null, (UniEvalUtsc.Contracts.Enums.TipoEncuesta)(int)x.Encuesta.Tipo)).ToArray())).ToArray();
        var periodo = evaluados.FirstOrDefault().Encuesta is { } enc ? await periodos.ObtenerPorGrupoAsync(enc.GrupoId, ct) : null;
        return Ok(new EvaluacionUnificadaDto(periodo?.Nombre ?? "", preguntas));
    }

    [HttpPost("mis-encuestas/lote/respuestas")]
    [ProducesResponseType(204)] [ProducesResponseType(400)] [ProducesResponseType(403)] [ProducesResponseType(409)]
    public async Task<IActionResult> EnviarLote([FromBody] IReadOnlyCollection<EnviarLoteRespuestasItemDto> request, [FromHeader(Name = "X-Usuario-Simulado")] string? usuarioSimulado = null, CancellationToken ct = default)
    {
        await enviar.EnviarLoteAsync(AlumnoId, request.Select(x => (x.EncuestaAsignadaId, (IReadOnlyCollection<CalificacionAspecto>)x.Respuestas.Select(r => new CalificacionAspecto(r.AspectoId, r.Calificacion)).ToArray())).ToArray(), ct);
        return NoContent();
    }

    [HttpPost("mis-encuestas/{id:int}/respuestas")]
    [ProducesResponseType(204)] [ProducesResponseType(400)] [ProducesResponseType(403)] [ProducesResponseType(409)]
    public async Task<IActionResult> EnviarUna(int id, [FromBody] EnviarRespuestasRequest request, [FromHeader(Name = "X-Usuario-Simulado")] string? usuarioSimulado = null, CancellationToken ct = default)
    {
        await enviar.EnviarAsync(id, AlumnoId, request.Respuestas.Select(r => new CalificacionAspecto(r.AspectoId, r.Calificacion)).ToArray(), ct);
        return NoContent();
    }

    [HttpGet("mis-encuestas/comprobante")]
    [ProducesResponseType<ComprobanteDto>(200)] [ProducesResponseType(404)]
    public async Task<ActionResult<ComprobanteDto>> Comprobante([FromHeader(Name = "X-Usuario-Simulado")] string? usuarioSimulado = null, CancellationToken ct = default)
    {
        var todas = await encuestas.ObtenerTodasAsync(AlumnoId, ct);
        if (todas.Count == 0 || todas.Any(e => e.Estado != EstadoEncuesta.Contestada)) return NotFound(new ProblemDetails { Status = 404, Title = "Comprobante no disponible", Detail = "Aún tienes encuestas pendientes." });
        var periodo = await periodos.ObtenerPorGrupoAsync(todas.First().GrupoId, ct);
        var fecha = todas.Max(e => e.FechaContestada ?? reloj.Hoy);
        return Ok(new ComprobanteDto(User.Identity?.Name ?? "Alumno", periodo?.Nombre ?? "", todas.Count, todas.Count, fecha));
    }
}
