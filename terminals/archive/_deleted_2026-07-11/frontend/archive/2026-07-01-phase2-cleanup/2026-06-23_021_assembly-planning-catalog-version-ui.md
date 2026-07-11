---
id: MSG-FRONTEND-021
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: /opt/spaceos/docs/planning/queue/2026-06-22_2214_consensus.md
created: 2026-06-23
content_hash: abeaf35df67fd55b5810ace69ec14cdc8c9772c773da32c4f38775108377a422
---

# Assembly Planning + Catalog Version Management — Frontend UI

**Konsenzus:** Unified timeline koncepció (Planner-B UX) + pragmatikus cache-first megközelítés (Planner-A). Phase 1 REST-alapú, WebSocket-ready architektúra.

## Prioritás sorrend

1. **Assembly Planning — Unified Timeline** (5-6 nap, TOP 3)
2. **Catalog Version Time-Travel** (4-5 nap, TOP 2)

---

## 1. Assembly Planning — Unified Timeline

### Komponens struktúra

```
src/components/assembly/
├── AssemblyPlanningView/
│   ├── LiveAssemblyTimeline.jsx
│   │   ├── TimelineBubble (plan=outline, actual=solid overlay)
│   │   ├── DeltaBadge (reusable ±indikátor, színkódolt)
│   │   └── FollowModeToggle (auto-scroll mai napra)
│   ├── VarianceSummaryCard.jsx (top banner KPI-k)
│   └── FilterToolbar.jsx (date range, "deviations only")
```

### State management

```javascript
const useLiveAssembly = (assemblyId) => {
  // Phase 1: React Query (polling 30s)
  const { data, isLoading } = useQuery({
    queryKey: ['assembly', assemblyId, 'timeline'],
    queryFn: () => fetch(`/api/assembly/${assemblyId}/timeline?merged=true`).then(r => r.json()),
    refetchInterval: 30000, // 30s polling
  });

  // Phase 2: WebSocket hook (drop-in replacement)
  // TODO: useWebSocket('/ws/assembly/:id/live-stream')

  return { timelineData: data, isLive: true, lastUpdate: new Date() };
};
```

### UX flow

**Single vertical timeline (mobil-first):**
- Plan events = szürke outline buborékok
- Actual events = színes solid buborékok átfedik a tervet
- Delta badge lebeg jobb felül (+15 perc = zöld, -30 perc = piros)
- Virtualized scrolling (`react-window`, 500+ event támogatás)

**Mobil:** Egyetlen oszlop, pinch-to-zoom timeline skálázás, bottom sheet filter

### Komponens példa

```jsx
// TimelineBubble.jsx
export function TimelineBubble({ event }) {
  const isActual = event.type === 'actual';
  const statusColor = {
    ok: 'green',
    delay: 'red',
    ahead: 'blue',
  }[event.status];

  return (
    <div className={`timeline-bubble ${isActual ? 'solid' : 'outline'} border-${statusColor}-500`}>
      <div className="step-name">{event.step}</div>
      <div className="material">{event.material}</div>
      {event.deltaMinutes && (
        <DeltaBadge delta={event.deltaMinutes} />
      )}
    </div>
  );
}

// DeltaBadge.jsx (reusable)
export function DeltaBadge({ delta }) {
  const color = delta > 0 ? 'green' : delta < 0 ? 'red' : 'gray';
  const sign = delta > 0 ? '+' : '';
  return (
    <span className={`badge bg-${color}-100 text-${color}-800`}>
      {sign}{delta} perc
    </span>
  );
}
```

---

## 2. Catalog Version Time-Travel

### Komponens struktúra

```
src/components/catalog/
├── CatalogVersionView/
│   ├── VersionTimeMachine.jsx
│   │   ├── VersionSlider (horizontal timeline, tick marks)
│   │   ├── SnapshotPreview (live preview panel)
│   │   └── DiffHighlight (changed fields auto-highlight)
│   └── VersionMetadata.jsx (author, timestamp, AI summary placeholder)
```

### State management

```javascript
const [selectedVersion, setSelectedVersion] = useState(null);
const [comparisonMode, setComparisonMode] = useState(false); // toggle slider vs diff

const { data: timeline } = useQuery({
  queryKey: ['catalog', catalogId, 'timeline'],
  queryFn: () => fetch(`/api/catalog/${catalogId}/timeline`).then(r => r.json()),
});

const { data: versionData } = useQuery({
  queryKey: ['catalog', catalogId, 'version', selectedVersion],
  queryFn: () => fetch(`/api/catalog/${catalogId}/version/${selectedVersion}`).then(r => r.json()),
  enabled: !!selectedVersion,
});
```

### UX flow

**Horizontal slider (mint video player):**
- Drag → preview instant frissül
- Key change events = színes tickmark a slideren
- "Compare Mode" toggle → két verzió diff view-ja (`react-diff-viewer`)
- "Restore" gomb → új draft produktot hoz létre snapshot alapján

**Mobil:** Slider + bottom preview panel (simplified diff)

### Komponens példa

```jsx
// VersionSlider.jsx
import ReactSlider from 'react-slider';

export function VersionSlider({ snapshots, onVersionChange }) {
  const marks = snapshots.map((s, idx) => ({
    value: idx,
    label: s.version,
    color: s.keyChanges?.length > 0 ? 'orange' : 'gray',
  }));

  return (
    <ReactSlider
      className="horizontal-slider"
      marks={marks}
      min={0}
      max={snapshots.length - 1}
      onChange={(idx) => onVersionChange(snapshots[idx])}
      renderThumb={(props) => <div {...props} className="slider-thumb" />}
      renderMark={(props) => (
        <div {...props} className={`slider-mark bg-${props.key.color}-500`} />
      )}
    />
  );
}

// DiffHighlight.jsx
import ReactDiffViewer from 'react-diff-viewer';

export function DiffHighlight({ oldVersion, newVersion }) {
  return (
    <ReactDiffViewer
      oldValue={JSON.stringify(oldVersion, null, 2)}
      newValue={JSON.stringify(newVersion, null, 2)}
      splitView={false}
      useDarkTheme={false}
    />
  );
}
```

---

## Library-k

```bash
# Assembly Planning
npm install react-window  # Virtualized scrolling

# Catalog Version
npm install react-diff-viewer  # Diff visualization
npm install react-slider       # Horizontal slider
```

---

## Amit Planner-A-tól veszünk át

✅ **Cached materialized view** handling — 30s polling ésszerű Phase 1-ben
✅ **Virtual scrolling** 500+ event kezeléshez
✅ **Inkrementális value delivery** — TOP 2 izolált win

## Amit Planner-B-től veszünk át

✅ **Unified timeline koncepció** (nem split-pane) — jobb mobil UX
✅ **Plan/Actual overlay vizualizáció** (outline vs solid buborékok)
✅ **DeltaBadge reusable komponens**
✅ **Follow mode** auto-scroll
✅ **Time-travel slider UX** catalog verziókezeléshez
✅ **WebSocket-ready architektúra** — Phase 1 REST, Phase 2 upgrade path

---

## DoD

- [ ] LiveAssemblyTimeline komponens működik
- [ ] TimelineBubble + DeltaBadge komponensek
- [ ] FollowModeToggle auto-scroll implementálva
- [ ] VarianceSummaryCard KPI-k megjelennek
- [ ] FilterToolbar date range + deviation filter
- [ ] CatalogVersionView komponens működik
- [ ] VersionSlider horizontal timeline
- [ ] SnapshotPreview live preview
- [ ] DiffHighlight react-diff-viewer integráció
- [ ] VersionMetadata author + timestamp
- [ ] Restore gomb → draft product creation
- [ ] Mobil responsive (bottom sheet, pinch-zoom)
- [ ] Virtual scrolling működik 500+ event esetén
- [ ] React Query 30s polling
- [ ] Storybook példák minden komponenshez

---

**Referencia:** `/opt/spaceos/docs/planning/queue/2026-06-22_2214_consensus.md`
