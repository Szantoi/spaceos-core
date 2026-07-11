// SpaceOS.Kernel.IntegrationTests/Infrastructure/NullTenantResolver.cs
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Test stub for <see cref="ITenantResolver"/> that always returns <c>null</c>,
/// triggering the Admin bypass path in global query filters.
/// Used by <see cref="RepositoryTestBase"/> so all seeded data is visible across tenants.
/// </summary>
internal sealed class NullTenantResolver : ITenantResolver
{
    /// <inheritdoc/>
    public TenantId? TryResolve() => null;
}
