using UniEvalUtsc.Application.Exceptions;
using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Contracts.Dtos;
using Rol = UniEvalUtsc.Contracts.Enums.Rol;
using EstadoEncuesta = UniEvalUtsc.Domain.Enums.EstadoEncuesta;

namespace UniEvalUtsc.Application.Services;

public sealed class MensajesService(
    IMensajeRepository mensajes, IResultadosRepository resultados, IUsuarioRepository usuarios, IReloj reloj)
{
    public async Task<RecordatorioCreadoDto> CrearRecordatorioAsync(
        string remitenteId, IReadOnlyCollection<string> roles, int grupoId, CancellationToken ct = default)
    {
        if (!roles.Contains(nameof(Rol.Coordinador)) && !roles.Contains(nameof(Rol.Administrador)))
            throw new AccesoDenegadoException("Solo Coordinador o Administrador puede enviar recordatorios.");
        var datos = await resultados.ObtenerDatosAsync(ct);
        var grupo = datos.Grupos.SingleOrDefault(g => g.Id == grupoId)
            ?? throw new NoEncontradoException("El grupo no existe.");
        var remitente = await usuarios.ObtenerAsync(remitenteId, ct)
            ?? throw new NoEncontradoException("No se encontró el usuario.");
        if (roles.Contains(nameof(Rol.Coordinador)) && remitente.CarreraId != grupo.CarreraId)
            throw new AccesoDenegadoException("El grupo no pertenece a la carrera autorizada.");
        var periodoActual = datos.Periodos.Where(p => p.Estado == UniEvalUtsc.Domain.Enums.EstadoPeriodo.Abierto)
            .OrderByDescending(p => p.FechaFin).FirstOrDefault()
            ?? throw new NoEncontradoException("No hay un periodo abierto.");
        var alumnosPendientes = datos.Encuestas.Where(e => e.GrupoId == grupoId
                && (e.PeriodoId == periodoActual.Id || e.PeriodoId == 0) && e.Estado == EstadoEncuesta.Pendiente)
            .Select(e => e.AlumnoId.ToString(System.Globalization.CultureInfo.InvariantCulture)).Distinct(StringComparer.Ordinal).ToArray();
        var creados = await mensajes.CrearParaAlumnosAsync(alumnosPendientes, remitente.Nombre,
            "Encuestas pendientes", "Tienes encuestas de desempeño pendientes. Entra a UniEval UTSC para responderlas.", reloj.Ahora, ct);
        return new RecordatorioCreadoDto(creados);
    }

    public async Task<IReadOnlyCollection<MensajeDto>> ObtenerMisMensajesAsync(string alumnoId, CancellationToken ct = default) =>
        (await mensajes.ObtenerPorAlumnoAsync(alumnoId, ct)).Select(Mapear).ToArray();

    public async Task MarcarLeidoAsync(Guid id, string alumnoId, CancellationToken ct = default)
    {
        var mensaje = await mensajes.ObtenerPorIdAsync(id, ct) ?? throw new NoEncontradoException("El mensaje no existe.");
        if (!string.Equals(mensaje.AlumnoId, alumnoId, StringComparison.Ordinal))
            throw new AccesoDenegadoException("El mensaje pertenece a otro alumno.");
        await mensajes.MarcarLeidoAsync(id, alumnoId, reloj.Ahora, ct);
    }

    public Task MarcarTodosLeidosAsync(string alumnoId, CancellationToken ct = default) =>
        mensajes.MarcarTodosLeidosAsync(alumnoId, reloj.Ahora, ct);

    private static MensajeDto Mapear(MensajeSistema m) => new(m.Id, m.Remitente, m.Titulo, m.Cuerpo, m.CreadoEl, m.LeidoEl);
}
