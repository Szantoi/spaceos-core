// SpaceOS.Kernel.Tests/Validators/EraseUserCommandValidatorTests.cs

using SpaceOS.Kernel.Application.UserProfiles.Commands;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

public sealed class EraseUserCommandValidatorTests
{
    private readonly EraseUserCommandValidator _validator = new();

    private static readonly Guid ValidTenantId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    // -------------------------------------------------------------------------
    // Validate_ValidCommand_PassesValidation
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _validator.Validate(new EraseUserCommand("auth0|user-abc", ValidTenantId));

        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // Validate_EmptyExternalUserId_FailsValidation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyExternalUserId_FailsValidation(string empty)
    {
        var result = _validator.Validate(new EraseUserCommand(empty, ValidTenantId));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EraseUserCommand.ExternalUserId));
    }

    // -------------------------------------------------------------------------
    // Validate_EmptyTenantId_FailsValidation
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyTenantId_FailsValidation()
    {
        var result = _validator.Validate(new EraseUserCommand("auth0|user-abc", Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EraseUserCommand.TenantId));
    }
}
