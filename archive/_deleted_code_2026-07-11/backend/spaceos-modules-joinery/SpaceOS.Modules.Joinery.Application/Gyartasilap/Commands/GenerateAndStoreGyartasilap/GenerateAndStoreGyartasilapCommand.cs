using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateAndStoreGyartasilap;

/// <summary>
/// Generates a Gyártásilap PDF for the specified order, stores it in MinIO WORM bucket,
/// and persists the metadata in the database.
/// </summary>
public sealed record GenerateAndStoreGyartasilapCommand(
    Guid TenantId,
    Guid JoineryOrderId,
    Guid? CuttingPlanId,
    string LabelVariant = "L1") : IRequest<Result<GenerateAndStoreGyartasilapResponse>>;
