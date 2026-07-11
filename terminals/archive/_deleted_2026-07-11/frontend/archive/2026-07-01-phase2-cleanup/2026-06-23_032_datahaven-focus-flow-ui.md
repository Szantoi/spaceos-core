---
id: MSG-FRONTEND-032
from: root
to: frontend
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-23
completed: 2026-06-23
content_hash: d83e6a6ca9fd669bc7e4443598ad0f402d3a7e6498cbd83e598b2771929d05e7
---

# Datahaven UI Bővítés — Focus Panel + Flow Editor

## Összefoglaló

Két új UI komponens hozzáadása a Datahaven Dashboard-hoz:
1. **Focus Panel** — Domain fókusz megjelenítése és szerkesztése
2. **Flow Editor** — Epic dependency gráf vizualizálása Mermaid alapon

## 1. Focus Panel

### Cél
A `docs/planning/domain-focus.md` tartalmának megjelenítése és interaktív szerkesztése.

### UI Elhelyezés
**Planning oldalon** (`/planning.html`) új tab: "Focus"

### Komponensek

```
┌─────────────────────────────────────────────────┐
│  Focus Panel                            [Save]  │
├─────────────────────────────────────────────────┤
│  Current Domain Focus                           │
│  ┌─────────────────────────────────────────────┐│
│  │  [manufacturing ▼]                          ││
│  └─────────────────────────────────────────────┘│
│                                                 │
│  Available Domains:                             │
│  • manufacturing - Gyártás, műhely, gépek       │
│  • sales - CRM, ajánlatok                       │
│  • logistics - Szállítás, raktár                │
│  • finance - Számlák, kontrolling               │
│  • quality - NCR, audit                         │
│  • hr - Jelenléti, műszak                       │
│  • all - Teljes rendszer                        │
│                                                 │
│  Szempontok (readonly markdown render):         │
│  ┌─────────────────────────────────────────────┐│
│  │ - Felhasználói érték: ...                   ││
│  │ - Backend kapcsolhatóság: ...               ││
│  │ - Iparági minták: ...                       ││
│  │ - Mobil első: ...                           ││
│  │ - Offline tűrés: ...                        ││
│  └─────────────────────────────────────────────┘│
└─────────────────────────────────────────────────┘
```

### API Endpoints (ÚJ — implementálandó)

**Backend route: `/opt/spaceos/datahaven-web/src/routes/planningRoutes.js`**

```javascript
// GET /api/planning/focus — domain-focus.md olvasása
router.get('/focus', async (req, res) => {
  const focusPath = path.join(SPACEOS_ROOT, 'docs/planning/domain-focus.md');
  const content = await fs.readFile(focusPath, 'utf-8');
  // Parse: current domain, szempontok
  res.json({ domain: 'manufacturing', criteria: [...], raw: content });
});

// PUT /api/planning/focus — domain-focus.md módosítása
router.put('/focus', async (req, res) => {
  const { domain } = req.body;
  // Módosítsd a `domain: <value>` sort a fájlban
  res.json({ success: true, domain });
});
```

### Frontend változások

1. **planning.html** — Új tab hozzáadása:
```html
<button class="stage-tab" data-stage="focus">Focus</button>
```

2. **planning.html** — Új panel:
```html
<div class="stage-panel" id="panel-focus">
  <div class="panel-header">
    <h2>Domain Focus</h2>
    <button class="btn-save" onclick="saveFocus()">Save</button>
  </div>
  <div class="focus-content">
    <select id="focus-domain"></select>
    <div id="focus-criteria"></div>
  </div>
</div>
```

3. **planning.js** — Új funkciók:
```javascript
async function loadFocus() { /* API hívás */ }
async function saveFocus() { /* PUT hívás */ }
```

---

## 2. Flow Editor (Epic Dependency Graph)

### Cél
Az `EPICS.yaml` vizualizálása interaktív Mermaid diagramként.

### UI Elhelyezés
**Planning oldalon** (`/planning.html`) — a meglévő "Workflow" tab átalakítása

### Komponensek

```
┌─────────────────────────────────────────────────────────────┐
│  Epic Flow                    [Refresh] [Export Mermaid]    │
├─────────────────────────────────────────────────────────────┤
│  Stats: 9 epics | 6 done | 2 active | 1 pending             │
│  Critical Path: KERNEL → JOINERY → ORCH → PORTAL            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                                                         ││
│  │         [KERNEL]──────┬──────[JOINERY]                  ││
│  │            │          │          │                      ││
│  │            ├──────[CUTTING]      │                      ││
│  │            │          │          │                      ││
│  │            └──────[INVENTORY]    │                      ││
│  │                       │          │                      ││
│  │                       └────[ORCH]│                      ││
│  │                            │     │                      ││
│  │                  [IDENTITY]┘     │                      ││
│  │                       │          │                      ││
│  │                       └────[PORTAL]                     ││
│  │                             │                           ││
│  │                       [DOORSTAR]                        ││
│  │                                                         ││
│  │  Mermaid diagram (rendered)                             ││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  Epic Details (click to expand):                            │
│  ┌───────────────────────────────────────────────────┐      │
│  │ EPIC-CUTTING-Q3                                   │      │
│  │ Status: [active ▼]  Target: 2026-09-30            │      │
│  │ Depends on: KERNEL  Parallel: JOINERY, INVENTORY  │      │
│  │ Description: Lapszabász modul...                  │      │
│  └───────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

### API Endpoints (MÁR LÉTEZIK)

A Graph API már működik a knowledge-service-ben:

```bash
# Epic graph lekérdezés
GET http://localhost:3456/api/graph/epics

# Mermaid diagram
GET http://localhost:3456/api/graph/mermaid/epic/EPICS

# Critical path
GET http://localhost:3456/api/graph/critical-path/epic/EPICS
```

### Frontend változások

1. **planning.html** — Workflow panel átalakítása:
```html
<div class="stage-panel" id="panel-workflow">
  <div class="panel-header">
    <h2>Epic Flow</h2>
    <div class="panel-actions">
      <button onclick="loadEpicFlow()">Refresh</button>
      <button onclick="exportMermaid()">Export</button>
    </div>
  </div>
  <div class="flow-stats" id="flow-stats"></div>
  <div class="mermaid-container" id="mermaid-diagram"></div>
  <div class="epic-details" id="epic-details"></div>
</div>
```

2. **planning.js** — Új funkciók:
```javascript
async function loadEpicFlow() {
  // 1. GET /api/graph/epics → graph data
  // 2. GET /api/graph/mermaid/epic/EPICS → mermaid string
  // 3. Render mermaid with mermaid.js library
  // 4. Calculate stats from graph data
}

function renderEpicDetails(epic) {
  // Epic kattintás kezelése
}

async function exportMermaid() {
  // Copy to clipboard
}
```

3. **Mermaid.js integráció:**
```html
<script src="https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js"></script>
<script>mermaid.initialize({ startOnLoad: false, theme: 'dark' });</script>
```

### CSS bővítés (planning.css)

```css
/* Flow container */
.mermaid-container {
  background: var(--bg-secondary);
  border-radius: 8px;
  padding: 20px;
  overflow: auto;
  min-height: 300px;
}

/* Epic status colors (Mermaid node styles) */
.node.done rect { fill: var(--success) !important; }
.node.active rect { fill: var(--warning) !important; }
.node.pending rect { fill: var(--text-muted) !important; }
.node.blocked rect { fill: var(--danger) !important; }

/* Flow stats */
.flow-stats {
  display: flex;
  gap: 16px;
  padding: 12px;
  background: var(--bg-tertiary);
  border-radius: 4px;
  margin-bottom: 16px;
}
```

---

## Implementációs sorrend

1. **Focus Panel backend** — planningRoutes.js bővítése (15 perc)
2. **Focus Panel frontend** — HTML + JS (20 perc)
3. **Flow Editor frontend** — Mermaid integráció (30 perc)
4. **CSS finomhangolás** — Stílus illeszkedés (10 perc)

## Referencia fájlok

| Fájl | Leírás |
|------|--------|
| `/opt/spaceos/datahaven-web/public/planning.html` | Planning oldal HTML |
| `/opt/spaceos/datahaven-web/public/js/planning.js` | Planning oldal JS |
| `/opt/spaceos/datahaven-web/public/css/planning.css` | Planning stílusok |
| `/opt/spaceos/datahaven-web/src/routes/planningRoutes.js` | Backend route-ok |
| `/opt/spaceos/docs/planning/domain-focus.md` | Focus fájl |
| `/opt/spaceos/docs/projects/EPICS.yaml` | Epic dependency config |
| `/opt/spaceos/spaceos-nexus/knowledge-service/src/api/graphRoutes.ts` | Graph API |

## DONE kritérium

- [ ] Focus Panel működik: domain olvasás és módosítás
- [ ] Flow Editor működik: Mermaid diagram renderelés
- [ ] Epic kattintás → részletek megjelennek
- [ ] Export Mermaid → clipboard
- [ ] CSS konzisztens a meglévő Datahaven stílussal
