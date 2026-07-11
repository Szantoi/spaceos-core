// SpaceOS.Kernel.Application/FlowEpics/Commands/UploadProof/UploadFlowEpicProofCommandValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.UploadProof;

/// <summary>
/// FluentValidation rules for <see cref="UploadFlowEpicProofCommand"/>.
/// </summary>
public sealed class UploadFlowEpicProofCommandValidator : AbstractValidator<UploadFlowEpicProofCommand>
{
    /// <summary>Initialises validation rules for <see cref="UploadFlowEpicProofCommand"/>.</summary>
    public UploadFlowEpicProofCommandValidator()
    {
        RuleFor(x => x.FlowEpicId)
            .NotEmpty()
            .WithMessage("FlowEpicId is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("FileName is required.");

        RuleFor(x => x.Content)
            .NotNull()
            .WithMessage("Content is required.")
            .Must(c => c.Length > 0)
            .WithMessage("Content must not be empty.");
    }
}
