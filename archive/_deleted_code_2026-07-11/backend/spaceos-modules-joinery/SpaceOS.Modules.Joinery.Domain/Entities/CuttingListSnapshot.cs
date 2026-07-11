using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SpaceOS.Modules.Joinery.Domain.Common;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Domain.Entities;

/// <summary>
/// Immutable snapshot of a cutting list calculation for a single <see cref="DoorItem"/>.
/// Once created the snapshot data never changes; <see cref="MarkNotLatest"/> demotes it
/// when a newer snapshot supersedes it.
/// </summary>
public sealed class CuttingListSnapshot : TenantScopedEntity
{
    private readonly List<CuttingListLine> _lines = new();
    private readonly List<CncInstruction> _cncInstructions = new();
    private readonly List<ProcessStep> _processSteps = new();

    public Guid DoorOrderId { get; private set; }
    public Guid DoorItemId { get; private set; }
    public string TemplateName { get; private set; } = string.Empty;
    public int TemplateVersion { get; private set; }
    public decimal InputWidth { get; private set; }
    public decimal InputHeight { get; private set; }
    public string ParameterOverridesJson { get; private set; } = "{}";
    public string ContentHash { get; private set; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; private set; }
    public bool IsLatest { get; private set; }

    public IReadOnlyList<CuttingListLine> Lines => _lines.AsReadOnly();
    public IReadOnlyList<CncInstruction> CncInstructions => _cncInstructions.AsReadOnly();
    public IReadOnlyList<ProcessStep> ProcessSteps => _processSteps.AsReadOnly();

    private CuttingListSnapshot() { } // EF Core

    /// <summary>
    /// Factory method. Validates inputs, computes a deterministic SHA-256 content hash
    /// that binds to the TenantId (SEC-06), and returns the new snapshot.
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="doorOrderId">Parent order.</param>
    /// <param name="doorItemId">Parent door item.</param>
    /// <param name="templateName">Calculation template name.</param>
    /// <param name="templateVersion">Calculation template version.</param>
    /// <param name="inputWidth">Door opening width used as input (must be &gt; 0 and &lt;= 10000).</param>
    /// <param name="inputHeight">Door opening height used as input (must be &gt; 0 and &lt;= 10000).</param>
    /// <param name="parameterOverridesJson">JSON overrides applied to the template (defaults to <c>{}</c>).</param>
    /// <param name="calculatedAt">UTC timestamp of when the calculation ran.</param>
    /// <param name="lines">Calculated cut-part lines — must contain at least one entry, max 200.</param>
    /// <param name="cncInstructions">Optional CNC instructions.</param>
    /// <param name="processSteps">Optional process steps.</param>
    /// <returns>The created snapshot.</returns>
    /// <exception cref="ArgumentException">Thrown when any validation rule fails.</exception>
    public static CuttingListSnapshot Create(
        Guid tenantId,
        Guid doorOrderId,
        Guid doorItemId,
        string templateName,
        int templateVersion,
        decimal inputWidth,
        decimal inputHeight,
        string? parameterOverridesJson,
        DateTimeOffset calculatedAt,
        IReadOnlyList<CuttingListLine> lines,
        IReadOnlyList<CncInstruction>? cncInstructions = null,
        IReadOnlyList<ProcessStep>? processSteps = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        if (inputWidth <= 0 || inputWidth > 10000)
            throw new ArgumentOutOfRangeException(nameof(inputWidth), "InputWidth must be > 0 and <= 10000.");

        if (inputHeight <= 0 || inputHeight > 10000)
            throw new ArgumentOutOfRangeException(nameof(inputHeight), "InputHeight must be > 0 and <= 10000.");

        if (lines is null || lines.Count == 0)
            throw new ArgumentException("A cutting list snapshot must contain at least one line.", nameof(lines));

        if (lines.Count > 200)
            throw new ArgumentException("A cutting list snapshot cannot contain more than 200 lines.", nameof(lines));

        var overrides = string.IsNullOrWhiteSpace(parameterOverridesJson) ? "{}" : parameterOverridesJson;
        var hash = ComputeContentHash(tenantId, doorItemId, templateName, templateVersion, inputWidth, inputHeight, overrides, lines);

        var snapshot = new CuttingListSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DoorOrderId = doorOrderId,
            DoorItemId = doorItemId,
            TemplateName = templateName,
            TemplateVersion = templateVersion,
            InputWidth = inputWidth,
            InputHeight = inputHeight,
            ParameterOverridesJson = overrides,
            ContentHash = hash,
            CalculatedAt = calculatedAt,
            IsLatest = true
        };

        snapshot._lines.AddRange(lines);

        if (cncInstructions is not null)
            snapshot._cncInstructions.AddRange(cncInstructions);

        if (processSteps is not null)
            snapshot._processSteps.AddRange(processSteps);

        return snapshot;
    }

    /// <summary>
    /// Demotes this snapshot so a newer one becomes the latest for the same door item.
    /// </summary>
    public void MarkNotLatest() => IsLatest = false;

    private static string ComputeContentHash(
        Guid tenantId,
        Guid doorItemId,
        string templateName,
        int templateVersion,
        decimal inputWidth,
        decimal inputHeight,
        string parameterOverridesJson,
        IReadOnlyList<CuttingListLine> lines)
    {
        // Include TenantId in the hash material so snapshots cannot be cross-tenant replayed (SEC-06).
        var payload = JsonSerializer.Serialize(new
        {
            TenantId = tenantId,
            DoorItemId = doorItemId,
            TemplateName = templateName,
            TemplateVersion = templateVersion,
            InputWidth = inputWidth,
            InputHeight = inputHeight,
            ParameterOverridesJson = parameterOverridesJson,
            Lines = lines
        });

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
