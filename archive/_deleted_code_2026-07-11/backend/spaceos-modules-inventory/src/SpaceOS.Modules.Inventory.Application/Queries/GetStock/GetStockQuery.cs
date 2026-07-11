using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Inventory.Application.Queries.GetStock;

public sealed record GetStockQuery(string MaterialType) : IRequest<Result<StockLevelResponse>>;
