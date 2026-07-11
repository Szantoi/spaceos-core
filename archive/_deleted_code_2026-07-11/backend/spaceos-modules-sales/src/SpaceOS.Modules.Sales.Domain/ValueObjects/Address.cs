namespace SpaceOS.Modules.Sales.Domain.ValueObjects;

/// <summary>Postal address value object. All fields are optional.</summary>
public sealed record Address(
    string? Street,
    string? City,
    string? ZipCode,
    string? Country)
{
    /// <summary>Returns an empty address with all null fields.</summary>
    public static Address Empty() => new(null, null, null, null);
}
