using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Anyaglista.Queries.GetAnyaglista;

/// <summary>Returns the Anyaglista for a given order.</summary>
public sealed record GetAnyaglistaQuery(
    Guid TenantId,
    Guid OrderId) : IRequest<Result<GetAnyaglistaResponse>>;

/// <summary>Response DTO for GetAnyaglistaQuery.</summary>
public sealed record GetAnyaglistaResponse(
    Guid AnyaglistaId,
    string StorageUrl,
    DateTimeOffset CreatedAt);
