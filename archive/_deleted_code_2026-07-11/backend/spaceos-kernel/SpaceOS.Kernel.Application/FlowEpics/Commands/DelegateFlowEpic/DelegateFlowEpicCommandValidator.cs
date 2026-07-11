using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.DelegateFlowEpic;

/// <summary>
/// FluentValidation rules for <see cref="DelegateFlowEpicCommand"/>.
/// </summary>
public class DelegateFlowEpicCommandValidator : AbstractValidator<DelegateFlowEpicCommand>
{
    /// <summary>Initialises validation rules for <see cref="DelegateFlowEpicCommand"/>.</summary>
    public DelegateFlowEpicCommandValidator()
    {
        RuleFor(x => x.EpicId)
            .NotEmpty()
            .WithMessage("EpicId is required.");

        RuleFor(x => x.GuestTenantId)
            .NotEmpty()
            .WithMessage("GuestTenantId is required.");
    }
}
