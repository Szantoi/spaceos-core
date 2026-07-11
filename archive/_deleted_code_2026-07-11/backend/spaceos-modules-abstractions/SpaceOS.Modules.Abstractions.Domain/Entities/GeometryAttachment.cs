using System.Text.RegularExpressions;
using Ardalis.Result;
using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Domain.Entities;

public sealed class GeometryAttachment : TenantScopedEntity
{
    private static readonly Regex FileRefRegex =
        new(@"^[a-zA-Z0-9_\-/]+\.(step|stp|ifc|obj|stl|dxf|3mf)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "STEP", "IFC", "OBJ", "STL", "DXF", "3MF"
    };

    public Guid SlotInstanceId { get; private set; }
    public GeometryLevel Level { get; private set; }
    public Guid? SpatialElementId { get; private set; }
    public string? SkeletonJson { get; private set; }
    public string? FileReference { get; private set; }
    public string? FileFormat { get; private set; }
    public string? FileHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private GeometryAttachment() { }

    public static Result<GeometryAttachment> Create(
        Guid tenantId, Guid slotInstanceId, GeometryLevel level,
        Guid? spatialElementId = null, string? skeletonJson = null,
        string? fileReference = null, string? fileFormat = null, string? fileHash = null)
    {
        // SEC-02: FileReference path traversal validation
        if (fileReference != null)
        {
            if (!FileRefRegex.IsMatch(fileReference))
                return Result<GeometryAttachment>.Invalid(new ValidationError("Invalid FileReference path"));
            if (fileReference.StartsWith('/'))
                return Result<GeometryAttachment>.Invalid(new ValidationError("FileReference must not be an absolute path"));
            if (fileReference.Contains(".."))
                return Result<GeometryAttachment>.Invalid(new ValidationError("FileReference must not contain path traversal"));
        }

        if (fileFormat != null && !AllowedFormats.Contains(fileFormat))
            return Result<GeometryAttachment>.Invalid(new ValidationError($"Invalid FileFormat: {fileFormat}"));

        return Result<GeometryAttachment>.Success(new GeometryAttachment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SlotInstanceId = slotInstanceId,
            Level = level,
            SpatialElementId = spatialElementId,
            SkeletonJson = skeletonJson,
            FileReference = fileReference,
            FileFormat = fileFormat?.ToUpperInvariant(),
            FileHash = fileHash,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
