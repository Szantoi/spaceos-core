using Ardalis.Result;

namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

public sealed record MaterialSpec
{
    public string? FrameMaterial { get; init; }
    public string? InsertMaterial { get; init; }
    public string? CladMaterial { get; init; }
    public string? FrameCoreMaterial { get; init; }
    public string? BlendeMaterial { get; init; }
    public string? CoatingMaterial { get; init; }

    private MaterialSpec() { }

    public static Result<MaterialSpec> Create(
        string? frameMaterial,
        string? insertMaterial,
        string? cladMaterial,
        string? frameCoreMaterial,
        string? blendeMaterial,
        string? coatingMaterial)
    {
        return Result<MaterialSpec>.Success(new MaterialSpec
        {
            FrameMaterial = frameMaterial,
            InsertMaterial = insertMaterial,
            CladMaterial = cladMaterial,
            FrameCoreMaterial = frameCoreMaterial,
            BlendeMaterial = blendeMaterial,
            CoatingMaterial = coatingMaterial
        });
    }
}
