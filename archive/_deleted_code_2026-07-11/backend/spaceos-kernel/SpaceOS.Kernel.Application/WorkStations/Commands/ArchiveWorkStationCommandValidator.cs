// SpaceOS.Kernel.Application/WorkStations/Commands/ArchiveWorkStationCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>Validates <see cref="ArchiveWorkStationCommand"/> input.</summary>
internal sealed class ArchiveWorkStationCommandValidator : AbstractValidator<ArchiveWorkStationCommand>
{
    /// <summary>Initialises validation rules for <see cref="ArchiveWorkStationCommand"/>.</summary>
    public ArchiveWorkStationCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}
