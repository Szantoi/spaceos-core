namespace SpaceOS.Modules.Inventory.Infrastructure.Adapters;

public interface IHttpContextTenantAccessor
{
    Guid TenantId { get; }
}
