# UniEval UTSC

Aplicación PWA para las encuestas de desempeño docente y de tutor de la UTSC. La solución principal es `UniEvalUtsc.slnx`.

## Mapa de carpetas

- `web/`: interfaz Blazor y pruebas del cliente (front: Eve y Kat).
- `api/`: API, dominio, aplicación, infraestructura y pruebas (back: Dan).
- `db/`: scripts y esquema de MariaDB (BD: Karen).
- `shared/`: contratos DTO y enums compartidos entre API y cliente.
- `docs/diseno/`: prototipo y documento de diseño.
- `docs/proceso/`: plan, decisiones y prompts de trabajo.

## Requisitos

- .NET SDK 10.
- Certificado HTTPS de desarrollo confiable si se desea ejecutar la API con `https://localhost:7133`.

## Ejecutar API y cliente web

Restaura y compila la solución desde la raíz:

```powershell
dotnet build UniEvalUtsc.slnx
```

Abre dos terminales desde la raíz del repositorio.

Terminal 1 — API (Swagger en `/swagger`; el proveedor predeterminado es `InMemory`):

```powershell
dotnet run --project api/src/UniEvalUtsc.Api
```

Terminal 2 — cliente Blazor WebAssembly PWA:

```powershell
dotnet run --project web/UniEvalUtsc.Web
```

La web queda disponible en `http://localhost:5186` y toma la dirección de la API de `web/UniEvalUtsc.Web/wwwroot/appsettings.json`. El origen de desarrollo está permitido por CORS en la configuración de Development de la API.

## Pruebas

```powershell
dotnet test UniEvalUtsc.slnx
```

Las pruebas de componentes usan xUnit y bUnit. Las pruebas E2E usan xUnit y Microsoft.Playwright; para ejecutarlas contra una web iniciada, configura `UNIEVAL_WEB_URL` con su URL y asegúrate de tener instalado el navegador Chromium de Playwright.

Después de compilar, instala Chromium y ejecuta la prueba E2E contra la web iniciada:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File web/tests/UniEvalUtsc.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
$env:UNIEVAL_WEB_URL = "http://localhost:5186"
dotnet test web/tests/UniEvalUtsc.E2E/UniEvalUtsc.E2E.csproj
```
