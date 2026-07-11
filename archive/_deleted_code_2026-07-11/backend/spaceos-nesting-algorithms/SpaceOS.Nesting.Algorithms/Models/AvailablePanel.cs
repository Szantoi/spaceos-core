namespace SpaceOS.Nesting.Algorithms.Models;

/// <summary>A panel available for cutting. Offcuts are prioritised over full panels.</summary>
public sealed record AvailablePanel(
    string PanelId,
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    bool IsOffcut);
