// SpaceOS.Kernel.Tests/Validators/VerifyChainQueryValidatorTests.cs

using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.AuditLog.Queries;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

/// <summary>Unit tests for <see cref="VerifyChainQueryValidator"/>.</summary>
public sealed class VerifyChainQueryValidatorTests
{
    private readonly VerifyChainQueryValidator _validator = new();

    // ── Valid input ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithValidTenantId_NoFromTo_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var query = new VerifyChainQuery(Guid.NewGuid(), null, null);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidTenantId_FromBeforeTo_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var query = new VerifyChainQuery(Guid.NewGuid(), now.AddDays(-7), now);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidTenantId_FromEqualsTo_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var query = new VerifyChainQuery(Guid.NewGuid(), now, now);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── TenantId empty ───────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var query = new VerifyChainQuery(Guid.Empty, null, null);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    // ── Date range inversion ─────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenFromIsAfterTo_ShouldHaveDateRangeError()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var query = new VerifyChainQuery(Guid.NewGuid(), now, now.AddDays(-1));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("DateRange");
    }
}
