// SpaceOS.Kernel.Application/Facilities/Commands/ArchiveFacilityCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>Validates <see cref="ArchiveFacilityCommand"/> input.</summary>
internal sealed class ArchiveFacilityCommandValidator : AbstractValidator<ArchiveFacilityCommand>
{
    /// <summary>Initialises validation rules for <see cref="ArchiveFacilityCommand"/>.</summary>
    public ArchiveFacilityCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}
