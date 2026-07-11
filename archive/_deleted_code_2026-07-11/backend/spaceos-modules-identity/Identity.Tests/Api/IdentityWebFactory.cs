// Identity.Tests/Api/IdentityWebFactory.cs

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Identity.Application.Common;
using Identity.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using StackExchange.Redis;

namespace Identity.Tests.Api;

/// <summary>
/// WebApplicationFactory that replaces real infrastructure with mocks/stubs
/// so API routing, auth enforcement, and serialization can be tested in-process.
/// </summary>
public sealed class IdentityWebFactory : WebApplicationFactory<Program>
{
    /// <summary>Shared mock — reset between tests if needed.</summary>
    public Mock<IMediator> MediatorMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration BEFORE service registration runs in Program.cs
        builder.UseSetting("Redis:ConnectionString", "");
        builder.UseSetting("Jwt:Authority", "");
        // AllowedHosts:"127.0.0.1" rejects TestServer's "localhost" Host header
        builder.UseSetting("AllowedHosts", "*");

        // Reset Kestrel binding — TestServer uses an in-process transport;
        // the port-5008/localhost-only restriction causes "Invalid Hostname" 400s.
        builder.ConfigureKestrel(opts => { opts.ListenAnyIP(0); });

        builder.ConfigureServices(services =>
        {
            // ── Swap real EF Core for in-memory ──────────────────────────────
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.AddDbContext<IdentityDbContext>(opts =>
                opts.UseInMemoryDatabase("IdentityTestDb"));

            // ── Swap Redis for null (graceful fallback path in services) ─────
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ => null!);

            // ── Replace MediatR with mock ────────────────────────────────────
            services.RemoveAll<IMediator>();
            services.AddSingleton(MediatorMock.Object);

            // ── Replace JWT Bearer with test auth scheme ──────────────────────
            // Remove existing JwtBearer options so no OIDC discovery is attempted
            services.PostConfigureAll<JwtBearerOptions>(opts =>
            {
                opts.Authority           = null;
                opts.MetadataAddress     = string.Empty;
                opts.RequireHttpsMetadata = false;
                opts.BackchannelHttpHandler = new AlwaysFailHttpHandler();
                opts.MapInboundClaims    = false;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = false,
                    ValidateAudience         = false,
                    ValidateLifetime         = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = TestJwtHelper.SecurityKey,
                    NameClaimType            = "preferred_username",
                    RoleClaimType            = "realm_access.roles"
                };
            });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>Creates an HttpClient pre-authenticated with a fake JWT.</summary>
    public HttpClient CreateClientWithFakeJwt(
        Guid? tenantId = null,
        Guid? userId   = null,
        string? role   = null)
    {
        var token = TestJwtHelper.CreateToken(
            tenantId ?? Guid.NewGuid(),
            userId   ?? Guid.NewGuid(),
            role);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Backchannel HTTP handler that always fails — prevents any OIDC discovery call.
    /// </summary>
    private sealed class AlwaysFailHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
    }
}

internal static class TestJwtHelper
{
    private static readonly byte[] KeyBytes =
        Encoding.UTF8.GetBytes("identity-test-signing-key-32bytes!!");

    public static readonly SymmetricSecurityKey SecurityKey =
        new(KeyBytes);

    public static string CreateToken(Guid tenantId, Guid userId, string? role)
    {
        var claims = new List<Claim>
        {
            new("sub",                "test-sub"),
            new("tid",                tenantId.ToString()),
            new("preferred_username", "testuser"),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (role is not null)
            claims.Add(new Claim("realm_access.roles", role));

        var creds = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:            "test",
            audience:          "identity-api",
            claims:            claims,
            expires:           DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
