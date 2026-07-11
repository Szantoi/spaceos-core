// Identity.Application/Common/DTOs/CreateUserDto.cs

namespace Identity.Application.Common.DTOs;

public sealed record CreateUserDto(string Email, string FirstName, string LastName);
