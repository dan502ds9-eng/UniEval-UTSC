using System.Globalization;
using System.Text;
using UniEvalUtsc.Contracts.Enums;

namespace UniEvalUtsc.Web.Services;

public sealed class FaqInMemoryService : IFaqService
{
    private const string RespuestaSinCoincidencia =
        "No encontré una respuesta exacta. Elige una pregunta frecuente o reformula tu duda con palabras como anonimato, resultados, periodo o encuestas.";

    private static readonly IReadOnlyDictionary<Rol, IReadOnlyList<PreguntaFrecuente>> PreguntasPorRol =
        new Dictionary<Rol, IReadOnlyList<PreguntaFrecuente>>
        {
            [Rol.Alumno] =
            [
                Crear(
                    "¿Cuántas me faltan?",
                    "Consulta en Mis encuestas cuántas te faltan. Si tienes pendientes, abre la siguiente desde ahí; al terminar todas verás tu comprobante.",
                    "faltan", "pendientes", "encuestas", "siguiente", "comprobante"),
                Crear(
                    "¿Puedo calificar a todos en una pantalla?",
                    "Sí. Todas las preguntas van en una sola página: en cada pregunta aparecen tus docentes y tu tutor. Calificas del 1 al 10 y envías una sola vez.",
                    "todos", "pantalla", "página", "preguntas", "docentes", "tutor", "calificar"),
                Crear(
                    "¿Funciona con lector de pantalla?",
                    "Sí. Cada docente es un grupo de opciones del 1 al 10. Con Tab cambias de docente; con las flechas o con los números eliges la calificación (el 0 cuenta como 10) y el lector te avisa cuántos faltan. Los colores son solo un apoyo: el número siempre se lee.",
                    "lector", "pantalla", "accesibilidad", "tab", "flechas", "teclado"),
                Crear(
                    "¿Puedo corregir después?",
                    "No. Una vez enviada, la encuesta ya no se puede modificar. Revisa tus números antes de confirmar.",
                    "corregir", "modificar", "cambiar", "después", "enviada"),
                Crear(
                    "¿Son anónimas?",
                    "Sí. Guardamos que ya contestaste, pero tus calificaciones no quedan ligadas a tu matrícula. Ni tu docente ni tu coordinador pueden ver qué calificaste.",
                    "anónimas", "anonimato", "privacidad", "matrícula", "quién", "respuestas"),
                Crear(
                    "¿Hasta cuándo puedo contestar?",
                    "Puedes contestar mientras el periodo esté abierto. Una vez enviada, la encuesta ya no se puede modificar.",
                    "hasta cuándo", "fecha", "cierre", "plazo", "periodo", "abierto"),
                Crear(
                    "Ir a mi siguiente encuesta",
                    "Abre Mis encuestas y selecciona la siguiente encuesta pendiente. Si ya contestaste todas, consulta tu comprobante.",
                    "siguiente", "abrir", "contestar", "encuesta", "pendiente")
            ],
            [Rol.Docente] =
            [
                Crear(
                    "¿Cuándo veo mis resultados?",
                    "Cuando el administrador cierre el periodo. Antes no se muestran, para cuidar el anonimato.",
                    "cuándo", "ver", "resultados", "cierre", "cerrar", "periodo"),
                Crear(
                    "¿Por qué no sale una materia?",
                    "Con menos de 5 respuestas no se muestran resultados, para proteger el anonimato de los alumnos.",
                    "materia", "no sale", "no aparece", "resultados", "respuestas", "cinco", "5", "anonimato"),
                Crear(
                    "¿Por qué piden verificación en dos pasos?",
                    RespuestaVerificacionDosPasos,
                    "verificación", "dos pasos", "código", "contraseña", "seguridad", "cuenta")
            ],
            [Rol.Tutor] =
            [
                Crear(
                    "¿Cuándo veo mis resultados?",
                    "Cuando el administrador cierre el periodo. Antes no se muestran.",
                    "cuándo", "ver", "resultados", "cierre", "cerrar", "periodo"),
                Crear(
                    "¿Qué se evalúa del tutor?",
                    "Siete aspectos del formato FORM-AC64, calificados de 1 a 10.",
                    "evalúa", "tutor", "aspectos", "FORM-AC64", "calificación", "siete"),
                Crear(
                    "¿Por qué piden verificación en dos pasos?",
                    RespuestaVerificacionDosPasos,
                    "verificación", "dos pasos", "código", "contraseña", "seguridad", "cuenta")
            ],
            [Rol.Coordinador] =
            [
                Crear(
                    "¿Puedo ver quién falta?",
                    "Sí. En cada grupo ves la lista de alumnos que no han contestado, sin ver sus respuestas.",
                    "quién falta", "alumnos", "pendientes", "grupo", "seguimiento"),
                Crear(
                    "¿Veo respuestas individuales?",
                    "No. Solo participación por grupo y promedios, nunca qué calificó cada alumno.",
                    "respuestas", "individuales", "calificó", "alumno", "anonimato", "promedios"),
                Crear(
                    "¿Cómo exporto un reporte?",
                    "Desde el tablero con el botón Exportar, en Excel o PDF con el encabezado institucional.",
                    "exportar", "reporte", "Excel", "PDF", "tablero"),
                Crear(
                    "¿Por qué piden verificación en dos pasos?",
                    RespuestaVerificacionDosPasos,
                    "verificación", "dos pasos", "código", "contraseña", "seguridad", "cuenta")
            ],
            [Rol.Administrador] =
            [
                Crear(
                    "¿Puedo ver qué contestó un alumno?",
                    "No. Las respuestas se guardan sin matrícula ni hora exacta. Ves participación y promedios, igual que el coordinador.",
                    "qué contestó", "respuestas", "alumno", "matrícula", "hora", "anonimato", "promedios"),
                Crear(
                    "¿Cómo abro las encuestas?",
                    "En Administración → Periodos, usa el botón Abrir aplicación. El sistema genera las encuestas de cada alumno: una por materia más una de tutor.",
                    "abrir", "encuestas", "periodos", "aplicación", "generar", "materia", "tutor"),
                Crear(
                    "¿Cómo cargo alumnos?",
                    "En Catálogos → Importar Excel/CSV. Te marcaré los errores por renglón para que corrijas el archivo.",
                    "cargar", "alumnos", "catálogos", "importar", "Excel", "CSV", "archivo"),
                Crear(
                    "¿Por qué piden verificación en dos pasos?",
                    RespuestaVerificacionDosPasos,
                    "verificación", "dos pasos", "código", "contraseña", "seguridad", "cuenta")
            ]
        };

    private const string RespuestaVerificacionDosPasos =
        "Porque tus resultados son sensibles. Además de tu contraseña de Microsoft 365 confirmas con un código; si alguien robara tu contraseña, no podría entrar.";

    public Task<IReadOnlyList<PreguntaFrecuente>> ObtenerPreguntasAsync(
        Rol rol,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ObtenerPorRol(rol));
    }

    public Task<string> ResponderAsync(
        Rol rol,
        string pregunta,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(pregunta);

        var consulta = Normalizar(pregunta);
        var palabrasConsulta = consulta
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        var coincidencia = ObtenerPorRol(rol)
            .Select(item => new
            {
                Pregunta = item,
                Coincidencias = item.PalabrasClave
                    .SelectMany(clave => Normalizar(clave).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .Distinct(StringComparer.Ordinal)
                    .Count(palabrasConsulta.Contains),
                CoincidenciaExacta = Normalizar(item.Pregunta) == consulta
            })
            .OrderByDescending(item => item.CoincidenciaExacta)
            .ThenByDescending(item => item.Coincidencias)
            .FirstOrDefault();

        return Task.FromResult(
            coincidencia is not null && (coincidencia.CoincidenciaExacta || coincidencia.Coincidencias > 0)
                ? coincidencia.Pregunta.Respuesta
                : RespuestaSinCoincidencia);
    }

    private static IReadOnlyList<PreguntaFrecuente> ObtenerPorRol(Rol rol) =>
        PreguntasPorRol.TryGetValue(rol, out var preguntas)
            ? preguntas
            : throw new ArgumentOutOfRangeException(nameof(rol), rol, "El rol no está soportado.");

    private static PreguntaFrecuente Crear(string pregunta, string respuesta, params string[] palabrasClave) =>
        new(pregunta, respuesta, palabrasClave);

    private static string Normalizar(string valor)
    {
        var descompuesto = valor.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        var separadorPendiente = false;

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caracter))
            {
                if (separadorPendiente && resultado.Length > 0)
                {
                    resultado.Append(' ');
                }

                resultado.Append(char.ToLowerInvariant(caracter));
                separadorPendiente = false;
            }
            else
            {
                separadorPendiente = true;
            }
        }

        return resultado.ToString();
    }
}
