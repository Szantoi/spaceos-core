// SpaceOS.Kernel.Application/Spaces/Commands/Validators/RegisterSpatialElementCommandValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.Spaces.Commands.Validators;

/// <summary>
/// FluentValidation rules for <see cref="RegisterSpatialElementCommand"/>.
/// </summary>
internal sealed class RegisterSpatialElementCommandValidator
    : AbstractValidator<RegisterSpatialElementCommand>
{
    /// <summary>Initialises validation rules for <see cref="RegisterSpatialElementCommand"/>.</summary>
    public RegisterSpatialElementCommandValidator()
    {
        RuleFor(x => x.PhysicalSpaceId).NotEmpty();
        RuleFor(x => x.FlowEpicId).NotEmpty();
        RuleFor(x => x.TradeType).IsInEnum();
        RuleFor(x => x.ElementType).NotEmpty().MaximumLength(50);
        RuleFor(x => x).Must(cmd => cmd.MinX < cmd.MaxX)
            .WithMessage("MinX must be less than MaxX");
        RuleFor(x => x).Must(cmd => cmd.MinY < cmd.MaxY)
            .WithMessage("MinY must be less than MaxY");
        RuleFor(x => x).Must(cmd => cmd.MinZ < cmd.MaxZ)
            .WithMessage("MinZ must be less than MaxZ");
    }
}
