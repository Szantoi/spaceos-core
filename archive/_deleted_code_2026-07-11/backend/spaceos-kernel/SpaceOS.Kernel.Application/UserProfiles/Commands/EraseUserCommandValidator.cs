// SpaceOS.Kernel.Application/UserProfiles/Commands/EraseUserCommandValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.UserProfiles.Commands;

/// <summary>Validates <see cref="EraseUserCommand"/> input before the handler is invoked.</summary>
internal sealed class EraseUserCommandValidator : AbstractValidator<EraseUserCommand>
{
    /// <summary>Initialises validation rules for <see cref="EraseUserCommand"/>.</summary>
    public EraseUserCommandValidator()
    {
        RuleFor(x => x.ExternalUserId)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TenantId)
            .NotEmpty();
    }
}
