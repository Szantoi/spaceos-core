namespace SpaceOS.Modules.Joinery.Application.Anyaglista.Repositories;

/// <summary>Repository interface for Anyaglista aggregate.</summary>
public interface IAnyaglistaRepository
{
    Task<Domain.Core.Anyaglista?> GetByOrderIdAsync(Guid orderId, Guid tenantId, CancellationToken ct);
    Task AddAsync(Domain.Core.Anyaglista anyaglista, CancellationToken ct);
}
