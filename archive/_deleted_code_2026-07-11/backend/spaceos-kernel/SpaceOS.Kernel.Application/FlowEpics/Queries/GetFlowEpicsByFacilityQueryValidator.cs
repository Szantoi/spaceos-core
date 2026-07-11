// SpaceOS.Kernel.Application/FlowEpics/Queries/GetFlowEpicsByFacilityQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Queries;

/// <summary>Validates <see cref="GetFlowEpicsByFacilityQuery"/> parameters.</summary>
internal sealed class GetFlowEpicsByFacilityQueryValidator : AbstractValidator<GetFlowEpicsByFacilityQuery>
{
    /// <summary>Initialises validation rules for facility ID and pagination parameters.</summary>
    public GetFlowEpicsByFacilityQueryValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty().WithMessage("FacilityId is required.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
    }
}
