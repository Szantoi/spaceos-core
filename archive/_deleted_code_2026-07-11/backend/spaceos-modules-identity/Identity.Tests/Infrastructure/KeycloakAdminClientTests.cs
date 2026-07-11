// Identity.Tests/Infrastructure/KeycloakAdminClientTests.cs

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Keycloak;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Identity.Tests.Infrastructure;

// Local DTO for test response construction (KcUserRepresentation is internal)
file sealed class TestKcUser
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, List<string>>? Attributes { get; set; }
}

public sealed class KeycloakAdminClientTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Email _email = Email.From("user@example.com");
    private readonly DisplayName _displayName = DisplayName.From("Kovács", "János");

    private static HttpClient BuildHttpClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => new(new DelegatingHandlerStub(handler)) { BaseAddress = new Uri("http://localhost:8080") };

    private KeycloakAdminClient BuildClient(
        HttpClient http, KeycloakOptions? opts = null)
    {
        var options = opts ?? new KeycloakOptions
        {
            BaseUrl = "http://localhost:8080/auth",
            Realm = "spaceos",
            ClientId = "spaceos-identity-service",
            ClientSecret = "test-secret"
        };

        var tokenProviderMock = new Mock<IKeycloakTokenProvider>();
        tokenProviderMock.Setup(tp => tp.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync("test-token");

        return new KeycloakAdminClient(
            http, tokenProviderMock.Object,
            Options.Create(options),
            NullLogger<KeycloakAdminClient>.Instance);
    }

    [Fact]
    public async Task CreateUserAsync_TidMismatch_ThrowsIdentityProviderException()
    {
        var kcId = "kc-user-123";
        var wrongTenantId = Guid.NewGuid();

        var http = BuildHttpClient(req =>
        {
            if (req.Method == HttpMethod.Post)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.Created);
                resp.Headers.Location = new Uri($"http://localhost:8080/auth/admin/realms/spaceos/users/{kcId}");
                return resp;
            }
            if (req.Method == HttpMethod.Get)
            {
                var user = new TestKcUser
                {
                    Id = kcId,
                    Email = "user@example.com",
                    FirstName = "Kovács",
                    LastName = "János",
                    Attributes = new() { ["tid"] = [wrongTenantId.ToString()] }
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(user)
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = BuildClient(http);

        await Assert.ThrowsAsync<IdentityProviderException>(
            () => client.CreateUserAsync(_tenantId, _email, _displayName));
    }

    [Fact]
    public async Task CreateUserAsync_HttpError_ThrowsIdentityProviderException()
    {
        var http = BuildHttpClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = BuildClient(http);

        await Assert.ThrowsAsync<IdentityProviderException>(
            () => client.CreateUserAsync(_tenantId, _email, _displayName));
    }

    [Fact]
    public void IdentityProviderException_StoresStatusCode()
    {
        var ex = new IdentityProviderException("Forbidden", 403);
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal("Forbidden", ex.Message);
    }

    [Fact]
    public async Task CreateUserAsync_MissingLocationHeader_ThrowsIdentityProviderException()
    {
        var http = BuildHttpClient(req =>
        {
            if (req.Method == HttpMethod.Post)
                return new HttpResponseMessage(HttpStatusCode.Created); // no Location
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = BuildClient(http);

        await Assert.ThrowsAsync<IdentityProviderException>(
            () => client.CreateUserAsync(_tenantId, _email, _displayName));
    }
}
