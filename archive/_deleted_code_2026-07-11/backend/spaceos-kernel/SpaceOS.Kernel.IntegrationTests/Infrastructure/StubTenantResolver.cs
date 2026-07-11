// SpaceOS.Kernel.IntegrationTests/Infrastructure/StubTenantResolver.cs
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Test stub for <see cref="ITenantResolver"/> that always returns a fixed <see cref="TenantId"/>.
/// Used by <see cref="SpaceOsApiFactory"/> to isolate integration tests to a single tenant.
/// </summary>
internal sealed class StubTenantResolver(TenantId tenantId) : ITenantResolver
{
    private readonly TenantId _tenantId = tenantId;

    /// <inheritdoc/>
    public TenantId? TryResolve() => _tenantId;
}
