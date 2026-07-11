// SpaceOS.Kernel.Application/ModuleRegistry/GetModuleRegistryQuery.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.ModuleRegistry;

/// <summary>
/// Query that returns the allowed and required modules for a given <c>TenantType</c> string.
/// </summary>
/// <param name="TenantType">Case-insensitive tenant type name (e.g. "Manufacturer", "PanelCutter").</param>
public record GetModuleRegistryQuery(string TenantType) : IRequest<Result<ModuleRegistryDto>>;

/// <summary>
/// Data transfer object returned by <see cref="GetModuleRegistryQuery"/>.
/// </summary>
/// <param name="Required">Modules that must be present for this tenant type.</param>
/// <param name="Allowed">All modules permitted for this tenant type (required + optional).</param>
public record ModuleRegistryDto(IReadOnlyList<string> Required, IReadOnlyList<string> Allowed);
