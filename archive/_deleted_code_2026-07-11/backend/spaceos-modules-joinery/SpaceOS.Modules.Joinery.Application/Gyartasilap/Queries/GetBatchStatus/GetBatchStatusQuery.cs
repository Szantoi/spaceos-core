using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateBatch;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.GetBatchStatus;

/// <summary>Returns the current status of a Gyártásilap batch job.</summary>
public sealed record GetBatchStatusQuery(
    Guid TenantId,
    Guid BatchId) : IRequest<Result<GetBatchStatusResponse>>;

/// <summary>Response DTO for GetBatchStatusQuery.</summary>
public sealed record GetBatchStatusResponse(
    Guid BatchId,
    BatchStatusDto Status,
    string? ZipStoragePath,
    DateTimeOffset CreatedAt);
