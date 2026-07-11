// SpaceOS.Kernel.Application/StageRegistry/Queries/GetStageHandoffsQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Validates <see cref="GetStageHandoffsQuery"/> input.</summary>
internal sealed class GetStageHandoffsQueryValidator : AbstractValidator<GetStageHandoffsQuery>
{
    /// <summary>Initialises validation rules for <see cref="GetStageHandoffsQuery"/>.</summary>
    public GetStageHandoffsQueryValidator()
    {
        RuleFor(x => x.FlowEpicId).NotEmpty();
    }
}
