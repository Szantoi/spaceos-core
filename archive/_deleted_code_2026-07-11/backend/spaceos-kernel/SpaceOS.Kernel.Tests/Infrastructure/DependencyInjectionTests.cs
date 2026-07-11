// SpaceOS.Kernel.Tests/Infrastructure/DependencyInjectionTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SpaceOS.Infrastructure;
using SpaceOS.Infrastructure.Data;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>Tests for <see cref="DependencyInjection"/>.</summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ProductionEnvironment_RegistersNpgsqlProvider()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=spaceos_test;Username=spaceos;Password=test"
            })
            .Build();

        var env = new Mock<IHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var services = new ServiceCollection();
        services.AddInfrastructureServices(config, env.Object);

        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
        var isNpgsql = options.Extensions.Any(
            e => e.GetType().Name.Contains("NpgsqlOptionsExtension", StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.True(isNpgsql);
    }

    [Fact]
    public void AddInfrastructure_DevelopmentEnvironment_RegistersSqliteProvider()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=SpaceOS.dev.db"
            })
            .Build();

        var env = new Mock<IHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        var services = new ServiceCollection();
        services.AddInfrastructureServices(config, env.Object);

        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
        var isSqlite = options.Extensions.Any(
            e => e.GetType().Name.Contains("SqliteOptionsExtension", StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.True(isSqlite);
    }
}
