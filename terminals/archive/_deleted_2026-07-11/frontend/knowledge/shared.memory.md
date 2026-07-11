# Shared Frontend Memory

> Minden task-hoz betöltődik - cross-domain patterns és általános szabályok.

## Tech Stack

- **Framework:** React 18 + TypeScript
- **Build:** Vite 5
- **Styling:** TailwindCSS 3
- **State:** React hooks (useState, useReducer, useContext)
- **Routing:** React Router 6
- **HTTP:** fetch API / axios
- **Charts:** Recharts, Mermaid, Cytoscape

## Project Structure

```
datahaven-web/client/
├── src/
│   ├── components/     ← Reusable UI components
│   ├── pages/          ← Route-level components
│   ├── hooks/          ← Custom React hooks
│   ├── services/       ← API calls
│   ├── types/          ← TypeScript interfaces
│   ├── styles/         ← CSS/Tailwind
│   ├── App.tsx         ← Router setup
│   └── main.tsx        ← Entry point
├── public/
├── index.html
├── vite.config.ts
├── tailwind.config.js
└── tsconfig.json
```

## Coding Patterns

### 1. Functional Components
```typescript
interface Props {
  title: string;
  onAction: () => void;
}

const MyComponent: React.FC<Props> = ({ title, onAction }) => {
  const [loading, setLoading] = useState(false);

  return (
    <div className="p-4">
      <h2>{title}</h2>
      <button onClick={onAction} disabled={loading}>
        {loading ? 'Loading...' : 'Click'}
      </button>
    </div>
  );
};

export default MyComponent;
```

### 2. Custom Hooks
```typescript
function useApi<T>(url: string) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    fetch(url)
      .then(r => r.json())
      .then(setData)
      .catch(setError)
      .finally(() => setLoading(false));
  }, [url]);

  return { data, loading, error };
}
```

### 3. Error Handling
```typescript
const ErrorBoundary: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [hasError, setHasError] = useState(false);

  if (hasError) {
    return <div className="text-red-500">Something went wrong</div>;
  }

  return <>{children}</>;
};
```

### 4. Form Handling
```typescript
const [formData, setFormData] = useState({ name: '', email: '' });

const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
  setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
};

const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  await submitForm(formData);
};
```

## TailwindCSS Conventions

```typescript
// Spacing: p-4 (1rem), m-2 (0.5rem)
// Colors: bg-blue-500, text-gray-700
// Responsive: sm:, md:, lg:, xl:
// Flex: flex, items-center, justify-between
// Grid: grid, grid-cols-3, gap-4

// Status colors
const statusClasses = {
  working: 'bg-green-100 text-green-800',
  idle: 'bg-gray-100 text-gray-600',
  error: 'bg-red-100 text-red-800',
};
```

## Build & Test Commands

```bash
# Development
npm run dev

# Build
npm run build

# Type check
npx tsc --noEmit

# Lint
npm run lint
```

## API Integration

```typescript
const API_BASE = import.meta.env.VITE_API_URL || '';
const AUTH_TOKEN = localStorage.getItem('auth_token');

async function apiFetch<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${AUTH_TOKEN}`,
      ...options?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }

  return response.json();
}
```

## Conventions

- **File naming:** PascalCase for components, camelCase for hooks/utils
- **Export:** Named exports for components, default for pages
- **Props:** Interface with `Props` suffix
- **Hooks:** `use` prefix
- **Types:** Separate `types/` folder for shared interfaces
