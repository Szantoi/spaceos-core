---
id: MSG-FE-006
from: root
to: fe
type: task
priority: high
status: READ
ref: FE-PORTAL
created: 2026-04-16
---

# FE-006 — Production Readiness: Error Boundary + 404 + Profil oldal

## Kontextus

WD: `/opt/spaceos/spaceos-doorstar-portal/`. CLAUDE.md kötelező olvasás.
Pipeline: INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX

## Előzmény

FE-005 DONE: StatusFilter + OrderHistoryPanel + OrderStatusBadge · 72 teszt · commit `9237fc2`.
A portal live: `https://portal.joinerytech.hu`.

Ez az utolsó feladat a Doorstar Soft Launch MVP-hez szükséges alapfunkciókból. Cél: ha valami elromlik, a felhasználó ne fehér üres képernyőt lásson; legyen profilje és ki tudjon jelentkezni.

## Implementálandó

### 1. `ErrorBoundary` komponens (`src/components/ErrorBoundary.tsx`)

React class component (ErrorBoundary csak class-ban implementálható):

```typescript
interface Props { children: React.ReactNode; fallback?: React.ReactNode }
interface State { hasError: boolean; error: Error | null }

class ErrorBoundary extends React.Component<Props, State> {
  // componentDidCatch: console.error(error, errorInfo)
  // render: ha hasError → fallback vagy default ErrorFallback
  // resetErrorBoundary() metódus (state reset)
}
```

Default fallback (`ErrorFallback`):
- Barátságos hibaüzenet: "Váratlan hiba történt"
- `error.message` megjelenítése (fejlesztőknek)
- "Újratöltés" gomb → `window.location.reload()` vagy `resetErrorBoundary()`
- Tailwind: középre igazítva, piros ikon (X-circle vagy hasonló)

**Beágyazás:** `App.tsx`-ben az összes route köré kerüljön.

### 2. `NotFoundPage` komponens (`src/pages/NotFoundPage.tsx`)

- URL: minden nem létező route (`*` catch-all, ProtectedRoute-on kívül is)
- Tartalom: 404, "Az oldal nem található", Vissza a főoldalra link (`/orders`)
- Tailwind: középre igazított, szürke szöveg

**Route hozzáadás** `App.tsx`-ben:
```typescript
<Route path="*" element={<NotFoundPage />} />
```

### 3. `ProfilePage` (`/profile`) + `useProfile` hook

**BFF API:**
```typescript
GET /bff/auth/me   // Már él (Orchestrator)
// Response:
{
  sub: string;
  email?: string;
  name?: string;
  tenantId: string;
  tenantName?: string;
  roles: string[];
}
```

**`useProfile` hook** (`src/hooks/useProfile.ts`):
```typescript
export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: () => apiClient.get('/auth/me').then(r => r.data),
    staleTime: 5 * 60_000,   // 5 perc — profil ritkán változik
  });
}
```

**`ProfilePage` tartalom:**
- Avatar placeholder (initials a névből, szürke kör)
- Név, email, tenantName megjelenítése (ha van)
- Szerepkörök listája (pill badge-ek)
- **Kijelentkezés gomb** → `userManager.signoutRedirect()` (oidc-client-ts, PKCE flow)
- Loading state, error state

**Navigáció:** A navbar/header-ben profil link → `/profile` (a meglévő header komponensbe ágyazva)

**Route:** ProtectedRoute mögött:
```typescript
<Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
```

## Tesztek (kötelező)

- `ErrorBoundary`: error rendereli a fallbacket, reset visszaállítja, children render ha nincs error
- `NotFoundPage`: 404 szöveg, vissza link megjelenik
- `ProfilePage`: loading state, error state, name/email/roles megjelenítés, logout gomb megjelenik
- `useProfile`: fetch, staleTime, error
- **≥10 új teszt**

## DoD

- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind zöld, ≥10 új teszt (összesen ≥82)
- [ ] `pnpm lint` → 0 hiba
- [ ] `pnpm typecheck` → 0 error
- [ ] `ErrorBoundary` az App root körül van (minden route védi)
- [ ] `NotFoundPage` catch-all route-on aktív
- [ ] `ProfilePage` ProtectedRoute mögött, `/profile` route-on
- [ ] Kijelentkezés gomb működik (`signoutRedirect`)
- [ ] Header-ben profil link látható bejelentkezve
- [ ] git commit + push

## Outbox

DONE: `mailbox/fe/outbox/2026-04-16_006_production-readiness-done.md`

## Skillек & Agentек

- `/senior-frontend` — React ErrorBoundary class pattern, OIDC signout, profil UX
- `/javascript-typescript-jest` — ErrorBoundary tesztek (componentDidCatch mock), vitest class component
- `/senior-qa` — error recovery, 404 navigáció, signout redirect edge case
- `/a11y-audit` — error üzenet aria-live, 404 fókusz kezelés, profil oldal landmark
- Agent: `expert-react-frontend-engineer` — oidc-client-ts signoutRedirect, ErrorBoundary reset pattern
- Agent: `se-security-reviewer` — signout flow, profil adatok megjelenítése (PII), token cleanup
- Sub-agenteket nyugodtan indíts párhuzamosan (pl. ErrorBoundary + NotFoundPage + ProfilePage egyszerre)
