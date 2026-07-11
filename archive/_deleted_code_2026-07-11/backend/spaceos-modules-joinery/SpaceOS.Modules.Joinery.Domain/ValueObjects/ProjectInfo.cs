using Ardalis.Result;

namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

public sealed record ProjectInfo
{
    public string? ClientName { get; init; }
    public string? ClientAddress { get; init; }
    public string? ClientPhone { get; init; }
    public DateOnly? DeliveryDate { get; init; }

    private ProjectInfo() { }

    public static Result<ProjectInfo> Create(
        string? clientName,
        string? clientAddress,
        string? clientPhone,
        DateOnly? deliveryDate)
    {
        return Result<ProjectInfo>.Success(new ProjectInfo
        {
            ClientName = clientName,
            ClientAddress = clientAddress,
            ClientPhone = clientPhone,
            DeliveryDate = deliveryDate
        });
    }
}
