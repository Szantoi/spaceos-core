using FluentValidation;
using SpaceOS.Modules.Joinery.Domain.Enums;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.AddDoorItem;

public sealed class AddDoorItemCommandValidator : AbstractValidator<AddDoorItemCommand>
{
    public AddDoorItemCommandValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

        RuleFor(x => x.Sorszam)
            .NotEmpty().WithMessage("Sorszam cannot be empty.")
            .MaximumLength(5).WithMessage("Sorszam cannot exceed 5 characters.");

        RuleFor(x => x.DoorType)
            .NotEmpty().WithMessage("DoorType cannot be empty.")
            .Must(v => Enum.TryParse<DoorType>(v, out _))
            .WithMessage("DoorType is not a valid value.");
    }
}
