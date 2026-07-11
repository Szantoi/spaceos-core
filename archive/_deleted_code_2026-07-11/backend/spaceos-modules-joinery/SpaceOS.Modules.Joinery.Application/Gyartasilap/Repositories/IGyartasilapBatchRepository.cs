using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;

/// <summary>Repository interface for GyartasilapBatch aggregate.</summary>
public interface IGyartasilapBatchRepository
{
    Task<GyartasilapBatch?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task AddAsync(GyartasilapBatch batch, CancellationToken ct);
    Task UpdateAsync(GyartasilapBatch batch, CancellationToken ct);
}
