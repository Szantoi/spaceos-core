// SpaceOS.Kernel.Application/StageRegistry/Queries/GetStageChainTemplateQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Validates <see cref="GetStageChainTemplateQuery"/> input.</summary>
internal sealed class GetStageChainTemplateQueryValidator : AbstractValidator<GetStageChainTemplateQuery>
{
    /// <summary>Initialises validation rules for <see cref="GetStageChainTemplateQuery"/>.</summary>
    public GetStageChainTemplateQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
