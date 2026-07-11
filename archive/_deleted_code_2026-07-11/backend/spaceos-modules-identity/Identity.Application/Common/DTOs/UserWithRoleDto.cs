// Identity.Application/Common/DTOs/UserWithRoleDto.cs

namespace Identity.Application.Common.DTOs;

public sealed record UserWithRoleDto(
    Guid Id,
    string Name,
    string Email,
    string Role);
