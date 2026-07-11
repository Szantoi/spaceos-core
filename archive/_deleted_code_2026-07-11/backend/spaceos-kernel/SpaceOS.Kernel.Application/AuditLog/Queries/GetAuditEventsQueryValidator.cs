// SpaceOS.Kernel.Application/AuditLog/Queries/GetAuditEventsQueryValidator.cs

using FluentValidation;

namespace SpaceOS.Kernel.Application.AuditLog.Queries;

/// <summary>Validates <see cref="GetAuditEventsQuery"/> input parameters.</summary>
internal sealed class GetAuditEventsQueryValidator : AbstractValidator<GetAuditEventsQuery>
{
    /// <summary>Initialises the validation rules for the audit events query.</summary>
    public GetAuditEventsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");

        RuleFor(x => x)
            .Must(q => q.From is null || q.To is null || q.From <= q.To)
            .WithName(nameof(GetAuditEventsQuery.From))
            .WithMessage("From must be before or equal to To.");
    }
}
