using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetProductionSheet;

/// <summary>
/// Returns a PDF production sheet for the given order.
/// The order must be in <c>Calculated</c> status.
/// </summary>
/// <param name="OrderId">The door order identifier.</param>
/// <param name="TenantId">The requesting tenant.</param>
public sealed record GetProductionSheetQuery(Guid OrderId, Guid TenantId) : IRequest<Result<Stream>>;
