// SpaceOS.Kernel.IntegrationTests/AuditLog/AuditEventRepositoryTests.cs

using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog.Specifications;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.AuditLog;

/// <summary>
/// Integration tests that verify <see cref="AuditEventRepository"/> behaviour
/// against a real in-memory SQLite schema created by <see cref="AuditRepositoryTestBase"/>.
/// </summary>
public sealed class AuditEventRepositoryTests : AuditRepositoryTestBase
{
    // -------------------------------------------------------------------------
    // AddAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="AuditEventRepository.AddAsync"/> followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persists
    /// the entity and it can subsequently be retrieved via <see cref="AllAuditEventsSpec"/>.
    /// </summary>
    [Fact]
    public async Task AuditEventRepository_AddAsync_PersistsToDatabase()
    {
        // Arrange
        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(),
            "TestEvent",
            Guid.NewGuid(),
            "{}",
            "a".PadLeft(64, 'a'));

        var repository = new AuditEventRepository(AuditContext);

        // Act
        await repository.AddAsync(auditEvent, TestContext.Current.CancellationToken);
        await AuditContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        AuditContext.ChangeTracker.Clear();

        var results = await repository.ListAsync(new AllAuditEventsSpec(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(results);
        Assert.Equal(auditEvent.Id, results[0].Id);
    }

    /// <summary>
    /// Verifies that two <see cref="AuditEventRepository.AddAsync"/> calls followed by
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> persist both
    /// entities and they can subsequently be retrieved via <see cref="AllAuditEventsSpec"/>.
    /// </summary>
    [Fact]
    public async Task AuditEventRepository_AddAsync_MultipleEvents_AllPersisted()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        var firstEvent = AuditEvent.Create(
            tenantId,
            "FirstEvent",
            Guid.NewGuid(),
            "{}",
            "a".PadLeft(64, 'a'));

        var secondEvent = AuditEvent.Create(
            tenantId,
            "SecondEvent",
            Guid.NewGuid(),
            "{}",
            "b".PadLeft(64, 'b'));

        var repository = new AuditEventRepository(AuditContext);

        // Act
        await repository.AddAsync(firstEvent, TestContext.Current.CancellationToken);
        await repository.AddAsync(secondEvent, TestContext.Current.CancellationToken);
        await AuditContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        AuditContext.ChangeTracker.Clear();

        var results = await repository.ListAsync(new AllAuditEventsSpec(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Id == firstEvent.Id);
        Assert.Contains(results, r => r.Id == secondEvent.Id);
    }
}
