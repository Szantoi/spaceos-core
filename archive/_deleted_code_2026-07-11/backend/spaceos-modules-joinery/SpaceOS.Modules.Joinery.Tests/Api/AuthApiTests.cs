using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// HTTP-layer security tests:
/// SEC-04 — every protected endpoint requires a valid Manufacturer JWT.
/// SEC-03 — wrong tenant_type gets 403 Forbidden.
/// </summary>
[Collection("Integration")]
public sealed class AuthApiTests : IClassFixture<JoineryWebFactory>
{
    private readonly JoineryWebFactory _factory;
    private static readonly Guid _anyId = Guid.NewGuid();

    public AuthApiTests(JoineryWebFactory factory) => _factory = factory;

    // ─── 401 — No token on every protected endpoint (SEC-04) ─────────────────

    [Theory]
    [InlineData("GET",  "/api/orders")]
    [InlineData("GET",  "/api/orders/{id}")]
    [InlineData("POST", "/api/orders")]
    [InlineData("POST", "/api/orders/{id}/items")]
    [InlineData("POST", "/api/orders/{id}/calculate")]
    [InlineData("GET",  "/api/orders/{id}/cutting-list")]
    [InlineData("GET",  "/api/orders/{id}/process-plan")]
    [InlineData("GET",  "/api/orders/{id}/hardware-list")]
    [InlineData("GET",  "/api/orders/{id}/material-req")]
    [InlineData("POST", "/api/orders/{id}/submit")]
    public async Task ProtectedEndpoints_WithoutToken_Return401(string method, string path)
    {
        var client = _factory.CreateClient();
        var url = path.Replace("{id}", _anyId.ToString());
        using var req = new HttpRequestMessage(new HttpMethod(method), url);

        var resp = await client.SendAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: $"{method} {path} requires authentication (SEC-04)");
    }

    // ─── 200 — Health endpoint is anonymous ──────────────────────────────────

    [Fact]
    public async Task Health_WithoutToken_Returns200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── 403 — Wrong tenant_type (non-Manufacturer) ──────────────────────────

    [Theory]
    [InlineData("GET",  "/api/orders")]
    [InlineData("POST", "/api/orders")]
    [InlineData("GET",  "/api/orders/{id}/cutting-list")]
    public async Task ProtectedEndpoints_NonManufacturerToken_Return403(string method, string path)
    {
        var url = path.Replace("{id}", _anyId.ToString());
        var token = _factory.MakeToken(Guid.NewGuid().ToString(), tenantType: "Supplier");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var req = new HttpRequestMessage(new HttpMethod(method), url);
        var resp = await client.SendAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            because: $"ManufacturerOnly policy rejects tenant_type=Supplier (SEC-04)");
    }

    // ─── 401 — Tampered / invalid token ──────────────────────────────────────

    [Fact]
    public async Task ListOrders_TamperedToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt");

        var resp = await client.GetAsync("/api/orders");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── 401 — Token signed with wrong key ───────────────────────────────────

    [Fact]
    public async Task ListOrders_WrongSigningKey_Returns401()
    {
        // Sign with a different secret — factory rejects it
        var otherKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("totally-wrong-secret-key-aaaaaaaaaaaaaa!"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            otherKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: [new System.Security.Claims.Claim("tenant_id", Guid.NewGuid().ToString()),
                     new System.Security.Claims.Claim("tenant_type", "Manufacturer")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

        var resp = await client.GetAsync("/api/orders");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
