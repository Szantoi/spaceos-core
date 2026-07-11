// Identity.Application/Common/UserMapper.cs

using Identity.Application.Common.DTOs;
using Identity.Domain.Aggregates;

namespace Identity.Application.Common;

internal static class UserMapper
{
    internal static UserDto ToDto(SpaceOSUser user) => new(
        Id: user.Id.Value,
        TenantId: user.TenantId,
        Email: user.Email.Value,
        FirstName: user.DisplayName.FirstName,
        LastName: user.DisplayName.LastName,
        Status: user.Status.ToString(),
        KcSyncStatus: user.KcSyncStatus.ToString());
}
