namespace SpaceOS.Modules.Joinery.Application.Orders.DTOs;

/// <summary>
/// Summary DTO for a cutting list snapshot returned by <c>GET /api/orders/{id}/snapshots</c>.
/// </summary>
/// <param name="Id">Snapshot identifier.</param>
/// <param name="DoorItemId">The door item this snapshot belongs to.</param>
/// <param name="TemplateName">Calculation template name used.</param>
/// <param name="TemplateVersion">Calculation template version used.</param>
/// <param name="InputWidth">Door opening width used as input.</param>
/// <param name="InputHeight">Door opening height used as input.</param>
/// <param name="ContentHash">Deterministic SHA-256 hash of the snapshot content.</param>
/// <param name="CalculatedAt">UTC timestamp when the snapshot was produced.</param>
/// <param name="LineCount">Number of cut-part lines in this snapshot.</param>
public sealed record SnapshotSummaryDto(
    Guid Id,
    Guid DoorItemId,
    string TemplateName,
    int TemplateVersion,
    decimal InputWidth,
    decimal InputHeight,
    string ContentHash,
    DateTimeOffset CalculatedAt,
    int LineCount);
