// SpaceOS.Kernel.Application/Nodes/Commands/RegisterNode/RegisterNodeCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Nodes.Commands.RegisterNode;

/// <summary>Validates <see cref="RegisterNodeCommand"/> input before the handler executes.</summary>
internal sealed class RegisterNodeCommandValidator : AbstractValidator<RegisterNodeCommand>
{
    /// <summary>Initialises validation rules for <see cref="RegisterNodeCommand"/>.</summary>
    public RegisterNodeCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.ServerUrl)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
