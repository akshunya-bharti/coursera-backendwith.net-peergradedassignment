using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Simple token validation middleware.
/// Valid tokens are read from the VALID_TOKENS environment variable (comma-separated).
/// Falls back to a single default token "demo-token" when not configured.
/// </summary>
public sealed class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenValidationMiddleware> _logger;
    private readonly string[] _validTokens;

    public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var env = Environment.GetEnvironmentVariable("VALID_TOKENS");
        if (string.IsNullOrWhiteSpace(env))
        {
            _validTokens = new[] { "demo-token" };
        }
        else
        {
            _validTokens = env.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        // Expect header: Authorization: Bearer <token>
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var value = authHeader.ToString();
            const string bearer = "Bearer ";
            if (value.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
            {
                var token = value.Substring(bearer.Length).Trim();
                if (!string.IsNullOrEmpty(token) && _validTokens.Any(t => string.Equals(t, token, StringComparison.Ordinal)))
                {
                    await _next(context);
                    return;
                }
            }
        }

        _logger.LogWarning("Unauthorized request for {Method} {Path}", context.Request.Method, context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        // Signal the expected scheme
        context.Response.Headers["WWW-Authenticate"] = "Bearer";
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
    }
}

public static class TokenValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TokenValidationMiddleware>();
    }
}
