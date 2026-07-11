---
id: MSG-ARCHITECT-003
from: root
to: architect
type: task
priority: high
status: READ
model: opus
ref:
created: 2026-06-21
content_hash: 406e87e2e98070f731e860004cced2f5e05ec6828c9e93e3ad2b7f01a2816bf3
---

# Marveen Hideg Indítási Stratégia — Nexus Architektúra Tervezés

## Kontextus

A Marveen projekt (https://github.com/Szotasz/marveen) egy intelligens agent infrastruktúrát épít, ahol az agentek **hideg indítással** dolgoznak:
- Nem töltik vissza az előző beszélgetést
- Helyette **intelligens memória rendszerrel** kompenzálnak
- Session indításkor a releváns memóriákat és tudásbázist betöltik

Ez a megközelítés most integrálásra kerül a SpaceOS Nexus knowledge-service-be.

## Marveen Skill Rendszer — Kulcs Komponensek

### 1. Hideg Indítás Flow
```
Session indítás
    ↓
CLAUDE.md + NEXUS_USAGE.md beolvasás
    ↓
Memory API lekérdezés (hot/warm memóriák)
    ↓
Kontextus építés releváns memóriákból
    ↓
Munka
    ↓
Session végén: /retrospective vagy /handoff
```

### 2. Memória Típusok és Életciklusok

| Típus | Élettartam | Mire való | Példa |
|---|---|---|---|
| **hot** | 24-48 óra | Aktív feladat kontextus | "Working on PR #42" |
| **warm** | 1-2 hét | Stabil preferenciák | "User prefers TypeScript strict" |
| **cold** | Hosszú távú | Architekturális döntések | "ADR-039: Event sourcing" |
| **shared** | Örök | Cross-agent tudás | "Production deploy requires VPN" |

### 3. Retrospective Skill
Session elemzés és önfejlesztés:
- A/B/C framework (What happened? Why? What should change?)
- Skill proposals (create/patch/delete)
- Memory proposals (save/update/retier)
- Workflow proposals
- User jóváhagyás után automatikus végrehajtás

### 4. Handoff Skill
Kontextus átadás session váltáskor:
- 5 kötelező szekció: Goal, Current Progress, What Worked, What Didn't Work, Next Steps
- Generálható fájlba vagy inter-agent üzenetbe
- Kanban, memóriák, daily log integrálása

### 5. Fleet Helper
Token-takarékos Python szkriptek:
- Determinisztikus munka (SQL, filter, format) Python-ban, nem LLM-ben
- Heartbeat gate pattern (olcsó turn ha nincs teendő)
- Dashboard API wrapper

## Feladat

Tervezd meg a Nexus Knowledge Service bővítését a Marveen hideg indítási stratégiával:

### 1. Session Start Hook
- Terminál indításkor Memory API automatikus lekérdezés
- Hot + warm memóriák összegyűjtése
- Knowledge Service releváns dokumentumok lekérdezése
- Kontextus injektálás a session-be

### 2. Session End Hook
- /retrospective trigger lehetőség
- Automatikus memória mentés (hot tier)
- /handoff generálás context limit előtt

### 3. Memory Tier Management
- Automatikus decay (hot → warm → cold)
- Daily digest generálás
- Cross-agent shared memóriák

### 4. Integráció a meglévő rendszerrel
- `config/terminals.json` — terminál specifikus memória kezelés
- Heartbeat — memória állapot monitoring
- Dashboard API — memória CRUD

## Elvárt Output

1. **ADR dokumentum** (`docs/adr/ADR-XXX_marveen-cold-start-strategy.md`)
   - Architekturális döntések indoklása
   - Trade-off-ok elemzése
   - Alternatívák és miért nem azokat választottuk

2. **Implementációs terv** (track-okra bontva)
   - Track A: Memory API bővítés (tier management, decay)
   - Track B: Session hooks (start/end)
   - Track C: Retrospective/Handoff integráció
   - Track D: Dashboard integráció

3. **API spec** (OpenAPI vagy TypeScript interface-ek)
   - Memory CRUD endpoints
   - Session lifecycle hooks
   - Terminal-specifikus konfiguráció

## Referenciák

- Marveen skills: `~/.claude/skills/` (retrospective, handoff, fleet-helper)
- Nexus meglévő Memory API: `/opt/spaceos/spaceos-nexus/knowledge-service/src/memory.ts`
- Nexus config: `/opt/spaceos/spaceos-nexus/knowledge-service/config/terminals.json`
- NEXUS_USAGE.md: `/opt/spaceos/spaceos-nexus/NEXUS_USAGE.md`

## Definition of Done

- [ ] ADR dokumentum elkészült
- [ ] Implementációs terv track-okra bontva
- [ ] API spec TypeScript interface-ekkel
- [ ] Trade-off elemzés (token cost, latency, storage)
- [ ] DONE outbox üzenet elküldve
