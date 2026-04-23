---
id: MSG-JOINERY-009-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-009
created: 2026-04-16
---

# MSG-JOINERY-009-DONE — Saga 500 diagnózis: deployment race condition

## Verdict: NEM KÓDHIBA — deployment timing probléma

Kód tiszta: **214/214 teszt zöld**, 0 error, 0 warning.

---

## Root Cause

**Deployment race condition**: az INFRA-101 redeploy (07:03) ELŐTT futott az E2E `43-joinery-cutting-integration` teszt a régi binárisok ellen.

### Bizonyíték

```
publish.bak-20260416-070302/   ← régi deployment (07:03 előtt)
  SpaceOS.Modules.Joinery.Api.dll      ✓
  SpaceOS.Modules.Joinery.Application.dll  ✓
  SpaceOS.Modules.Cutting.Contracts.dll   ✗ HIÁNYZIK
  SpaceOS.Modules.Inventory.Contracts.dll ✗ HIÁNYZIK

publish/                       ← jelenlegi deployment (07:03 után)
  SpaceOS.Modules.Joinery.Api.dll      ✓
  SpaceOS.Modules.Cutting.Contracts.dll   ✓
  SpaceOS.Modules.Inventory.Contracts.dll ✓
```

### Mi okozta a 500-at?

A `SubmitDoorOrderCommandHandler` (commit `69db855`, 2026-04-16 06:51) `ICuttingProvider`-t injektál. A régi deployment futott a `SpaceOS.Modules.Cutting.Contracts.dll` nélkül → DI resolution `InvalidOperationException` → 500.

### Lehetséges B és C opció kizárva

- **B (graceful degradation try/catch hatókör)**: a try/catch helyesen a SubmitCuttingSheetAsync hívást védi. Nem ez okozta.
- **C (adatállapot)**: a 500 már az endpoint belépésekor keletkezett (DI resolution hiba), nem adatállapot-függő.

---

## Jelenlegi állapot

```
git HEAD: 69db855 feat(joinery): complete DoD for Modules.Joinery v1
dotnet build  → 0 error, 0 warning
dotnet test   → 214/214 passed
```

### ICuttingProvider integráció (MSG-JOINERY-008) — ALREADY DONE

- `CuttingProviderStub` : `ICuttingProvider` → loggol, graceful degradation
- `SubmitDoorOrderCommandHandler` → `BuildCuttingSheet()` + try/catch wrapper
- DI: `services.AddScoped<ICuttingProvider, CuttingProviderStub>()`
- NuGet.Config: LocalCutting source `/opt/spaceos/spaceos-modules-cutting/nupkg/`

---

## Teendő (E2E terminál)

Az E2E `43-joinery-cutting-integration` teszt **re-run** szükséges a friss deployment ellen. A teszt várhatóan zöld lesz.

Ha az E2E infrastruktúra nem biztosítja a deployment-stabilizáció utáni futtatást (health check + timeout), érdemes az E2E pipeline-ba beépíteni:
1. `POST /health` → 200 megvárása deployment után
2. Csak ezután indítani a `43-joinery-cutting-integration` tesztet
