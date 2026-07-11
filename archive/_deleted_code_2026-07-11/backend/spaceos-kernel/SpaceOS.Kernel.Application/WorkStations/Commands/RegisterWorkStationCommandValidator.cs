using FluentValidation;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

public class RegisterWorkStationCommandValidator : AbstractValidator<RegisterWorkStationCommand>
{
    public RegisterWorkStationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("WorkStation name is required.")
            .MaximumLength(100).WithMessage("WorkStation name cannot exceed 100 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("WorkStation type is required.")
            .MaximumLength(50).WithMessage("WorkStation type cannot exceed 50 characters.");

        RuleFor(x => x.FacilityId)
            .NotEmpty().WithMessage("FacilityId is required.");
    }
}
