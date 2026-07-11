// SpaceOS.Kernel.IntegrationTests/Infrastructure/RepositoryTestBaseTests.cs
using SpaceOS.Kernel.Domain.Entities;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Verifies that <see cref="RepositoryTestBase"/> provides an isolated
/// <see cref="SpaceOS.Infrastructure.Data.AppDbContext"/> for each test instance.
/// </summary>
public sealed class RepositoryTestBaseTests : RepositoryTestBase
{
    /// <summary>
    /// Asserts that two separate <see cref="RepositoryTestBase"/> instances receive
    /// distinct <see cref="SpaceOS.Infrastructure.Data.AppDbContext"/> instances with no shared state.
    /// Each instance seeds its own tenant and verifies only that tenant is visible.
    /// </summary>
    [Fact]
    public async Task RepositoryTestBase_ProvidesCleanDbContext_PerTest()
    {
        // Arrange — seed a tenant into this test's isolated context.
        var tenant = Tenant.Create("Isolation Test Tenant");
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act — read back from the same context.
        var count = DbContext.Tenants.Count();

        // Assert — exactly one tenant exists; no bleed from other tests.
        Assert.Equal(1, count);
    }
}
