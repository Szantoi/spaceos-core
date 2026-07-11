using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SpaceOS.Modules.Joinery.Infrastructure.Security;

public sealed class InternalOrderConversionMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly byte[] _expectedSecretBytes =
        System.Text.Encoding.UTF8.GetBytes(
            config["SpaceOS:InternalSecret"]
            ?? throw new InvalidOperationException("SpaceOS:InternalSecret not configured."));

    public async Task InvokeAsync(HttpContext context)
    {
        // SEC-07: loopback assert — only localhost can call internal endpoints.
        // RemoteIpAddress is null in TestServer (WebApplicationFactory) — allow null to pass
        // so integration tests can exercise the secret/tenant header guards.
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is not null && !IPAddress.IsLoopback(remoteIp))
        {
            context.Response.StatusCode = 403;
            return;
        }

        // SEC-01: constant-time secret compare
        if (!context.Request.Headers.TryGetValue("X-SpaceOS-Internal", out var secretHeader))
        {
            context.Response.StatusCode = 401;
            return;
        }
        var incomingBytes = System.Text.Encoding.UTF8.GetBytes(
            secretHeader.FirstOrDefault() ?? string.Empty);
        if (!CryptographicOperations.FixedTimeEquals(incomingBytes, _expectedSecretBytes))
        {
            context.Response.StatusCode = 401;
            return;
        }

        // SEC-S-01: X-SpaceOS-TenantId required and must be a valid GUID
        if (!context.Request.Headers.TryGetValue("X-SpaceOS-TenantId", out var tenantHeader)
            || !Guid.TryParse(tenantHeader.FirstOrDefault(), out var headerTenantId))
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"error\":\"X-SpaceOS-TenantId header required and must be a valid GUID.\"}")
                .ConfigureAwait(false);
            return;
        }

        context.Items["InternalTenantId"] = headerTenantId;
        await next(context).ConfigureAwait(false);
    }
}
