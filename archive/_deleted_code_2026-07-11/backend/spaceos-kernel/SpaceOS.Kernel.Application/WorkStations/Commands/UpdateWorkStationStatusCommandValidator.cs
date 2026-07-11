using FluentValidation;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

public class UpdateWorkStationStatusCommandValidator : AbstractValidator<UpdateWorkStationStatusCommand>
{
    public UpdateWorkStationStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("WorkStation Id is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("NewStatus must be a valid WorkStationStatus value.");
    }
}
