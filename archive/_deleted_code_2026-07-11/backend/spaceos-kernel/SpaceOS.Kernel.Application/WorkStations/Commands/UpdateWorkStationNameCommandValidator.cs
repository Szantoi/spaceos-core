using FluentValidation;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Validates <see cref="UpdateWorkStationNameCommand"/> requests.
/// </summary>
public class UpdateWorkStationNameCommandValidator : AbstractValidator<UpdateWorkStationNameCommand>
{
    public UpdateWorkStationNameCommandValidator()
    {
        RuleFor(x => x.WorkStationId)
            .NotEmpty().WithMessage("WorkStationId is required.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New name is required.")
            .MaximumLength(100).WithMessage("WorkStation name cannot exceed 100 characters.");
    }
}
