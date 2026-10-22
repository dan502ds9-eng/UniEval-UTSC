# Decisiones de diseño — respuesta a la sección 4 de `docs/proceso/PLAN.md`

Este documento **manda sobre el PDF y sobre la demo** cuando haya diferencias. `AGENTS.md` lo referencia.
Equipo 2 · 21 oct 2026.

## D-01 · Evaluación unificada de 5to (pantalla única)
- **Estado: pendiente de validar con subdirección. Cambia el formato institucional: el tutor de 5to se evalúa con FORM-AC16 en lugar de FORM-AC64.**
- Es **solo una forma de presentar** las encuestas. El modelo de datos del PDF **no cambia**: al abrir el periodo se crea una `encuesta_asignada` por cada materia del grupo (RN-02, RN-03) y una de tutor (RN-04). Ejemplo DSM05AV: 7 + 1 = 8 asignaciones.
- `/alumno/encuesta` carga **todas** las asignaciones pendientes del alumno en el periodo abierto y las muestra como la demo: cada aspecto del FORM-AC16 con una fila por docente y la del tutor al final.
- La fila del tutor usa el **mismo texto de pregunta** (aspecto AC16) que los docentes, como pidió el equipo, aunque su asignación apunte a la plantilla FORM-AC64. En la API se resuelve así: la asignación de tutor de 5to usa la plantilla FORM-AC16 vigente. FORM-AC64 queda para estadías (D-02). Si la subdirección pide AC64 para el tutor de 5to, solo cambia la plantilla de esa asignación.
- **Envío:** un solo botón "Enviar evaluación". El cliente manda **un POST por lote**, `POST /api/mis-encuestas/lote/respuestas` con `[{ encuestaAsignadaId, respuestas:[{aspectoId, calificacion}] }]`. La API procesa todas en **una transacción** (todo o nada, RNF-07): por cada asignación crea una `respuesta` anónima (UUID, sin alumno ni hora) y marca la asignación como contestada. Si alguna falla, no se guarda ninguna.
- Se mantiene `POST /api/mis-encuestas/{id}/respuestas` (una sola asignación) tal como lo define el PDF, para compatibilidad y pruebas.

## D-02 · Estadías (6to y 10mo)
- **No hay sexto rol ni perfil aparte.** "Alumno en estadías" no existe en el selector de acceso. Si queda código de la demo que lo menciona, se ignora.
- Nueva tabla `asignacion_estadia (alumno_id, periodo_id, tutor_docente_id)`, única por alumno y periodo. La captura el Administrador, por alumno o con "asignar a todo el grupo".
- Al **abrir el periodo**: los grupos marcados como estadía (`grupo.es_estadia = true`) no generan encuestas docentes. Por cada alumno con asignación se crea **una** `EncuestaAsignada.ParaTutor(...)` con la plantilla **FORM-AC64** y el tutor de estadías. Los alumnos sin tutor asignado no reciben encuesta y aparecen en el aviso del Administrador.
- Para el alumno, la pantalla es la misma `/alumno/encuesta`, con 7 aspectos × 1 evaluado.

## D-03 · Comprobante y anonimato
- El comprobante muestra **solo**: nombre del alumno, periodo, "N de N encuestas contestadas" y la **fecha (día)** en que completó la última.
- Esa fecha sale de `encuesta_asignada.fecha_contestada` (tipo `DateOnly`, como ya está en el dominio). Esa tabla dice **quién** contestó, no **qué**, y el PDF la permite.
- `respuesta` y `respuesta_detalle` **nunca** guardan fecha, hora ni alumno (RN-09). No hay vínculo entre ambas tablas.
- Endpoint: `GET /api/mis-encuestas/comprobante`. Responde 404 si aún hay pendientes.

## D-04 · Identificador de encuesta en la ruta
- `/alumno/encuesta` **no lleva id**: siempre trabaja con el conjunto de asignaciones pendientes del periodo abierto (D-01).
- Al recargar la página: se vuelven a pedir las asignaciones a la API y se restaura el borrador de `localStorage` (clave por alumno y periodo). Las calificaciones de asignaciones que ya no estén pendientes se descartan.
- Si no hay pendientes, redirige a `/alumno/comprobante`.

## D-05 · Confirmación (P-04)
- Es un **diálogo modal** dentro de `/alumno/encuesta`, sin ruta propia (en celular: hoja inferior). Captura el foco, Esc cierra y el foco regresa a "Enviar evaluación".

## D-06 · Mensajes y recordatorios
- Entran en la **primera entrega**, con API:
  - `POST /api/recordatorios` `{ grupoId }` — Coordinador (solo grupos de su carrera) y Administrador. Crea un mensaje para cada alumno del grupo **con encuestas pendientes**.
  - `GET /api/mis-mensajes` — Alumno.
  - `PUT /api/mis-mensajes/{id}/leido` y `PUT /api/mis-mensajes/leidos` (todos).
- Tabla `mensaje (id, alumno_id, remitente, titulo, cuerpo, creado_el, leido_el)`. No guarda nada de las respuestas.
- Mientras la API no exista, el cliente usa un servicio simulado con los mismos DTOs.

## D-07 · Resultados propios (docente y tutor)
- `GET /api/mis-resultados?periodoId=&cargaAcademicaId=` — `cargaAcademicaId` identifica materia y grupo del docente. Para el tutor se usa `grupoId=`.
- `GET /api/mis-resultados/resumen?periodoId=` — tarjetas de Inicio, con todas sus materias o grupos.
- `GET /api/mis-resultados/historico?cargaAcademicaId=` (o `grupoId=`) — promedio general por periodo **cerrado**.
- La respuesta incluye: promedio general, nivel, respuestas / alumnos del grupo, promedio de la carrera y, por aspecto, `{ aspectoId, texto, textoCorto, promedio, promedioCarrera }`.
- RN-10: con menos de 5 respuestas se responde solo `{ oculto: true, respuestas: n }`, sin promedios.
- RN-11: el periodo actual sin cerrar responde `{ disponible: false }`.

## D-08 · Resultados de docentes y de tutores (coordinador y administrador)
- Dos pantallas y dos rutas separadas, como la demo: `/resultados-docentes` y `/resultados-tutores`.
- API: `GET /api/resultados/docentes?periodoId=&carreraId=&cuatrimestre=` y `GET /api/resultados/tutores?...`. El detalle va en `GET /api/resultados/docentes/{cargaAcademicaId}` y `GET /api/resultados/tutores/{grupoId}` (o `/estadias/{tutorDocenteId}`).
- Filtro de cuatrimestre: `6` o `10` devuelven docentes vacíos y solo tutores de estadías de ese cuatrimestre.
- Para el Coordinador, la API **ignora** el `carreraId` enviado y usa la carrera de su cuenta.

## D-09 · Reportes por rol
- Docente y Tutor **sí** exportan, pero **solo lo suyo**: `GET /api/mis-resultados/exportar?periodoId=&cargaAcademicaId=&formato=pdf|xlsx`.
- Coordinador y Administrador: `GET /api/resultados/exportar?tipo=docente|tutor|concentrado|participacion|historico&periodoId=&carreraId=&formato=pdf|xlsx`.
- RN-10 aplica también a las exportaciones.

## D-10 · Formatos de exportación
- **xlsx y pdf** (RF-16). El encabezado institucional (código, revisión y fecha del formato) es **configurable** en `appsettings` mientras se definen, como pide el PDF.

## D-11 · Importación masiva
- `POST /api/importar/alumnos` y `POST /api/importar/carga` con `multipart/form-data` (archivo `.xlsx` o `.csv`, máximo 5 MB).
- Respuesta: `{ renglones, importados, errores: [{ renglon, campo, mensaje }] }`. Si hay errores, **no** se importa nada (todo o nada) y el cliente muestra la lista.
- `GET /api/importar/plantilla/{tipo}` descarga el archivo de ejemplo.

## D-12 · Plantillas
- `GET /api/plantillas` (vigentes y borradores) y `GET /api/plantillas/{id}`.
- `POST /api/plantillas/{id}/borrador` crea un borrador (versión n+1) copiando la vigente. Si ya existe un borrador, lo devuelve.
- `PUT /api/plantillas/{id}/aspectos` reemplaza la **lista ordenada completa** de aspectos del borrador (con eso se cubren editar, agregar, eliminar y reordenar). Solo funciona en borradores, como ya valida `Plantilla.AgregarAspecto`.
- `POST /api/plantillas/{id}/publicar` vuelve vigente el borrador y deja la anterior como histórica. Aplica **desde el siguiente periodo que se abra** (RN-12): cada encuesta_asignada guarda su plantilla_id al abrir el periodo (un periodo usa AC16 y AC64 a la vez); no existe periodo.plantilla_id.
- `DELETE /api/plantillas/{id}` solo para borradores ("Descartar cambios").

## D-13 · Periodos
- `GET /api/periodos`, `GET /api/periodos/{id}`, `POST /api/periodos` y `PUT /api/periodos/{id}`. Las fechas solo se editan **en preparación**.
- `POST /api/periodos/{id}/abrir` **genera las asignaciones** (D-01 y D-02) en una transacción y devuelve cuántas creó. `POST /api/periodos/{id}/cerrar` cierra la captura y libera resultados (RN-11).
- Solo puede haber **un periodo abierto** a la vez.

## D-14 · Umbral de anonimato
- El mínimo de **5 respuestas** (RN-10) aplica **igual a docentes y tutores**, en pantallas, gráficas, exportaciones y API. Por debajo de 5 nunca se envían promedios ni detalles por aspecto, solo `{ oculto: true, respuestas: n }`.
- Nota: en `PLAN.md` se cita "RN-12 para tutor", pero RN-12 es la de plantillas versionadas; la regla correcta es RN-10.
