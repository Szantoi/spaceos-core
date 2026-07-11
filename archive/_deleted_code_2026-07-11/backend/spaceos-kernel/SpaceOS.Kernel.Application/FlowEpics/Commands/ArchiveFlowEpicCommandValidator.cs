// SpaceOS.Kernel.Application/FlowEpics/Commands/ArchiveFlowEpicCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>Validates <see cref="ArchiveFlowEpicCommand"/> input.</summary>
internal sealed class ArchiveFlowEpicCommandValidator : AbstractValidator<ArchiveFlowEpicCommand>
{
    /// <summary>Initialises validation rules for <see cref="ArchiveFlowEpicCommand"/>.</summary>
    public ArchiveFlowEpicCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}
