---
name: spaceos-arch-planner
description: >
  SpaceOS-specific architecture planning skill. Wraps ddd-arch-planner with full
  SpaceOS context pre-loaded: Kernel frozen decisions, layer boundaries, approved packages,
  existing security debt register, and brand/tenant model.

  ACTIVATE when the user asks for a SpaceOS phase or sprint implementation plan at the
  END of a design session: "SpaceOS Phase tervdokumentum", "következő fázis terve",
  "Claude Code-nak szánt SpaceOS terv", "implementációs csomag", "készítsd el a tervdokumentumot",
  "Phase 3B tervezése", "Phase 3C tervezése", "Modules.Joinery terv",
  "multi-brand portal terv", "escrow architecture", "SpaceOS security debt zárás".

  Do NOT activate for ad-hoc SpaceOS questions — only for end-of-session plan generation.
---

# SpaceOS Architecture Planner

SpaceOS-specifikus wrapper a `ddd-arch-planner` pipeline fölé.
**Minden általános szabályért olvasd el: `../ddd-arch-planner/SKILL.md`**

Ez a skill a SpaceOS-specifikus kontextust adja hozzá automatikusan.

---

## 1. Pre-loaded SpaceOS context

### Stack (frozen)

| Réteg | Stack | Státusz |
|-------|-------|---------|
| L2 Kernel | .NET 8, EF Core 8, PostgreSQL 16, MediatR, FluentValidation, Ardalis.Result+Specification | FROZEN |
| L3 Orchestrator | Node.js 22, TypeScript 5, Express 4, PM2 | Deployed |
| L4 Portal | React 18, TypeScript 5, Vite 5, Tailwind 3, TanStack Query, Zustand | Deployed |
| L5 Infra | Nginx, Let's Encrypt, UFW, systemd, GitHub Actions CI | Deployed |

### Kernel frozen decisions

- Kernel is frozen — no new domain concepts added directly
- All extension logic lives in the Modules layer (Modules.Abstractions, Modules.Joinery, etc.)
- FlowEpic is the atomic unit of work
- Orchestrator is stateless — no DB; each brand gets its own BFF instance
- Data axiom: Data → Rules → Geometry — frontend never computes measurements
- LLM is fully decoupled from Kernel (Island Architecture)
- Layer dependency: Domain ← Application ← Infrastructure (Domain: zero external deps)

### Approved packages (Kernel only)

MediatR · FluentValidation · Ardalis.Result · Ardalis.Specification · EF Core 8 · xUnit v3 · Moq

Anything outside this list must be discussed before proposing.

### Current test baseline

Always load the latest `Codebase_Status_YYYYMMDD.md` for the actual count.
Reference baseline: ~1049 total (Kernel ~686 · Orchestrator ~76 · Portal ~224 · E2E ~63)

### Open security debt (Phase 3B scope)

| ID | Tétel | Priority |
|----|-------|----------|
| P1-3 | AggregateSnapshot entitás + tábla | HIGH |
| P1-4 | Outbox Pattern (SnapshotService) | HIGH |
| P1-8 | ProofHash + WORM storage | HIGH |
| P2-1 | Chain Integrity Verifier API | MEDIUM |
| P2-2 | Snapshot Query API | MEDIUM |
| P2-3 | GDPR pseudonymizáció + PII szeparáció | MEDIUM |
| P2-4 | Audit alerting + anomaly detection | MEDIUM |
| P2-5 | Genesis hash deployment-time generálás | MEDIUM |
| P2-6 | Kriptográfiai algoritmus migration plan | MEDIUM |

### Domain inheritance (Modules layer)

```
Modules.Joinery (base Driver)
├── OpeningElement → Cabinet / Door / Window branches
└── Doorstar Kft. = Door manufacturer brand skin (first reference customer)
```

### Handshake / actor model

- Any TenantType can initiate with any other (including self-tasks)
- HandshakeVisibilityScope enforces data sovereignty at DB level
- SyncSignals: only EpicId + FSM state + HMAC hash — never content

---

## 2. Phase map

| Phase | Tartalom | Státusz |
|-------|----------|---------|
| Sprint D Phase 1 | Infra Hardening | CLOSED_DONE |
| Sprint D Phase 1.5 | App Security (JWT ES256, RLS, RefreshToken) | READY v4 |
| Sprint D Phase 2 | Tool Registry + P1 Security Debt | READY v4 |
| Phase 3A | Spatial BIM Core + Modules.Joinery | READY v3 |
| Phase 3B | AggregateSnapshot + Outbox + ProofHash + WORM | DESIGN |
| Phase 3C | Multi-brand Portal (Turborepo) | DESIGN |
| IdP Integration | Auth0 / Keycloak | DESIGN |
| Horizon 2 | Escrow GA — Sink upgrade + RFC 3161 TSA | AFTER 3B |

---

## 3. Document naming convention

```
SpaceOS_{PhaseName}_Architecture_v{N}.md
```

Examples:
- `SpaceOS_Phase3B_Architecture_v1.md`
- `SpaceOS_Phase3C_MultiPlatform_v1.md`
- `SpaceOS_IdP_Integration_v1.md`

---

## 4. SpaceOS-specific DoD additions

Append to every DoD Összesített section:

```markdown
- [ ] Meglévő {N} teszt zöld (Kernel + Orchestrator + Portal + E2E)
- [ ] dotnet list package --vulnerable → 0 high/critical
- [ ] grep -r "BuildServiceProvider" --include="*.cs" → 0 találat
- [ ] EXPLAIN ANALYZE: Index Scan minden új query endpointon
- [ ] Golden Rules 1–12 teljesül (compile + lint gate)
- [ ] Migration suppressTransaction: true ahol CONCURRENTLY szükséges
```

---

## 5. Migration numbering

Last used: 0019 (Phase 3A)
Phase 3B starts from: 0020

---

## 6. Workflow

1. User requests a phase design (e.g. "Phase 3B tervezése")
2. Load: latest Codebase_Status, Security_Task_Register, previous phase docs
3. Run ddd-arch-planner pipeline with this context pre-loaded
4. When user asks for "implementációs terv" or "Claude Code csomag":
   - Assemble final vN document
   - Append Section 9 (Claude Code implementációs csomag)
   - Output as .md file using naming convention above

For all pipeline mechanics, review formats, finding tables, DoD structure, and status banners:
→ follow `references/ddd-arch-planner-pipeline.md`

---

## 7. Sub-skill betöltési utasítások

A review pipeline minden fázisában töltsd be a megfelelő sub-skill fájlt.
**Sorrendben olvasd — ne egyszerre.**

| Review fázis | Betöltendő fájl | Mikor |
|---|---|---|
| v2 — DB review | `references/sub-database-designer.md` | v1 draft kész, DB schema review előtt |
| v2 — Schema review | `references/sub-database-schema-designer.md` | DB designer után, ERD + RLS generáláshoz |
| v3 — Security review | `references/sub-senior-security.md` | v2 lezárva, security pass előtt |
| v4 — Backend review | `references/sub-senior-backend.md` | v3 lezárva, ha még van CRITICAL/HIGH finding |

**Betöltési instrukció (minden fázis elején):**
```
Olvasd el a [fájlnév] tartalmát.
Alkalmazd az ott leírt analitikai keretrendszert a jelenlegi tervdokumentumra.
Minden findinget a SpaceOS finding formátumban dokumentálj: [ID] · [súly] · [terület] · [probléma] · [javítás]
```
