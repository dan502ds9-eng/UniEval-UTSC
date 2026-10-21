using UniEvalUtsc.Application.Interfaces;
using UniEvalUtsc.Infrastructure.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
    return Task.CompletedTask;
}));

var app = builder.Build();

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
app.UseAuthorization();
app.MapControllers();

app.Run();
