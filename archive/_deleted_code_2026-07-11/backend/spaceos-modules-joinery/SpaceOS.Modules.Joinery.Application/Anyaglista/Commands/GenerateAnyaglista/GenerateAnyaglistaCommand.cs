using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Anyaglista.Commands.GenerateAnyaglista;

/// <summary>
/// Generates an Anyaglista (material requirements list) PDF for the specified order,
/// uploads it to MinIO, and persists the metadata.
/// </summary>
public sealed record GenerateAnyaglistaCommand(
    Guid TenantId,
    Guid OrderId) : IRequest<Result<GenerateAnyaglistaResponse>>;

/// <summary>Response returned after successful Anyaglista generation.</summary>
public sealed record GenerateAnyaglistaResponse(
    Guid AnyaglistaId,
    string StorageUrl);
