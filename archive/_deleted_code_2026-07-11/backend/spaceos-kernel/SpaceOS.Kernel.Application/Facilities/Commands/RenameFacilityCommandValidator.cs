using FluentValidation;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>
/// Validates <see cref="RenameFacilityCommand"/> requests.
/// </summary>
public class RenameFacilityCommandValidator : AbstractValidator<RenameFacilityCommand>
{
    public RenameFacilityCommandValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty().WithMessage("FacilityId is required.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New name is required.")
            .MaximumLength(100).WithMessage("Facility name cannot exceed 100 characters.");
    }
}
