using FluentValidation;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateDoorOrder;

public sealed class CreateDoorOrderCommandValidator : AbstractValidator<CreateDoorOrderCommand>
{
    public CreateDoorOrderCommandValidator()
    {
        RuleFor(x => x.FlowEpicId)
            .NotEqual(Guid.Empty).WithMessage("FlowEpicId cannot be empty.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId cannot be empty.")
            .MaximumLength(30).WithMessage("ProjectId cannot exceed 30 characters.");

        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage("ProjectName cannot be empty.")
            .MaximumLength(200).WithMessage("ProjectName cannot exceed 200 characters.");
    }
}
