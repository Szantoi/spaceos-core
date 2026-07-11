// SpaceOS.Kernel.Application/Facilities/Queries/GetFacilitiesByTenantQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Facilities.Queries;

/// <summary>Validates <see cref="GetFacilitiesByTenantQuery"/> parameters.</summary>
internal sealed class GetFacilitiesByTenantQueryValidator : AbstractValidator<GetFacilitiesByTenantQuery>
{
    /// <summary>Initialises validation rules for tenant ID and pagination parameters.</summary>
    public GetFacilitiesByTenantQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
    }
}
