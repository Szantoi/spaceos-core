// SpaceOS.Kernel.Application/StageRegistry/Queries/GetLatestHandoffQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Validates <see cref="GetLatestHandoffQuery"/> input.</summary>
internal sealed class GetLatestHandoffQueryValidator : AbstractValidator<GetLatestHandoffQuery>
{
    /// <summary>Initialises validation rules for <see cref="GetLatestHandoffQuery"/>.</summary>
    public GetLatestHandoffQueryValidator()
    {
        RuleFor(x => x.FlowEpicId).NotEmpty();
        RuleFor(x => x.SourceStageCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TargetStageCode).NotEmpty().MaximumLength(30);
    }
}
