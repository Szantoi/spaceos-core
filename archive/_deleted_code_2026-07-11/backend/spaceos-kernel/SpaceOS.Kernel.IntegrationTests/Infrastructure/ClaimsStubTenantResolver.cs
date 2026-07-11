// SpaceOS.Kernel.IntegrationTests/Infrastructure/ClaimsStubTenantResolver.cs
using Microsoft.AspNetCore.Http;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Integration-test stub for <see cref="ITenantResolver"/> that reads the <c>tid</c> claim
/// from the current <see cref="HttpContext"/>, mirroring the production <c>HttpTenantResolver</c>.
/// Falls back to <see cref="SpaceOsApiFactory.TestTenantId"/> when no HTTP context or <c>tid</c> claim
/// is present so that repository-level tests (which have no HTTP request) remain unaffected.
/// </summary>
internal sealed class ClaimsStubTenantResolver(IHttpContextAccessor httpContextAccessor) : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc/>
    public TenantId? TryResolve()
    {
        var tidClaim = _httpContextAccessor.HttpContext?
            .User.FindFirst("tid")?.Value;

        if (string.IsNullOrWhiteSpace(tidClaim))
            return SpaceOsApiFactory.TestTenantId;

        return Guid.TryParse(tidClaim, out var guid)
            ? TenantId.From(guid)
            : SpaceOsApiFactory.TestTenantId;
    }
}
