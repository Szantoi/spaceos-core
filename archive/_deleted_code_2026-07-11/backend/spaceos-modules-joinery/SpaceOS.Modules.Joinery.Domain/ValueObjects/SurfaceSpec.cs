using Ardalis.Result;
using SpaceOS.Modules.Joinery.Domain.Enums;

namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

public sealed record SurfaceSpec
{
    public SurfaceType SurfaceType { get; init; }
    public string? Color { get; init; }
    public string? ColorCode { get; init; }
    public string? Pattern { get; init; }
    public string? PatternType { get; init; }
    public string? PatternProfile { get; init; }
    public string? CoatingColor { get; init; }
    public bool HasBlende { get; init; }
    public bool HasWallPanel { get; init; }

    private SurfaceSpec() { }

    public static Result<SurfaceSpec> Create(
        SurfaceType surfaceType,
        string? color,
        string? colorCode,
        string? pattern,
        string? patternType,
        string? patternProfile,
        string? coatingColor,
        bool hasBlende,
        bool hasWallPanel)
    {
        return Result<SurfaceSpec>.Success(new SurfaceSpec
        {
            SurfaceType = surfaceType,
            Color = color,
            ColorCode = colorCode,
            Pattern = pattern,
            PatternType = patternType,
            PatternProfile = patternProfile,
            CoatingColor = coatingColor,
            HasBlende = hasBlende,
            HasWallPanel = hasWallPanel
        });
    }
}
