using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Infrastructure.InMemory;
using UniEvalUtsc.Api.Auth;
using UniEvalUtsc.Api.Middleware;
using UniEvalUtsc.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
var modoAuth = builder.Configuration["Auth:Modo"] ?? "Simulado";
builder.Services.AddScoped<IUsuarioRepository, RepositorioUsuarioInMemory>();
if (string.Equals(modoAuth, "Simulado", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddAuthentication("Simulado").AddScheme<AuthenticationSchemeOptions, UsuarioSimuladoHandler>("Simulado", _ => { });
}
else
{
    // Registro futuro: AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd")) cuando exista el registro Entra ID.
    throw new InvalidOperationException("El modo de autenticación configurado no está disponible.");
}
builder.Services.AddAuthorization(options =>
{
    foreach (var rol in new[] { "Alumno", "Docente", "Tutor", "Coordinador", "Administrador" })
        options.AddPolicy(rol, policy => policy.RequireAuthenticatedUser().RequireRole(rol));
});
var origenesPermitidos = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("WebClient", policy =>
    policy.WithOrigins(origenesPermitidos)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var proveedorDatos = builder.Configuration["Datos:Proveedor"] ?? "InMemory";
if (string.Equals(proveedorDatos, "InMemory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<AlmacenEvaluacionInMemory>();
    builder.Services.AddScoped<IPeriodoRepository, RepositorioPeriodoInMemory>();
    builder.Services.AddScoped<IPlantillaRepository, RepositorioPlantillaInMemory>();
    builder.Services.AddScoped<IEncuestaAsignadaRepository, RepositorioEncuestaAsignadaInMemory>();
    builder.Services.AddScoped<RepositorioRespuestaInMemory>();
    builder.Services.AddScoped<IRespuestaRepository>(services =>
        services.GetRequiredService<RepositorioRespuestaInMemory>());
    builder.Services.AddScoped<IUnitOfWork, UnidadDeTrabajoInMemory>();
    builder.Services.AddSingleton<IReloj, RelojSistema>();
    builder.Services.AddScoped<EnviarRespuestaService>();
    builder.Services.AddScoped<ConsultarEncuestasAlumnoService>();
    builder.Services.AddScoped<ICatalogoConsultaRepository, RepositorioCatalogoConsultaInMemory>();
}
else
{
    throw new InvalidOperationException(
        $"El proveedor de datos '{proveedorDatos}' no está configurado en esta versión.");
}

builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, ctx, ct) =>
{
    doc.Info.Title = "UniEval UTSC API";
    doc.Info.Description = "Encuestas de desempeño docente y de tutor";
    if (string.Equals(modoAuth, "Simulado", StringComparison.OrdinalIgnoreCase))
    {
        var componentes = doc.Components ?? new Microsoft.OpenApi.OpenApiComponents();
        doc.Components = componentes;
        componentes.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        var esquema = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
            Name = "X-Usuario-Simulado",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description = "Id de un usuario semilla. Ejemplos: 1 (Alumno), 2 (Docente), 3 (Tutor), 4 (Coordinador), 5 (Administrador), 6 (Docente y Tutor)."
        };
        componentes.SecuritySchemes["UsuarioSimulado"] = esquema;
        var referencia = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("UsuarioSimulado", doc);
        var rutas = (IEnumerable<Microsoft.OpenApi.IOpenApiPathItem>?)doc.Paths?.Values
            ?? Enumerable.Empty<Microsoft.OpenApi.IOpenApiPathItem>();
        foreach (var operacion in rutas.SelectMany(path => path.Operations!.Values))
        {
            operacion.Security ??= [];
            operacion.Security.Add(new Microsoft.OpenApi.OpenApiSecurityRequirement { [referencia] = [] });
        }
    }

    var nodosRuta = doc.Paths;
    if (nodosRuta is not null && nodosRuta.TryGetValue("/api/mis-encuestas/lote/respuestas", out var lotePath)
        && lotePath.Operations?.Values.FirstOrDefault() is { } loteOperacion)
    {
        if (loteOperacion.RequestBody?.Content?.TryGetValue("application/json", out var tipoLote) == true)
        {
            tipoLote.Examples ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiExample>();
            tipoLote.Examples["Lote completo"] = new Microsoft.OpenApi.OpenApiExample
            {
                Summary = "Una respuesta anónima por asignación",
                Value = System.Text.Json.Nodes.JsonNode.Parse("""
                    [
                      { "encuestaAsignadaId": 1, "respuestas": [
                        { "aspectoId": 1, "calificacion": 8 }, { "aspectoId": 2, "calificacion": 8 },
                        { "aspectoId": 3, "calificacion": 8 }, { "aspectoId": 4, "calificacion": 8 },
                        { "aspectoId": 5, "calificacion": 8 }, { "aspectoId": 6, "calificacion": 8 },
                        { "aspectoId": 7, "calificacion": 8 }, { "aspectoId": 8, "calificacion": 8 },
                        { "aspectoId": 9, "calificacion": 8 }, { "aspectoId": 10, "calificacion": 8 }
                      ] }
                    ]
                    """)
            };
        }
        if (loteOperacion.Responses?.TryGetValue("204", out var respuestaLote) == true)
            respuestaLote.Description = "204 No Content: todas las asignaciones del lote se guardaron; si alguna falla, no se guarda ninguna.";
    }

    if (nodosRuta is not null && nodosRuta.TryGetValue("/api/mis-encuestas/evaluacion", out var evaluacionPath)
        && evaluacionPath.Operations?.Values.FirstOrDefault() is { } evaluacionOperacion
        && evaluacionOperacion.Responses?.TryGetValue("200", out var respuestaEvaluacion) == true
        && respuestaEvaluacion.Content?.TryGetValue("application/json", out var tipoEvaluacion) == true)
    {
        tipoEvaluacion.Examples ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiExample>();
        tipoEvaluacion.Examples["Evaluación unificada"] = new Microsoft.OpenApi.OpenApiExample
        {
            Summary = "Aspecto con los evaluados del alumno",
            Value = System.Text.Json.Nodes.JsonNode.Parse("""
                {
                  "periodo": "Sep–Dic 2026",
                  "preguntas": [{
                    "aspectoId": 1,
                    "numero": 1,
                    "texto": "Al inicio del curso dio a conocer los objetivos.",
                    "evaluados": [{ "encuestaAsignadaId": 1, "nombre": "Docente", "materia": "Inglés V", "tipo": 0 }]
                  }]
                }
                """)
        };
    }
    return Task.CompletedTask;
}));

var app = builder.Build();
app.UseMiddleware<ErroresMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "UniEval UTSC API v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
