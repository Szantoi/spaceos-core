// Identity.Application/Common/ICurrentUserContext.cs

namespace Identity.Application.Common;

public interface ICurrentUserContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    bool IsAdmin { get; }
    bool IsSuperAdmin { get; }
}
