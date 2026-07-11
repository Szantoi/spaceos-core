// SpaceOS.Kernel.Application/Spaces/Commands/Validators/LinkTaskToElementCommandValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.Spaces.Commands.Validators;

/// <summary>
/// FluentValidation rules for <see cref="LinkTaskToElementCommand"/>.
/// </summary>
internal sealed class LinkTaskToElementCommandValidator
    : AbstractValidator<LinkTaskToElementCommand>
{
    /// <summary>Initialises validation rules for <see cref="LinkTaskToElementCommand"/>.</summary>
    public LinkTaskToElementCommandValidator()
    {
        RuleFor(x => x.FlowTaskId).NotEmpty();
        RuleFor(x => x.SpatialElementId).NotEmpty();
        RuleFor(x => x.WorkPhase).IsInEnum();
    }
}
