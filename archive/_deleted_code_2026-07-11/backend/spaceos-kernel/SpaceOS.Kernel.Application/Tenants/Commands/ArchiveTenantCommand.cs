// SpaceOS.Kernel.Application/Tenants/Commands/ArchiveTenantCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>Archives a <see cref="Domain.Entities.Tenant"/> by setting its <c>IsArchived</c> flag.</summary>
/// <param name="Id">The unique identifier of the tenant to archive.</param>
public sealed record ArchiveTenantCommand(Guid Id) : IRequest<Result>;
