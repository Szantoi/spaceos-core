---
description: how to execute the Architect Epic Planning phase (Phase 1)
---

# Architect – Epic Planning (Fázis 1)

**Szerepkör:** Architect
**Trigger:** Orchestrator dispatch (P5 sablon)
**FSM Output:** Epic → `Planning`
**Forrás:** `Operative_Process_Framework_Standard.md` — Fázis 1

---

## Lépések

### 1. Input dokumentumok beolvasása

Olvasd be az alábbi fájlokat:

```
goal.md                             ← Epic célkitűzés
dependency_map.md                   ← Fennálló függőségek
decisions/ADR-*.md                  ← Korábbi architektúra döntések
src/agent-system/database/standards/  ← Releváns szabványok
```

### 2. Alternatívák értékelése

Az **Epic Plan** tartalmára vonatkozó feltétel: **minimum 2 alternatív megközelítést** kell kiértékelni. Dokumentáld mindkettőt a `plan.md`-ben:

- **Megközelítés A:** [technikai leírás, előnyök, hátrányok]
- **Megközelítés B:** [technikai leírás, előnyök, hátrányok]
- **Választott megközelítés + indoklás**

### 3. ADR írása (ha szükséges)

Ha a tervezési folyamat során **kritikus architekturális döntés** születik:

```
decisions/ADR-<NNN>-<slug>.md       ← sablon: architect_signoff.template.md alapján
```

ADR szükséges, ha:
- Új technológia vagy keretrendszer bevezetése
- API szerződés érintett
- Cross-team függőség keletkezik
- Visszafordíthatatlan döntés

### 4. Epic Plan dokumentum elkészítése

Hozd létre a plan fájlt:

```
Hozd létre a plan fájlt az Epic gyökerében (Lean: `docs/<project>/epics/<EPIC_ID>/plan.md` vagy Enterprise: `docs/<project>/milestones/<M>/epics/<EPIC_ID>/plan.md`):
```

Kötelező tartalom:
- [ ] Legalább 2 értékelt alternatíva
- [ ] Választott megközelítés indoklással
- [ ] Technikai függőségek listája
- [ ] Kockázatok és mitigálásuk
- [ ] ADR hivatkozások (ha van)

### 5. Definition of Done ellenőrzés

- [ ] Min. 2 alternatíva dokumentálva
- [ ] ADR kritikus döntésekhez csatolva
- [ ] Függőségek dokumentálva és feloldhatók
- [ ] `plan.md` létrehozva

### 6. Visszajelzés az Orchestratornak

Küldj üzenetet:
- Célpont: `messages/orchestrator/<timestamp>_from-architect_planning-done.md`
- `state.md` frissítés: Epic → `Planning` (confirmed)

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Epic Plan | `docs/<project>/epics/<EPIC_ID>/plan.md` |
| ADR (opcionális) | `decisions/ADR-<NNN>-<slug>.md` |

## Következő fázis

→ **Fázis 2:** `tech-lead-task-planning.md`
