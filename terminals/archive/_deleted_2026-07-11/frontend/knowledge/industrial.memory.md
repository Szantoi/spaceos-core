# Industrial UI Domain Memory

> Automatikusan betöltődik ha a feladat Industrial Flow Editor-hoz kapcsolódik.

## Domain Scope

- **App:** `datahaven-web/client` (Industrial components)
- **Felelősség:** Workflow visualization, Drag-drop editor, Graph-based UI
- **Tech stack:** React 18, Cytoscape.js, React Flow, TailwindCSS

## Aktív Patterns

### 1. Cytoscape Graph Integration
```typescript
import cytoscape from 'cytoscape';

const IndustrialFlowEditor: React.FC = () => {
  const containerRef = useRef<HTMLDivElement>(null);
  const [cy, setCy] = useState<cytoscape.Core | null>(null);

  useEffect(() => {
    if (containerRef.current) {
      const instance = cytoscape({
        container: containerRef.current,
        elements: [],
        style: [
          { selector: 'node', style: { 'background-color': '#666', 'label': 'data(label)' } },
          { selector: 'edge', style: { 'width': 3, 'line-color': '#ccc' } }
        ],
        layout: { name: 'dagre' }
      });
      setCy(instance);
    }
  }, []);

  return <div ref={containerRef} className="w-full h-[600px]" />;
};
```

### 2. Drag-Drop Node Creation
```typescript
const handleDrop = (e: React.DragEvent) => {
  e.preventDefault();
  const nodeType = e.dataTransfer.getData('nodeType');
  const position = cy?.renderer().projectIntoViewport(e.clientX, e.clientY);

  if (cy && position) {
    cy.add({
      group: 'nodes',
      data: { id: `node-${Date.now()}`, label: nodeType, type: nodeType },
      position: { x: position[0], y: position[1] }
    });
  }
};
```

### 3. Node Types (Industrial Workflow)
```typescript
type NodeType =
  | 'material-input'    // Anyag bemenet
  | 'cutting-operation' // Vágási művelet
  | 'assembly-step'     // Összeszerelés
  | 'quality-check'     // Minőségellenőrzés
  | 'output'            // Kimenet
  | 'decision';         // Döntési pont

const NODE_CONFIGS: Record<NodeType, NodeConfig> = {
  'material-input': { color: '#4CAF50', icon: '📦', label: 'Anyag' },
  'cutting-operation': { color: '#2196F3', icon: '✂️', label: 'Vágás' },
  'assembly-step': { color: '#FF9800', icon: '🔧', label: 'Szerelés' },
  'quality-check': { color: '#9C27B0', icon: '✅', label: 'QC' },
  'output': { color: '#F44336', icon: '📤', label: 'Kimenet' },
  'decision': { color: '#607D8B', icon: '❓', label: 'Döntés' },
};
```

### 4. Edge Connection Rules
```typescript
const VALID_CONNECTIONS: Record<NodeType, NodeType[]> = {
  'material-input': ['cutting-operation', 'assembly-step'],
  'cutting-operation': ['assembly-step', 'quality-check', 'output'],
  'assembly-step': ['quality-check', 'output', 'decision'],
  'quality-check': ['output', 'decision', 'assembly-step'],
  'decision': ['cutting-operation', 'assembly-step', 'output'],
  'output': [],
};

const canConnect = (source: NodeType, target: NodeType): boolean => {
  return VALID_CONNECTIONS[source]?.includes(target) ?? false;
};
```

### 5. Workflow Serialization
```typescript
interface WorkflowDefinition {
  nodes: Array<{ id: string; type: NodeType; position: { x: number; y: number }; data: Record<string, unknown> }>;
  edges: Array<{ id: string; source: string; target: string; label?: string }>;
  metadata: { name: string; version: string; createdAt: string };
}

const serializeWorkflow = (cy: cytoscape.Core): WorkflowDefinition => ({
  nodes: cy.nodes().map(n => ({
    id: n.id(),
    type: n.data('type'),
    position: n.position(),
    data: n.data()
  })),
  edges: cy.edges().map(e => ({
    id: e.id(),
    source: e.source().id(),
    target: e.target().id(),
    label: e.data('label')
  })),
  metadata: { name: 'workflow', version: '1.0', createdAt: new Date().toISOString() }
});
```

## Komponensek

| Komponens | Fájl | Funkció |
|-----------|------|---------|
| IndustrialFlowEditor | `components/Industrial/FlowEditor.tsx` | Fő editor |
| NodePalette | `components/Industrial/NodePalette.tsx` | Drag source |
| PropertyPanel | `components/Industrial/PropertyPanel.tsx` | Node szerkesztés |
| WorkflowToolbar | `components/Industrial/Toolbar.tsx` | Save, Load, Export |

## Legutóbbi Tanulságok

- **Cytoscape layout** után `fit()` kell a renderhez
- **dagre layout** függőleges workflow-hoz
- **cose layout** organikus elrendezéshez
- **Touch events** mobile-on külön kezelendők
