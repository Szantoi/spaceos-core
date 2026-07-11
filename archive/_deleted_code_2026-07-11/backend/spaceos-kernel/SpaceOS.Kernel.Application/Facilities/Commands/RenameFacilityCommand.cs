using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>
/// Command to rename an existing facility.
/// </summary>
/// <param name="FacilityId">The facility identifier.</param>
/// <param name="NewName">The new display name.</param>
public record RenameFacilityCommand(Guid FacilityId, string NewName) : IRequest<Result>;
