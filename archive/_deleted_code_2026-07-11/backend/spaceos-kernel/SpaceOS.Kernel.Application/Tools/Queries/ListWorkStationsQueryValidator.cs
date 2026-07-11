// SpaceOS.Kernel.Application/Tools/Queries/ListWorkStationsQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Validates <see cref="ListWorkStationsQuery"/> pagination parameters.</summary>
internal sealed class ListWorkStationsQueryValidator : AbstractValidator<ListWorkStationsQuery>
{
    /// <summary>Initialises the pagination validation rules.</summary>
    public ListWorkStationsQueryValidator()
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
