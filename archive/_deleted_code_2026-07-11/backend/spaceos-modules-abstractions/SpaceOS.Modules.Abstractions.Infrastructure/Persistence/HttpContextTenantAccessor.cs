using Microsoft.AspNetCore.Http;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence;

public sealed class HttpContextTenantAccessor : IHttpContextTenantAccessor
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextTenantAccessor(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? TenantId
    {
        get
        {
            var claim = _accessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}
