// SpaceOS.Kernel.IntegrationTests/Infrastructure/RepositoryTestBase.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Abstract base class for repository integration tests that need a clean
/// <see cref="AppDbContext"/> backed by an in-memory SQLite database, without
/// starting the full HTTP host.
/// </summary>
/// <remarks>
/// Each test class that inherits this base gets an isolated database: the
/// <see cref="SqliteConnection"/> is opened and the schema is created in
/// <see cref="InitializeAsync"/>, then torn down in <see cref="DisposeAsync"/>.
/// This guarantees no shared state between test classes (or between test runs
/// when xUnit creates separate instances per test method).
/// </remarks>
public abstract class RepositoryTestBase : IAsyncLifetime
{
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Gets the <see cref="AppDbContext"/> configured with the per-test in-memory SQLite database.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    protected AppDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Initialises a new <see cref="RepositoryTestBase"/> and opens the in-memory SQLite connection.
    /// </summary>
    protected RepositoryTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    /// <summary>
    /// Creates a new <see cref="AppDbContext"/> bound to the per-test SQLite connection
    /// and ensures the database schema is created. A stub <see cref="Domain.Auth.ITenantResolver"/>
    /// returning <c>null</c> (Admin bypass) is used so all seeded data is visible to tests.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new AppDbContext(options, new NullTenantResolver());
        await DbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the <see cref="AppDbContext"/> and releases the underlying SQLite connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
