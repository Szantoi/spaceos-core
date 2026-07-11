// SpaceOS.Kernel.Application/WorkStations/Queries/GetWorkStationsByFacilityQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.WorkStations.Queries;

/// <summary>Validates <see cref="GetWorkStationsByFacilityQuery"/> parameters.</summary>
internal sealed class GetWorkStationsByFacilityQueryValidator : AbstractValidator<GetWorkStationsByFacilityQuery>
{
    /// <summary>Initialises validation rules for facility ID and pagination parameters.</summary>
    public GetWorkStationsByFacilityQueryValidator()
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
