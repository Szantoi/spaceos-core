---
id: MSG-ARCHITECT-012
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-010
created: 2026-06-23
content_hash: 1174c0c94328f272dc99b8959ccb2f99b6356e47e541b8ea6acc8664e10df514
---

# ✅ DONE — Datahaven UI Focus Area + Flow Editor Architecture

## Summary

Elkészítettem a **Datahaven UI bővítési terv v1** architektúra dokumentumot. A tervezés magában foglalja:

1. **Focus Area Panel** — `domain-focus.md` megjelenítése és szerkesztése (7 domain + szempont lista)
2. **Flow/Workflow Editor** — `EPICS.yaml` interaktív Mermaid gráf vizualizáció és szerkesztés

## Artifact

**Lokáció:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

**Terjedelem:** 12 fejezet, 900+ sor

### Tartalom:

1. **Executive Summary** — Két komponens áttekintése, elhelyezési stratégia
2. **Focus Area Panel Design** — UI mockup, placement options (3 alternatíva), recommendation: Planning page top panel
3. **Flow/Workflow Editor Design** — Mermaid.js alapú interaktív graph, epic details panel, drag-drop consideration
4. **Component Placement Strategy** — Planning page layout hierarchia
5. **Data Flow Diagrams** — 4 diagram (Focus read/write, Workflow load/edit)
6. **API Requirements** — 2 új endpoint specifikáció:
   - `GET/PUT /api/planning/domain-focus` (új planningRoutes.ts)
   - `PUT /api/graph/epics/:id` (graphRoutes.ts kiterjesztés)
7. **CSS/Design Guidelines** — Dark theme színpaletta, responsive breakpoints, status color mapping
8. **Implementation Roadmap** — 3 fázis, 18-26 nap becsült idő
9. **Security & Performance** — XSS védelem, cycle detection, cache stratégia
10. **Open Questions & Trade-offs** — Placement decision, real-time sync options, Git auto-commit vs direct write
11. **Alternative Approaches** — Mermaid.js vs React Flow vs D3.js comparison
12. **Appendix** — API endpoint summary, Mermaid example output

## Kulcs döntések

### Focus Area Panel

**Elhelyezés:** Planning oldal, pipeline overview fölött (Option B)

**Indoklás:**
- A domain fókusz **közvetlenül befolyásolja a planning pipeline-t** (Haiku scanner ezt a fájlt olvassa)
- Kontextuális illeszkedés: Planning oldal = planning domain config
- Nem terheli túl a Dashboard oldalt

**Interakció:**
- Domain dropdown (7 opció)
- Criteria display (markdown → HTML renderelés)
- Edit mode (textarea)
- Save → `PUT /api/planning/domain-focus`

### Flow/Workflow Editor

**Elhelyezés:** Planning oldal, `Workflow` tab (jelenleg üres placeholder)

**Indoklás:**
- Külön tab → nincs vizuális zsúfoltság a fő planning view-n
- Power user funkció → szándékos navigáció szükséges
- Desktop only (tablet/mobile: "Desktop screen required" üzenet)

**Technológia:** Mermaid.js + panzoom.js

**Interakció:**
- Epic node click → details panel (név, státusz, dependencies)
- Status dropdown → `PUT /api/graph/epics/:id` (validációval)
- Dependency add/remove → modal form (Phase 1), drag-drop (Phase 2)
- Export Mermaid diagram button

## Új API endpointok

### 1. Planning Focus API (planningRoutes.ts — ÚJ)

```typescript
GET /api/planning/domain-focus
→ { domain: "manufacturing", criteria: "...", updated_at: "..." }

PUT /api/planning/domain-focus
→ { domain: "sales", criteria: "..." }
```

**Validáció:**
- Domain ∈ [manufacturing, sales, logistics, finance, quality, hr, all]
- Criteria markdown sanitization (XSS védelem)
- Rate limiting: 10 write/min

### 2. Epic Update API (graphRoutes.ts — KITERJESZTÉS)

```typescript
PUT /api/graph/epics/:id
→ { status: "done", depends_on: [...], parallel_with: [...] }
```

**Validáció:**
- Status transition rules (pending→active→done, blocked states)
- Cycle detection (detectCycles() használata)
- Done status: verify all dependencies are done

## Security considerations

✅ **Focus Area Panel:**
- Bearer token authentication
- Domain whitelist validation
- Markdown sanitization (DOMPurify)
- Atomic file write (temp + rename pattern)

✅ **Flow Editor:**
- Status transition state machine
- Cycle detection before YAML write
- Cache invalidation on update
- YAML injection protection

## Implementation estimate

| Phase | Scope | Days |
|-------|-------|------|
| **Phase 1** | Focus Area Panel (backend + frontend) | 5-7 |
| **Phase 2** | Flow/Workflow Editor (backend + frontend) | 10-14 |
| **Phase 3** | Polish, optimization, mobile responsiveness | 3-5 |
| **Total** | MVP Ready | **18-26 days** |

## Nyitott kérdések (Review szükséges)

### 1. Focus Area Panel elhelyezés

**Ajánlás:** Planning page (Option B)

**Alternatíva:** Dashboard sidebar (Option A) — ha a dashboard a fő munkaterület

**Döntés szükséges:** Root vagy Conductor preferenciája?

### 2. Real-time sync stratégia

**Phase 1 ajánlás:** Polling (5 sec interval)

**Phase 2 upgrade:** Server-Sent Events (SSE) real-time push

**Trade-off:** SSE hatékonyabb, de több backend complexity

### 3. EPICS.yaml írás stratégia

**Phase 1:** Direct file write (simple)

**Phase 2 ajánlás:** Auto-commit on write (Git audit trail)

**Trade-off:** Git commit 200-500ms latency, de teljes history + rollback capability

## Next steps

Ha **APPROVED**:
1. Backend terminal: Implementálja a 2 új API endpoint-ot
2. Frontend terminal: Implementálja a 2 UI komponenst
3. Designer terminal: Review CSS styling (opcionális)

Ha **REVISE** vagy **REJECT**:
- Jelezd a feedback-et, átdolgozom a tervet

## MCP visszajelzés

### Használt eszközök ✅
- Datahaven status API (session start/end regisztráció)
- Read tool (referencia fájlok)
- Graph API típusok és endpoint-ok kutatása

### Hiányzó eszközök 🔧
- Hasznos lenne egy **MCP tool az ADR kereséshez** (jelenleg manuális file read)
- Nincs eszköz a **cross-module dependency vizualizációhoz** (amit most a Flow Editor fog megoldani)
- Jó lenne egy **knowledge search API wrapper** MCP tool-ként (jelenleg közvetlenül API-t hívok)

---

**Artifact:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

**Status:** DRAFT (review pending)

**Awaiting:** Root or Conductor approval decision
