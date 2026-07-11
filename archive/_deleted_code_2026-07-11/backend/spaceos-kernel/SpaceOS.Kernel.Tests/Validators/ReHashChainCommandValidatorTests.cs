// SpaceOS.Kernel.Tests/Validators/ReHashChainCommandValidatorTests.cs

using SpaceOS.Kernel.Application.AuditLog.Commands;
using SpaceOS.Kernel.Domain.AuditLog;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

public sealed class ReHashChainCommandValidatorTests
{
    private readonly ReHashChainCommandValidator _validator = new();

    private static readonly Guid ValidTenantId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    // -------------------------------------------------------------------------
    // Validate_ValidCommand_PassesValidation
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _validator.Validate(new ReHashChainCommand(ValidTenantId, HashAlgorithmType.SHA3_256));

        Assert.True(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // Validate_EmptyTenantId_FailsValidation
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_EmptyTenantId_FailsValidation()
    {
        var result = _validator.Validate(new ReHashChainCommand(Guid.Empty, HashAlgorithmType.SHA256));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReHashChainCommand.TenantId));
    }

    // -------------------------------------------------------------------------
    // Validate_InvalidAlgorithmValue_FailsValidation
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_InvalidAlgorithmValue_FailsValidation()
    {
        var result = _validator.Validate(new ReHashChainCommand(ValidTenantId, (HashAlgorithmType)999));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReHashChainCommand.TargetAlgorithm));
    }
}
