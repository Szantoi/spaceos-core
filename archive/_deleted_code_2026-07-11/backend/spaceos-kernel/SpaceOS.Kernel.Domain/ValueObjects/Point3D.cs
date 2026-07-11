namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Value object representing a three-dimensional point with integer coordinates in millimetres.
/// </summary>
public readonly record struct Point3D(int X, int Y, int Z);
