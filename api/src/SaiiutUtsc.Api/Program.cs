var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, ctx, ct) =>
{
    doc.Info.Title = "SAIIUT-UTSC API";
    doc.Info.Description = "Encuestas de desempeño docente y de tutor";
    return Task.CompletedTask;
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "SAIIUT API v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
