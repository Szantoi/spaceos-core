# SpaceOS.Modules.Contracts — CLAUDE.md

## Stack
- .NET 8 class library (no runtime, no DI, no EF Core)
- Ardalis.Result 9.x — only external dependency
- Target: NuGet package consumed by other SpaceOS modules

## Purpose
Shared interface contracts for the SpaceOS module ecosystem.
Defines: IModuleProvider, ICuttingProvider, IInventoryProvider, IProcurementProvider
and all associated DTOs, events, enums, and request objects.

## Solution structure
```
SpaceOS.Modules.Contracts/        ← class library, ships as NuGet
  Shared/                         ← IModuleProvider, ModuleEvent, ProviderCapability
  Cutting/                        ← ICuttingProvider + DTOs/Events/Enums/Requests
  Inventory/                      ← IInventoryProvider + DTOs/Events/Enums/Requests
  Procurement/                    ← IProcurementProvider + DTOs/Events/Enums/Requests
SpaceOS.Modules.Contracts.Tests/  ← xUnit + FluentAssertions
```

## Golden Rules
1. No TenantId in any Request/input DTO — always resolved from JWT (SEC-01)
2. TenantId present in output DTOs as read-only field (SEC-01 output side)
3. Consumer MUST check ProviderCapability.HasFlag before calling optional methods (SEC-05)
4. Consumer MUST verify ModuleEvent.TenantId matches JWT claim (SEC-03)
5. TreatWarningsAsErrors=true + GenerateDocumentationFile=true — every public type needs XML doc summary
6. NO inline XML doc comments inside positional record parameter lists — CS1587 error; put docs on the record type summary instead
7. CancellationToken parameter is always named `ct`

## Build and pack
```bash
dotnet build          # 0 error, 0 warning
dotnet test           # 20 tests, all green
dotnet pack SpaceOS.Modules.Contracts/SpaceOS.Modules.Contracts.csproj -o ./artifacts
```

## Output
artifacts/SpaceOS.Modules.Contracts.1.0.0.nupkg
