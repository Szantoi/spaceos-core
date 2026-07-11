// SpaceOS.Kernel.Application/Tools/Queries/ListFacilitiesQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Validates <see cref="ListFacilitiesQuery"/> pagination parameters.</summary>
internal sealed class ListFacilitiesQueryValidator : AbstractValidator<ListFacilitiesQuery>
{
    /// <summary>Initialises the pagination validation rules.</summary>
    public ListFacilitiesQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId must be a non-empty GUID.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(50).WithMessage("PageSize cannot exceed 50.");
    }
}
