// SpaceOS.Kernel.Application/Auth/Commands/RevokeTokenCommand.cs

using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Auth.Commands;

/// <summary>
/// Revokes an active refresh token (logout).
/// Idempotent: if the token does not exist or is already revoked, returns <see cref="Result.Success"/> (BE-P15-11).
/// </summary>
/// <param name="RefreshToken">The raw 43-character Base64Url opaque refresh token to revoke.</param>
public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<Result>;
