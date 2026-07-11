using FluentValidation;

namespace SpaceOS.Kernel.Application.WorkStations.Queries;

/// <summary>FluentValidation rules for <see cref="GetWorkStationByIdQuery"/>.</summary>
public class GetWorkStationByIdQueryValidator : AbstractValidator<GetWorkStationByIdQuery>
{
    /// <summary>Initialises validation rules for <see cref="GetWorkStationByIdQuery"/>.</summary>
    public GetWorkStationByIdQueryValidator()
    {
        RuleFor(x => x.WorkStationId).NotEmpty();
    }
}
