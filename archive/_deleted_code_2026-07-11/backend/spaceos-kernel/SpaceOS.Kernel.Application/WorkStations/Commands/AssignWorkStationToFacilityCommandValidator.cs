using FluentValidation;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Validates <see cref="AssignWorkStationToFacilityCommand"/> requests.
/// </summary>
public class AssignWorkStationToFacilityCommandValidator : AbstractValidator<AssignWorkStationToFacilityCommand>
{
    public AssignWorkStationToFacilityCommandValidator()
    {
        RuleFor(x => x.WorkStationId)
            .NotEmpty().WithMessage("WorkStationId is required.");

        RuleFor(x => x.NewFacilityId)
            .NotEmpty().WithMessage("NewFacilityId is required.");
    }
}
