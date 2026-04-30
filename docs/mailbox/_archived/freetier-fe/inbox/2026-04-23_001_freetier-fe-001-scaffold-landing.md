---
id: MSG-FREETIER-FE-001
from: root
to: freetier-fe
type: task
priority: high
status: REVOKED
created: 2026-04-23
---

# FREETIER-FE-001 — Repo scaffold + Landing page + Nesting kalkulátor (Nap 1–4)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **API:** `https://freetier.joinerytech.hu` (LIVE, 162 teszt)
> **Domain:** `eszkozok.joinerytech.hu` (nginx majd)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Nap 1 — Projekt scaffold

```bash
pnpm create vite spaceos-freetier-portal --template react-ts
cd spaceos-freetier-portal
pnpm add tailwindcss @tailwindcss/vite react-router-dom
pnpm add -D vitest @testing-library/react @testing-library/jest-dom jsdom
```

**vite.config.ts:**
```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: { sourcemap: false }, // SEC-UI-03
  server: { proxy: { '/api': 'http://127.0.0.1:5010' } },
  test: { environment: 'jsdom', globals: true, setupFiles: './src/test-setup.ts' },
});
```

**Fontos:** A fájlokat a `/opt/spaceos/spaceos-freetier-portal/` könyvtárba hozd létre. Ha a Vite scaffold más mappába teszi, mozgasd ide.

**.env:**
```
VITE_API_BASE_URL=http://127.0.0.1:5010
```

**.env.production:**
```
VITE_API_BASE_URL=https://freetier.joinerytech.hu
```

---

## Nap 1.5 — API client + types

**`src/api/client.ts`:**
```typescript
const BASE = import.meta.env.VITE_API_BASE_URL || '';

export async function apiFetch<T>(path: string, opts?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    credentials: 'include', // ft_sess cookie automatikusan megy
    headers: { 'Content-Type': 'application/json', ...opts?.headers },
    ...opts,
  });
  if (!res.ok) {
    if (res.status === 401) throw new AuthError();
    throw new ApiError(res.status, await res.text());
  }
  return res.json();
}
```

**FONTOS:** `credentials: 'include'` kötelező — a `ft_sess` HttpOnly cookie így megy automatikusan. Nincs Bearer token, nincs Authorization header.

**`src/api/types.ts`:** API response DTO-k (NestingResult, Workspace, WorkspaceRevision, ShareView, stb.)

---

## Nap 2–3 — Landing page + Nesting kalkulátor

A fő oldal (`/`) = SEO landing + interaktív nesting kalkulátor.

### Layout

```
┌─────────────────────────────────────────┐
│  Logo   [Bejelentkezés]                 │  ← header
├─────────────────────────────────────────┤
│                                         │
│  🪚 Ingyenes Lapszabász Kalkulátor      │  ← hero section
│  Számold ki az optimális anyagfelhasználást │
│                                         │
├────────────────────┬────────────────────┤
│  Lap méret:        │  Eredmény:         │
│  [2800] x [2070]   │  ┌──────────────┐  │
│                    │  │  SVG vizuali- │  │
│  Alkatrészek:      │  │  záció        │  │
│  + [Név] [W] [H]  │  │  (sheet +     │  │
│  + [Név] [W] [H]  │  │   placed      │  │
│  + [Név] [W] [H]  │  │   parts)      │  │
│  [+ Alkatrész]     │  └──────────────┘  │
│                    │                    │
│  [🔄 Számolás]     │  Yield: 78.5%     │
│                    │  Waste: 21.5%     │
│                    │  Sheets: 2        │
├────────────────────┴────────────────────┤
│  [💾 Mentés workspace-be] ← auth kell  │
│  [📤 Megosztás] ← auth kell            │
├─────────────────────────────────────────┤
│  Miért SpaceOS? · Funkciók · Upgrade    │  ← footer / CTA
└─────────────────────────────────────────┘
```

### Nesting input form
- Sheet méret: width + height (mm) — default 2800x2070
- Alkatrész lista: név + width + height + quantity
- [+ Alkatrész hozzáadás] gomb
- [Számolás] gomb → `POST /api/freetier/nest`
- Validáció: max 500 alkatrész, 1-10000mm méret (SEC-08)

### Nesting result megjelenítés
- SVG vizualizáció: sheet outline + placed rectangles (színkódolt)
- Statisztikák: yield %, waste %, sheet count, total area
- Alkatrész lista eredménnyel (placed/not placed)

### Responsive
- Mobile-first (375px+)
- Desktop: side-by-side input/result
- Mobile: stacked (input felül, result alul)

**Tesztek (+15):**
- API client: fetch mock, 401 handling, cookie credentials
- NestingCalculator: render, add part, remove part, submit, validation
- Result display: SVG render, stats display
- Responsive: mobile/desktop layout

---

## Nap 4 — Routing + Auth pages

**React Router:**
```typescript
<Routes>
  <Route path="/" element={<HomePage />} />
  <Route path="/auth/verify" element={<AuthVerifyPage />} />
  <Route path="/share/:prefix/:token" element={<SharePage />} />
  <Route path="/workspaces" element={<ProtectedRoute><WorkspacesPage /></ProtectedRoute>} />
  <Route path="/workspaces/:id" element={<ProtectedRoute><WorkspaceDetailPage /></ProtectedRoute>} />
  <Route path="/upgrade" element={<ProtectedRoute><UpgradePage /></ProtectedRoute>} />
</Routes>
```

**Magic Link Login flow:**
1. [Bejelentkezés] gomb → email input modal
2. Submit → `POST /api/freetier/auth/magic-link` → 202
3. "Ellenőrizd az email-edet" üzenet
4. User klikk a linkre → `/auth/verify?token=...&email=...`
5. `AuthVerifyPage` → `POST /api/freetier/auth/verify` → redirect `/workspaces`

**ProtectedRoute:** Ellenőrzi, hogy van-e aktív session (egy lightweight endpoint hívással, pl. `GET /api/freetier/workspaces` — ha 401, redirect `/` + login modal).

**Share page:** Public, no auth — `GET /api/freetier/share/{prefix}/{token}` → nesting result megjelenítés

**Tesztek (+10):**
- Routing: minden route renderel
- AuthVerifyPage: token extract, API call, redirect
- ProtectedRoute: 401 → redirect
- SharePage: public view render

---

## Definition of Done

- [ ] Vite + React 18 + TypeScript + Tailwind scaffold
- [ ] API client (`credentials: 'include'`, no Bearer)
- [ ] Landing page: nesting kalkulátor form + SVG result
- [ ] Responsive: mobile 375px + desktop
- [ ] React Router: 6 route (/, /auth/verify, /share/:p/:t, /workspaces, /workspaces/:id, /upgrade)
- [ ] Magic link login flow (email → 202 → verify page)
- [ ] ProtectedRoute + SharePage (public)
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 25 pass
- [ ] `pnpm lint` 0 error
- [ ] Outbox DONE üzenet küldve
