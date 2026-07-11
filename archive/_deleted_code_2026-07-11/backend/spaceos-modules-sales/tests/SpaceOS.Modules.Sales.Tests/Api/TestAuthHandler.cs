using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SpaceOS.Modules.Sales.Tests.Api;

/// <summary>Always-succeeds authentication handler for Sales endpoint tests.</summary>
public sealed class SalesTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("tenant_id", DefaultTenantId.ToString()),
            new Claim("sub", "sub:testuser"),
            new Claim("role", "sales_user"),
            new Claim("role", "tenant_admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>Always-fails authentication handler for 401 tests.</summary>
public sealed class SalesNoAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.Fail("No auth in test"));
}
