using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>
/// FluentValidation rules for <see cref="CreateFlowEpicCommand"/>.
/// </summary>
public class CreateFlowEpicCommandValidator : AbstractValidator<CreateFlowEpicCommand>
{
    /// <summary>Initialises validation rules for <see cref="CreateFlowEpicCommand"/>.</summary>
    public CreateFlowEpicCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.TargetFacilityId)
            .NotEmpty().WithMessage("TargetFacilityId is required.");
    }
}
