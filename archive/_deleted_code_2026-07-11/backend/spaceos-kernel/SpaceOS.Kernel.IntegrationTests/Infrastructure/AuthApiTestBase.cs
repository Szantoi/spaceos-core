// SpaceOS.Kernel.IntegrationTests/Infrastructure/AuthApiTestBase.cs
using System.Net.Http.Headers;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that require an authenticated HTTP client.
/// Inherits <see cref="ApiTestBase"/> and exposes a <see cref="GenerateToken"/>
/// helper for test methods that need to create additional role-scoped clients.
/// The default <see cref="ApiTestBase.Client"/> is pre-authorised with an Admin JWT.
/// </summary>
public abstract class AuthApiTestBase : ApiTestBase
{
    /// <summary>
    /// Generates a signed JWT for the given role scoped to <see cref="SpaceOsApiFactory.TestTenantId"/>
    /// by default, or to an explicit <paramref name="tenantId"/> when provided.
    /// </summary>
    /// <param name="role">The RBAC role claim value (e.g. "Admin", "Designer", "Joiner").</param>
    /// <param name="tenantId">
    /// The tenant identifier to embed in the <c>tid</c> claim.
    /// Defaults to <see cref="SpaceOsApiFactory.TestTenantId"/> when <see langword="null"/>.
    /// </param>
    /// <returns>A signed JWT Bearer token string.</returns>
    protected static string GenerateToken(string role, Guid? tenantId = null) =>
        JwtTokenHelper.GenerateToken(
            role,
            tenantId ?? SpaceOsApiFactory.TestTenantId.Value);

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> from the factory and attaches a Bearer token
    /// for the given role to its default request headers.
    /// The caller is responsible for disposing the returned client.
    /// </summary>
    /// <param name="role">The RBAC role claim value.</param>
    /// <param name="tenantId">
    /// The tenant identifier for the <c>tid</c> claim.
    /// Defaults to <see cref="SpaceOsApiFactory.TestTenantId"/> when <see langword="null"/>.
    /// </param>
    /// <returns>An authorised <see cref="HttpClient"/>.</returns>
    protected HttpClient CreateClientForRole(string role, Guid? tenantId = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(role, tenantId));
        return client;
    }
}
