// Identity.Infrastructure/CurrentUser/CurrentUserContext.cs

using System.Security.Claims;
using Identity.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure.CurrentUser;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserContext(IHttpContextAccessor accessor) => _accessor = accessor;

    // SEC-09: tid kizárólag JWT-ből — header/body/querystring elfogadás TILTVA
    public Guid TenantId =>
        Guid.Parse(_accessor.HttpContext!.User.FindFirstValue("tid")
            ?? throw new UnauthorizedAccessException("tid claim missing from JWT"));

    public Guid UserId =>
        Guid.Parse(_accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("sub claim missing from JWT"));

    public bool IsAdmin =>
        _accessor.HttpContext!.User.IsInRole("TenantAdmin");

    public bool IsSuperAdmin =>
        _accessor.HttpContext!.User.IsInRole("SuperAdmin");
}
