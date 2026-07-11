namespace SpaceOS.Modules.Joinery.Domain.Enums;

public enum DoorOrderStatus
{
    Draft,
    ConfirmedFromSales,  // Created via Quote→Order conversion (Sales integration)
    Submitted,
    Calculating,        // Graph Engine dolgozik
    Calculated,         // Minden item snapshot kész
    CalculationFailed,  // Graph Engine hiba
    InProduction,
    Completed,
    Cancelled
}
