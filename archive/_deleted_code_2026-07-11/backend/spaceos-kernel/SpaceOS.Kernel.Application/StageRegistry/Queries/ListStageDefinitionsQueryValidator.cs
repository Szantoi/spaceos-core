// SpaceOS.Kernel.Application/StageRegistry/Queries/ListStageDefinitionsQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Validates <see cref="ListStageDefinitionsQuery"/> input.</summary>
internal sealed class ListStageDefinitionsQueryValidator : AbstractValidator<ListStageDefinitionsQuery>
{
    /// <summary>Initialises validation rules for <see cref="ListStageDefinitionsQuery"/>.</summary>
    public ListStageDefinitionsQueryValidator()
    {
        // ListStageDefinitionsQuery carries no user-provided parameters — validator is intentionally empty
        // and exists to satisfy the A10 companion-validator requirement.
    }
}
