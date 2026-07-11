using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Internal.Dtos;

namespace SpaceOS.Kernel.Application.Internal.Queries;

/// <summary>
/// Internal cross-module read — returns minimal tenant actor info for ADR-039 read path.
/// SEC-S-09: response contains no contact, billing, or PII fields.
/// </summary>
/// <param name="RequesterTenantId">Tenant performing the lookup (X-SpaceOS-TenantId header).</param>
/// <param name="TargetTenantId">Tenant being looked up (route {id}).</param>
public sealed record GetTenantActorQuery(
    Guid RequesterTenantId,
    Guid TargetTenantId) : IRequest<Result<TenantActorResponse>>;
