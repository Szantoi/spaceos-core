// SpaceOS.Kernel.Application/StageRegistry/Queries/ListStageChainTemplatesQuery.cs
using System;
using System.Collections.Generic;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Returns all <see cref="Domain.Entities.StageChainTemplate"/> records for a tenant.</summary>
/// <param name="TenantId">The identifier of the tenant whose chain templates to return.</param>
public sealed record ListStageChainTemplatesQuery(Guid TenantId)
    : IRequest<Result<IReadOnlyList<StageChainTemplateDto>>>;
