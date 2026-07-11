using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Application.Templates.Commands;

public sealed record CloneProductTemplateCommand(
    Guid SourceTemplateId,
    Guid JwtTenantId) : IRequest<Result<ProductTemplate>>;

public sealed class CloneProductTemplateHandler : IRequestHandler<CloneProductTemplateCommand, Result<ProductTemplate>>
{
    private readonly IAbstractionsRepository _repository;

    public CloneProductTemplateHandler(IAbstractionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductTemplate>> Handle(
        CloneProductTemplateCommand request, CancellationToken cancellationToken)
    {
        var source = await _repository.GetTemplateWithAllAsync(request.SourceTemplateId, request.JwtTenantId, cancellationToken)
                                      .ConfigureAwait(false);
        if (source == null) return Result<ProductTemplate>.NotFound($"Template {request.SourceTemplateId} not found");
        if (source.TenantId != request.JwtTenantId)
            return Result<ProductTemplate>.Forbidden(); // SEC-05: cross-tenant clone blocked

        // Determine next version
        var maxVersion = await _repository.GetMaxVersionAsync(
            request.JwtTenantId, source.Name, cancellationToken).ConfigureAwait(false);

        var cloneResult = ProductTemplate.Create(request.JwtTenantId, source.TradeType, source.Name);
        if (!cloneResult.IsSuccess) return cloneResult;
        var clone = cloneResult.Value;

        // Copy slots
        var slotIdMap = new Dictionary<Guid, Guid>();
        foreach (var slot in source.Slots)
        {
            var slotResult = clone.AddSlot(slot.Name, slot.ComponentType, slot.DefaultMaterial,
                slot.DefaultThickness, slot.Quantity, slot.IsVirtual, slot.SemanticRole, slot.SortOrder);
            if (!slotResult.IsSuccess) return Result<ProductTemplate>.Error(slotResult.Errors.First());
            slotIdMap[slot.Id] = slotResult.Value.Id;
        }

        // Copy connections (remapping slot IDs)
        foreach (var conn in source.Connections)
        {
            if (!slotIdMap.TryGetValue(conn.ParentSlotId, out var newParent)) continue;
            if (!slotIdMap.TryGetValue(conn.ChildSlotId, out var newChild)) continue;
            Guid? newSecondary = conn.SecondaryParentSlotId.HasValue
                && slotIdMap.TryGetValue(conn.SecondaryParentSlotId.Value, out var ns) ? ns : null;

            clone.AddConnection(newParent, newChild, conn.Axis,
                conn.Operator, conn.Operand, conn.MultiplierCount,
                newSecondary, conn.JointType, conn.MachiningOp, conn.ProcessPhase,
                conn.GrooveDepth, conn.GrooveWidth, conn.DrillDiameter, conn.DrillDepth,
                conn.Angle, conn.Radius, conn.JointNote);
        }

        // Copy parameters
        foreach (var param in source.Parameters)
            clone.SetParameter(param.Key, param.Value, param.Description);

        await _repository.AddTemplateAsync(clone, cancellationToken).ConfigureAwait(false);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ProductTemplate>.Success(clone);
    }
}
