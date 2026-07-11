// SpaceOS.Kernel.Tests/Infrastructure/SpaceLayerConfigurationTests.cs
using Microsoft.EntityFrameworkCore;
using Moq;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Entities;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>
/// Unit tests verifying EF Core model metadata for <see cref="SpaceLayer"/> as configured
/// by <c>SpaceLayerConfiguration</c>. No database connection is opened — EF Core builds
/// the model graph at context construction time, independent of the underlying provider.
/// </summary>
public sealed class SpaceLayerConfigurationTests
{
    private static AppDbContext BuildContext()
    {
        var resolverMock = new Mock<ITenantResolver>();
        resolverMock.Setup(r => r.TryResolve()).Returns((SpaceOS.Kernel.Domain.ValueObjects.TenantId?)null);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=spaceos_metadata_probe;Username=x;Password=x")
            .Options;

        return new AppDbContext(options, resolverMock.Object);
    }

    [Fact]
    public void SpaceLayerConfig_IntentDataJson_MapsToJsonb()
    {
        // Arrange
        using var context = BuildContext();
        var entityType = context.Model.FindEntityType(typeof(SpaceLayer))!;

        // Act
        var property = entityType.FindProperty(nameof(SpaceLayer.IntentDataJson))!;
        var columnType = property.GetColumnType();

        // Assert
        Assert.Equal("jsonb", columnType);
    }

    [Fact]
    public void SpaceLayerConfig_ExternalAuthTokenRef_HasMaxLength()
    {
        // Arrange
        using var context = BuildContext();
        var entityType = context.Model.FindEntityType(typeof(SpaceLayer))!;

        // Act
        var property = entityType.FindProperty(nameof(SpaceLayer.ExternalAuthTokenRef))!;
        var maxLength = property.GetMaxLength();

        // Assert
        Assert.Equal(500, maxLength);
    }
}
