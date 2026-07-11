// SpaceOS.Kernel.Application/SpaceLayers/Commands/ArchiveSpaceLayerCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>Validates <see cref="ArchiveSpaceLayerCommand"/> input.</summary>
internal sealed class ArchiveSpaceLayerCommandValidator : AbstractValidator<ArchiveSpaceLayerCommand>
{
    /// <summary>Initialises validation rules for <see cref="ArchiveSpaceLayerCommand"/>.</summary>
    public ArchiveSpaceLayerCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}
