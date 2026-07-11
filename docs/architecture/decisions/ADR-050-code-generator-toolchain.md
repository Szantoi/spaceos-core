# ADR-050: Code Generator Toolchain Bevezetése

**Státusz:** ACCEPTED
**Dátum:** 2026-06-30
**Döntéshozó:** Root
**Kutatás:** Explorer (MSG-EXPLORER-006)

---

## Kontextus

A SpaceOS Walking Skeleton megközelítést használ, ahol az E2E pipeline előbb épül, a matematika utóbb mélyül. A manuális boilerplate kód írása:
- Időigényes (3.75 óra/modul)
- Hibalehetőségeket rejt (API contract eltérések)
- Nem skálázódik több modul esetén

Az Explorer terminál 20 kódgenerátor eszközt kutatott és értékelt.

---

## Döntés

### Elfogadott Toolchain

| Fázis | Eszköz | Licensz | Prioritás |
|-------|--------|---------|-----------|
| **Phase 1** | Orval (Frontend) + NSwag (Backend) | MIT | **MOST** |
| Phase 4 | Hygen (Frontend components) | MIT | Később |
| Phase 2-3 | dotnet new + Roslyn | MIT | Parkolópályán |

### Phase 1 Részletek

**Orval (Frontend):**
- OpenAPI spec → React Query hooks auto-generálás
- `src/api/generated/kernelApi.ts`
- Breaking change = TypeScript compile error

**NSwag (Backend/Orchestrator):**
- OpenAPI spec → TypeScript client
- `src/clients/KernelClient.ts`
- Orchestrator ↔ Kernel sync automatikus

---

## Indoklás

### Miért Phase 1 most?

| Szempont | Érték |
|----------|-------|
| Setup idő | 2-4 óra |
| ROI | 97% időmegtakarítás API változásnál |
| Risk | Alacsony (battle-tested eszközök) |
| Karbantartás | Minimális (CI/CD integráció) |

### Miért nem Phase 2-3 most?

| Szempont | Érték |
|----------|-------|
| Roslyn learning curve | 1-2 hét |
| Modulok/év | ~3-5 (nem 10+) |
| Megtakarítás | ~19 óra/év |
| Befektetés | ~40 óra setup |

**Következtetés:** ROI negatív a jelenlegi modul számmal.

---

## Következmények

### Pozitív
- API contract eltérések automatikusan detektálva
- Frontend ↔ Backend sync hibák megszűnnek
- CI/CD-ben futtatható validáció

### Negatív
- Kernel OpenAPI XML docs kiegészítése szükséges (blocker)
- Új dependency a build pipeline-ban

### Semleges
- Phase 2-3 később újraértékelhető (>10 modul/év esetén)

---

## Implementáció

### Phase 1 Task (Backend Terminal)

1. **Kernel:** OpenAPI XML docs kiegészítése
2. **Portal:** Orval setup (`npm install orval`)
3. **Orchestrator:** NSwag setup
4. **CI/CD:** GitHub Actions integráció

### Elfogadási Kritériumok

- [x] Kernel OpenAPI spec teljes (minden endpoint dokumentált)
- [x] Portal: `npm run generate:api` működik
- [x] Orchestrator: NSwag client generálás működik
- [x] CI: API változás → auto-regenerate → PR (DRAFT workflow)

---

## Kapcsolódó Dokumentáció

- `docs/knowledge/patterns/CODE_GENERATOR_CATALOGUE.md` — teljes kutatás (20 eszköz)
- `terminals/explorer/outbox/2026-06-30_037_code-generator-research-done.md` — DONE összefoglaló

---

## Full Roadmap (4 Fázis → MCP Integration)

### Phase 1: Orval + NSwag Alapok (MOST)
**Státusz:** ✅ COMPLETE (2026-06-30, Backend Terminal MSG-BACKEND-105)
- Kernel OpenAPI XML docs
- Orval setup (Portal)
- NSwag setup (Orchestrator)
- CI/CD integráció

### Phase 2: SpaceOS Wrapper Scripts
**Státusz:** PLANNED
```bash
/opt/spaceos/scripts/codegen/
  ├── generate-api-client.sh    # Orval + NSwag unified wrapper
  ├── generate-component.sh     # Hygen wrapper (SpaceOS patterns)
  └── generate-module.sh        # dotnet new wrapper
```

### Phase 3: SpaceOS CLI
**Státusz:** PLANNED
```bash
# Unified CLI minden generátorhoz
spaceos generate api-client          # Orval + NSwag
spaceos generate component FlowEpic  # Hygen + SpaceOS templates
spaceos generate module Pricing      # dotnet new + DDD structure
```

### Phase 4: MCP Tool Integration
**Státusz:** PLANNED

**Új MCP toolok a Knowledge Service-ben:**

```typescript
// spaceos-nexus/knowledge-service/src/codegen/
mcp__spaceos__generate_api_client({
  source: "kernel",      // kernel | orchestrator
  target: "portal"       // portal | orchestrator
})

mcp__spaceos__generate_component({
  name: "FlowEpicCard",
  category: "feature",   // feature | ui | layout
  withTest: true,
  withStory: true
})

mcp__spaceos__generate_module({
  name: "Pricing",
  aggregate: "Quote",
  states: ["Draft", "Submitted", "Approved", "Rejected"]
})
```

**Használat terminálokból:**
```
Frontend Terminal: "Generálj egy FlowEpicCard komponenst"
  → mcp__spaceos__generate_component({ name: "FlowEpicCard", ... })
  → Fájlok létrejönnek + TypeScript compile check
  → Terminál folytatja a logika implementálását
```

---

## SpaceOS Custom Testreszabások

| Terület | Custom Megoldás |
|---------|-----------------|
| **Auth** | Automatikus tenant header minden request-ben |
| **Error** | SpaceOS error kódok → magyar hibaüzenetek |
| **Audit** | Minden API hívás logolva (ki, mit, mikor) |
| **RLS** | Row-level security context injection |
| **Retry** | Exponential backoff + circuit breaker |
| **Types** | SpaceOS domain típusok (FlowEpic, Tenant, etc.) |

---

## Felülvizsgálat

**Phase 1:** 2026-07 (Backend Terminal befejezi)
**Phase 2-3:** 2026 Q3 (wrapper scripts + CLI)
**Phase 4:** 2026 Q3-Q4 (MCP tool integration)
**Roslyn review:** 2026 Q4 (ha >10 modul/év)
