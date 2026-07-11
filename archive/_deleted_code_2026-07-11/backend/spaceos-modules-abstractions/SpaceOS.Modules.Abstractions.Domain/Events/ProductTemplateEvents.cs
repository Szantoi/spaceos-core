using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Domain.Events;

public record ProductTemplateCreated(Guid TemplateId, Guid TenantId, string TradeType, string Name) : IDomainEvent;
public record ProductTemplateVersioned(Guid NewTemplateId, Guid SourceTemplateId, int NewVersion) : IDomainEvent;
public record CalculationCompleted(Guid TemplateId, Guid TenantId, int CuttingListCount) : IDomainEvent;
public record GeometryAttached(Guid SlotId, Guid AttachmentId, GeometryLevel Level) : IDomainEvent;
