using Ardalis.Result;

namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

public sealed record GlazingSpec
{
    public string? GlazingType { get; init; }
    public string? GlazingColor { get; init; }
    public string? GlazingStyle { get; init; }
    public string? GlazingPattern { get; init; }

    private GlazingSpec() { }

    public static Result<GlazingSpec> Create(
        string? glazingType,
        string? glazingColor,
        string? glazingStyle,
        string? glazingPattern)
    {
        return Result<GlazingSpec>.Success(new GlazingSpec
        {
            GlazingType = glazingType,
            GlazingColor = glazingColor,
            GlazingStyle = glazingStyle,
            GlazingPattern = glazingPattern
        });
    }
}
