# Datahaven Domain Memory

> Automatikusan betöltődik ha a feladat Datahaven Dashboard-hoz kapcsolódik.

## Domain Scope

- **App:** `datahaven-web` (client + server)
- **Felelősség:** Agent monitoring, Planning pipeline, Kanban, Projects
- **Tech stack:** React 18, TypeScript, Express, SSE, Mermaid, Cytoscape

## Aktív Patterns

### 1. SSE Real-time Updates
```typescript
// Server-Sent Events for live updates
useEffect(() => {
  const eventSource = new EventSource('/api/sse/terminals');

  eventSource.onmessage = (event) => {
    const data = JSON.parse(event.data);
    setTerminals(data.terminals);
  };

  return () => eventSource.close();
}, []);
```

### 2. Terminal Status Cards
```typescript
interface TerminalStatus {
  name: string;
  status: 'working' | 'idle' | 'error';
  currentTask?: string;
  sessionActive: boolean;
  inboxCount: number;
  outboxCount: number;
}

const TerminalCard: React.FC<{ terminal: TerminalStatus }> = ({ terminal }) => (
  <div className={`p-4 rounded-lg ${statusColors[terminal.status]}`}>
    <h3 className="font-bold">{terminal.name.toUpperCase()}</h3>
    <StatusBadge status={terminal.status} />
    {terminal.currentTask && <p className="text-sm">{terminal.currentTask}</p>}
    <div className="flex gap-2 mt-2">
      <span>📥 {terminal.inboxCount}</span>
      <span>📤 {terminal.outboxCount}</span>
    </div>
  </div>
);
```

### 3. Planning Pipeline Visualization
```typescript
// 5-stage pipeline: Idea → Selected → Debate → Consensus → Queue
const STAGES = ['ideas', 'selected', 'debate', 'consensus', 'queue'] as const;

const PlanningPipeline: React.FC = () => {
  const [counts, setCounts] = useState<Record<string, number>>({});

  // Fetch counts for each stage
  useEffect(() => {
    fetch('/api/planning/counts')
      .then(r => r.json())
      .then(setCounts);
  }, []);

  return (
    <div className="flex justify-between">
      {STAGES.map(stage => (
        <StageColumn key={stage} name={stage} count={counts[stage] || 0} />
      ))}
    </div>
  );
};
```

### 4. Mermaid Graph Rendering
```typescript
import mermaid from 'mermaid';

const MermaidGraph: React.FC<{ definition: string }> = ({ definition }) => {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (containerRef.current) {
      mermaid.render('graph', definition).then(({ svg }) => {
        containerRef.current!.innerHTML = svg;
      });
    }
  }, [definition]);

  return <div ref={containerRef} />;
};
```

## Pages

| Page | URL | Funkció |
|------|-----|---------|
| Dashboard | `/` | Terminal status cards, metrics |
| Kanban | `/kanban` | Dual-track: Discovery + Delivery |
| Planning | `/planning` | 5-stage pipeline visualization |
| Projects | `/projects` | Gantt timeline, epic list |

## API Endpoints (Express)

| Endpoint | Method | Response |
|----------|--------|----------|
| `/api/dashboard` | GET | Terminal statuses, metrics |
| `/api/sse/terminals` | GET | SSE stream |
| `/api/planning/counts` | GET | Pipeline stage counts |
| `/api/kanban/items` | GET | Kanban cards |
| `/api/projects` | GET | Project list with epics |

## Legutóbbi Tanulságok

- **SSE reconnect** logic szükséges (5s retry)
- **Mermaid init** csak egyszer az app lifecycle-ban
- **Auth token** header minden API hívásban
- **Responsive grid** 1-4 oszlopos terminál cards
