// SpaceOS.Kernel.Application/Tenants/Queries/GetAllTenantsQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Tenants.Queries;

/// <summary>Validates <see cref="GetAllTenantsQuery"/> pagination parameters.</summary>
internal sealed class GetAllTenantsQueryValidator : AbstractValidator<GetAllTenantsQuery>
{
    /// <summary>Initialises the pagination validation rules.</summary>
    public GetAllTenantsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
    }
}
