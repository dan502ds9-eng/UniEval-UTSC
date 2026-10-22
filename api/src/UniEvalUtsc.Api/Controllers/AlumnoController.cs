using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEvalUtsc.Application.Services;
using UniEvalUtsc.Contracts.Dtos;
using UniEvalUtsc.Domain.Entities;

namespace UniEvalUtsc.Api.Controllers;

[ApiController]
[Route("api/mis-encuestas")]
[Authorize(Policy = "Alumno")]
public sealed class AlumnoController(ConsultarEncuestasAlumnoService consultas, EnviarRespuestaService enviar) : ControllerBase
{
    private int AlumnoId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType<AvanceEncuestasDto>(200)]
    public async Task<ActionResult<AvanceEncuestasDto>> MisEncuestas(CancellationToken ct) =>
        Ok(await consultas.ObtenerMisEncuestasAsync(AlumnoId, ct));

    [HttpGet("evaluacion")]
    [ProducesResponseType<EvaluacionUnificadaDto>(200)]
    public async Task<ActionResult<EvaluacionUnificadaDto>> Evaluacion(CancellationToken ct) =>
        Ok(await consultas.ObtenerEvaluacionAsync(AlumnoId, ct));

    [HttpPost("lote/respuestas")]
    [ProducesResponseType(204)] [ProducesResponseType(400)] [ProducesResponseType(403)] [ProducesResponseType(409)]
    public async Task<IActionResult> EnviarLote([FromBody] IReadOnlyCollection<EnviarLoteRespuestasItemDto> request, CancellationToken ct)
    {
        await enviar.EnviarLoteAsync(AlumnoId, request.Select(x => (x.EncuestaAsignadaId,
            (IReadOnlyCollection<CalificacionAspecto>)x.Respuestas.Select(r => new CalificacionAspecto(r.AspectoId, r.Calificacion)).ToArray())).ToArray(), ct);
        return NoContent();
    }

    [HttpPost("{id:int}/respuestas")]
    [ProducesResponseType(204)] [ProducesResponseType(400)] [ProducesResponseType(403)] [ProducesResponseType(409)]
    public async Task<IActionResult> EnviarUna(int id, [FromBody] EnviarRespuestasRequest request, CancellationToken ct)
    {
        await enviar.EnviarAsync(id, AlumnoId, request.Respuestas.Select(r => new CalificacionAspecto(r.AspectoId, r.Calificacion)).ToArray(), ct);
        return NoContent();
    }

    [HttpGet("comprobante")]
    [ProducesResponseType<ComprobanteDto>(200)] [ProducesResponseType(404)]
    public async Task<ActionResult<ComprobanteDto>> Comprobante(CancellationToken ct) =>
        Ok(await consultas.ObtenerComprobanteAsync(AlumnoId, User.Identity?.Name ?? "Alumno", ct));
}
