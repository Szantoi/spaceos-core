---
id: MSG-ABSTRACTIONS-011
from: root
to: abstractions
type: task
priority: medium
status: READ
ref: SpaceOS_Modules_Contracts_Architecture_v4_2.md
created: 2026-04-25
---

# ABSTRACTIONS-011 — Contracts v4.2 FreeTier extension points

> **Tervdok:** `docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md` — KÖTELEZŐ olvasmány!
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** Contracts v1.3.0 DEPLOYED

## Feladat

A tervdok v4.2 definiálja a FreeTier extension point-okat a Contracts NuGet-ben. Implementáld ami a spec-ben van.

Olvasd el a tervdokot és implementáld a benne leírt változtatásokat. A spec tartalmaz mindent.

## Definition of Done

- [ ] Contracts NuGet bővítve a tervdok szerint
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 55 pass (52 előző + min 3 új)
- [ ] Outbox DONE
