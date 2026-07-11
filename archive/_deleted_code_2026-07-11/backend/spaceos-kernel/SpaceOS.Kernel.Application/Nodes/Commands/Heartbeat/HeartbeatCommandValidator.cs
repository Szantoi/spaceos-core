// SpaceOS.Kernel.Application/Nodes/Commands/Heartbeat/HeartbeatCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Nodes.Commands.Heartbeat;

/// <summary>Validates <see cref="HeartbeatCommand"/> input before the handler executes.</summary>
internal sealed class HeartbeatCommandValidator : AbstractValidator<HeartbeatCommand>
{
    /// <summary>Initialises validation rules for <see cref="HeartbeatCommand"/>.</summary>
    public HeartbeatCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();
    }
}
