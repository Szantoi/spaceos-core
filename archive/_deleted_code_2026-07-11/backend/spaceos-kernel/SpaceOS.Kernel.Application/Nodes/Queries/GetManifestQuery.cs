// SpaceOS.Kernel.Application/Nodes/Queries/GetManifestQuery.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Nodes.Queries;

/// <summary>
/// Returns the <see cref="NodeManifestDto"/> for the node registered under the given tenant.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose manifest to retrieve.</param>
public sealed record GetManifestQuery(Guid TenantId) : IRequest<Result<NodeManifestDto>>;
