// SpaceOS.Kernel.Tests/AuditLog/Sha256HashProviderTests.cs

using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

public sealed class Sha256HashProviderTests
{
    private readonly Sha256HashProvider _provider = new();

    // -------------------------------------------------------------------------
    // AlgorithmType_ReturnsSha256
    // -------------------------------------------------------------------------

    [Fact]
    public void AlgorithmType_ReturnsSha256()
    {
        Assert.Equal(HashAlgorithmType.SHA256, _provider.AlgorithmType);
    }

    // -------------------------------------------------------------------------
    // ComputeHash_ReturnsLowercaseHexString_Of64Characters
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeHash_ReturnsLowercaseHexString_Of64Characters()
    {
        var hash = _provider.ComputeHash("hello world");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    // -------------------------------------------------------------------------
    // ComputeHash_SameInput_ProducesSameHash
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeHash_SameInput_ProducesSameHash()
    {
        var hash1 = _provider.ComputeHash("deterministic");
        var hash2 = _provider.ComputeHash("deterministic");

        Assert.Equal(hash1, hash2);
    }

    // -------------------------------------------------------------------------
    // ComputeHash_DifferentInput_ProducesDifferentHash
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeHash_DifferentInput_ProducesDifferentHash()
    {
        var hash1 = _provider.ComputeHash("input-a");
        var hash2 = _provider.ComputeHash("input-b");

        Assert.NotEqual(hash1, hash2);
    }

    // -------------------------------------------------------------------------
    // ComputeHash_KnownInput_ProducesKnownHash
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeHash_KnownInput_ProducesKnownHash()
    {
        // SHA-256("") = e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
        var hash = _provider.ComputeHash(string.Empty);

        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", hash);
    }

    // -------------------------------------------------------------------------
    // ComputeHash_NullInput_ThrowsArgumentNullException
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeHash_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.ComputeHash(null!));
    }
}
