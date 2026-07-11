// SpaceOS.Kernel.Application/AuditLog/Queries/VerifyChainQueryValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.AuditLog.Queries;

/// <summary>Validates <see cref="VerifyChainQuery"/> input.</summary>
internal sealed class VerifyChainQueryValidator : AbstractValidator<VerifyChainQuery>
{
    /// <summary>Initialises the validation rules.</summary>
    public VerifyChainQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();

        RuleFor(x => x)
            .Must(q => !q.From.HasValue || !q.To.HasValue || q.From <= q.To)
            .WithName("DateRange")
            .WithMessage("'From' must be earlier than or equal to 'To'.");
    }
}
