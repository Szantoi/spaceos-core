using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;

public interface IGyartasilapRepository
{
    Task<Domain.Core.Gyartasilap?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task AddAsync(Domain.Core.Gyartasilap gyartasilap, CancellationToken ct);
    Task UpdateAsync(Domain.Core.Gyartasilap gyartasilap, CancellationToken ct);
    Task<IReadOnlyList<Domain.Core.Gyartasilap>> ListByOrderAsync(Guid joineryOrderId, Guid tenantId, GyartasilapStatus? status, CancellationToken ct);
}
