namespace SpaceOS.Nesting.Algorithms.Models;

/// <summary>
/// A dimensioned rectangular waste piece remaining after panel packing.
/// X and Y are the top-left coordinates relative to the panel origin (0,0).
/// </summary>
public sealed record WastePiece(
    decimal X,
    decimal Y,
    decimal WidthMm,
    decimal HeightMm)
{
    public decimal AreaMm2 => WidthMm * HeightMm;
}
