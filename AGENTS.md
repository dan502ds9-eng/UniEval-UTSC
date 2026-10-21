# AGENTS.md — UniEval UTSC (repo `UniEval-UTSC`)

Contexto permanente para Codex. Léelo completo antes de cualquier tarea en este repositorio.

## 1. Qué es el proyecto
**UniEval UTSC** (nombre interno del sistema: SAIIUT) sustituye los formatos de Excel **FORM-AC16** (Desempeño Docente, 10 aspectos) y **FORM-AC64** (Desempeño del Tutor, 7 aspectos) de la Universidad Tecnológica Santa Catarina. Es una aplicación web responsiva, instalable como PWA, que los alumnos contestan desde el celular.

- **Referencia visual obligatoria:** `docs/prototipo/unieval-demo.html`. Es la simulación aprobada por el equipo y la subdirección. Ábrela en el navegador y replica su estructura, comportamiento, textos y estilo. Si este documento y la demo difieren, manda este documento.
- **Decisiones del equipo:** `docs/DECISIONES.md` resuelve las dudas de `docs/PLAN.md` (D-01 a D-14). **Manda sobre el PDF y la demo**; léelo antes de cada prompt.
- **Documento de diseño:** `docs/Diseno_Sistema_Encuestas_Desempeno_UTSC.pdf` define reglas (RN), requerimientos (RF/RNF), pantallas (P-01 a P-12) y API.
- Los datos de la demo son **de ejemplo**. Nunca los dejes fijos en el código de producción: van en datos semilla (seed) o en servicios simulados (mock).

## 2. Stack
| Capa | Tecnología |
|---|---|
| Lenguaje principal | **C#** sobre **.NET 10** (el que ya usa `api/`) |
| Cliente (interfaz) | **Blazor WebAssembly PWA** (`blazorwasm --pwa`), componentes `.razor` + CSS aislado (`.razor.css`) |
| API | **ASP.NET Core Web API** ya existente en `api/` (Microsoft.AspNetCore.OpenApi + Swagger UI en `/swagger`) |
| Datos | **MariaDB** con **EF Core** (proveedor Pomelo; verifica la versión compatible con el .NET instalado) |
| Autenticación | Microsoft 365 / **Microsoft Entra ID** (MSAL para Blazor, Microsoft.Identity.Web en la API). Mientras no exista el registro de la app: modo **Simulado** con selector de rol, controlado por configuración |
| JavaScript | Solo con JS interop, en `wwwroot/js/unieval.js`, para lo que Blazor no cubre: atrapar el foco en diálogos, `speechSynthesis` (lectura en voz alta), `localStorage` (tema, tamaño de texto, borrador) |
| Pruebas | xUnit + **bUnit** (componentes) y **Playwright para .NET** + **axe-core** (accesibilidad) |

No agregues librerías de componentes (MudBlazor, Radzen, Bootstrap, etc.). El diseño es propio y vive en `wwwroot/css/` con variables CSS.

## 3. Estructura del repositorio (ya existe; respétala)
```
UniEval-UTSC/
├─ AGENTS.md
├─ api/                                   ← back (Dan) · NO cambiar su arquitectura
│  ├─ UniEvalUtsc.slnx                    ← solución principal (agrega aquí los proyectos de web/ en la carpeta /web/)
│  ├─ src/UniEvalUtsc.Domain/             ← entidades con setters privados, fábricas y DomainException/ValidacionException
│  ├─ src/UniEvalUtsc.Application/        ← servicios de caso de uso, interfaces de repositorio, IUnitOfWork, IReloj
│  ├─ src/UniEvalUtsc.Infrastructure/     ← Persistence (EF Core/MariaDB), Repositories, Auth
│  ├─ src/UniEvalUtsc.Api/                ← Controllers, Middleware, Extensions, Program.cs
│  ├─ src/UniEvalUtsc.Contracts/          ← NUEVO: DTOs (records) y enums compartidos por Api y Web
│  └─ tests/UniEvalUtsc.Tests/            ← xUnit (Unit/)
├─ web/                                   ← front (Eve y Kat)
│  ├─ UniEvalUtsc.Web/                    ← Blazor WebAssembly PWA
│  │  ├─ Layout/ (MainLayout, Header, StaffShell)
│  │  ├─ Pages/Alumno|Docente|Coordinador|Admin|Acceso/
│  │  ├─ Components/Ui/  Components/Encuesta/
│  │  ├─ Services/ (clientes HTTP tipados + simulados)
│  │  └─ wwwroot/ (css/, js/unieval.js, img/, manifest.webmanifest, service-worker.js)
│  └─ tests/UniEvalUtsc.Web.Tests/        ← bUnit  ·  tests/UniEvalUtsc.E2E/ ← Playwright + axe
├─ db/                                    ← scripts de MariaDB (Karen)
├─ mobile/                                ← reservado; la app móvil es la PWA de web/
├─ infra/  ·  .github/                    ← despliegue y CI
└─ docs/  (api/, prototipo/unieval-demo.html, assets/, PROMPTS_CODEX.md, PDF de diseño)
```
**Reglas de dependencias:**
- `UniEvalUtsc.Web` solo referencia `UniEvalUtsc.Contracts`; nunca a Domain, Application ni Infrastructure.
- `UniEvalUtsc.Api` mapea entidades ↔ DTOs de Contracts.
- La API debe funcionar sin la BD mientras Karen termina el esquema. Las interfaces de `Application/Interfaces` tienen dos implementaciones en `Infrastructure`: `InMemory` (datos semilla iguales a la demo) y `EfCore` (MariaDB). Se elige por configuración (`Datos:Proveedor = InMemory | MariaDb`).
- Sigue el estilo del dominio existente: setters privados, constructores privados para EF Core, fábricas (`EncuestaAsignada.ParaDocente/ParaTutor`) y reglas dentro de las entidades (`Periodo.AceptaRespuestas`, `Plantilla.Publicar`). Las excepciones se traducen a HTTP: NoEncontrado → 404, AccesoDenegado → 403, Validacion → 400, Domain → 409.

## 4. Roles (5)
Un usuario puede tener más de un rol (por ejemplo, docente que también es tutor).

| Rol | Qué hace | Qué NO puede ver |
|---|---|---|
| **Alumno** | Ve "Mis encuestas", contesta una sola vez, guarda borrador, recibe mensajes y recordatorios | Resultados |
| **Docente** | Inicio, Mis resultados (por materia y grupo), Histórico, Reportes | Quién contestó qué; resultados de otros |
| **Tutor** | Igual que Docente, con FORM-AC64 | Igual que Docente |
| **Coordinador** | Inicio, Seguimiento (participación, lista de quién falta, recordatorios), Resultados de docentes, Resultados de tutores, Reportes. **Solo su carrera** (filtro de carrera bloqueado) | Respuestas individuales |
| **Administrador** | Inicio, Periodos (abrir/cerrar), Carga académica, **Estadías**, Catálogos, **Plantillas** (editar/agregar/eliminar/reordenar aspectos), Seguimiento, Resultados, Reportes, de todas las carreras | Respuestas individuales |

Etiquetas exactas en la interfaz: "Alumno", "Docente", "Tutor", "Coordinador", "Administrador".

## 5. Reglas de negocio
- **RN-01** Un alumno pertenece a un solo grupo por periodo.
- **RN-02/03** Una encuesta docente por cada materia del grupo; si un docente da dos materias, son dos encuestas.
- **RN-04/05** Un tutor por grupo y periodo. Total por alumno = materias + 1.
- **RN-06** Todos los aspectos son obligatorios, con un entero de 1 a 10. Sin comentarios.
- **RN-07** Una encuesta enviada no se modifica ni se contesta de nuevo.
- **RN-08** Solo se contesta con el periodo **ABIERTO**.
- **RN-09 (la más importante)** Anonimato: `encuesta_asignada` (quién contestó) y `respuesta` (qué contestó) **no tienen vínculo**. `respuesta.id` es UUID, sin `alumno_id` ni fecha y hora exacta. El envío es una transacción: se insertan las respuestas y se marca como contestada, o no se guarda nada.
- **RN-10** Los resultados de un docente o tutor solo se muestran con **5 o más respuestas**.
- **RN-11** Docentes y tutores ven resultados del periodo actual **solo cuando el administrador lo cierra**.
- **RN-12** Las plantillas tienen versión; publicar una versión nueva aplica a partir del **siguiente periodo**.
- **Estadías (6to y 10mo):** el alumno **no** evalúa docentes; solo evalúa a su **tutor de estadías** (FORM-AC64), que el Administrador asigna por alumno o por grupo. **No existe un perfil aparte**: el alumno entra con su correo institucional y el sistema le muestra solo esa encuesta.
- **Evaluación unificada (5to):** en una sola página, cada pregunta del FORM-AC16 lista a los docentes del grupo y al tutor como **última fila** (mismo texto de pregunta para todos). Total = preguntas × (docentes + 1). Ejemplo DSM05AV: 10 × 8 = 80 respuestas.

## 6. Sistema de diseño (debe verse igual que la demo)
**Tipografías** (Google Fonts): títulos en *Space Grotesk* 500/700; texto en *Inter* 400/500/600/700.
**Todas las medidas de texto en `rem`** y los contenedores con `max-width` en `rem`. Nada de alturas fijas en px en elementos con texto; usa `min-height`.

| Token | Claro | Oscuro |
|---|---|---|
| `--bg` | #FFF3E6 + patrón `fondo-iconos-claro.svg` | #16120E + `fondo-iconos-oscuro.svg` |
| `--card` | #FFFFFF | #211B16 |
| `--ink` (texto) | #14110F | #F2EBE3 |
| `--mute` (texto secundario) | #4A443D | #C9BEB2 |
| `--line` (bordes) | #E0D3C0 | #3B3129 |
| `--primary` (botón principal) | #FF8200 con **texto #14110F** | #FF8A1F con texto #14110F |
| `--link` | #B34700 | #FFA24D |
| `--focus` (contorno de foco) | #B34700 | #FFB070 |
| `--ok` / `--bad` (diferencias) | #187A41 / #B3261E | #5CCB8A / #FF7A6B |

- **Niveles de resultados:** Bajo `< 5` (#B3261E, texto blanco) · Medio `5–7.9` (#E0A100, texto oscuro) · Alto `≥ 8` (#187A41, texto blanco).
- **Escala del selector 1–10** (fondo/texto al seleccionar): 1 #B3261E/blanco, 2 #C73E1D/blanco, 3 #E0551A, 4 #F28C1A, 5 #EDBB2A, 6 #B7C333, 7 #7DBB45, 8 #4CAF50 (3–8 con texto #14110F), 9 #187A41/blanco, 10 #11602F/blanco. Leyenda: "1–4 Bajo", "5–7 Medio", "8–10 Alto".
- **Formas:** tarjetas con radio de 18px y borde de 1–2px `--line`; botones con radio de 12px; pills y etiquetas con 99px. Sin degradados decorativos. El único degradado permitido es la capa oscura sobre la foto del inicio de sesión.
- **Inicio de sesión:** foto del campus a pantalla completa con capa oscura del lado del texto (izquierda en escritorio, completa en celular), título "Tu opinión mejora la UTSC", tarjeta de vidrio oscuro translúcido, selector "Tipo de usuario" y botón "Iniciar sesión con Microsoft 365".
- **Barra superior:** delgada y translúcida, logo "U" naranja + "UniEval / UTSC". A la derecha: botón **Aa** (tamaño de texto), **campana** (solo alumno, con contador), **luna/sol** (modo nocturno) y menú de usuario (iniciales + rol → nombre, rol y "Cerrar sesión"). No uses "Entrar" en el menú.
- **Personal (docente, tutor, coordinador y administrador):** menú lateral fijo en escritorio y pestañas desplazables en celular. El botón "← Volver al inicio" lleva **siempre al Inicio del rol**, nunca a la pantalla anterior. En el Inicio no hay botón de regreso.
- **Correcaminos IA:** ícono redondo flotante en la **esquina inferior izquierda**, con globito "¿Qué puedo hacer por ti?" al pasar el mouse o enfocar. El chat se abre a la izquierda, con preguntas frecuentes **por rol** en una fila deslizable.
- **Tarjetas expandibles:** al expandir una, las demás no se estiran (`align-items:start` en las cuadrículas).
- **Sin íconos ni emojis** en pantallas de docentes y administrativos (es una plataforma seria). Del lado del alumno se permiten.
- **Datos:** no muestres la etiqueta "Datos de ejemplo".

## 7. Accesibilidad (obligatoria, WCAG 2.2 AA)
- **1.4.3 Contraste:** 4.5:1 en texto normal y 3:1 en texto grande. Incluye texto sobre la foto, textos de ejemplo en campos (placeholders) y ambos temas.
- **1.4.4 Texto al 200%:** control "Aa" con 100/125/150/200 % (cambia el `font-size` de `html`; se recuerda en `localStorage`). Con 150 % o más, las barras fijas dejan de ser fijas, el selector pasa a 2 filas de 5 y no hay scroll horizontal.
- **Lectores de pantalla:** un `h1` por pantalla; al cambiar de pantalla, el foco va al `h1` y se actualiza `document.title` ("Mis resultados · UniEval UTSC"). Tablas con `caption` y `th scope="col"`. La barra de avance es `role="progressbar"`. Las calificaciones son `radiogroup` con texto accesible ("8 de 10, alto"). Una región `aria-live` anuncia el avance. El chat es `role="log"`.
- **Teclado:** todo se puede hacer sin mouse. Teclas 0–9 califican (0 = 10) y avanzan a la siguiente fila. Al completar una pregunta, el acordeón se cierra solo y se abre la siguiente con el foco en ella. Los diálogos (confirmar envío, chat, panel de mensajes) atrapan el foco, se cierran con Esc y regresan el foco al botón que los abrió. Los menús se navegan con ↑/↓/Inicio/Fin.
- **Foco visible** de 3px `--focus` en todo lo que se puede enfocar.
- **Botón "🔊 Escuchar pregunta"** en cada pregunta (opcional según el navegador, con `speechSynthesis` en es-MX).
- **Objetivos táctiles** de al menos 44px (RNF-03).

## 8. Convenciones
- Código en inglés técnico solo donde es estándar (`Program.cs`, `Controllers`); **nombres de dominio, textos de interfaz y comentarios en español de México**.
- C#: `nullable enable`, `record` para DTOs, servicios por inyección de dependencias, `async` de punta a punta.
- Rutas del cliente: `/`, `/alumno`, `/alumno/encuesta`, `/alumno/comprobante`, `/docente/{seccion}`, `/tutor/{seccion}`, `/coordinador/{seccion}`, `/admin/{seccion}`, `/acceso/{rol}`.
- Rutas de la API: prefijo `/api`, según la sección 10 del documento de diseño (más las extensiones en `docs/PROMPTS_CODEX.md`). En desarrollo, la API corre en `http://localhost:5291` / `https://localhost:7133` (ver `launchSettings.json`).
- Nunca guardes contraseñas. Los correos de otros usuarios se muestran enmascarados (`d***@utsc.edu.mx`) salvo para el Administrador.
- Antes de terminar una tarea: `dotnet build api/UniEvalUtsc.slnx` sin errores ni advertencias nuevas y `dotnet test api/UniEvalUtsc.slnx` en verde. No borres ni rompas las pruebas existentes de `api/tests`.
- Commits pequeños por funcionalidad, en ramas `feature/<modulo>` (front: `feature/web-<modulo>`), con PR a `main`.
