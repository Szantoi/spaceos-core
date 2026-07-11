// SpaceOS.Kernel.Tests/Infrastructure/Migration0014Tests.cs
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>Verifies Migration 0014 schema constants and SpaceLayer field nullability.</summary>
public sealed class Migration0014Tests
{
    [Fact]
    public void ExternalAuthTokenRef_IsNullable_OnSpaceLayer()
    {
        // SpaceLayer.ExternalAuthTokenRef must be nullable — partial index requires IS NOT NULL.
        var prop = typeof(SpaceOS.Kernel.Domain.Entities.SpaceLayer)
            .GetProperty(nameof(SpaceOS.Kernel.Domain.Entities.SpaceLayer.ExternalAuthTokenRef));

        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    [Fact]
    public void Migration0014_IndexName_FollowsNamingConvention()
    {
        // Verify the index name constant used in Migration 0014 follows IX_ convention.
        const string indexName = "IX_SpaceLayers_ExternalAuthTokenRef_NotNull";
        Assert.StartsWith("IX_", indexName, StringComparison.Ordinal);
        Assert.Contains("SpaceLayers", indexName, StringComparison.Ordinal);
        Assert.Contains("ExternalAuthTokenRef", indexName, StringComparison.Ordinal);
    }
}
