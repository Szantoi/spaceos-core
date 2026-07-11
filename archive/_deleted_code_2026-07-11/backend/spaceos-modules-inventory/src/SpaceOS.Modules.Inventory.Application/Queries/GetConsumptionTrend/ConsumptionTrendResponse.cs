namespace SpaceOS.Modules.Inventory.Application.Queries.GetConsumptionTrend;

public sealed record ConsumptionTrendResponse(
    string MaterialType,
    IReadOnlyList<DailyConsumptionEntry> DailyData,
    decimal AverageDailyConsumption);

public sealed record DailyConsumptionEntry(DateTime Date, decimal Area);
