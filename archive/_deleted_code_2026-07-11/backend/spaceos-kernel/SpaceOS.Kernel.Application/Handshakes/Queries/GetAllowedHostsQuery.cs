// SpaceOS.Kernel.Application/Handshakes/Queries/GetAllowedHostsQuery.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.DTOs;

namespace SpaceOS.Kernel.Application.Handshakes.Queries;

/// <summary>
/// Query that returns the list of allowed B2B host tenants for the current authenticated tenant.
/// Results are capped at 20 entries (SEC-P3CP-08).
/// </summary>
public sealed record GetAllowedHostsQuery() : IRequest<Result<IReadOnlyList<AllowedHostDto>>>;
