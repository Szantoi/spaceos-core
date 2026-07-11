// SpaceOS.Kernel.Application/Spaces/Commands/Validators/RegisterPhysicalSpaceCommandValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.Spaces.Commands.Validators;

/// <summary>
/// FluentValidation rules for <see cref="RegisterPhysicalSpaceCommand"/>.
/// </summary>
internal sealed class RegisterPhysicalSpaceCommandValidator
    : AbstractValidator<RegisterPhysicalSpaceCommand>
{
    /// <summary>Initialises validation rules for <see cref="RegisterPhysicalSpaceCommand"/>.</summary>
    public RegisterPhysicalSpaceCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.WidthMm).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(x => x.HeightMm).GreaterThan(0).LessThanOrEqualTo(30_000);
        RuleFor(x => x.DepthMm).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(x => x.CellSizeMm).GreaterThanOrEqualTo(100).LessThanOrEqualTo(5_000);
        RuleFor(x => x.SpaceType).IsInEnum();
    }
}
