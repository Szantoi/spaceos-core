---
id: MSG-ARCH-002
from: root
to: architect
type: task
priority: high
status: READ
ref: MSG-ARCH-001
created: 2026-04-20
---

# ARCH-002 — Cutting Planning v4: Sessions B+C arch spec

> **Sub-agent:** `SE: Architect` ajánlott a mély Well-Architected review-hoz
> **Input:** `docs/tasks/new/SpaceOS_Growth_Strategy_v1.md` — Section 8: Cutting Planning v4

## Kontextus

A Contracts v4.2 (ARCH-001) lezárult. Az előző outbox üzenetedben jelezted:

> "Az aktuális CUTTING-026/027/028 implementáció a 'Phase 1 lightweight' — a teljes architecture még nem implementált. Miután a Contracts 1.3.0 kész, a következő arch doc: SpaceOS_Modules_Cutting_Planning_Architecture_v4.md (Sessions B+C)."

Contracts 1.3.0 ✅ kész. Indítható a Cutting Planning v4 Sessions B+C spec.

## Kért kimenet

**Fájl:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`

A spec tartalmazza:

- **CuttingPlan aggregate full FSM** — státuszok, átmenetek, invariánsok
- **DaySlot value object** — definíció, validáció, kapacitásszámítás
- **ICapacityModel interfész** — metódusok, contract, implementációs stratégiák
- **IReworkPolicy interfész** — rework logika, policy pattern alkalmazás
- **PriorityProfile** — prioritási rangsor, ütközés feloldás
- **Kapcsolat a meglévő kódbázissal** — CUTTING-026/027/028/030 mit csinál, mi hiányzik
- **Implementációs sorrend** — milyen sorrendben adja ki Root a feladatokat

## Elvárt outbox formátum

`mailbox/outbox/2026-04-20_002_cutting-planning-v4-response.md`

Kötelező szekciók (ARCH-001 mintájára): Összefoglaló · Elemzés · Döntési opciók · Javasolt spec · Kockázatok / nyitott kérdések Gábornak
