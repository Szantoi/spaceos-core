// SpaceOS.Kernel.Application/Snapshots/Queries/GetSnapshotVersionsQueryValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.Snapshots.Queries;

/// <summary>Validates <see cref="GetSnapshotVersionsQuery"/> input.</summary>
internal sealed class GetSnapshotVersionsQueryValidator : AbstractValidator<GetSnapshotVersionsQuery>
{
    /// <summary>Initialises the validation rules.</summary>
    public GetSnapshotVersionsQueryValidator()
    {
        RuleFor(x => x.AggregateId).NotEmpty();
    }
}
