using Ardalis.Result;

namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

public sealed record HardwareSpec
{
    public string? LockType { get; init; }
    public string? LockSize { get; init; }
    public string? StrikeType { get; init; }
    public string? HandleType { get; init; }
    public string? HandleColor { get; init; }
    public string? HandleKit { get; init; }
    public bool KeyholeDrilling { get; init; }
    public bool AutoThreshold { get; init; }
    public bool PanelTensioner { get; init; }
    public string? HingeType { get; init; }
    public int HingeCount { get; init; }
    public string? HingeSpacing { get; init; }
    public string? HingeColor { get; init; }
    public string? EdgeStripType { get; init; }
    public string? EdgeStripColor { get; init; }
    public string? SealType { get; init; }
    public string? SealColor { get; init; }

    private HardwareSpec() { }

    public static Result<HardwareSpec> Create(
        string? lockType,
        string? lockSize,
        string? strikeType,
        string? handleType,
        string? handleColor,
        string? handleKit,
        bool keyholeDrilling,
        bool autoThreshold,
        bool panelTensioner,
        string? hingeType,
        int hingeCount,
        string? hingeSpacing,
        string? hingeColor,
        string? edgeStripType,
        string? edgeStripColor,
        string? sealType,
        string? sealColor)
    {
        return Result<HardwareSpec>.Success(new HardwareSpec
        {
            LockType = lockType,
            LockSize = lockSize,
            StrikeType = strikeType,
            HandleType = handleType,
            HandleColor = handleColor,
            HandleKit = handleKit,
            KeyholeDrilling = keyholeDrilling,
            AutoThreshold = autoThreshold,
            PanelTensioner = panelTensioner,
            HingeType = hingeType,
            HingeCount = hingeCount,
            HingeSpacing = hingeSpacing,
            HingeColor = hingeColor,
            EdgeStripType = edgeStripType,
            EdgeStripColor = edgeStripColor,
            SealType = sealType,
            SealColor = sealColor
        });
    }
}
