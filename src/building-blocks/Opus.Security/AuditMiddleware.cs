using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Opus.Security;

public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task Invoke(HttpContext ctx)
    {
        await _next(ctx);
        _logger.LogInformation("AUDIT {Method} {Path} {Status} {User}", ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, ctx.User?.Identity?.Name);
    }
}
