using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialReqPdf;

/// <summary>
/// Returns a material requirements PDF for the given order.
/// </summary>
public sealed record GetMaterialReqPdfQuery(Guid OrderId, Guid TenantId) : IRequest<Result<Stream>>;
