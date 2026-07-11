namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Value object representing width, height and depth dimensions in millimetres.
/// </summary>
public readonly record struct DimensionVector(int WidthMm, int HeightMm, int DepthMm);
