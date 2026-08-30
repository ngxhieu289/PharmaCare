using System.Security.Claims;
using System.Text.Json;
using PharmaCare.Api.Data;
using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Middleware;

public sealed class MutationAuditMiddleware(RequestDelegate next, ILogger<MutationAuditMiddleware> logger)
{
    private static readonly HashSet<string> MutatingMethods =
        [HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

    public async Task InvokeAsync(HttpContext httpContext, AppDbContext dbContext)
    {
        await next(httpContext);

        if (!MutatingMethods.Contains(httpContext.Request.Method) ||
            httpContext.Response.StatusCode is < 200 or >= 300 ||
            httpContext.Request.Path.StartsWithSegments("/api/auth") ||
            !Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return;

        try
        {
            var controller = httpContext.Request.RouteValues["controller"]?.ToString() ?? "Unknown";
            var entityId = httpContext.Request.RouteValues["id"]?.ToString()
                ?? httpContext.Request.RouteValues["userId"]?.ToString()
                ?? httpContext.Request.RouteValues["orderId"]?.ToString()
                ?? string.Empty;
            dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = $"HTTP_{httpContext.Request.Method}",
                EntityName = controller,
                EntityId = entityId,
                NewValues = JsonSerializer.Serialize(new
                {
                    Path = httpContext.Request.Path.Value,
                    StatusCode = httpContext.Response.StatusCode
                }),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
            });
            await dbContext.SaveChangesAsync(httpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to record mutation audit for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
    }
}
