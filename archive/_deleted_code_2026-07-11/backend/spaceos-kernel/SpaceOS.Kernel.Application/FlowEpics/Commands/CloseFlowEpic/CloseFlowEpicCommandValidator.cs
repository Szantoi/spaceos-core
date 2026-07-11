// SpaceOS.Kernel.Application/FlowEpics/Commands/CloseFlowEpic/CloseFlowEpicCommandValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.CloseFlowEpic;

/// <summary>
/// FluentValidation rules for <see cref="CloseFlowEpicCommand"/>.
/// </summary>
public sealed class CloseFlowEpicCommandValidator : AbstractValidator<CloseFlowEpicCommand>
{
    /// <summary>Initialises validation rules for <see cref="CloseFlowEpicCommand"/>.</summary>
    public CloseFlowEpicCommandValidator()
    {
        RuleFor(x => x.FlowEpicId)
            .NotEmpty()
            .WithMessage("FlowEpicId is required.");

        RuleFor(x => x.ProofUrl)
            .NotEmpty()
            .WithMessage("ProofUrl is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ProofUrl must be a valid absolute URL.");

        RuleFor(x => x.ProofHash)
            .NotEmpty()
            .WithMessage("ProofHash is required.")
            .Length(64)
            .WithMessage("ProofHash must be exactly 64 characters (SHA-256 hex).")
            .Matches("^[0-9a-fA-F]{64}$")
            .WithMessage("ProofHash must contain only hexadecimal characters.");
    }
}
