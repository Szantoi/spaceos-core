using Ardalis.Result;
using SpaceOS.Modules.Joinery.Domain.Common;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Events;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Domain.Aggregates;

public sealed class DoorOrder : TenantScopedEntity
{
    private const int MaxItems = 500;
    private const int MaxErrorLength = 2000;

    public string ProjectId { get; private set; } = string.Empty;
    public string? ProjectName { get; private set; }
    public Guid FlowEpicId { get; private set; }
    public ProjectInfo? ProjectInfo { get; private set; }
    public DoorOrderStatus Status { get; private set; }
    public string? CalculationError { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. Incremented on every state-changing operation.
    /// </summary>
    public int Version { get; private set; } = 1;

    // Conversion fields — only populated for orders created via Quote→Order conversion
    public Guid? CustomerId { get; private set; }
    public Guid? LinkedTenantId { get; private set; }
    public Guid? SourceQuoteId { get; private set; }
    public string? SourceContentHash { get; private set; }
    public DateTimeOffset? ConfirmedFromSalesAt { get; private set; }
    public string? Currency { get; private set; }
    public decimal? TotalNet { get; private set; }
    public decimal? TotalVat { get; private set; }
    public decimal? TotalGross { get; private set; }

    private readonly List<DoorItem> _items = new();
    public IReadOnlyList<DoorItem> Items => _items.AsReadOnly();

    private readonly List<DoorOrderConvertedLine> _convertedLines = new();
    public IReadOnlyList<DoorOrderConvertedLine> ConvertedLines => _convertedLines.AsReadOnly();

    private DoorOrder() { } // EF Core

    public static Result<DoorOrder> Create(
        Guid tenantId,
        string projectId,
        string? projectName,
        Guid flowEpicId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return Result<DoorOrder>.Invalid(new ValidationError("ProjectId", "ProjectId cannot be empty."));

        if (flowEpicId == Guid.Empty)
            return Result<DoorOrder>.Invalid(new ValidationError("FlowEpicId", "FlowEpicId cannot be empty."));

        var order = new DoorOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            ProjectName = projectName,
            FlowEpicId = flowEpicId,
            Status = DoorOrderStatus.Draft,
            Version = 1
        };

        order.AddDomainEvent(new DoorOrderCreated(order.Id, tenantId, projectId));

        return Result<DoorOrder>.Success(order);
    }

    public Result AddItem(DoorItem item)
    {
        if (Status != DoorOrderStatus.Draft)
            return Result.Invalid(new ValidationError("Status", "Items can only be added to orders in Draft status."));

        if (_items.Count >= MaxItems)
            return Result.Invalid(new ValidationError("Items", $"An order cannot have more than {MaxItems} items."));

        _items.Add(item);
        AddDomainEvent(new DoorItemAdded(Id, TenantId, item.Id));

        return Result.Success();
    }

    /// <summary>
    /// Submits the order for calculation. Only valid from <see cref="DoorOrderStatus.Draft"/>.
    /// Requires at least one item.
    /// </summary>
    public Result Submit()
    {
        if (_items.Count == 0)
            return Result.Invalid(new ValidationError("Items", "Cannot submit an order with no items."));

        if (Status != DoorOrderStatus.Draft)
            return Result.Invalid(new ValidationError("Status", "Only Draft orders can be submitted."));

        Status = DoorOrderStatus.Submitted;
        Version++;
        AddDomainEvent(new DoorOrderSubmitted(Id, TenantId));

        return Result.Success();
    }

    /// <summary>
    /// Marks the order as being processed by the Graph Engine. Only valid from <see cref="DoorOrderStatus.Submitted"/>.
    /// </summary>
    public Result MarkCalculating()
    {
        if (Status != DoorOrderStatus.Submitted)
            return Result.Invalid(new ValidationError("Status", "Only Submitted orders can transition to Calculating."));

        Status = DoorOrderStatus.Calculating;
        Version++;

        return Result.Success();
    }

    /// <summary>
    /// Marks the order as fully calculated. Only valid from <see cref="DoorOrderStatus.Calculating"/>.
    /// </summary>
    public Result MarkCalculated()
    {
        if (Status != DoorOrderStatus.Calculating)
            return Result.Invalid(new ValidationError("Status", "Only Calculating orders can transition to Calculated."));

        Status = DoorOrderStatus.Calculated;
        Version++;
        AddDomainEvent(new DoorOrderCalculated(Id, TenantId, _items.Count));

        return Result.Success();
    }

    /// <summary>
    /// Marks the order as having failed during calculation. Only valid from <see cref="DoorOrderStatus.Calculating"/>.
    /// The <paramref name="reason"/> is truncated to 2000 characters.
    /// </summary>
    public Result MarkCalculationFailed(string reason)
    {
        if (Status != DoorOrderStatus.Calculating)
            return Result.Invalid(new ValidationError("Status", "Only Calculating orders can transition to CalculationFailed."));

        Status = DoorOrderStatus.CalculationFailed;
        CalculationError = reason?.Length > MaxErrorLength ? reason[..MaxErrorLength] : reason;
        Version++;
        AddDomainEvent(new DoorOrderCalculationFailed(Id, TenantId, CalculationError));

        return Result.Success();
    }

    /// <summary>
    /// Reverts the order to Draft so it can be corrected and re-submitted.
    /// Only valid from <see cref="DoorOrderStatus.CalculationFailed"/> or <see cref="DoorOrderStatus.Calculated"/>.
    /// </summary>
    public Result RevertToDraft()
    {
        if (Status is not (DoorOrderStatus.CalculationFailed or DoorOrderStatus.Calculated))
            return Result.Invalid(new ValidationError("Status", "Only CalculationFailed or Calculated orders can be reverted to Draft."));

        Status = DoorOrderStatus.Draft;
        CalculationError = null;
        Version++;
        AddDomainEvent(new DoorOrderReverted(Id, TenantId));

        return Result.Success();
    }

    public static Result<DoorOrder> CreateFromConversion(
        Guid id,
        Guid tenantId,
        Guid customerId,
        Guid? linkedTenantId,
        Guid sourceQuoteId,
        string contentHash,
        string currency,
        decimal totalNet,
        decimal totalVat,
        decimal totalGross,
        IReadOnlyList<ConversionLineData> lines,
        IClock clock)
    {
        if (id == Guid.Empty)
            return Result<DoorOrder>.Invalid(new ValidationError("Id", "Id required."));
        if (tenantId == Guid.Empty)
            return Result<DoorOrder>.Invalid(new ValidationError("TenantId", "TenantId required."));
        if (customerId == Guid.Empty)
            return Result<DoorOrder>.Invalid(new ValidationError("CustomerId", "CustomerId required."));
        if (sourceQuoteId == Guid.Empty)
            return Result<DoorOrder>.Invalid(new ValidationError("SourceQuoteId", "SourceQuoteId required."));
        if (string.IsNullOrWhiteSpace(contentHash) || contentHash.Length > 256)
            return Result<DoorOrder>.Invalid(new ValidationError("ContentHash", "ContentHash: 1..256 char required."));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result<DoorOrder>.Invalid(new ValidationError("Currency", "Currency: ISO 4217 3-char required."));
        if (totalNet <= 0)
            return Result<DoorOrder>.Invalid(new ValidationError("TotalNet", "TotalNet must be > 0."));
        if (totalGross <= 0)
            return Result<DoorOrder>.Invalid(new ValidationError("TotalGross", "TotalGross must be > 0."));
        if (!lines.Any())
            return Result<DoorOrder>.Invalid(new ValidationError("Lines", "At least one converted line required."));

        // Build and validate each converted line
        var builtLines = new List<DoorOrderConvertedLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            var lineResult = DoorOrderConvertedLine.Create(
                Guid.NewGuid(), l.SourceTemplateId, l.Description,
                l.Quantity, l.UnitPriceNet, l.VatRate, l.DiscountPercent, l.SortOrder);
            if (!lineResult.IsSuccess)
                return Result<DoorOrder>.Invalid(lineResult.ValidationErrors);
            builtLines.Add(lineResult.Value);
        }

        var projectId = $"CONV-{sourceQuoteId:N}";
        var order = new DoorOrder
        {
            Id = id,
            TenantId = tenantId,
            CustomerId = customerId,
            LinkedTenantId = linkedTenantId,
            SourceQuoteId = sourceQuoteId,
            SourceContentHash = contentHash,
            Currency = currency,
            TotalNet = totalNet,
            TotalVat = totalVat,
            TotalGross = totalGross,
            Status = DoorOrderStatus.ConfirmedFromSales,
            ConfirmedFromSalesAt = clock.UtcNow,
            Version = 1,
            ProjectId = projectId[..Math.Min(30, projectId.Length)],
            FlowEpicId = Guid.NewGuid(),
        };
        order._convertedLines.AddRange(builtLines);
        order.AddDomainEvent(new DoorOrderCreatedFromConversion(id, tenantId, customerId, sourceQuoteId));
        return Result<DoorOrder>.Success(order);
    }

    public void SetProjectInfo(ProjectInfo? info) => ProjectInfo = info;
}
