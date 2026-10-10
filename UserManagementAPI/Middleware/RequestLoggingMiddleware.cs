using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace UserManagementAPI.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // If an exception bubbles up, log it with a 500 marker and rethrow so other handlers can run
            _logger.LogError(ex, "HTTP {Method} {Path} -> Exception", method, path);
            throw;
        }
        finally
        {
            var status = context.Response?.StatusCode;
            _logger.LogInformation("HTTP {Method} {Path} -> {StatusCode}", method, path, status);
        }
    }
}

public static class RequestLoggingExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
