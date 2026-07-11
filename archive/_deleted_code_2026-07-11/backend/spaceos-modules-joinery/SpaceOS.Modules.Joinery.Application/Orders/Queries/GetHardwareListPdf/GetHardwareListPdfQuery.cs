using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetHardwareListPdf;

/// <summary>
/// Returns a hardware list PDF for the given order.
/// </summary>
public sealed record GetHardwareListPdfQuery(Guid OrderId, Guid TenantId) : IRequest<Result<Stream>>;
