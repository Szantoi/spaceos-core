using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.SpaceLayers.Queries;

public record GetSpaceLayerByIdQuery(Guid SpaceLayerId) : IRequest<Result<SpaceLayerDto>>;
