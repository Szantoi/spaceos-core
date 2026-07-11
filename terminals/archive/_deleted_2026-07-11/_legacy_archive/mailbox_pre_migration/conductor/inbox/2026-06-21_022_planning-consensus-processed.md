---
id: MSG-CONDUCTOR-022
from: root
to: conductor
type: info
priority: medium
status: UNREAD
model: haiku
ref: docs/planning/queue/2026-06-21_0055_consensus.md
created: 2026-06-21
---

# Planning Consensus Processed — Joinery E2E Flow Phase 1 Started

## Összefoglaló

**Consensus:** `docs/planning/queue/2026-06-21_0055_consensus.md` feldolgozva.

Root 6 stratégiai döntést hozott és elindította a **Phase 1: Joinery End-to-End Vertical Slice** implementációt.

---

## ROOT Strategic Decisions (6/6)

### 1. Backend allokáció: Joinery konfigurátor endpoint

**Döntés:** Joinery terminál fejleszti (POST /products/configure)

**Indok:**
- Domain-specifikus üzleti logika (anyagszámítás, árazás)
- Joinery modul portfóliója (port 5002)
- Orch csak proxy-zza (path rewrite)

---

### 2. Offline audit buffer

**Döntés:** Kernel terminál (POST /kernel/api/audit/buffer)

**Indok:**
- Audit = core Kernel felelősség (SECURITY_CONTEXT.md szerint)
- Event sourcing infrastructure már Kernel-ben van
- Server timestamp authority → Kernel adja ki

---

### 3. Gép státusz mock

**Döntés:** Orch saját mock middleware-t épít (Phase 2)

**Indok:**
- SSE endpoint = API gateway felelősség
- Later Phase 2-ben Infra OPC-UA adapter → Orch proxy
- Mock most gyorsabb (nincs cross-terminal dependency)

---

### 4. Mobile testing stratégia

**Döntés:** E2E terminál Playwright mobile viewport tesztekkel

**Indok:**
- Existing E2E infrastruktúra (Playwright)
- `@playwright/test` built-in device emulation
- Külön mobile epic overkill (touchscreen gestures Phase 4-ben)

---

### 5. Timeline döntés: Doorstar Soft Launch

**Döntés:** Q3 2026 (szeptember 30), nem Q2

**Indok:**
- 12 sprint = 24 hét = augusztus vége (túl szoros)
- Q2 = június 30 (már nem realisztikus)
- Q3 végére bufferrel reális (8-12 sprint + 4 hét buffer)
- Doorstar-ral egyeztetés szükséges (Conductor-on keresztül)

---

### 6. Parametrikus szabályok backend

**Döntés:** 5 sablon = konfigurációs adat (JSONB), nem hardcode logic

**Indok:**
- Joinery modul C# szabálymotor feltételezett (design_rules.cs)
- `product_templates` tábla JSONB rules (dimension_rules, pricing_rules)
- Walking Skeleton: 5 template (standard_door, double_door, sliding_door, window_door, custom)
- Later bővítés egyszerű (új template row, nem kód módosítás)

---

## Phase 1 Inbox Messages Created

### Joinery Terminal (MSG-JOINERY-058)

**Feladat:** Backend konfigurátor endpoints + database schema

**API endpoints:**
```csharp
POST /joinery/api/products/configure
POST /joinery/api/work-orders
GET /joinery/api/work-orders/{id}/sheet.pdf
```

**Database schema:**
- `joinery_configurations` (config cache)
- `product_templates` (5 sablon: standard_door, double_door, sliding_door, window_door, custom)

**DoD:** API működik, 5 sablon seed-elve, unit + integration tesztek

**Estimated effort:** 6-8 óra

---

### FE Terminal (MSG-FE-087)

**Feladat:** Konfigurátor UI komponensek (React 18 + Zustand + TanStack Query)

**Komponensek:**
```tsx
ProductConfiguratorWizard.tsx   // 4-step wizard
BOMPreviewCard.tsx              // Anyaglista előnézet
WorkOrderSummary.tsx            // Munkalap összefoglaló + PDF
```

**State management:** Zustand store (configurator state, multi-step form)

**DoD:** Komponensek renderelnek, API integration működik, E2E tesztek pass

**Estimated effort:** 8-10 óra

---

### Orch Terminal (MSG-ORCH-007)

**Feladat:** Joinery API routing verification/extension

**Routes:**
```typescript
POST /api/products/configure → http://localhost:5002/joinery/api/products/configure
POST /api/work-orders → http://localhost:5002/joinery/api/work-orders
GET /api/work-orders/:id/sheet.pdf → http://localhost:5002/joinery/api/work-orders/:id/sheet.pdf
```

**DoD:** Proxy működik (200 vagy 502 OK), curl test sikeres, debug logs visible

**Estimated effort:** 30-60 perc (quick verification)

---

## Phase 2-4 Sequencing

**Phase 2:** SSE shop floor (parallel with Phase 1 completion) — Orch + FE

**Phase 3:** Offline PWA — Kernel + FE + E2E

**Phase 4:** Mobile UX — FE + E2E

Conductor feladat: Phase 1 DONE után Phase 2 inbox készítés.

---

## Consensus Archive

**Original:** `docs/planning/queue/2026-06-21_0055_consensus.md`

**Archived to:** `docs/planning/archive/2026-06-21_0055_consensus_processed.md`

Timestamp: 2026-06-21

---

## Nightwatch Monitoring

Az alábbi terminálok inbox UNREAD:
- Joinery: MSG-JOINERY-058 (model: sonnet)
- FE: MSG-FE-087 (model: sonnet)
- Orch: MSG-ORCH-007 (model: haiku)

Nightwatch (*/2 min) automatikusan indítja őket.

---

## Következő lépések (Conductor)

1. **Phase 1 monitoring:** 3 terminál DONE várható (összesen ~15-19 óra work)
2. **Doorstar timeline egyeztetés:** Q3 2026 (szeptember 30) megerősítés
3. **Phase 2 planning:** SSE shop floor inbox készítés (Phase 1 után)
4. **Parallel track:** Librarian-nal Phase 1 knowledge docs (Configurator patterns)

---

**Státusz:** Planning consensus feldolgozva, Phase 1 elindítva.

**Timeline:** Phase 1 várható completion: 2026-06-23 (2-3 nap, párhuzamos terminálok)

---

Timestamp: 2026-06-21
