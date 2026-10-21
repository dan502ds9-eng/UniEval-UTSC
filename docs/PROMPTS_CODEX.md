# Prompts para Codex — UniEval UTSC

**Cómo usarlos**
1. Descomprime el paquete en la raíz del repo `UniEval-UTSC` (`AGENTS.md` en la raíz; `docs/prototipo/`, `docs/assets/` y este archivo en `docs/`). Pon también el PDF del documento de diseño en `docs/`. Codex lee `AGENTS.md` solo cada vez que trabaja en el repo.
2. Da los prompts **en orden, uno por uno**. No pases al siguiente hasta que compile, las pruebas pasen y revises la pantalla contra la demo.
3. Haz un commit por prompt en su rama (`feature/...`) y únela a `main` cuando quede bien.
4. Si Codex se desvía, recuérdale: *"Revisa AGENTS.md y compáralo con docs/prototipo/unieval-demo.html"*.

> Reparto sugerido: P1 → Dan o el front · P2 a P10 → Eve y Kat (`web/`) · P11 → Dan (`api/`) · P12 → Dan con Karen (`db/`).

---

## P0 · Lectura y plan (sin escribir código)
```
Lee AGENTS.md completo, abre docs/prototipo/unieval-demo.html y revisa el PDF de diseño en docs/.
No escribas código todavía. Entrégame:
1) Un mapa de pantallas por rol (alumno, docente, tutor, coordinador, administrador) con sus rutas Blazor.
2) La lista de componentes reutilizables que vas a crear, con sus parámetros.
3) La lista de endpoints de la API que necesita cada pantalla.
4) Las dudas o contradicciones que encuentres entre la demo, AGENTS.md y el PDF.
Guárdalo en docs/PLAN.md.
```

## P1 · Proyecto web dentro del repo existente
```
El back ya existe en api/ (UniEvalUtsc.slnx con Domain, Application, Infrastructure, Api y tests). No cambies su arquitectura ni rompas sus pruebas.
1) Crea api/src/UniEvalUtsc.Contracts (classlib net10.0) con DTOs como records y enums compartidos (Rol, EstadoPeriodo, TipoEncuesta, EstadoEncuesta, Nivel)
   y constantes de reglas (MinimoRespuestas = 5, EscalaMin = 1, EscalaMax = 10). Que lo referencien Api y Web.
2) Crea web/UniEvalUtsc.Web con la plantilla Blazor WebAssembly PWA de .NET 10 (manifest: nombre "UniEval UTSC", short_name "UniEval", theme_color #FF8200, background_color #FFF3E6, lang es-MX).
   Quita las páginas de ejemplo (Counter, Weather) y Bootstrap.
3) Crea web/tests/UniEvalUtsc.Web.Tests (xUnit + bUnit) y web/tests/UniEvalUtsc.E2E (xUnit + Microsoft.Playwright).
4) Agrega los cuatro proyectos nuevos a api/UniEvalUtsc.slnx (Contracts en /src/; los de web en una carpeta /web/).
5) Configura el cliente: HttpClient con BaseAddress desde wwwroot/appsettings.json (ApiBaseUrl = https://localhost:7133) y en la API una política CORS para el origen del cliente en desarrollo.
6) En Infrastructure agrega implementaciones InMemory de las interfaces existentes (IPeriodoRepository, IPlantillaRepository, IEncuestaAsignadaRepository, IRespuestaRepository, IUnitOfWork) y de IReloj,
   con datos semilla idénticos a la demo: grupo DSM05AV con sus 7 materias y docentes, la tutora Mónica Janeth Rodríguez Reyna, [Grupo 5to B], [Grupo 6to A] y [Grupo 10mo A] en estadías,
   periodo Sep–Dic 2026 abierto y 3 cerrados, plantillas FORM-AC16 (10 aspectos) y FORM-AC64 (7) con los textos del anexo del PDF. Regístralas según Datos:Proveedor = InMemory.
7) Actualiza el README raíz con cómo correr la API y la web a la vez (dos terminales o perfil de inicio múltiple en Visual Studio).
Criterio: dotnet build api/UniEvalUtsc.slnx y dotnet test en verde (incluidas las pruebas existentes); la web abre en blanco con el layout base; Swagger sigue en /swagger.
```

## P2 · Sistema de diseño (CSS + componentes base)
```
Implementa el sistema de diseño de la sección 6 de AGENTS.md en web/UniEvalUtsc.Web:
- wwwroot/css/tokens.css: variables para el tema claro y el oscuro (html[data-theme=dark]), todas las medidas de texto en rem.
- wwwroot/css/app.css: base, tipografías (Space Grotesk / Inter), fondo con el patrón de íconos según el tema, foco visible de 3px, reglas para el tamaño de texto (html[data-ts="150"|"200"]).
- Mueve las imágenes de docs/assets a wwwroot/img (campus.jpg, correcaminos.jpg, fondo-iconos-claro.svg, fondo-iconos-oscuro.svg).
- Components/Ui: Boton (Primario, Contorno, Enlace, Pequeño), Tarjeta, Pill (nivel Bajo/Medio/Alto según el valor), Etiqueta, CampoTexto y Selector con <label> visible, BotonRegresar,
  Acordeon (details/summary accesible), Dialogo (modal con trampa de foco, Esc y regreso del foco), PanelLateral (entra desde la derecha), Toast (role=status), BarraProgreso (role=progressbar), RegionViva (aria-live).
- wwwroot/js/unieval.js + servicio de interop: trapFocus/releaseFocus, focusElement, speak/cancelSpeech (es-MX), get/set de localStorage con try/catch.
- Una página /dev/componentes que muestre todos los componentes en ambos temas.
Copia los colores, radios y espaciados de docs/prototipo/unieval-demo.html; no inventes otros.
Pruebas bUnit: Pill calcula el nivel correcto (4.9 → Bajo, 5 → Medio, 7.9 → Medio, 8 → Alto) y Dialogo devuelve el foco al cerrarse.
```

## P3 · Layout, barra superior y navegación por rol
```
Crea MainLayout y StaffShell igual que la demo:
- Barra superior translúcida y pegada arriba: logo "U" + "UniEval / UTSC"; a la derecha, Aa (menú 100/125/150/200 % que cambia html.style.fontSize y se guarda),
  campana con contador (solo Alumno), luna/sol (tema, se guarda y al primer ingreso respeta prefers-color-scheme) y menú de usuario (iniciales + rol → nombre, rol, "Cerrar sesión").
  Los menús se abren con Enter/↓, se recorren con ↑/↓/Inicio/Fin y Esc los cierra devolviendo el foco. Sin sesión iniciada no se muestra el menú de usuario.
- StaffShell: menú lateral sticky en escritorio (≥ 900px) con el nombre y el rol arriba, y pestañas desplazables en celular. Secciones por rol según la sección 4 de AGENTS.md.
  Botón "← Volver al inicio" en todas las secciones menos en Inicio; siempre lleva al Inicio del rol.
- Al cambiar de ruta: foco al h1, document.title = "<h1> · UniEval UTSC" y anuncio en la RegionViva ("Pantalla: …").
- Guardas de ruta por rol (un alumno no puede abrir /admin).
- Un AuthService con dos modos por configuración: Simulado (selector de rol, como la demo) y Entra (MSAL). Implementa solo Simulado; deja Entra con TODO y la configuración preparada.
```

## P4 · Inicio de sesión y acceso de personal
```
Implementa P-01 como en la demo:
- Pantalla con la foto campus.jpg a pantalla completa y la capa oscura (gradiente a la izquierda en escritorio, capa completa en celular), título "Tu opinión mejora la UTSC" con la rayita naranja,
  el texto de apoyo y una tarjeta de vidrio oscuro con "Iniciar sesión", el selector "Tipo de usuario" (Alumno, Docente, Tutor, Coordinador, Administrador) y el botón "Iniciar sesión con Microsoft 365".
- Alumno: entra directo a /alumno.
- Personal: /acceso/{rol}, con tarjeta "Acceso de personal", campos con etiqueta (correo institucional, contraseña), "Continuar" y un segundo paso con un código de 6 dígitos (verificación en dos pasos).
  Enter dentro de un campo avanza. Botón "← Regresar al inicio de sesión" (en el paso 2: "← Regresar").
- Verifica el contraste del texto sobre la foto (≥ 4.5:1 en texto normal, ≥ 3:1 en el título) en 390, 768, 1280 y 1920 px de ancho, en claro y oscuro.
```

## P5 · Alumno: Mis encuestas, evaluación, comprobante y mensajes
```
Implementa el flujo del alumno (P-02 a P-05) idéntico a la demo:
1) /alumno "Mis encuestas": avance "x de N contestadas", tarjeta "Evaluación del cuatrimestre" con la lista de materias y docentes y, al final, el tutor (o solo "Tutor de estadías" si su grupo está en estadías),
   estado (Pendiente / Borrador guardado · n de N / Contestada) y botón "Contestar evaluación" / "Continuar evaluación". Si el periodo no está ABIERTO, el botón queda deshabilitado con su aviso (RN-08).
2) /alumno/encuesta:
   - Resumen en 3 bloques con ícono: Periodo actual, Preguntas (y formato), Evaluados.
   - Barra pegajosa: "n de N respondidas", progressbar, leyenda "1–4 Bajo / 5–7 Medio / 8–10 Alto" y "Ir a la siguiente pendiente →".
   - Una pregunta por acordeón (solo la primera pendiente abierta); en la cabecera, número, texto y avance "3 / 8" o "✓ Completa".
     Dentro: botón "🔊 Escuchar pregunta" y una fila por docente (número, materia, nombre) más la fila del tutor resaltada con borde naranja punteado.
   - Selector de calificación = radiogroup de 10 opciones con los colores de la escala; 10 en una fila en escritorio y 2 filas de 5 en celular o con texto ≥ 150 %.
   - Teclas 0–9 califican y avanzan; al completar la pregunta se cierra y se abre la siguiente con el foco.
   - Barra inferior fija: "Guardar borrador y salir" (guarda en localStorage, regresa a Mis encuestas y muestra un toast) y "Enviar evaluación" (habilitado solo al completar todo).
   - Al enviar: Dialogo "¿Enviar tu evaluación?" con aviso de que no se puede modificar.
3) /alumno/comprobante: "¡Gracias!" con sello COMPROBANTE, fecha y periodo.
4) Mensajes: la campana abre un PanelLateral con la lista (remitente, fecha, "Nuevo", "Marcar como leído", "Marcar todos como leídos").
   Al entrar con mensajes sin leer, aparece una notificación emergente (role=status) con "Ver mensaje" y "Cerrar".
Usa solo datos del servicio (InMemory por ahora), nada fijo en los componentes.
Pruebas: total = preguntas × (docentes + 1); en estadías solo aparecen el tutor y el FORM-AC64; el borrador se recupera; no se puede enviar incompleto.
```

## P6 · Docente y Tutor
```
Implementa las secciones de Docente y Tutor (P-06) en StaffShell:
- Inicio: tarjeta de información (Periodo, Estado, Privacidad), selector de periodo y tarjetas "Mis materias" (o "Mi grupo de tutoría") con promedio + Pill, "x de y alumnos contestaron" y "Ver resultados".
  Con menos de 5 respuestas: aviso RN-10 y botón deshabilitado. Periodo actual sin cerrar: "Disponible al cierre del periodo" (RN-11).
- Mis resultados: selector de periodo, segmentado por materia y grupo, tres indicadores (Promedio general, Respuestas, Contra la carrera),
  "Áreas de oportunidad" y "Fortalezas" (3 y 3) y el componente PromedioPorAspecto (ver abajo). Botón "Descargar reporte (PDF)".
- Histórico: gráfica de línea en SVG propio (sin librerías), con el valor y el periodo en cada punto y descripción accesible, más una tabla Periodo / Promedio / Nivel.
- Reportes: tabla por periodo y materia con "Descargar PDF".
Componente PromedioPorAspecto (también lo usan el coordinador y el administrador):
- Acordeón cerrado por defecto; en la cabecera: "Promedio por aspecto", "N aspectos · x por debajo de 8 · de menor a mayor puntuación".
- Leyenda: "Barra: puntuación obtenida (0 a 10)", "Línea: promedio de la carrera" y los niveles.
- Filas ordenadas de MENOR a MAYOR puntuación, numeradas; a la derecha, Puntuación, Carrera y diferencia (+ en verde, − en rojo); barra del color del nivel con la marca vertical del promedio de la carrera.
Sin íconos ni emojis en estas pantallas.
```

## P7 · Coordinador
```
Implementa Coordinador (P-07 y P-08). Solo ve su carrera: el filtro Carrera aparece bloqueado.
- Filtros: Periodo, Carrera, Cuatrimestre (Todos, 5to cuatrimestre, 6to · estadías, 10mo · estadías).
- Inicio: indicadores (Participación, Alumnos que ya contestaron, Promedio de docentes, Por debajo de 8), "Menor participación" (3 grupos) y "Áreas de mejora" con "Ver todo →".
- Seguimiento: tarjetas por grupo con el porcentaje que contestó (barra roja < 60, amarilla < 85, verde ≥ 85), "Faltan x de y", "Ver quién falta (n)" que expande SOLO esa tarjeta (las demás no se estiran)
  y "Enviar recordatorio" (genera un mensaje para los alumnos de ese grupo; ver P11). Los grupos de estadías dicen "Solo evalúan a su tutor de estadías".
- Resultados de docentes y Resultados de tutores: tabla de menor a mayor promedio con "Ver detalle →". Con 6to o 10mo, Resultados de docentes muestra un aviso con enlace a Resultados de tutores,
  y Resultados de tutores muestra solo al tutor de estadías de ese cuatrimestre.
- Detalle: cabecera (rol, nombre, materia · grupo · periodo, Promedio general + Pill, Contra la carrera, Respuestas), Áreas de oportunidad / Fortalezas y PromedioPorAspecto. Exportar PDF.
- Reportes: Resultado por docente, Resultado por tutor, Concentrado por carrera, Participación y Comparativo histórico, cada uno con Excel y PDF.
Siempre visible: "Ves participación y promedios. Nunca qué calificó cada alumno (RN-09)."
```

## P8 · Administrador
```
Implementa Administrador (P-09 a P-12) con todas las carreras (filtro Carrera habilitado) y reutiliza Seguimiento, Resultados y Reportes de P7.
- Inicio: estado del periodo (pill), fecha de cierre e indicadores.
- Periodos: periodo actual con estado (En preparación / Encuestas abiertas / Cerrado), 4 fechas, texto explicativo según el estado y botones "Abrir aplicación de encuestas" / "Cerrar aplicación y liberar resultados".
  Abrir genera las encuestas (RF-08); cerrar libera resultados a docentes y tutores (RF-17, RN-11). Lista de periodos anteriores y "Crear periodo".
- Carga académica: por grupo de 5to, tabla Materia → Docente (select con aria-label "Docente de <materia>") y fila del tutor del grupo. Total "x materias + 1 tutor = N encuestas". Importar desde Excel/CSV.
- Estadías: grupo de 6to o 10mo, tabla Alumno + correo institucional / Empresa / Tutor de estadías (select), "Asignar a todo el grupo",
  aviso "n alumnos sin tutor de estadías: no se les generará encuesta" y "Guardar asignaciones".
- Catálogos: segmentado Carreras / Grupos / Materias / Docentes / Alumnos, buscador, "Agregar", "Importar Excel/CSV" con reporte de errores por renglón y "Desactivar/Reactivar" (baja lógica).
- Plantillas: FORM-AC16 y FORM-AC64 con versión y estado (Vigente / Borrador). Por aspecto: ↑ ↓ Editar (textarea) Eliminar; "Agregar aspecto";
  "Publicar versión n+1" (habilitado solo con cambios) y "Descartar cambios". Aviso RN-12.
- Tarjeta fija: "Ni el administrador ni el coordinador pueden ver qué calificó cada alumno (RN-09)."
```

## P9 · Correcaminos IA (preguntas frecuentes)
```
Implementa el asistente como en la demo:
- Botón flotante redondo con la imagen correcaminos.jpg en la esquina INFERIOR IZQUIERDA (más arriba si existe la barra inferior de la encuesta), con globito "¿Qué puedo hacer por ti?" en hover o foco.
  aria-label "Abrir asistente Correcaminos IA", aria-expanded y aria-controls.
- Al abrir: Dialogo no bloqueante pero con trampa de foco, a la izquierda; cabecera naranja con texto oscuro; mensajes con role="log";
  preguntas frecuentes por rol en una fila deslizable (en varias filas con texto ≥ 150 %); campo de texto con etiqueta y botón Enviar.
- Las respuestas salen de un servicio IFaqService con las preguntas y respuestas por rol tomadas de la demo (objetos A0 y AX de su script), con coincidencia por palabras clave.
  Déjalo listo para cambiarlo después por un modelo de IA real sin tocar el componente.
- Esc cierra y devuelve el foco al botón flotante.
```

## P10 · Accesibilidad y pruebas de extremo a extremo
```
Agrega pruebas en web/tests/UniEvalUtsc.E2E con Playwright para .NET + axe-core (reglas wcag2a, wcag2aa, wcag21aa, wcag22aa):
- Recorre todas las pantallas de los 5 roles, en claro y oscuro, en 390 px y 1280 px: 0 violaciones de axe.
- Con texto al 200 % (html[data-ts="200"]), en 390 px y 1280 px: sin scroll horizontal y sin texto recortado.
- Solo con teclado: el alumno inicia sesión, contesta las 80 respuestas con las teclas numéricas, envía (el diálogo atrapa el foco) y llega al comprobante;
  el chat y el panel de mensajes se cierran con Esc y devuelven el foco.
- Cada cambio de pantalla deja el foco en el h1 y actualiza document.title.
Corrige todo lo que fallen las pruebas.
```

## P11 · Contrato de la API (back)
```
Lee docs/DECISIONES.md: sus rutas y contratos (D-01 a D-14) mandan sobre esta lista y sobre el PDF.
En api/ implementa los endpoints de la sección 10 del PDF siguiendo la arquitectura existente: controladores en UniEvalUtsc.Api/Controllers, casos de uso en Application/Services (como EnviarRespuestaService), DTOs en UniEvalUtsc.Contracts y un middleware que traduzca NoEncontrado→404, AccesoDenegado→403, Validacion→400 y Domain→409. Sobre los repositorios InMemory:
GET /api/auth/perfil · GET /api/mis-encuestas · GET /api/mis-encuestas/{id} · POST /api/mis-encuestas/{id}/respuestas (400 / 403 / 409 según el PDF)
GET /api/mis-resultados?periodo= · GET /api/seguimiento?periodo=&carrera=&grupo= · GET /api/resultados/docentes · GET /api/resultados/tutores · GET /api/resultados/exportar?formato=xlsx|pdf
CRUD /api/carreras, /api/grupos, /api/materias, /api/docentes, /api/alumnos · POST /api/importar/alumnos y /api/importar/carga · GET/PUT /api/grupos/{id}/carga
GET/POST/PUT /api/plantillas (versionado RN-12) · GET/POST/PUT /api/periodos · POST /api/periodos/{id}/abrir y /cerrar
Extensiones de esta versión:
- Estadías: GET/PUT /api/estadias/{grupoId}/asignaciones (tutor por alumno; al abrir el periodo, a esos alumnos solo se les genera la encuesta FORM-AC64 de su tutor).
- Mensajes: POST /api/recordatorios {grupoId} (Coordinador, Administrador) · GET /api/mis-mensajes · PUT /api/mis-mensajes/{id}/leido.
- Evaluación unificada: GET /api/mis-encuestas/evaluacion devuelve preguntas × evaluados para la pantalla única; el POST divide las calificaciones en una "respuesta" anónima por encuesta asignada.
Reglas en el servidor: rol por endpoint (RNF-05), periodo ABIERTO (RN-08), envío único (RN-07), mínimo de 5 respuestas (RN-10), resultados al cierre (RN-11), envío en transacción y respuesta.id como UUID sin alumno ni hora (RN-09).
Documenta todo en Swagger con ejemplos. Pruebas xUnit para cada regla. Después conecta los servicios del cliente a estos endpoints y quita los datos simulados del cliente.
```

## P12 · Conexión a MariaDB (cuando el esquema de Karen esté listo)
```
En api/src/UniEvalUtsc.Infrastructure/Persistence implementa el DbContext con EF Core + Pomelo para MariaDB (usa los constructores privados de las entidades), mapeando al esquema de los scripts de Karen en db/ (no lo cambies sin avisar), y en Repositories las implementaciones EfCore de las interfaces de Application.
Tablas de la sección 7 del PDF: usuario, usuario_rol, periodo, carrera, materia, docente, alumno, grupo, inscripcion, carga_academica, plantilla, aspecto, encuesta_asignada, respuesta, respuesta_detalle,
más las de estadías (asignación de tutor por alumno) y mensajes.
- encuesta_asignada y respuesta NO se relacionan; respuesta.id es UUID y no tiene alumno_id ni fecha y hora.
- El envío usa una transacción explícita.
- Cadena de conexión en user-secrets o variables de entorno, nunca en el repo.
- Datos semilla equivalentes a los InMemory para desarrollo.
- Cambia Datos:Proveedor a MariaDb y corre las mismas pruebas de la API contra un contenedor de MariaDB (Testcontainers).
```

---

### Prompt corto para correcciones de interfaz
```
Compara la pantalla <ruta> con su equivalente en docs/prototipo/unieval-demo.html (rol <rol>, sección <sección>) en 390 px y 1280 px, en claro y oscuro.
Lista las diferencias de estructura, textos, colores, espaciados y comportamiento, y corrígelas siguiendo AGENTS.md.
```
