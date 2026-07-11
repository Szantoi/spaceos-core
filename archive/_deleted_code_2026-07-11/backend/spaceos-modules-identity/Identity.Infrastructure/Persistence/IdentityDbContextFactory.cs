// Identity.Infrastructure/Persistence/IdentityDbContextFactory.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools (migrations) at design time.
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("IDENTITY_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=spaceos;Username=spaceos_app;Password=dev";

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__efmigrations_history", "identity"))
            .Options;

        return new IdentityDbContext(options);
    }
}
