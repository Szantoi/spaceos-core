// SpaceOS.Kernel.Application/AuditLog/Queries/GetAuditEventsQuery.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Kernel.Application.AuditLog.Queries;

/// <summary>
/// Returns a single page of audit events, optionally filtered by tenant and/or date range.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose audit events to retrieve. <see langword="null"/> returns events for all tenants.</param>
/// <param name="EventType">The event type name to filter by (e.g. TenantCreatedEvent). <see langword="null"/> returns all event types.</param>
/// <param name="From">Inclusive lower bound for event occurrence time. <see langword="null"/> means no lower bound.</param>
/// <param name="To">Inclusive upper bound for event occurrence time. <see langword="null"/> means no upper bound.</param>
/// <param name="Page">1-based page number. Defaults to 1.</param>
/// <param name="PageSize">Maximum items per page. Defaults to 20, maximum 100.</param>
public sealed record GetAuditEventsQuery(
    Guid? TenantId,
    string? EventType,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize) : IRequest<Result<PagedList<AuditEventDto>>>;
