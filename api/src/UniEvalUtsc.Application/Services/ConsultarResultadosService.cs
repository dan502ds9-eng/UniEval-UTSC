using UniEvalUtsc.Application.Exceptions;
using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Contracts;
using UniEvalUtsc.Contracts.Dtos;
using UniEvalUtsc.Domain.Entities;
using UniEvalUtsc.Domain.Exceptions;
using EstadoEncuesta = UniEvalUtsc.Domain.Enums.EstadoEncuesta;
using EstadoPeriodo = UniEvalUtsc.Domain.Enums.EstadoPeriodo;
using TipoEncuesta = UniEvalUtsc.Domain.Enums.TipoEncuesta;
using Rol = UniEvalUtsc.Contracts.Enums.Rol;
using Nivel = UniEvalUtsc.Contracts.Enums.Nivel;

namespace UniEvalUtsc.Application.Services;

public sealed class ConsultarResultadosService(IResultadosRepository repositorio, IUsuarioRepository usuarios)
{
    public async Task<object> ObtenerPropioAsync(
        string usuarioId, IReadOnlyCollection<string> roles, string? carreraId,
        int periodoId, int? cargaAcademicaId, int? grupoId, CancellationToken ct = default)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        var datos = await repositorio.ObtenerDatosAsync(ct);
        var periodo = ObtenerPeriodo(datos, periodoId);
        if (periodo.Estado != EstadoPeriodo.Cerrado) return new ResultadoNoDisponibleDto(false);

        if (cargaAcademicaId is int cargaId)
        {
            if (!roles.Contains(nameof(Rol.Docente)) || !(usuario.CargasAcademicasId ?? []).Contains(cargaId))
                throw new AccesoDenegadoException("No tienes acceso a esta carga académica.");
            var carga = datos.Cargas.SingleOrDefault(x => x.Id == cargaId)
                ?? throw new NoEncontradoException("La carga académica no existe.");
            ValidarCarrera(carreraId, datos.Grupos.Single(x => x.Id == carga.GrupoId).CarreraId);
            return CalcularPersonal(datos, periodo, TipoEncuesta.Docente, cargaAcademicaId: cargaId);
        }

        if (grupoId is int grupoTutorId)
        {
            if (!roles.Contains(nameof(Rol.Tutor)) || !(usuario.GruposTutorId ?? []).Contains(grupoTutorId))
                throw new AccesoDenegadoException("No tienes acceso a este grupo de tutoría.");
            var grupo = datos.Grupos.SingleOrDefault(x => x.Id == grupoTutorId)
                ?? throw new NoEncontradoException("El grupo no existe.");
            ValidarCarrera(carreraId, grupo.CarreraId);
            return CalcularPersonal(datos, periodo, TipoEncuesta.Tutor, grupoId: grupoTutorId);
        }

        throw new ValidacionException("Debes indicar cargaAcademicaId o grupoId.");
    }

    public async Task<object> ObtenerResumenPropioAsync(
        string usuarioId, IReadOnlyCollection<string> roles, int periodoId, CancellationToken ct = default)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        var datos = await repositorio.ObtenerDatosAsync(ct);
        var periodo = ObtenerPeriodo(datos, periodoId);
        if (periodo.Estado != EstadoPeriodo.Cerrado) return new ResultadoNoDisponibleDto(false);
        var resultados = new List<object>();
        if (roles.Contains(nameof(Rol.Docente)))
            resultados.AddRange((usuario.CargasAcademicasId ?? []).Distinct().Select(id => CalcularPersonal(datos, periodo, TipoEncuesta.Docente, cargaAcademicaId: id)));
        if (roles.Contains(nameof(Rol.Tutor)))
            resultados.AddRange((usuario.GruposTutorId ?? []).Distinct().Select(id => CalcularPersonal(datos, periodo, TipoEncuesta.Tutor, grupoId: id)));
        return new { disponible = true, resultados };
    }

    public async Task<IReadOnlyCollection<object>> ObtenerHistoricoPropioAsync(
        string usuarioId, IReadOnlyCollection<string> roles, string? carreraId,
        int? cargaAcademicaId, int? grupoId, CancellationToken ct = default)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        var datos = await repositorio.ObtenerDatosAsync(ct);
        TipoEncuesta tipo;
        int? carga = null;
        int? grupo = null;
        if (cargaAcademicaId is int cargaId)
        {
            if (!roles.Contains(nameof(Rol.Docente)) || !(usuario.CargasAcademicasId ?? []).Contains(cargaId))
                throw new AccesoDenegadoException("No tienes acceso a esta carga académica.");
            carga = cargaId;
            tipo = TipoEncuesta.Docente;
            var datosCarga = datos.Cargas.SingleOrDefault(x => x.Id == cargaId)
                ?? throw new NoEncontradoException("La carga académica no existe.");
            ValidarCarrera(carreraId, datos.Grupos.Single(x => x.Id == datosCarga.GrupoId).CarreraId);
        }
        else if (grupoId is int grupoIdTutor)
        {
            if (!roles.Contains(nameof(Rol.Tutor)) || !(usuario.GruposTutorId ?? []).Contains(grupoIdTutor))
                throw new AccesoDenegadoException("No tienes acceso a este grupo de tutoría.");
            grupo = grupoIdTutor;
            tipo = TipoEncuesta.Tutor;
            var datosGrupo = datos.Grupos.SingleOrDefault(x => x.Id == grupoIdTutor)
                ?? throw new NoEncontradoException("El grupo no existe.");
            ValidarCarrera(carreraId, datosGrupo.CarreraId);
        }
        else throw new ValidacionException("Debes indicar cargaAcademicaId o grupoId.");

        return datos.Periodos.Where(p => p.Estado == EstadoPeriodo.Cerrado).OrderBy(p => p.FechaFin)
            .Select(p =>
            {
                var resultado = Calcular(datos, p, tipo, carga, grupo);
                return resultado is ResultadoCalculado visible
                    ? new PuntoHistorico(p.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Nombre,
                        visible.PromedioGeneral, visible.Nivel.ToString())
                    : resultado;
            }).ToArray();
    }

    public async Task<IReadOnlyCollection<object>> ObtenerResultadosGestionAsync(
        string usuarioId, IReadOnlyCollection<string> roles, string? carreraSolicitada,
        int? periodoId, int? cuatrimestre, bool tutores, CancellationToken ct = default)
    {
        var carrera = await CarreraEfectivaAsync(usuarioId, roles, carreraSolicitada, ct);
        var datos = await repositorio.ObtenerDatosAsync(ct);
        var periodos = periodoId is int id ? [ObtenerPeriodo(datos, id)] : datos.Periodos.Where(x => x.Estado == EstadoPeriodo.Cerrado).ToArray();
        var grupos = datos.Grupos.Where(g => carrera is null || g.CarreraId == carrera)
            .Where(g => cuatrimestre is null || g.Cuatrimestre == cuatrimestre)
            .Where(g => tutores || !g.EsEstadia)
            .Where(g => !tutores || (cuatrimestre is 6 or 10 ? g.EsEstadia : !g.EsEstadia));
        var resultados = new List<object>();
        foreach (var periodo in periodos)
        foreach (var grupo in grupos)
        {
            if (tutores)
            {
                resultados.Add(MapearGestion(Calcular(datos, periodo, TipoEncuesta.Tutor, grupoId: grupo.Id),
                    datos, periodo, grupo, null, TipoEncuesta.Tutor));
                continue;
            }
            foreach (var carga in datos.Cargas.Where(c => c.GrupoId == grupo.Id))
                resultados.Add(MapearGestion(Calcular(datos, periodo, TipoEncuesta.Docente, cargaAcademicaId: carga.Id),
                    datos, periodo, grupo, carga, TipoEncuesta.Docente));
        }
        return resultados;
    }

    public async Task<object> ObtenerDetalleGestionAsync(
        string usuarioId, IReadOnlyCollection<string> roles, string? carreraSolicitada,
        int periodoId, int? cargaAcademicaId, int? grupoId, int? tutorDocenteId, CancellationToken ct = default)
    {
        var carrera = await CarreraEfectivaAsync(usuarioId, roles, carreraSolicitada, ct);
        var datos = await repositorio.ObtenerDatosAsync(ct);
        var periodo = ObtenerPeriodo(datos, periodoId);
        if (cargaAcademicaId is int cargaId)
        {
            var carga = datos.Cargas.SingleOrDefault(x => x.Id == cargaId) ?? throw new NoEncontradoException("La carga académica no existe.");
            ValidarCarrera(carrera, datos.Grupos.Single(x => x.Id == carga.GrupoId).CarreraId);
            return MapearGestion(Calcular(datos, periodo, TipoEncuesta.Docente, cargaAcademicaId: cargaId), datos,
                periodo, datos.Grupos.Single(g => g.Id == carga.GrupoId), carga, TipoEncuesta.Docente);
        }
        if (grupoId is int tutorGrupoId)
        {
            var grupo = datos.Grupos.SingleOrDefault(x => x.Id == tutorGrupoId) ?? throw new NoEncontradoException("El grupo no existe.");
            ValidarCarrera(carrera, grupo.CarreraId);
            return MapearGestion(Calcular(datos, periodo, TipoEncuesta.Tutor, grupoId: tutorGrupoId), datos,
                periodo, grupo, null, TipoEncuesta.Tutor);
        }
        if (tutorDocenteId is int tutorId)
        {
            var gruposTutor = datos.Grupos.Where(g => g.EsEstadia && g.TutorEvaluadoId == tutorId && (carrera is null || g.CarreraId == carrera)).ToArray();
            if (gruposTutor.Length == 0) throw new AccesoDenegadoException("El tutor de estadías no pertenece a la carrera autorizada.");
            var gruposIds = gruposTutor.Select(g => g.Id).ToHashSet();
            var respuestas = datos.Respuestas.Where(r => r.PeriodoId == periodoId && r.Tipo == TipoEncuesta.Tutor
                && r.TutorEvaluadoId == tutorId && gruposIds.Contains(r.GrupoId)).ToArray();
            return MapearGestion(CalcularDesdeRespuestas(datos, periodo, respuestas, gruposTutor.Sum(g => g.TotalAlumnos),
                gruposTutor[0].CarreraId), datos, periodo, gruposTutor[0], null, TipoEncuesta.Tutor, tutorId);
        }
        throw new ValidacionException("Debes indicar el identificador del evaluado.");
    }

    public async Task<IReadOnlyCollection<GrupoSeguimientoDto>> ObtenerSeguimientoAsync(
        string usuarioId, IReadOnlyCollection<string> roles, string? carreraSolicitada,
        int periodoId, string? carreraId, int? grupoId, CancellationToken ct = default)
    {
        var carrera = await CarreraEfectivaAsync(usuarioId, roles, carreraId ?? carreraSolicitada, ct);
        var datos = await repositorio.ObtenerDatosAsync(ct);
        _ = ObtenerPeriodo(datos, periodoId);
        var todosUsuarios = await usuarios.ObtenerTodosAsync(ct);
        var grupos = datos.Grupos.Where(g => carrera is null || g.CarreraId == carrera)
            .Where(g => grupoId is null || g.Id == grupoId)
            .ToArray();
        if (grupoId is int solicitado && grupos.Length == 0 && datos.Grupos.Any(g => g.Id == solicitado))
            throw new AccesoDenegadoException("El grupo no pertenece a la carrera autorizada.");
        return grupos.Select(grupo =>
        {
            var asignaciones = datos.Encuestas.Where(e => e.GrupoId == grupo.Id && (e.PeriodoId == periodoId || e.PeriodoId == 0)).ToArray();
            var pendientes = asignaciones.Where(e => e.Estado == EstadoEncuesta.Pendiente).Select(e => e.AlumnoId).Distinct().ToArray();
            var respondieron = asignaciones.GroupBy(e => e.AlumnoId)
                .Count(alumno => alumno.Any(e => e.Estado == EstadoEncuesta.Contestada)
                    && alumno.All(e => e.Estado == EstadoEncuesta.Contestada));
            var faltantes = todosUsuarios.Where(u => pendientes.Contains(int.TryParse(u.Id, out var value) ? value : -1))
                .Select(u => new AlumnoPendienteDto(u.Nombre, roles.Contains(nameof(Rol.Administrador)) ? u.Correo : Enmascarar(u.Correo))).ToArray();
            var total = Math.Max(grupo.TotalAlumnos, asignaciones.Select(e => e.AlumnoId).Distinct().Count());
            return new GrupoSeguimientoDto(grupo.Id, grupo.Nombre, grupo.CarreraId, grupo.Cuatrimestre,
                total, respondieron, total == 0 ? 0 : Niveles.Redondear(respondieron * 100m / total), faltantes);
        }).ToArray();
    }

    private async Task<string?> CarreraEfectivaAsync(string usuarioId, IReadOnlyCollection<string> roles, string? solicitada, CancellationToken ct)
    {
        if (roles.Contains(nameof(Rol.Coordinador)))
            return (await ObtenerUsuarioAsync(usuarioId, ct)).CarreraId
                ?? throw new AccesoDenegadoException("La cuenta coordinadora no tiene carrera asignada.");
        return roles.Contains(nameof(Rol.Administrador)) ? solicitada : throw new AccesoDenegadoException("No tienes acceso a estos resultados.");
    }

    private static object Calcular(DatosResultados datos, Periodo periodo, TipoEncuesta tipo, int? cargaAcademicaId = null, int? grupoId = null)
    {
        var respuestas = datos.Respuestas.Where(r => r.PeriodoId == periodo.Id && r.Tipo == tipo
            && (cargaAcademicaId is null || r.CargaAcademicaId == cargaAcademicaId)
            && (grupoId is null || r.GrupoId == grupoId)).ToArray();
        var grupo = cargaAcademicaId is int carga
            ? datos.Grupos.Single(g => g.Id == datos.Cargas.Single(c => c.Id == carga).GrupoId)
            : datos.Grupos.Single(g => g.Id == grupoId);
        return CalcularDesdeRespuestas(datos, periodo, respuestas, grupo.TotalAlumnos, grupo.CarreraId);
    }

    private static object CalcularDesdeRespuestas(DatosResultados datos, Periodo periodo,
        IReadOnlyCollection<Respuesta> respuestas, int totalAlumnos, string carreraId)
    {
        var numeroRespuestas = respuestas.Select(r => r.Id).Distinct().Count();
        if (numeroRespuestas < ReglasNegocio.MinimoRespuestas)
            return new ResultadoOcultoDto(true, numeroRespuestas);

        var detalles = respuestas.SelectMany(r => r.Detalles.Select(d => (Respuesta: r, Detalle: d))).ToArray();
        var aspectos = detalles.GroupBy(x => x.Detalle.AspectoId).OrderBy(x => x.Key).Select(agrupacion =>
        {
            var promedio = Niveles.Redondear(agrupacion.Average(x => (decimal)x.Detalle.Calificacion));
            var plantilla = datos.Plantillas.FirstOrDefault(p => p.Id == agrupacion.First().Respuesta.PlantillaId);
            var aspecto = plantilla?.Aspectos.FirstOrDefault(a => a.Id == agrupacion.Key);
            var texto = aspecto?.Texto ?? $"Aspecto {agrupacion.Key}";
            var textoCorto = texto.Length > 48 ? $"{texto[..45]}…" : texto;
            return new AspectoResultado(agrupacion.Key, texto, textoCorto, promedio, promedio, Niveles.Nivel(promedio).ToString());
        }).ToArray();
        var promedioGeneral = Niveles.Redondear(detalles.Average(x => (decimal)x.Detalle.Calificacion));
        var promedioCarrera = promedioGeneral;
        if (respuestas.Count > 0)
        {
            var tipo = respuestas.First().Tipo;
            var respuestasCarrera = datos.Respuestas.Where(r => r.PeriodoId == periodo.Id && r.Tipo == tipo
                && datos.Grupos.Any(g => g.Id == r.GrupoId && g.CarreraId == carreraId));
            var detallesCarrera = respuestasCarrera.SelectMany(r => r.Detalles).ToArray();
            if (detallesCarrera.Length > 0)
                promedioCarrera = Niveles.Redondear(detallesCarrera.Average(x => (decimal)x.Calificacion));
        }
        return new ResultadoCalculado(numeroRespuestas, promedioGeneral, Niveles.Nivel(promedioGeneral),
            totalAlumnos, promedioCarrera, aspectos);
    }

    private static object CalcularPersonal(DatosResultados datos, Periodo periodo, TipoEncuesta tipo,
        int? cargaAcademicaId = null, int? grupoId = null)
    {
        var resultado = Calcular(datos, periodo, tipo, cargaAcademicaId, grupoId);
        if (resultado is not ResultadoCalculado visible) return resultado;
        var carga = cargaAcademicaId is int id ? datos.Cargas.Single(c => c.Id == id) : null;
        var grupo = carga is null ? datos.Grupos.Single(g => g.Id == grupoId) : datos.Grupos.Single(g => g.Id == carga.GrupoId);
        var elementos = new ElementoResultado(
            carga?.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? grupo.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            carga?.NombreMateria ?? (grupo.EsEstadia ? "Tutoría de estadías" : "Tutoría"),
            grupo.Nombre,
            visible.Respuestas,
            visible.Alumnos);
        var periodoDto = new PeriodoResultado(periodo.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            periodo.Nombre, periodo.Estado == EstadoPeriodo.Abierto, periodo.Estado == EstadoPeriodo.Cerrado);
        return new ResultadoPersonal(elementos, periodoDto, visible.PromedioGeneral, visible.Respuestas,
            visible.Alumnos, visible.PromedioCarrera, visible.Aspectos);
    }

    private static object MapearGestion(object resultado, DatosResultados datos, Periodo periodo,
        GrupoResultadoDatos grupo, CargaResultadoDatos? carga, TipoEncuesta tipo, int? tutorId = null)
    {
        if (resultado is not ResultadoCalculado visible) return resultado;
        return new ResultadoCoordinacion(
            carga?.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? tutorId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? grupo.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            grupo.CarreraId,
            tipo == TipoEncuesta.Docente ? carga!.NombreDocente : grupo.EsEstadia ? "Tutor de estadías" : "Tutor de grupo",
            tipo == TipoEncuesta.Docente ? $"{carga!.NombreMateria} · {grupo.Nombre}" : grupo.Nombre,
            periodo.Nombre,
            grupo.Cuatrimestre,
            visible.PromedioGeneral,
            visible.PromedioCarrera,
            visible.Respuestas,
            visible.Aspectos,
            tipo == TipoEncuesta.Tutor,
            tipo == TipoEncuesta.Tutor && grupo.EsEstadia);
    }

    private async Task<UsuarioSistema> ObtenerUsuarioAsync(string id, CancellationToken ct) =>
        await usuarios.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("No se encontró el usuario.");

    private static Periodo ObtenerPeriodo(DatosResultados datos, int id) =>
        datos.Periodos.SingleOrDefault(p => p.Id == id) ?? throw new NoEncontradoException("No se encontró el periodo.");

    private static void ValidarCarrera(string? carreraAutorizada, string carreraRecurso)
    {
        if (carreraAutorizada is not null && !string.Equals(carreraAutorizada, carreraRecurso, StringComparison.Ordinal))
            throw new AccesoDenegadoException("El recurso pertenece a otra carrera.");
    }

    private static string Enmascarar(string correo)
    {
        var separador = correo.IndexOf('@');
        return separador <= 0 ? "d***@utsc.edu.mx" : $"d***{correo[separador..]}";
    }

    private sealed record ResultadoCalculado(int Respuestas, decimal PromedioGeneral,
        Nivel Nivel, int Alumnos, decimal PromedioCarrera, IReadOnlyList<AspectoResultado> Aspectos);
}
