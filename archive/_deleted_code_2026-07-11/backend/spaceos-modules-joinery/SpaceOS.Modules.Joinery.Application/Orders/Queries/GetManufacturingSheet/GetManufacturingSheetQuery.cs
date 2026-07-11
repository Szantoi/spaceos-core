using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetManufacturingSheet;

/// <summary>
/// Returns a manufacturing sheet PDF for the given order.
/// Works for any order status — does not require Calculated.
/// </summary>
/// <param name="OrderId">The door order identifier.</param>
/// <param name="TenantId">The requesting tenant.</param>
public sealed record GetManufacturingSheetQuery(Guid OrderId, Guid TenantId) : IRequest<Result<Stream>>;
