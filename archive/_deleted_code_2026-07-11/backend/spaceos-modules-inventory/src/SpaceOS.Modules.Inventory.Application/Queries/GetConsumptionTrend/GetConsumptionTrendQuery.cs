using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetConsumptionTrend;

public sealed record GetConsumptionTrendQuery(string MaterialType, DateTime From, DateTime To)
    : IRequest<Result<ConsumptionTrendResponse>>;
