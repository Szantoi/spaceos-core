// SpaceOS.Kernel.Tests/Application/GetSnapshotVersionsQueryValidatorTests.cs

using FluentValidation.TestHelper;
using SpaceOS.Kernel.Application.Snapshots.Queries;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetSnapshotVersionsQueryValidator"/>.</summary>
public sealed class GetSnapshotVersionsQueryValidatorTests
{
    private readonly GetSnapshotVersionsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_PassesValidation()
    {
        var query  = new GetSnapshotVersionsQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyAggregateId_FailsValidation()
    {
        var query  = new GetSnapshotVersionsQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(q => q.AggregateId);
    }
}
