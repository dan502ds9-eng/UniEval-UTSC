# Plan de pantallas, componentes y API

## Alcance y fuentes revisadas

- `AGENTS.md`: arquitectura, rutas previstas, roles, reglas de negocio, diseño y accesibilidad.
- `docs/diseno/prototipo/unieval-demo.html`: prototipo abierto y recorrido de acceso, evaluación del alumno y navegación por roles.
- `docs/diseno/Diseno_Sistema_Encuestas_Desempeno_UTSC.pdf`: catálogo P-01 a P-12, requisitos y sección 10 (API REST).

El directorio de controladores de `api/src/UniEvalUtsc.Api/` solo contiene `.gitkeep`; no hay endpoints implementados que deban tratarse como contrato vigente. Las rutas de este plan se basan en la sección 10 del PDF y son **propuestas por documentar e implementar**, no una descripción de una API ya disponible. Todas llevan el prefijo `/api`; el PDF indica que requieren el token de Microsoft 365. El modo simulado debe seguir siendo configurable y no guardar contraseñas.

## 1. Mapa de pantallas por rol

El inicio de sesión P-01 es común. `/` presenta el acceso y `/acceso/{rol}` permite el flujo de acceso por rol previsto en `AGENTS.md` (incluido el selector simulado mientras esté habilitado). En cada rol de personal, `inicio` es la página de entrada y el botón «Volver al inicio» debe regresar ahí, no a la ruta anterior.

| Rol | Ruta Blazor | Pantalla y alcance |
|---|---|---|
| Todos | `/` y `/acceso/{rol}` | **P-01 Acceso.** Inicio de sesión Microsoft 365; selector de rol solo en modo simulado. |
| Alumno | `/alumno` | **P-02 Mis encuestas.** Avance y tarjetas pendientes/contestadas de docentes y tutor. El prototipo presenta la evaluación unificada en una tarjeta. |
| Alumno | `/alumno/encuesta` | **P-03 Contestar encuesta.** Preguntas, escala 1–10, avance y borrador local. En quinto, una pregunta lista a cada docente y al tutor como última fila (10 preguntas × 8 evaluados = 80 calificaciones en el ejemplo de `AGENTS.md`). |
| Alumno | Dentro de `/alumno/encuesta` | **P-04 Confirmar envío.** Diálogo u hoja inferior con resumen y confirmación irreversible; no requiere ruta independiente en el diseño actual. |
| Alumno | `/alumno/comprobante` | **P-05 Encuestas completas.** Confirmación final y comprobante. |
| Alumno | Componente global, sin ruta definida | Mensajes/recordatorios y Correcaminos IA disponibles desde la navegación común. |
| Docente | `/docente/inicio` | Inicio del rol y resumen de sus resultados. |
| Docente | `/docente/resultados` | **P-06 Mis resultados.** Resultados propios por materia y grupo, promedios por aspecto y periodo. |
| Docente | `/docente/historico` | Histórico de resultados propios. |
| Docente | `/docente/reportes` | Reportes propios y exportación permitida. |
| Tutor | `/tutor/inicio` | Inicio del rol. |
| Tutor | `/tutor/resultados` | **P-06 Mis resultados.** Resultados propios de tutoría por grupo y periodo. |
| Tutor | `/tutor/historico` | Histórico de resultados propios. |
| Tutor | `/tutor/reportes` | Reportes propios y exportación permitida. |
| Coordinador | `/coordinador/inicio` | Inicio del rol, limitado a su carrera. |
| Coordinador | `/coordinador/seguimiento` | **P-07 Seguimiento.** Participación por grupo y alumnado pendiente; sin respuestas individuales. Filtro de carrera bloqueado a su carrera. |
| Coordinador | `/coordinador/resultados-docentes` | **P-08 Resultados de docentes.** Promedios y detalle agregado por aspecto. |
| Coordinador | `/coordinador/resultados-tutores` | Resultados agregados de tutores de su carrera. |
| Coordinador | `/coordinador/reportes` | Reportes de su carrera. |
| Administrador | `/admin/inicio` | Inicio global. |
| Administrador | `/admin/periodos` | **P-12 Periodos.** Crear, abrir y cerrar periodos. |
| Administrador | `/admin/carga` | **P-10 Carga académica.** Asignar materias/docentes y tutor a cada grupo. |
| Administrador | `/admin/estadias` | Asignar tutor de estadías por alumno o grupo; flujo añadido por `AGENTS.md` y presente en la demo. |
| Administrador | `/admin/catalogos` | **P-09 Catálogos.** Carreras, grupos, materias, docentes y alumnos; búsqueda, edición y carga masiva. |
| Administrador | `/admin/plantillas` | **P-11 Plantillas.** Aspectos AC16/AC64, orden y publicación versionada. |
| Administrador | `/admin/seguimiento` | **P-07 Seguimiento.** Participación global y alumnos pendientes, sin respuestas individuales. |
| Administrador | `/admin/resultados-docentes` | **P-08 Resultados de docentes**, con filtros globales y detalle agregado. |
| Administrador | `/admin/resultados-tutores` | Resultados agregados de tutores. |
| Administrador | `/admin/reportes` | Reportes y exportación global. |

`/docente/{seccion}`, `/tutor/{seccion}`, `/coordinador/{seccion}` y `/admin/{seccion}` son las plantillas de ruta prescritas por `AGENTS.md`; las rutas concretas anteriores usan los identificadores de navegación de la demo. El PDF no asigna identificadores de ruta.

## 2. Componentes reutilizables previstos

Los parámetros siguientes son contratos de diseño iniciales, no componentes implementados. Los eventos deben exponerse con `EventCallback`/`EventCallback<T>` y los controles de encuesta deben conservar teclado, foco visible y nombres accesibles.

### Estructura y navegación

| Componente | Parámetros previstos |
|---|---|
| `AppHeader` | `Usuario`, `Rol`, `Iniciales`, `MensajesSinLeer`, `TemaOscuro`, `TamañoTexto`, `OnTemaChanged`, `OnTamañoTextoChanged`, `OnMensajes`, `OnCerrarSesion`. |
| `TextSizeControl` | `Value` (100/125/150/200), `ValueChanged`. Persistencia en `localStorage`; a partir de 150% se aplican los ajustes responsivos de accesibilidad. |
| `ThemeToggle` | `IsDark`, `IsDarkChanged`. |
| `UserMenu` | `Nombre`, `Rol`, `Iniciales`, `OnCerrarSesion`. |
| `PersonalShell` | `Rol`, `SeccionActual`, `ItemsMenu`, `ChildContent`; navegación lateral en escritorio y pestañas desplazables en móvil. |
| `PageHeading` | `Titulo`, `Subtitulo` opcional; facilita foco del `h1` y actualización del título del documento al navegar. |
| `CorrecaminosChat` | `Rol`, `IsOpen`, `IsOpenChanged`, `PreguntasFrecuentes`, `Mensajes`, `OnEnviar`; región `role="log"` y manejo accesible de diálogo/foco. |
| `LiveStatus` | `Mensaje`, `Nivel`; anuncios de avance, validación y resultado mediante región viva. |

### Encuestas y resultados

| Componente | Parámetros previstos |
|---|---|
| `SurveyProgress` | `Completadas`, `Total`, `Label`; expone `role="progressbar"` y valores accesibles. |
| `SurveyAssignmentCard` | `Encuesta`, `Estado`, `OnContestar`; muestra materia/docente o tutor y estado. |
| `EvaluationQuestion` | `Aspecto`, `Evaluados`, `Calificaciones`, `Expandida`, `OnCalificacionChanged`, `OnExpandir`, `OnEscuchar`; admite docente(s) y tutor como última fila cuando aplique. |
| `RatingScale` | `Value`, `ValueChanged`, `LabelAccesible`, `Disabled`; valores enteros del 1 al 10, grupo de radios y texto de nivel. |
| `ConfirmSubmissionDialog` | `IsOpen`, `Resumen`, `OnConfirmar`, `OnCancelar`; captura y devuelve el foco, admite Escape. |
| `ResultFilterBar` | `Periodos`, `Periodo`, `Carreras`, `Carrera`, `Grupos`, `Grupo`, `Materia`, `CarreraBloqueada`, `OnChanged`. |
| `MetricCard` | `Etiqueta`, `Valor`, `Nota` opcional, `Nivel` opcional. |
| `AspectResultsChart` | `Aspectos`, `Comparativo` opcional, `Titulo`; alternativa textual accesible a la visualización. |
| `ResultsTable` | `Caption`, `Columnas`, `Filas`, `OnSeleccionar`; tabla con encabezados `scope="col"` y detalle agregado únicamente. |

### Administración

| Componente | Parámetros previstos |
|---|---|
| `CatalogTable` | `TipoCatalogo`, `Columnas`, `Filas`, `PuedeEditar`, `OnCrear`, `OnGuardar`, `OnBajaLogica`. |
| `ImportFilePanel` | `TipoImportacion` (alumnos/carga), `OnImportar`, `ResultadoPorRenglon`; incluye errores de validación de la carga. |
| `AcademicLoadEditor` | `Grupo`, `Asignaciones`, `OnGuardar`; edición de materias/docentes y tutoría del grupo. |
| `InternshipAssignmentEditor` | `Grupos`, `Alumnos`, `Tutores`, `AsignacionActual`, `OnGuardar`; selecciona alcance por grupo o alumno. |
| `TemplateEditor` | `Plantilla`, `Aspectos`, `Orden`, `Version`, `TipoFormato`, `OnGuardar`, `OnPublicar`; no modifica periodos existentes. |
| `PeriodEditor` | `Periodo`, `Estado`, `OnGuardar`, `OnAbrir`, `OnCerrar`. |

## 3. Endpoints necesarios por pantalla

Los nombres, métodos y parámetros de esta tabla reproducen el contrato de la sección 10 del PDF. Se indica dónde la pantalla necesita más información o comportamiento del que ese contrato detalla.

| Pantalla | Endpoints previstos |
|---|---|
| P-01 Acceso | `GET /api/auth/perfil` para validar el token y obtener nombre/roles. El acceso simulado no requiere autenticación real; el endpoint se usa en modo Microsoft 365. |
| P-02 Mis encuestas | `GET /api/mis-encuestas` (periodo abierto, asignaciones y estado). |
| P-03 Contestar encuesta | `GET /api/mis-encuestas/{id}` para instrucciones/aspectos y verificación de estado. El borrador se guarda localmente según el PDF; no se especifica endpoint de borrador. |
| P-04 Confirmar envío | `POST /api/mis-encuestas/{id}/respuestas`, cuerpo `{ "respuestas": [{ "aspectoId": 1, "calificacion": 9 }] }`. Validar atomicidad y errores: 400 por calificación/aspecto inválido o faltante, 403 por asignación ajena, 409 por envío previo o periodo cerrado. |
| P-05 Encuestas completas | No hay endpoint de comprobante en el PDF; la pantalla puede seguir al éxito del POST anterior, pero el contrato debe definir los datos del comprobante y cómo recuperar ese estado. |
| Inicio y P-06 Mis resultados (docente/tutor) | `GET /api/mis-resultados?periodo={id}` para el resumen de `/docente/inicio` o `/tutor/inicio`, resultados, histórico y reporte propio. El filtro de materia/grupo de P-06 no aparece en la ruta publicada; acordar parámetros y forma de respuesta. Respetar umbral mínimo de 5 y liberación al cierre. |
| Inicio y P-07 Seguimiento (coordinador/administrador) | `GET /api/seguimiento?periodo={id}&carrera={id}&grupo={id}` para el resumen de inicio y el seguimiento. La API debe hacer cumplir la carrera asignada del coordinador, independientemente del filtro enviado. |
| P-08 Resultados de docentes | `GET /api/resultados/docentes?periodo={id}&carrera={id}`; el detalle de aspecto y filtro por grupo/materia no se define completamente. |
| Resultados de tutores (coordinador/administrador) | `GET /api/resultados/tutores?periodo={id}&carrera={id}`; validar el alcance de carrera del coordinador. |
| Catálogos (administrador) | `GET/POST/PUT/DELETE /api/carreras`, `/api/grupos`, `/api/materias`, `/api/docentes`, `/api/alumnos` según el catálogo. Para carga masiva: `GET/PUT /api/importar/alumnos` y `GET/PUT /api/importar/carga` tal como se listan en el PDF; el método de envío de archivo necesita confirmación. |
| Carga académica (administrador) | `GET/POST/PUT /api/grupos/{id}/carga` para leer y asignar materias/docentes/tutor. |
| Estadías (administrador) | No se especifica endpoint. Se requiere una operación para asignar el tutor de estadías por alumno o grupo y consultar esas asignaciones. |
| Plantillas (administrador) | `GET/POST/PUT /api/plantillas`. No se detallan rutas por aspecto, reordenamiento, eliminación ni publicación versionada. |
| Periodos (administrador) | `POST /api/periodos`, `POST /api/periodos/{id}/abrir`, `POST /api/periodos/{id}/cerrar`. El listado y edición de periodos no tienen rutas explícitas en el PDF. |
| Reportes (personal autorizado) | `GET /api/resultados/exportar?formato=xlsx`. El PDF también exige Excel y PDF, pero el ejemplo de ruta solo menciona `xlsx`; faltan criterios de carrera, periodo, tipo de reporte y permisos en la ruta descrita. La sección 10 asigna esta ruta a coordinador/administrador, mientras `AGENTS.md` también incluye Reportes para docente/tutor. |
| Mensajes/recordatorios (alumno) | Sin endpoint de mensajes, lectura/no lectura o recordatorios en la sección 10. |

Los resultados de P-08 y reportes deben ser agregados; ninguna ruta debe exponer respuestas asociadas a un alumno. La lista de personas pendientes para seguimiento no debe permitir inferir qué calificación dio cada una.

## 4. Dudas y contradicciones por resolver

| Tema | Diferencia observada | Decisión necesaria |
|---|---|---|
| Encuesta unificada de quinto | `AGENTS.md` exige una sola página con cada pregunta aplicada a todos los docentes y el tutor al final. La demo muestra FORM-AC16, 10 preguntas y 8 evaluados como una evaluación unificada. El PDF describe una encuesta por materia y otra del tutor (RN-02 a RN-05, flujo y P-02/P-03). | Confirmar que la regla especial de quinto sustituye el conteo/formato individual del PDF solo para ese cuatrimestre y definir cómo se representa en asignaciones y respuestas anónimas. |
| Estadías | `AGENTS.md` añade que en 6.º/10.º solo se evalúa al tutor de estadías y contempla asignación por alumno o grupo. La demo incluye una sección de Estadías y una opción de simulación «Alumno en estadías». El PDF contempla tutoría por grupo, pero no un flujo de estadías. | Definir datos, asignación y generación de encuestas para estadías; tratar «Alumno en estadías» como selector de escenario de prueba, no como sexto rol, según la regla de `AGENTS.md`. |
| Comprobante y anonimato | P-05 del PDF permite comprobante «con fecha» y el flujo menciona mostrarlo al tutor. `AGENTS.md` prohíbe fecha/hora exacta en la respuesta y separa asignación de calificaciones. | Precisar si el comprobante muestra solo fecha local/periodo y cómo se obtiene sin persistir ni revelar un vínculo entre identidad y calificaciones. |
| Identificador de encuesta | La API usa `/mis-encuestas/{id}` y el POST también requiere `{id}`. La ruta Blazor prescrita es `/alumno/encuesta`, sin identificador en la URL. | Elegir transporte de la asignación seleccionada (parámetro de ruta, query o estado protegido) y definir el comportamiento de recarga/enlace directo. |
| Confirmación P-04 | El PDF cataloga P-04 como pantalla; la estructura Blazor publicada solo incluye `/alumno/encuesta` y `/alumno/comprobante`. | Mantener P-04 como modal/hoja dentro del formulario, salvo que se acuerde explícitamente una ruta propia. |
| Mensajes | La demo y `AGENTS.md` muestran campana, contador, panel y recordatorios. El PDF no enumera un endpoint de mensajes. | Definir si mensajes se leen de una API, son contenido local/administrativo o quedan fuera de la primera entrega. |
| Resultados propios | P-06 pide filtro por materia y grupo, y la demo separa inicio, resultados e histórico. La API solo especifica `GET /mis-resultados?periodo=`. | Añadir filtros/contrato de respuesta para grupo y materia, y establecer si el endpoint cubre histórico además del periodo actual. |
| Resultados por persona evaluada | La demo y `AGENTS.md` separan resultados de docentes y tutores; el PDF P-08 se titula «Resultados por docente», aunque sección 10 incluye rutas distintas de docentes y tutores. | Confirmar rutas/pantallas y filtros del detalle agregado para ambos tipos. |
| Reportes por rol | `AGENTS.md` incluye Reportes para docente y tutor; la tabla de rutas del PDF asigna `/resultados/exportar` a coordinador/administrador. | Definir si docente/tutor pueden exportar sus propios resultados y aplicar esa autorización en la API sin permitir exportaciones de otros. |
| Exportaciones | RF-16 exige Excel y PDF, pero la ruta del PDF solo ilustra `formato=xlsx`. | Acordar formatos aceptados y parámetros del reporte antes de implementar la UI. |
| Importación masiva | RF-04 especifica importar Excel/CSV y mostrar errores por renglón; sección 10 lista `GET, PUT` para `/importar/...`, sin aclarar carga/descarga ni tipo de contenido. | Definir el verbo de carga de archivo y respuesta de errores; conservar lectura/descarga si también es necesaria. |
| Plantillas | P-11 y `AGENTS.md` requieren agregar, eliminar, reordenar aspectos y publicar versión nueva. El API solo lista `GET/POST/PUT /plantillas`. | Definir endpoints/cuerpo para aspectos y publicación y garantizar que una versión publicada rige desde el siguiente periodo. |
| Periodos | P-12 requiere consultar estado/fechas y operar abrir/cerrar; sección 10 lista creación y acciones, pero no consulta/listado ni actualización de fechas. | Completar el contrato para listar/ver/editar periodos y aclarar cuándo se generan asignaciones. |
| Umbral de anonimato | `AGENTS.md` aplica mínimo de 5 respuestas a resultados de docente o tutor; el PDF formula RN-10 para docente y RN-12 para tutor. | Aplicar la regla uniformemente antes de diseñar gráficos, exportaciones y respuestas API; no mostrar conteos/detalles que permitan eludirla. |
