// SpaceOS.Kernel.Tests/Application/RefreshTokenHasherTests.cs

using SpaceOS.Kernel.Application.Auth;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="RefreshTokenHasher"/>.</summary>
public sealed class RefreshTokenHasherTests
{
    [Fact]
    public void GenerateOpaqueToken_Returns43CharacterBase64UrlString()
    {
        // Act
        var token = RefreshTokenHasher.GenerateOpaqueToken();

        // Assert
        Assert.Equal(43, token.Length);
    }

    [Fact]
    public void GenerateOpaqueToken_DoesNotContainBase64Padding()
    {
        // Act
        var token = RefreshTokenHasher.GenerateOpaqueToken();

        // Assert — Base64Url should not contain '=' padding
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void GenerateOpaqueToken_TwoCallsReturnDistinctValues()
    {
        // Act
        var t1 = RefreshTokenHasher.GenerateOpaqueToken();
        var t2 = RefreshTokenHasher.GenerateOpaqueToken();

        // Assert — CSPRNG; collision probability is negligible
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public void HashToken_Returns64CharLowercaseHexString()
    {
        // Arrange
        var token = RefreshTokenHasher.GenerateOpaqueToken();

        // Act
        var hash = RefreshTokenHasher.HashToken(token);

        // Assert
        Assert.Equal(64, hash.Length);
        Assert.All(hash, c => Assert.Contains(c, "0123456789abcdef"));
    }

    [Fact]
    public void HashToken_SameInputProducesSameHash()
    {
        // Arrange
        var token = RefreshTokenHasher.GenerateOpaqueToken();

        // Act
        var hash1 = RefreshTokenHasher.HashToken(token);
        var hash2 = RefreshTokenHasher.HashToken(token);

        // Assert — deterministic
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToken_DifferentInputsProduceDifferentHashes()
    {
        // Arrange
        var t1 = RefreshTokenHasher.GenerateOpaqueToken();
        var t2 = RefreshTokenHasher.GenerateOpaqueToken();

        // Act
        var h1 = RefreshTokenHasher.HashToken(t1);
        var h2 = RefreshTokenHasher.HashToken(t2);

        // Assert
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void VerifyToken_MatchingToken_ReturnsTrue()
    {
        // Arrange
        var raw  = RefreshTokenHasher.GenerateOpaqueToken();
        var hash = RefreshTokenHasher.HashToken(raw);

        // Act
        var result = RefreshTokenHasher.VerifyToken(raw, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyToken_NonMatchingToken_ReturnsFalse()
    {
        // Arrange
        var raw       = RefreshTokenHasher.GenerateOpaqueToken();
        var otherHash = RefreshTokenHasher.HashToken(RefreshTokenHasher.GenerateOpaqueToken());

        // Act
        var result = RefreshTokenHasher.VerifyToken(raw, otherHash);

        // Assert
        Assert.False(result);
    }
}
