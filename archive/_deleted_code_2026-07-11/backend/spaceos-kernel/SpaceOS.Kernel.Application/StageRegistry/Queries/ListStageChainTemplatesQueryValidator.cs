// SpaceOS.Kernel.Application/StageRegistry/Queries/ListStageChainTemplatesQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Validates <see cref="ListStageChainTemplatesQuery"/> input.</summary>
internal sealed class ListStageChainTemplatesQueryValidator : AbstractValidator<ListStageChainTemplatesQuery>
{
    /// <summary>Initialises validation rules for <see cref="ListStageChainTemplatesQuery"/>.</summary>
    public ListStageChainTemplatesQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
