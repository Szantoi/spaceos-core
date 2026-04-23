# Joinery Infrastructure — Done

**Date:** 2026-04-09
**Task:** SpaceOS.Modules.Joinery.Infrastructure implementálása

## Eredmény

Build: **0 error, 0 warning** ✓

## Létrehozott fájlok

### Persistence
- `Persistence/JoineryDbContext.cs` — DbContext, schema: `spaceos_joinery`, ApplyConfigurationsFromAssembly
- `Persistence/Configurations/DoorOrderConfiguration.cs` — OwnsOne(ProjectInfo), HasMany(Items), RLS-ready, DomainEvents Ignore
- `Persistence/Configurations/DoorItemConfiguration.cs` — OwnsOne mind a 6 VO-hoz (Dimensions, FixSide, MovingSide, Glazing, Hardware, Materials, Processing), enum HasConversion<string>
- `Persistence/Configurations/DoorTypeRuleConfiguration.cs` — HasKey(DoorType)
- `Persistence/Configurations/PartDimensionRuleConfiguration.cs` — HasKey(Id), IX_PartDimensionRules_DoorType
- `Persistence/Configurations/ProcessTaskTemplateConfiguration.cs` — HasKey(TaskId)
- `Persistence/Configurations/GlobalConstantConfiguration.cs` — HasKey(Key)

### Migrations
- `Migrations/20260409000001_Migration_0001_InitialSchema.cs` — teljes DDL SQL (migrationBuilder.Sql), RLS policies, REVOKE/GRANT GlobalConstants
- `Migrations/20260409000001_Migration_0001_InitialSchema.Designer.cs` — stub
- `Migrations/JoineryDbContextModelSnapshot.cs` — stub

### Services
- `Services/DoorCalculationService.cs` — CalculateCuttingList + CalculateFinishedDimensions implementálva
- `Services/HardwareResolutionService.cs` — stub (Array.Empty)
- `Services/ProcessFlowService.cs` — stub (ProcessTaskTemplate → ProcessTask map)
- `Services/MaterialRequirementService.cs` — stub (Array.Empty)

### DI
- `DependencyInjection.cs` — AddDbContext(Npgsql) + 4 Scoped service regisztráció

## Megjegyzések
- `IsArchived` nem létezik a Domain DoorOrder-en — kihagyva a konfigból
- Migration Designer és Snapshot stub-ok, a build zöld marad
- Minden namespace: `SpaceOS.Modules.Joinery.Infrastructure.*`
