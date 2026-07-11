using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Facilities.Queries;

/// <summary>
/// Query to retrieve a single facility by its unique identifier.
/// </summary>
/// <param name="FacilityId">The identifier of the facility to retrieve.</param>
public record GetFacilityByIdQuery(Guid FacilityId) : IRequest<Result<FacilityDto>>;
