using FluentValidation;

namespace SpaceOS.Kernel.Application.Facilities.Queries;

/// <summary>
/// FluentValidation rules for <see cref="GetFacilityByIdQuery"/>.
/// </summary>
public class GetFacilityByIdQueryValidator : AbstractValidator<GetFacilityByIdQuery>
{
    /// <summary>Initialises validation rules for <see cref="GetFacilityByIdQuery"/>.</summary>
    public GetFacilityByIdQueryValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
    }
}
