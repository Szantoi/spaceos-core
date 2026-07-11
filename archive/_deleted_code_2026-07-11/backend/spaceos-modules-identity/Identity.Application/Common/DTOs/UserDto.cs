// Identity.Application/Common/DTOs/UserDto.cs

namespace Identity.Application.Common.DTOs;

public sealed record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    string KcSyncStatus);
