using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Commands.UseOffcutInJob;

public record UseOffcutInJobCommand(Guid OffcutId, Guid JobId) : IRequest<Result<UseOffcutInJobResponse>>;

public record UseOffcutInJobResponse(string Status, Guid UsedInJobId, DateTime UsedAt);
