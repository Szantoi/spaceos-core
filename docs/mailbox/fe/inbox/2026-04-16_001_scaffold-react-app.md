---
id: MSG-FE-001
from: root
to: fe
type: task
priority: high
status: READ
created: 2026-04-16
---

# FE-001 — Doorstar Portal React app scaffold

## Kontextus

Ez a spaceos-fe tmux session. A working directory: `/opt/spaceos/spaceos-doorstar-portal/`.
Olvasd el a CLAUDE.md fájlt — az összes constraint ott van.
A mailbox protokollt kövesd: INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX.

## Feladat

Hozd létre a Doorstar Portal React alkalmazás alapvázát.

## Technológiai stack

- **React 18** + **TypeScript** + **Vite**
- **pnpm** (package manager)
- **Tailwind CSS v3**
- **oidc-client-ts** (Keycloak auth)
- **React Router v6**
- **vitest** + **@testing-library/react** (tesztek)

## Elvégzendő lépések

### 1. Vite projekt létrehozás

```bash
cd /opt/spaceos/spaceos-doorstar-portal
pnpm create vite . --template react-ts
pnpm install
```

### 2. Függőségek

```bash
pnpm add oidc-client-ts react-oidc-context react-router-dom
pnpm add -D tailwindcss postcss autoprefixer vitest @testing-library/react @testing-library/user-event @testing-library/jest-dom jsdom @vitejs/plugin-react
npx tailwindcss init -p
```

### 3. Konfiguráció fájlok

**vite.config.ts** — sourcemap kikapcsolva prod-ban (SEC-UI-03):
```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => ({
  plugins: [react()],
  build: {
    sourcemap: mode !== 'production',
  },
  server: {
    port: 5173,
    proxy: {
      '/bff': {
        target: process.env.VITE_BFF_BASE_URL || 'http://localhost:3000',
        changeOrigin: true,
      },
    },
  },
}));
```

**tailwind.config.js:**
```js
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: { extend: {} },
  plugins: [],
};
```

**vitest.config.ts:**
```typescript
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
  },
});
```

**.env.example** (commitolható, .env.local nem):
```
VITE_KEYCLOAK_URL=http://localhost:8080/auth
VITE_KEYCLOAK_REALM=spaceos
VITE_KEYCLOAK_CLIENT_ID=portal-app
VITE_BFF_BASE_URL=http://localhost:3000
```

**.gitignore** — ellenőrizd hogy tartalmazza:
```
.env.local
dist/
node_modules/
```

### 4. Forrás struktúra

```
src/
├── auth/
│   ├── keycloak.config.ts     ← UserManager, InMemoryWebStorage (CLAUDE.md alapján)
│   └── AuthProvider.tsx       ← AuthProvider wrapper (react-oidc-context)
├── components/
│   └── ProtectedRoute.tsx     ← Auth guard: ha nem bejelentkezett → redirect KC login
├── pages/
│   ├── LoginPage.tsx          ← "Bejelentkezés" gomb → userManager.signinRedirect()
│   ├── CallbackPage.tsx       ← /callback: userManager.signinRedirectCallback()
│   └── DashboardPage.tsx      ← Placeholder: "Üdvözöljük, {user.profile.name}"
├── test/
│   └── setup.ts               ← @testing-library/jest-dom import
├── App.tsx                    ← React Router routes
└── main.tsx                   ← AuthProvider wrapper, StrictMode
```

### 5. Alapvető tesztek

Legalább 3 teszt:
- `LoginPage` renderel login gombot
- `ProtectedRoute` bejelentkezetlen usert redirectál
- `DashboardPage` bejelentkezett usernek megjelenik a köszöntő

### 6. Build és check

```bash
pnpm build       # → dist/ létrejön, 0 error
pnpm test        # → minden teszt zöld
pnpm lint        # → 0 hiba (eslint)
pnpm typecheck   # → tsc --noEmit, 0 error
```

### 7. Git commit

```bash
git add .
git commit -m "feat: React 18 + Vite scaffold, OIDC auth, basic routing"
git push origin main
```

## DoD

- [ ] `pnpm build` → 0 error, `dist/` létrejön
- [ ] `pnpm test` → minden teszt zöld (≥3 teszt)
- [ ] `pnpm lint` → 0 hiba
- [ ] `pnpm typecheck` → 0 error
- [ ] `src/auth/keycloak.config.ts` — `InMemoryWebStorage` (sessionStorage/localStorage TILOS)
- [ ] `vite.config.ts` — `sourcemap: false` prod build-ben
- [ ] `.env.example` commitolva, `.env.local` gitignore-ban
- [ ] git commit + push main-ra

## Outbox

Ha kész, írj DONE-t: `mailbox/fe/outbox/2026-04-16_001_scaffold-react-app-done.md`
Tartalmazza: test count, build output, commit hash.
