using System.Net;
using System.Text.Json;
using ParkSentry.Application.Common;

namespace ParkSentry.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex, context.RequestServices.GetRequiredService<IWebHostEnvironment>());
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, IWebHostEnvironment env)
    {
        var (status, message) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message),
            ForbiddenException => (HttpStatusCode.Forbidden, exception.Message),
            ValidationException => (HttpStatusCode.BadRequest, exception.Message),
            ParkSentry.Application.Common.ApplicationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, env.IsDevelopment() || env.EnvironmentName == "Testing" ? exception.Message : "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
