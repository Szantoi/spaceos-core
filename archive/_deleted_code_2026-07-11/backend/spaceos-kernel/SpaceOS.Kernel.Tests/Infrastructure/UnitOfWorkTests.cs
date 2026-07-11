// SpaceOS.Kernel.Tests/Infrastructure/UnitOfWorkTests.cs

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Domain.Auth;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>Unit tests for <see cref="UnitOfWork"/>.</summary>
public sealed class UnitOfWorkTests
{
    /// <summary>
    /// Builds an <see cref="AppDbContext"/> backed by an in-memory SQLite connection
    /// so that <see cref="AppDbContext.SaveChangesAsync"/> can be exercised without
    /// a real PostgreSQL server.
    /// </summary>
    private static (AppDbContext Context, SqliteConnection Connection) BuildContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var resolverMock = new Mock<ITenantResolver>();
        resolverMock.Setup(r => r.TryResolve()).Returns((SpaceOS.Kernel.Domain.ValueObjects.TenantId?)null);

        var context = new AppDbContext(options, resolverMock.Object);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoTrackedChanges_CompletesWithoutException()
    {
        // Arrange
        var (context, connection) = BuildContext();
        await using (connection)
        await using (context)
        {
            var uow = new UnitOfWork(context);

            // Act + Assert — no exception expected; 0 entities tracked means 0 rows written
            await uow.SaveChangesAsync(TestContext.Current.CancellationToken);
            Assert.True(true);
        }
    }
}
