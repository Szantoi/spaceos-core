// SpaceOS.Kernel.Api.Tests/Infrastructure/FixedTenantResolver.cs
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Api.Tests.Infrastructure;

/// <summary>
/// Test stub for <see cref="ITenantResolver"/> that always returns a fixed non-null <see cref="TenantId"/>.
/// Used in <see cref="ApiFactory"/> for endpoint tests that exercise write operations,
/// where a valid non-empty TenantId is required for domain aggregate creation.
/// </summary>
internal sealed class FixedTenantResolver(TenantId tenantId) : ITenantResolver
{
    private readonly TenantId _tenantId = tenantId;

    /// <inheritdoc/>
    public TenantId? TryResolve() => _tenantId;
}
