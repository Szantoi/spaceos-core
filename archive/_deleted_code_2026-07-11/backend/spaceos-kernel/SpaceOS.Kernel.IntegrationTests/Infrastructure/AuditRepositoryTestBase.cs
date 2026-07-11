// SpaceOS.Kernel.IntegrationTests/Infrastructure/AuditRepositoryTestBase.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Abstract base class for audit-log repository integration tests that need a clean
/// <see cref="AuditDbContext"/> backed by an in-memory SQLite database.
/// </summary>
/// <remarks>
/// Each test class that inherits this base gets an isolated database: the
/// <see cref="SqliteConnection"/> is opened and the schema is created in
/// <see cref="InitializeAsync"/>, then torn down in <see cref="DisposeAsync"/>.
/// This guarantees no shared state between test classes (or between test runs
/// when xUnit creates separate instances per test method).
/// </remarks>
public abstract class AuditRepositoryTestBase : IAsyncLifetime
{
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Gets the <see cref="AuditDbContext"/> configured with the per-test in-memory SQLite database.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    protected AuditDbContext AuditContext { get; private set; } = null!;

    /// <summary>
    /// Initialises a new <see cref="AuditRepositoryTestBase"/> and opens the in-memory SQLite connection.
    /// </summary>
    protected AuditRepositoryTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    /// <summary>
    /// Creates a new <see cref="AuditDbContext"/> bound to the per-test SQLite connection
    /// and ensures the database schema is created.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        AuditContext = new AuditDbContext(BuildOptions());
        await AuditContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the <see cref="AuditDbContext"/> and releases the underlying SQLite connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await AuditContext.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Builds <see cref="DbContextOptions{AuditDbContext}"/> bound to the shared SQLite connection.
    /// Derived tests that need multiple context instances (e.g. concurrency tests) can call
    /// this method to create additional contexts sharing the same physical connection.
    /// </summary>
    protected DbContextOptions<AuditDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite(_connection)
            .Options;
}
