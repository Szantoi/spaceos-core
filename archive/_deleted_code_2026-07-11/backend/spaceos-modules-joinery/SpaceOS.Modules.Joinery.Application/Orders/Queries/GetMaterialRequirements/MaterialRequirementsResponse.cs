using SpaceOS.Modules.Joinery.Domain.Results;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialRequirements;

public sealed record MaterialRequirementsResponse(
    Guid OrderId,
    IReadOnlyList<MaterialRequirement> Requirements);
