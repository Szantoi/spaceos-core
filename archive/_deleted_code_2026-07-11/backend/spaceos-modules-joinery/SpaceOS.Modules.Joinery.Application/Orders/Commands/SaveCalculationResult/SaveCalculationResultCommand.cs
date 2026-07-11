using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.SaveCalculationResult;

/// <summary>
/// Persists a calculation result snapshot for a single door item and,
/// when all items in the order are done, transitions the order to Calculated.
/// </summary>
public sealed record SaveCalculationResultCommand(
    Guid DoorOrderId,
    Guid DoorItemId,
    Guid TenantId,
    string TemplateName,
    int TemplateVersion,
    decimal InputWidth,
    decimal InputHeight,
    string? ParameterOverridesJson,
    IReadOnlyList<CuttingListLine> Lines,
    IReadOnlyList<CncInstruction> CncInstructions,
    IReadOnlyList<ProcessStep> ProcessSteps) : IRequest<Result>;
