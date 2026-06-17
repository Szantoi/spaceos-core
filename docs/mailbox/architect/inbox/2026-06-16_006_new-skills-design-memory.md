---
id: MSG-ARCH-006
from: root
to: architect
type: task
priority: high
status: READ
model: opus
created: 2026-06-16
---

# Architect — Új skill környezet + DESIGN_MEMORY

## Változások

A VPS-en most elérhetők az alábbi skill-ek a claude.ai projektből migrált anyagok alapján:

```
/opt/spaceos/.claude/skills/
  spaceos-arch-planner/          ← TE HASZNÁLD MINDEN TERVDOKNÁL
  ddd-arch-planner/
  spaceos-frontend-arch-planner/
  spaceos-conductor/
  spaceos-session-kickoff/
  saas-metrics-coach/
```

## spaceos-arch-planner skill — kötelező

Minden tervdokumentum (MSG-ARCH-* task outputja) ezt a skillben lévő review pipeline-t kövesse:

- **v1** Első vázlat
- **v2** Devil's advocate review (mi üthet el, mi hiányzik)
- **v3** Stakeholder fit (Doorstar napi munkája, operátor POV)
- **v4** Végleges, ADR-be is betölthető döntés

Ne adj ki tervdokumentumot v4 szint alatt.

## DESIGN_MEMORY — zárolt döntések

```
docs/knowledge/architecture/DESIGN_MEMORY.md
```

Ez az irányadó dokumentum a claude.ai projektből. A benne lévő ADR-ek **zároltak**, nem nyithatók újra vitára:

- **ADR-010** Modular monolith (nem microservice)
- **ADR-014** pgvector a Kernel PostgreSQL-ben (vektoros keresés itt él)
- **ADR-018/019/020** Tenant isolation, RBAC, audit event immutability
- **ADR-024** IParametricProduct interfész — Kernel nem tudja mi a termék
- **ADR-039** Cross-modul kommunikáció: nincs direct DB hozzáférés, csak publikus API

## Nyitott taskok

Te vagy felelős az alábbi inbox üzenetekért:

1. **MSG-ARCH-003** + **MSG-ARCH-004** — Planning pipeline konzenzus tervek → implementálható spec
2. **MSG-ARCH-005** — RAG knowledge base tervdok

Kérem, mindhárom tervdokumentumhoz alkalmazzad a spaceos-arch-planner v1→v4 folyamatot.

## Dokumentáció elvárás

Minden tervdokumentumhoz legyen:
- **Döntési indoklás** (miért ezt, mi a másik lehetőség és miért ejtettük)
- **ADR hivatkozás** ha meglévő döntésre támaszkodik
- **Implementációs sorrend** (mi blokkolja mit)

DONE outboxban jelezd mikor mindhárom tervdok kész.
