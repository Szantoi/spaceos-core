// SpaceOS.Kernel.Application/Auth/Commands/RevokeTokenCommandValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.Auth.Commands;

/// <summary>Validates <see cref="RevokeTokenCommand"/> input format.</summary>
internal sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    /// <summary>Initialises the validator.</summary>
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .Length(43, 43)
            .WithMessage("Refresh token must be exactly 43 characters.");
    }
}
