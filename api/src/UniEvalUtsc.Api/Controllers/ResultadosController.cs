using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEvalUtsc.Application.Services;

namespace UniEvalUtsc.Api.Controllers;

[ApiController]
[Route("api/mis-resultados")]
[Authorize(Roles = "Docente,Tutor")]
public sealed class MisResultadosController(ConsultarResultadosService consultas) : ControllerBase
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IReadOnlyCollection<string> Roles => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

    [HttpGet]
    [ProducesResponseType<UniEvalUtsc.Contracts.Dtos.ResultadoPropioDto>(200)]
    public async Task<ActionResult<UniEvalUtsc.Contracts.Dtos.ResultadoPropioDto>> Obtener([FromQuery] int periodoId, [FromQuery] int? cargaAcademicaId,
        [FromQuery] int? grupoId, CancellationToken ct) =>
        Ok(await consultas.ObtenerPropioAsync(UsuarioId, Roles, null, periodoId, cargaAcademicaId, grupoId, ct));

    [HttpGet("resumen")]
    [ProducesResponseType<UniEvalUtsc.Contracts.Dtos.ResumenResultadosDto>(200)]
    public async Task<ActionResult<UniEvalUtsc.Contracts.Dtos.ResumenResultadosDto>> Resumen([FromQuery] int periodoId, CancellationToken ct) =>
        Ok(await consultas.ObtenerResumenPropioAsync(UsuarioId, Roles, periodoId, ct));

    [HttpGet("historico")]
    [ProducesResponseType<IReadOnlyCollection<UniEvalUtsc.Contracts.Dtos.PuntoHistoricoDto>>(200)]
    public async Task<ActionResult<IReadOnlyCollection<UniEvalUtsc.Contracts.Dtos.PuntoHistoricoDto>>> Historico([FromQuery] int? cargaAcademicaId, [FromQuery] int? grupoId, CancellationToken ct) =>
        Ok(await consultas.ObtenerHistoricoPropioAsync(UsuarioId, Roles, null, cargaAcademicaId, grupoId, ct));
}

[ApiController]
[Route("api/resultados")]
[Authorize(Roles = "Coordinador,Administrador")]
public sealed class ResultadosController(ConsultarResultadosService consultas) : ControllerBase
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IReadOnlyCollection<string> Roles => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

    [HttpGet("docentes")]
    [ProducesResponseType<IReadOnlyCollection<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>>(200)]
    public async Task<ActionResult<IReadOnlyCollection<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>>> Docentes([FromQuery] int? periodoId, [FromQuery] string? carreraId,
        [FromQuery] int? cuatrimestre, CancellationToken ct) =>
        Ok(await consultas.ObtenerResultadosGestionAsync(UsuarioId, Roles, carreraId, periodoId, cuatrimestre, false, ct));

    [HttpGet("tutores")]
    [ProducesResponseType<IReadOnlyCollection<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>>(200)]
    public async Task<ActionResult<IReadOnlyCollection<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>>> Tutores([FromQuery] int? periodoId, [FromQuery] string? carreraId,
        [FromQuery] int? cuatrimestre, CancellationToken ct) =>
        Ok(await consultas.ObtenerResultadosGestionAsync(UsuarioId, Roles, carreraId, periodoId, cuatrimestre, true, ct));

    [HttpGet("docentes/{cargaAcademicaId:int}")]
    [ProducesResponseType<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>(200)]
    public async Task<ActionResult<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>> DetalleDocente(int cargaAcademicaId, [FromQuery] int periodoId,
        [FromQuery] string? carreraId, CancellationToken ct) =>
        Ok(await consultas.ObtenerDetalleGestionAsync(UsuarioId, Roles, carreraId, periodoId, cargaAcademicaId, null, null, ct));

    [HttpGet("tutores/{grupoId:int}")]
    [ProducesResponseType<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>(200)]
    public async Task<ActionResult<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>> DetalleTutor(int grupoId, [FromQuery] int periodoId,
        [FromQuery] string? carreraId, CancellationToken ct) =>
        Ok(await consultas.ObtenerDetalleGestionAsync(UsuarioId, Roles, carreraId, periodoId, null, grupoId, null, ct));

    [HttpGet("tutores/estadias/{tutorDocenteId:int}")]
    [ProducesResponseType<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>(200)]
    public async Task<ActionResult<UniEvalUtsc.Contracts.Dtos.ResultadoGestionDto>> DetalleTutorEstadias(int tutorDocenteId, [FromQuery] int periodoId,
        [FromQuery] string? carreraId, CancellationToken ct) =>
        Ok(await consultas.ObtenerDetalleGestionAsync(UsuarioId, Roles, carreraId, periodoId, null, null, tutorDocenteId, ct));
}

[ApiController]
[Route("api/seguimiento")]
[Authorize(Roles = "Coordinador,Administrador")]
public sealed class SeguimientoController(ConsultarResultadosService consultas) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<UniEvalUtsc.Contracts.Dtos.GrupoSeguimientoDto>>(200)]
    public async Task<IActionResult> Obtener([FromQuery] int periodoId, [FromQuery] string? carreraId,
        [FromQuery] int? grupoId, CancellationToken ct)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        return Ok(await consultas.ObtenerSeguimientoAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, roles,
            carreraId, periodoId, carreraId, grupoId, ct));
    }
}

[ApiController]
[Route("api/recordatorios")]
[Authorize(Roles = "Coordinador,Administrador")]
public sealed class RecordatoriosController(MensajesService mensajes) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<UniEvalUtsc.Contracts.Dtos.RecordatorioCreadoDto>(200)]
    public async Task<IActionResult> Crear([FromBody] UniEvalUtsc.Contracts.Dtos.RecordatorioRequestDto request, CancellationToken ct)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        return Ok(await mensajes.CrearRecordatorioAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, roles, request.GrupoId, ct));
    }
}

[ApiController]
[Route("api/mis-mensajes")]
[Authorize(Policy = "Alumno")]
public sealed class MisMensajesController(MensajesService mensajes) : ControllerBase
{
    private string AlumnoId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<UniEvalUtsc.Contracts.Dtos.MensajeDto>>(200)]
    public async Task<IActionResult> Obtener(CancellationToken ct) => Ok(await mensajes.ObtenerMisMensajesAsync(AlumnoId, ct));

    [HttpPut("{id:guid}/leido")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> MarcarLeido(Guid id, CancellationToken ct)
    {
        await mensajes.MarcarLeidoAsync(id, AlumnoId, ct);
        return NoContent();
    }

    [HttpPut("leidos")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> MarcarTodosLeidos(CancellationToken ct)
    {
        await mensajes.MarcarTodosLeidosAsync(AlumnoId, ct);
        return NoContent();
    }
}
