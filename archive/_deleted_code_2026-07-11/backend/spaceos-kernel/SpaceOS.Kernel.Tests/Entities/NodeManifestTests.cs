// SpaceOS.Kernel.Tests/Entities/NodeManifestTests.cs

using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

/// <summary>Unit tests for <see cref="NodeManifest"/> aggregate root invariants.</summary>
public sealed class NodeManifestTests
{
    private static readonly TenantId ValidTenantId = TenantId.New();
    private const string ValidServerUrl = "https://node.example.com";

    [Fact]
    public void Create_WithValidArgs_SetsServerUrl()
    {
        // Act
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);

        // Assert
        Assert.Equal(ValidServerUrl, manifest.ServerUrl);
    }

    [Fact]
    public void Create_WithValidArgs_SetsTenantId()
    {
        // Act
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);

        // Assert
        Assert.Equal(ValidTenantId, manifest.TenantId);
    }

    [Fact]
    public void Create_WithValidArgs_AssignsNonEmptyId()
    {
        // Act
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);

        // Assert
        Assert.NotEqual(Guid.Empty, manifest.Id);
    }

    [Fact]
    public void Create_WithValidArgs_SetsCreatedAtToNearNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.True(manifest.CreatedAt >= before && manifest.CreatedAt <= after);
    }

    [Fact]
    public void Create_WithValidArgs_SetsUpdatedAtToNearNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.True(manifest.UpdatedAt >= before && manifest.UpdatedAt <= after);
    }

    [Fact]
    public void Create_SetsDefaultVersion1()
    {
        // Act
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);

        // Assert
        Assert.Equal(1, manifest.Version);
    }

    [Fact]
    public void Create_SetsLastHeartbeatAtToNull()
    {
        // Act
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);

        // Assert
        Assert.Null(manifest.LastHeartbeatAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceServerUrl_ThrowsArgumentException(string url)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => NodeManifest.Create(ValidTenantId, url));
    }

    [Fact]
    public void RecordHeartbeat_SetsLastHeartbeatAt()
    {
        // Arrange
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);
        var before = DateTimeOffset.UtcNow;

        // Act
        manifest.RecordHeartbeat(false);

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.NotNull(manifest.LastHeartbeatAt);
        Assert.True(manifest.LastHeartbeatAt >= before && manifest.LastHeartbeatAt <= after);
    }

    [Fact]
    public void RecordHeartbeat_SetsUpdatedAt()
    {
        // Arrange
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);
        var before = DateTimeOffset.UtcNow;

        // Act
        manifest.RecordHeartbeat(false);

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.True(manifest.UpdatedAt >= before && manifest.UpdatedAt <= after);
    }

    [Fact]
    public void RecordHeartbeat_IncrementsVersion()
    {
        // Arrange
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);
        var versionBefore = manifest.Version;

        // Act
        manifest.RecordHeartbeat(false);

        // Assert
        Assert.Equal(versionBefore + 1, manifest.Version);
    }

    [Fact]
    public void RecordHeartbeat_CalledTwice_IncrementsVersionTwice()
    {
        // Arrange
        var manifest = NodeManifest.Create(ValidTenantId, ValidServerUrl);

        // Act
        manifest.RecordHeartbeat(false);
        manifest.RecordHeartbeat(false);

        // Assert
        Assert.Equal(3, manifest.Version);
    }
}
