---
description: how to execute the Architect Sign-off review after Epic Review (Phase 6)
---

# Architect – Sign-off (Fázis 6)

**Szerepkör:** Architect
**Trigger:** Orchestrator dispatch (P5) — `epic_review.md` elkészült
**FSM Output:**
  - APPROVED → Epic: `Review` → `Done`
  - CONDITIONAL → minor fix → újra sign-off
  - REJECTED → Epic: `Review` → `Planning`
**Forrás:** `Operative_Process_Framework_Standard.md` — Fázis 6

---

## Lépések

### 1. Input dokumentumok beolvasása

```
<EPIC_ROOT>/epic_review.md              ← Tech Lead értékelése
<EPIC_ROOT>/plan.md                     ← Eredeti Architect terv
decisions/ADR-*.md                                          ← Vonatkozó ADR-ek
```

Ezen felül **kötelező** a forráskód érintett részeinek áttekintése!

### 2. Architekturális ellenőrzési szempontok

| Szempont | Ellenőrzés |
|:---------|:-----------|
| **Clean Architecture** | Rétegek nem sértik egymást (Domain ← Application ← Infrastructure) |
| **DDD elvek** | Bounded Context határok betartva, Aggregate-ek konzisztensek |
| **ADR betartás** | Az érintett ADR-ekben foglalt döntések implementálva |
| **Tech Debt szint** | Elfogadható szint — nem blokkolja a jövőbeli fejlesztést |
| **API szerződések** | Publikus API-k backward compatible (ha alkalmazható) |
| **Biztonsági szempontok** | Nincs nyilvánvaló biztonsági kockázat |

### 3. Döntés meghozatala

#### APPROVED ✅

Ha minden ellenőrzési szempont teljesül:

```
<EPIC_ROOT>/architect_signoff.md
```

Sablon: `src/agent-system/database/roles/discovery/architect/templates/architect_signoff.template.md`

- Tartalom: összefoglaló értékelés, jóváhagyás indoklása
- `state.md` frissítés: Epic → `Done`
- Orchestratornak: `messages/orchestrator/<timestamp>_from-architect_signoff-approved.md`

#### CONDITIONAL ⚠️

Ha kisebb, nem blokkoló javítások szükségesek:

- Dokumentáld a szükséges javításokat a `architect_signoff.md`-ben (`CONDITIONAL` státusszal)
- Jelöld ki a felelőst (Developer/Tech Lead)
- Határozd meg a határidőt
- Javítás után újra sign-off szükséges (ugyanez a workflow)

#### REJECTED ❌

Ha alapvető architekturális problémák vannak:

- Dokumentáld a konkrét problémákat a `architect_signoff.md`-ben (`REJECTED` státusszal)
- `state.md` frissítés: Epic → `Planning`
- Orchestratornak: `messages/orchestrator/<timestamp>_from-architect_signoff-rejected.md`
- → vissza **Fázis 1**-be

### 4. Definition of Done (APPROVED esetén)

- [ ] Minden architekturális szempont teljesítve
- [ ] `architect_signoff.md` elkészítve APPROVED státusszal
- [ ] `state.md` frissítve

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Architect Sign-off | `<EPIC_ROOT>/architect_signoff.md` |

## Következő fázis

→ **Fázis 7:** `epic-closure.md`
