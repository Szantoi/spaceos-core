namespace SpaceOS.Modules.Joinery.Domain.Results;

public sealed record MaterialRequirement(
    string Material,
    decimal Thickness,
    decimal TotalM2,
    decimal TotalLinearMeter);
