using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>
/// Validates <see cref="UpdateFlowEpicTitleCommand"/> requests.
/// </summary>
public class UpdateFlowEpicTitleCommandValidator : AbstractValidator<UpdateFlowEpicTitleCommand>
{
    public UpdateFlowEpicTitleCommandValidator()
    {
        RuleFor(x => x.FlowEpicId)
            .NotEmpty().WithMessage("FlowEpicId is required.");

        RuleFor(x => x.NewTitle)
            .NotEmpty().WithMessage("New title is required.")
            .MaximumLength(200).WithMessage("FlowEpic title cannot exceed 200 characters.");
    }
}
