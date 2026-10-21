using Microsoft.AspNetCore.Mvc;
using UniEvalUtsc.Application.Exceptions;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Api.Middleware;

public sealed class ErroresMiddleware(RequestDelegate next, ILogger<ErroresMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            var (status, titulo) = ex switch
            {
                NoEncontradoException => (404, "No encontrado"),
                AccesoDenegadoException => (403, "Acceso denegado"),
                ValidacionException => (400, "Solicitud inválida"),
                DomainException => (409, "Conflicto de negocio"),
                _ => (500, "Error interno")
            };
            if (status == 500) logger.LogError(ex, "Error no controlado en la API");
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = titulo, Detail = ex.Message, Instance = context.Request.Path });
        }
    }
}
