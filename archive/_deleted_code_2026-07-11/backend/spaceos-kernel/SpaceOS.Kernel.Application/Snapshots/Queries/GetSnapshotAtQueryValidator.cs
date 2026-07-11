// SpaceOS.Kernel.Application/Snapshots/Queries/GetSnapshotAtQueryValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.Snapshots.Queries;

/// <summary>Validates <see cref="GetSnapshotAtQuery"/> input.</summary>
internal sealed class GetSnapshotAtQueryValidator : AbstractValidator<GetSnapshotAtQuery>
{
    /// <summary>Initialises the validation rules.</summary>
    public GetSnapshotAtQueryValidator()
    {
        RuleFor(x => x.AggregateId).NotEmpty();
    }
}
