// SpaceOS.Kernel.Tests/Application/GetSnapshotAtQueryValidatorTests.cs

using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.Snapshots.Queries;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetSnapshotAtQueryValidator"/>.</summary>
public sealed class GetSnapshotAtQueryValidatorTests
{
    private readonly GetSnapshotAtQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_PassesValidation()
    {
        var query  = new GetSnapshotAtQuery(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyAggregateId_FailsValidation()
    {
        var query  = new GetSnapshotAtQuery(Guid.Empty, DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(q => q.AggregateId);
    }
}
