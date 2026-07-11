using FluentValidation;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>
/// FluentValidation rules for <see cref="CreateFacilityCommand"/>.
/// </summary>
public class CreateFacilityCommandValidator : AbstractValidator<CreateFacilityCommand>
{
    /// <summary>Initialises validation rules for <see cref="CreateFacilityCommand"/>.</summary>
    public CreateFacilityCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Facility Name are required.")
            .MaximumLength(100).WithMessage("Facility Name must not exceed 100 characters.");
    }
}
