// SpaceOS.Kernel.Application/ModuleRegistry/GetModuleRegistryQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Services;

namespace SpaceOS.Kernel.Application.ModuleRegistry;

/// <summary>
/// Handles <see cref="GetModuleRegistryQuery"/>: resolves the <c>TenantType</c> string, queries the
/// <see cref="IModuleRegistryService"/>, and returns a <see cref="ModuleRegistryDto"/>.
/// </summary>
internal sealed class GetModuleRegistryQueryHandler : IRequestHandler<GetModuleRegistryQuery, Result<ModuleRegistryDto>>
{
    private readonly IModuleRegistryService _moduleRegistry;

    /// <summary>Initialises a new <see cref="GetModuleRegistryQueryHandler"/>.</summary>
    /// <param name="moduleRegistry">The module registry domain service.</param>
    public GetModuleRegistryQueryHandler(IModuleRegistryService moduleRegistry)
    {
        ArgumentNullException.ThrowIfNull(moduleRegistry);
        _moduleRegistry = moduleRegistry;
    }

    /// <summary>Executes the query and returns the module registry DTO.</summary>
    public Task<Result<ModuleRegistryDto>> Handle(GetModuleRegistryQuery request, CancellationToken ct)
    {
        if (!Enum.TryParse<TenantType>(request.TenantType, ignoreCase: true, out var tenantType))
            return Task.FromResult(Result<ModuleRegistryDto>.NotFound(
                $"Unknown TenantType: '{request.TenantType}'. Valid values: {string.Join(", ", Enum.GetNames<TenantType>())}"));

        var required = _moduleRegistry.GetRequiredModules(tenantType);
        var allowed  = _moduleRegistry.GetAllowedModules(tenantType);

        return Task.FromResult(Result.Success(new ModuleRegistryDto(required, allowed)));
    }
}
