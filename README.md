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

## Publicar y servir la demo

Publica el cliente Blazor en una carpeta estática:

```powershell
dotnet publish web/UniEvalUtsc.Web -c Release -o publicado -p:WasmApplicationEnvironmentName=Demo
```

Para habilitar el selector de usuarios semilla en la demo, el servidor estático debe enviar el encabezado `Blazor-Environment: Demo` al servir `publicado/wwwroot`. Por ejemplo, con `dotnet-serve`:

```powershell
dotnet tool install --global dotnet-serve
dotnet-serve -d publicado/wwwroot -p 5199 --fallback-file index.html -h "Blazor-Environment: Demo"
```

El entorno `Demo` carga `appsettings.Demo.json` y activa la autenticación simulada. Al publicar, `WasmApplicationEnvironmentName=Demo` fija el entorno del cliente en `Demo`; el encabezado permite que el hosting estático anuncie el mismo entorno. Las publicaciones normales sin esa propiedad usan `Production`, que conserva Entra y no ofrece el acceso de demo.
