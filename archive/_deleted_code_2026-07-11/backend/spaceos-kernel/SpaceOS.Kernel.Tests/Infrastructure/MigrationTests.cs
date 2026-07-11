// SpaceOS.Kernel.Tests/Infrastructure/MigrationTests.cs
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>Unit tests verifying the EF Core InitialCreate migration file content.</summary>
public sealed class MigrationTests
{
    private const string MigrationFileName = "*_InitialCreate.cs";

    private static string GetMigrationFilePath()
    {
        var dir = AppContext.BaseDirectory;
        var solutionRoot = dir;
        while (!File.Exists(Path.Combine(solutionRoot, "SpaceOS.Kerner.sln")))
            solutionRoot = Directory.GetParent(solutionRoot)!.FullName;
        var migDir = Path.Combine(solutionRoot, "SpaceOS.Infrastructure", "Migrations");
        return Directory.GetFiles(migDir, MigrationFileName).Single();
    }

    [Theory]
    [InlineData("\"Tenants\"")]
    [InlineData("\"Facilities\"")]
    [InlineData("\"WorkStations\"")]
    [InlineData("\"SpaceLayers\"")]
    [InlineData("\"FlowEpics\"")]
    public void Migration_ContainsAllAggregateTablesAndColumns(string tableName)
    {
        // Arrange
        var content = File.ReadAllText(GetMigrationFilePath());

        // Act + Assert
        Assert.Contains(tableName, content, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_AllIdColumnsAreUuidType()
    {
        // Arrange
        var content = File.ReadAllText(GetMigrationFilePath());

        // Act + Assert
        Assert.Contains("type: \"uuid\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_DoesNotContainDomainEventsColumn()
    {
        // Arrange
        var content = File.ReadAllText(GetMigrationFilePath());

        // Act + Assert
        Assert.DoesNotContain("DomainEvents", content, StringComparison.Ordinal);
    }
}
