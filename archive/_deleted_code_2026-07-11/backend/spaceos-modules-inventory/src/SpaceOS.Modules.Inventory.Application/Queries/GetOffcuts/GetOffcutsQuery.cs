using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcuts;

public sealed record GetOffcutsQuery(string MaterialType) : IRequest<Result<IReadOnlyList<OffcutResponse>>>;
