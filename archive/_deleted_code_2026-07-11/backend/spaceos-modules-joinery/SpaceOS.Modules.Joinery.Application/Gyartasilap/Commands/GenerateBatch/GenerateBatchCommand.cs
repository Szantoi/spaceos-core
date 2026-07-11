using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateBatch;

/// <summary>
/// Command to generate a ZIP archive of multiple Gyártásilap PDFs for a single order.
/// </summary>
public sealed record GenerateBatchCommand(
    Guid TenantId,
    Guid OrderId,
    IReadOnlyList<Guid> GyartasilapIds) : IRequest<Result<GenerateBatchResponse>>;

/// <summary>Response returned after a successful batch ZIP generation.</summary>
public sealed record GenerateBatchResponse(
    Guid BatchId,
    string? ZipStoragePath,
    BatchStatusDto Status);

/// <summary>DTO mapping of BatchStatus enum for response serialization.</summary>
public enum BatchStatusDto { Pending = 0, Generating = 1, Ready = 2, Failed = 3 }
