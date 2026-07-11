// SpaceOS.Kernel.Api.Tests/Infrastructure/ApiClaimsTenantResolver.cs
using Microsoft.AspNetCore.Http;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Api.Tests.Infrastructure;

/// <summary>
/// Test stub for <see cref="ITenantResolver"/> used in the Api.Tests project.
/// Reads the <c>tid</c> claim from the current <see cref="HttpContext"/> JWT,
/// mirroring the production <c>HttpTenantResolver</c>.
/// Falls back to <see cref="ApiFactory.TestTenantId"/> when no <c>tid</c> claim is present.
/// </summary>
internal sealed class ApiClaimsTenantResolver(IHttpContextAccessor httpContextAccessor) : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc/>
    public TenantId? TryResolve()
    {
        var tidClaim = _httpContextAccessor.HttpContext?
            .User.FindFirst("tid")?.Value;

        if (string.IsNullOrWhiteSpace(tidClaim))
            return ApiFactory.TestTenantId;

        return Guid.TryParse(tidClaim, out var guid)
            ? TenantId.From(guid)
            : ApiFactory.TestTenantId;
    }
}
