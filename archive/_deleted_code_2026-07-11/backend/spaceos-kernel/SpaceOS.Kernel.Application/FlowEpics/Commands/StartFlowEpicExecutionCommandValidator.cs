using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

public class StartFlowEpicExecutionCommandValidator : AbstractValidator<StartFlowEpicExecutionCommand>
{
    public StartFlowEpicExecutionCommandValidator()
    {
        RuleFor(x => x.FlowEpicId)
            .NotEmpty().WithMessage("FlowEpicId is required.");
    }
}
