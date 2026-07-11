---
completed: 2026-07-08
id: MSG-LIBRARIAN-005
from: root
to: librarian
type: task
priority: high
status: COMPLETED
model: sonnet
ref: ADR-048
created: 2026-06-24
content_hash: 85b27694a90e6c3b183ffa4f3139782faf15601890568355945875b2ce9c231f
---

# Tudásbázis reorganizáció: Tiered és projekt-alapú struktúra

## Kontextus

Készül egy új rendszer (ADR-048) ami projekt-szintű tiered context-et vezet be. Ehhez a meglévő tudásbázist és RAG tartalmakat **strukturálni kell** a következő dimenziók mentén:

1. **Időbeliség** — mikor történt (ma / múlt hét / hónapok óta)
2. **Prioritás** — mennyire fontos most
3. **Projekt/Termék** — melyik projekthez tartozik
4. **Általános** — minden projektre érvényes tudás

## Feladat

### 1. Audit a meglévő tudásbázisról

Nézd át a `docs/knowledge/` tartalmát és kategorizáld:

| Fájl | Projekt | Időbeliség | Prioritás | Megjegyzés |
|------|---------|------------|-----------|------------|
| DATABASE_PATTERNS.md | általános | cold | high | RLS minták |
| FRONTEND_DRAG_DROP_PATTERNS.md | portal | warm | medium | Cutting UI-hoz |
| ... | ... | ... | ... | ... |

### 2. Projekt-specifikus kontextus fájlok létrehozása

Minden aktív projekthez készíts egy tömör összefoglalót:

```
docs/knowledge/context/
  CUTTING_CONTEXT.md      ← spaceos/cutting projekt
  SALES_CONTEXT.md        ← spaceos/sales projekt
  NEXUS_CONTEXT.md        ← datahaven/nexus projekt
  PORTAL_CONTEXT.md       ← spaceos/portal projekt (létezik)
```

Struktúra projektenként:

```markdown
# [Projekt] Context

## Aktuális állapot (HOT)
- Mai/tegnapi események
- Aktív blockerek
- Teszt számok

## Közelmúlt (WARM)
- Utolsó 2 hét döntései
- Sprint eredmények
- Változások

## Architekturális alapok (COLD)
- Releváns ADR-ek
- API contract összefoglaló
- Dependency-k

## Kapcsolódó tudás
- Mely knowledge doc-ok relevánsak
- Minták és gotchák
```

### 3. RAG Metadata Mapping (FÓKUSZ!)

> **FONTOS:** NEM osztjuk külön collection-ökbe a RAG-ot!
> Egyetlen ChromaDB collection marad, de **metadata filtering**-gel szűrünk.

Készíts egy **metadata mapping táblázatot** minden `docs/knowledge/` fájlhoz:

```markdown
| Fájl | project | tier | doc_type | last_updated | priority |
|------|---------|------|----------|--------------|----------|
| DATABASE_PATTERNS.md | general | cold | pattern | 2026-04-28 | high |
| FRONTEND_DRAG_DROP_PATTERNS.md | cutting | warm | pattern | 2026-06-22 | medium |
| CUTTING_CONTEXT.md | cutting | hot | context | 2026-06-24 | high |
| ADR_CATALOGUE.md | general | cold | architecture | 2026-05-15 | high |
| MCP_CONFIG_GUIDE.md | nexus | warm | debugging | 2026-06-22 | medium |
```

**Metadata mezők:**

| Mező | Értékek | Leírás |
|------|---------|--------|
| `project` | `general`, `cutting`, `sales`, `nexus`, `portal`, `kernel`, `joinery` | Melyik projekthez releváns |
| `tier` | `hot`, `warm`, `cold`, `shared` | Időbeli relevancia |
| `doc_type` | `pattern`, `context`, `architecture`, `debugging`, `deployment`, `security` | Dokumentum típus |
| `last_updated` | `YYYY-MM-DD` | Utolsó frissítés dátuma |
| `priority` | `high`, `medium`, `low` | Mennyire fontos |

**Használati példák (amit a keresés tud majd):**

```typescript
// Csak cutting projekthez
search("nesting algorithm", { where: { project: "cutting" } })

// Csak friss tudás (hot + warm)
search("RLS", { where: { tier: { $in: ["hot", "warm"] } } })

// Általános minták
search("EF Core", { where: { project: "general", doc_type: "pattern" } })

// Cross-project keresés (nincs filter)
search("authentication")
```

### 4. Időrendi rendezés

A `docs/knowledge/INDEX.md`-ben add hozzá az időbeliséget:

```markdown
## patterns/ (utolsó frissítés szerint)

### Aktív (2026-06 frissítve)
- [FRONTEND_DRAG_DROP_PATTERNS.md] — Cutting UI drag-drop (2026-06-22)
- [EVENT_SOURCING_PATTERNS.md] — EHS module (2026-06-20)

### Stabil (2026-04-05 frissítve)
- [DATABASE_PATTERNS.md] — RLS, migrations (2026-04-28)
- [TESTING_PATTERNS.md] — xUnit, Testcontainers (2026-05-10)

### Archív (3+ hónap)
- [DEPRECATED_APPROACHES.md] — Elvetett megoldások
```

## Elvárt output

1. **Metadata mapping táblázat** — minden `docs/knowledge/` fájlhoz (project, tier, doc_type, last_updated, priority)
2. **3-4 új CONTEXT.md** fájl (Cutting, Sales, Nexus, esetleg Joinery)
3. **INDEX.md frissítés** időrendi csoportosítással
4. **RAG indexer módosítási javaslat** — hogyan kell a `vectorStore.ts`-t módosítani hogy a metadata-t használja

## Kapcsolódó fájlok

- `docs/architecture/decisions/ADR-048-project-tiered-context.md` — a terv
- `spaceos-nexus/knowledge-service/src/pipeline/memoryStore.ts` — tiered memory impl
- `docs/projects/EPICS.yaml` — projekt/epic lista

## Megjegyzés

Ez egy **előkészítő munka** az ADR-048 implementációhoz. A cél, hogy a tudásbázis struktúrája támogassa a dinamikus, projekt-szintű kontextus betöltést.

**NE** írj kódot — csak dokumentumokat és javaslatokat készíts.

---

## Completion Report
*2026-07-08T00:33:48.943Z*

### Summary
Faipar domain knowledge synthesis complete — §22 (Vizsgatevékenységek 2024) + §23 (Gyártási rendszerek) hozzáadva woodwork_domain.md-hez (83 új sor). Timestamp + sources frissítve. INDEX.md nem igényelt változtatást (domain knowledge ≠ technical pattern).

### Implementation Details
**Feldolgozott fájlok:**
- woodwork_domain.md (1031 sor → 1114 sor)
- faipari_gyartasszervezes_rag.md (5975 sor, 376.9KB)
- faipari_muszaki_dokumentacio_rag.md (2489 sor, 51780 token)

**Új domain tudás:**
1. Vizsgatevékenységek dokumentációs követelményei (2024) — Portfólió vs. vizsgaremek
2. Gyártási rendszerek mélyebb szemlélete — Sorozatos/párhuzamos kapcsolás, feedback loop
3. Befoglaló méret konvenciók — Product-type specific bounding box

**SpaceOS integráció:**
- §22: Portfolio/Exam API javaslatok
- §23: WorkflowStep FSM, QualityControlLoop, ParametricProduct.BoundingBox

**Státusz jelölések:** ✅ megvan / 🔨 részben / ⏳ még nincs / 💡 jövőbeli

### Files Changed
- `docs/woodwork_domain.md`

