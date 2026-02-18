using System.Text.Json;
using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            string supportId;
            try
            {
                var logService = context.RequestServices.GetRequiredService<IAppLogService>();
                supportId = await logService.LogErrorAsync(ex, context);
            }
            catch
            {
                // If DB logging fails, still generate a support ID for the user
                supportId = $"ERR-{Random.Shared.Next(0xFFFF):X4}-{Random.Shared.Next(0xFFFF):X4}";
            }

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                var body = JsonSerializer.Serialize(new
                {
                    error = "An unexpected error occurred.",
                    supportId
                });
                await context.Response.WriteAsync(body);
            }
            else
            {
                // Store support ID for the error page to display
                context.Items["SupportId"] = supportId;
                throw; // Re-throw so UseStatusCodePagesWithReExecute can handle it
            }
        }
    }
}
