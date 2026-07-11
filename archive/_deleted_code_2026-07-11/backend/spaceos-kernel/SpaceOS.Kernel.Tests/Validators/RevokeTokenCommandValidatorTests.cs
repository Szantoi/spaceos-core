// SpaceOS.Kernel.Tests/Validators/RevokeTokenCommandValidatorTests.cs

using SpaceOS.Kernel.Application.Auth;
using SpaceOS.Kernel.Application.Auth.Commands;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

/// <summary>Unit tests for <see cref="RevokeTokenCommandValidator"/>.</summary>
public sealed class RevokeTokenCommandValidatorTests
{
    private readonly RevokeTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidToken_PassesValidation()
    {
        // Arrange
        var token = RefreshTokenHasher.GenerateOpaqueToken();

        // Act
        var result = _validator.Validate(new RevokeTokenCommand(token));

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyToken_FailsValidation()
    {
        // Act
        var result = _validator.Validate(new RevokeTokenCommand(string.Empty));

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RevokeTokenCommand.RefreshToken));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("thisTokenIsWayTooLongForABase64UrlEncodedRefreshTokenThatShouldBeExactly43Chars")]
    public void Validate_WrongLengthToken_FailsValidation(string token)
    {
        // Act
        var result = _validator.Validate(new RevokeTokenCommand(token));

        // Assert
        Assert.False(result.IsValid);
    }
}
