namespace SpaceOS.Nesting.Algorithms.Models;

/// <summary>The result of placing parts onto a single panel.</summary>
public sealed class PanelAssignment
{
    public string PanelId { get; }
    public string MaterialCode { get; }
    public decimal PanelWidthMm { get; }
    public decimal PanelHeightMm { get; }
    public IReadOnlyList<PlacedPart> PlacedParts { get; }
    public decimal WasteAreaMm2 { get; }
    public decimal UtilizationPercent { get; }
    public IReadOnlyList<WastePiece> WastePieces { get; init; } = Array.Empty<WastePiece>();

    public PanelAssignment(
        string panelId,
        string materialCode,
        decimal panelWidthMm,
        decimal panelHeightMm,
        IReadOnlyList<PlacedPart> placedParts)
    {
        PanelId = panelId;
        MaterialCode = materialCode;
        PanelWidthMm = panelWidthMm;
        PanelHeightMm = panelHeightMm;
        PlacedParts = placedParts;

        var totalPartArea = placedParts.Sum(p => p.WidthMm * p.HeightMm);
        var panelArea = panelWidthMm * panelHeightMm;
        WasteAreaMm2 = panelArea - totalPartArea;
        UtilizationPercent = panelArea > 0
            ? Math.Round(totalPartArea / panelArea * 100m, 2)
            : 0m;
    }
}
