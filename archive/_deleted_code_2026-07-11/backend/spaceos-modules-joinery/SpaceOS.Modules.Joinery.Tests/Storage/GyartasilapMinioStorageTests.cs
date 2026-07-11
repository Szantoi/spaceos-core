using FluentAssertions;
using SpaceOS.Modules.Joinery.Infrastructure.Storage;

namespace SpaceOS.Modules.Joinery.Tests.Storage;

/// <summary>
/// Unit tests for GyartasilapMinioStorage key scheme and config logic.
/// </summary>
public class GyartasilapMinioStorageTests
{
    [Theory]
    [InlineData("L1")]
    [InlineData("L2")]
    [InlineData("L3")]
    [InlineData("L4")]
    public void BuildObjectKey_ProducesCorrectScheme(string variant)
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var planId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var key = GyartasilapMinioStorage.BuildObjectKey(tenantId, planId, variant);

        key.Should().Be($"{tenantId}/{planId}/gyartasilap_{variant}.pdf");
    }

    [Fact]
    public void BuildObjectKey_DifferentVariants_ProduceDifferentKeys()
    {
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var keyL1 = GyartasilapMinioStorage.BuildObjectKey(tenantId, planId, "L1");
        var keyL4 = GyartasilapMinioStorage.BuildObjectKey(tenantId, planId, "L4");

        keyL1.Should().NotBe(keyL4);
    }

    [Fact]
    public void BuildObjectKey_DifferentTenants_ProduceDifferentKeys()
    {
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var key1 = GyartasilapMinioStorage.BuildObjectKey(tenant1, planId, "L1");
        var key2 = GyartasilapMinioStorage.BuildObjectKey(tenant2, planId, "L1");

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void BuildObjectKey_DifferentPlans_ProduceDifferentKeys()
    {
        var tenantId = Guid.NewGuid();
        var plan1 = Guid.NewGuid();
        var plan2 = Guid.NewGuid();

        var key1 = GyartasilapMinioStorage.BuildObjectKey(tenantId, plan1, "L2");
        var key2 = GyartasilapMinioStorage.BuildObjectKey(tenantId, plan2, "L2");

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void BuildObjectKey_EndsWith_PdfExtension()
    {
        var key = GyartasilapMinioStorage.BuildObjectKey(Guid.NewGuid(), Guid.NewGuid(), "L3");

        key.Should().EndWith(".pdf");
    }

    [Fact]
    public void BuildObjectKey_ContainsTenantId()
    {
        var tenantId = Guid.NewGuid();
        var key = GyartasilapMinioStorage.BuildObjectKey(tenantId, Guid.NewGuid(), "L1");

        key.Should().Contain(tenantId.ToString());
    }

    [Fact]
    public void ReplaceHost_WithPublicEndpoint_ReplacesHostInPresignedUrl()
    {
        var internalUrl = "http://127.0.0.1:9000/gyartasilap/tenant/batch.zip?X-Amz-Signature=abc123";
        var publicEndpoint = "https://joinerytech.hu/minio";

        var result = GyartasilapMinioStorage.ReplaceHost(internalUrl, publicEndpoint);

        result.Should().StartWith("https://joinerytech.hu/minio/gyartasilap/tenant/batch.zip");
        result.Should().Contain("X-Amz-Signature=abc123");
        result.Should().NotContain("127.0.0.1");
    }

    [Fact]
    public void ReplaceHost_WithoutPublicEndpoint_ReturnsOriginalUrl()
    {
        var internalUrl = "http://127.0.0.1:9000/gyartasilap/tenant/batch.zip?sig=abc";

        var resultNull = GyartasilapMinioStorage.ReplaceHost(internalUrl, null);
        var resultEmpty = GyartasilapMinioStorage.ReplaceHost(internalUrl, "");
        var resultWhitespace = GyartasilapMinioStorage.ReplaceHost(internalUrl, "  ");

        resultNull.Should().Be(internalUrl);
        resultEmpty.Should().Be(internalUrl);
        resultWhitespace.Should().Be(internalUrl);
    }
}
