# SpaceOS — Kódbázis összesített állapotleírás

**Utolsó frissítés:** 2026-03-31
**Tesztelő:** Gabor
**Környezet:** localhost (dev)

---

## Rendszer architektúra

```
Browser  http://localhost:5173
  │
  ▼
L4  Design Portal     (React 18 · Vite · Tailwind)     port 5173
  │  /bff/*  →  proxy
  ▼
L3  Orchestrator       (Node.js 20 · Express · TS)     port 3000
  │  /api/*  →  JWT forwarded
  ▼
L2  Kernel API         (C# .NET 8 · Minimal API)       port 5000
  │
  ▼
L1  PostgreSQL 16      (dev: SQLite)                    port 5432
  │
  ↕
LLM Provider           (Anthropic / OpenAI / Mock)
```

---

## Projekt állapotok összesítése

| Projekt | Réteg | Stack | Epicek | Státusz | Tesztek | Build |
|---|---|---|---|---|---|---|
| **SpaceOS.Kernel** | L2 Backend | .NET 8, EF Core 8, PostgreSQL | E1–E10 | Mind `CLOSED_DONE` | 350 pass / 0 fail | 0 error, 0 warning |
| **SpaceOS.Orchestrator** | L3 BFF | Node.js 20, TypeScript 5, Express 4 | E11–E17 | Mind `CLOSED_DONE` | 50 pass / 0 fail | 0 TS error |
| **SpaceOS.DesignPortal** | L4 Frontend | React 18, TypeScript 5, Vite, Tailwind | E18–E27 | Mind `CLOSED_DONE` | 174 pass / 0 fail | 0 TS error |

**Összesen:** 27 epic, 574 teszt, 0 hiba.

---

## Részletes állapotleírások

| Projekt | Codebase Status fájl |
|---|---|
| Kernel | [SpaceOS.Kerner/docs/Codebase_Status.md](../SpaceOS.Kerner/docs/Codebase_Status.md) |
| Orchestrator | [spaceos.orchestrator/docs/Codebase_Status.md](../spaceos.orchestrator/docs/Codebase_Status.md) |
| Design Portal | [design-portal/docs/Codebase_Status.md](../design-portal/docs/Codebase_Status.md) |

---

## Epic összesítés

### L2 — Kernel (C# .NET 8)

| Epic | Cím | Státusz |
|------|-----|---------|
| E1 | Domain Layer (Aggregates, VOs, Events) | `CLOSED_DONE` |
| E2 | Application Layer (CQRS, MediatR) | `CLOSED_DONE` |
| E3 | Infrastructure (EF Core + PostgreSQL) | `CLOSED_DONE` |
| E4 | API Layer (Minimal API, JWT Auth) | `CLOSED_DONE` |
| E5 | Unit Tests | `CLOSED_DONE` |
| E6 | Integration Tests | `CLOSED_DONE` |
| E7 | Docker + Docker Compose | `CLOSED_DONE` |
| E8 | Audit Log (Append-only, SHA-256) | `CLOSED_DONE` |
| E9 | Rate Limiting | `CLOSED_DONE` |
| E10 | OpenAPI / Swagger | `CLOSED_DONE` |

### L3 — Orchestrator (Node.js)

| Epic | Cím | Státusz |
|------|-----|---------|
| E11 | Project Bootstrap & Health | `CLOSED_DONE` |
| E12 | LLM Provider Abstraction | `CLOSED_DONE` |
| E13 | Tool Registry & Kernel Action Dispatch | `CLOSED_DONE` |
| E14 | Interpreter Service (Agentic Loop) | `CLOSED_DONE` |
| E15 | Kernel Proxy & Auth Middleware | `CLOSED_DONE` |
| E16 | Unit & Integration Tests | `CLOSED_DONE` |
| E17 | VPS Deployment (pm2 + nginx) | `CLOSED_DONE` |

### L4 — Design Portal (React)

| Epic | Cím | Státusz |
|------|-----|---------|
| E18 | Project Bootstrap | `CLOSED_DONE` |
| E19 | Auth & Protected Routes | `CLOSED_DONE` |
| E20 | AppShell & Navigation | `CLOSED_DONE` |
| E21 | Tenant & Facility CRUD | `CLOSED_DONE` |
| E22 | WorkStation + FSM | `CLOSED_DONE` |
| E23 | SpaceLayer Management | `CLOSED_DONE` |
| E24 | FlowEpic Kanban + B2B | `CLOSED_DONE` |
| E25 | Audit Log | `CLOSED_DONE` |
| E26 | Chat UI | `CLOSED_DONE` |
| E27 | Dashboard | `CLOSED_DONE` |

---

## Security állapot

### L2 Kernel

| Terület | Állapot |
|---|---|
| JWT Bearer (HS256) | RBAC: Joiner, Designer, Admin |
| Rate limiting | fixed 100/min GET, sliding 20/min POST/PUT |
| Audit log | Append-only, SHA-256 hash, `jsonb` payload |
| Docker | Non-root user, health check, Alpine image |
| Secrets | Env var only — soha nem committed |

### L3 Orchestrator

| ID | Szabály | Állapot |
|----|---------|---------|
| S1 | No user input in system prompt | PASS |
| S2 | JWT verify, not decode | PASS |
| S3 | No API keys in source | PASS |
| S4 | Kernel port not exposed | PASS |
| S5 | Curated tool registry | PASS |
| S6 | MAX_TOOL_ITERATIONS = 5 | PASS |
| S7 | Rate limiting on all BFF routes | PASS |
| S8 | No CORS wildcard in production | PASS |
| S9 | Express binds 127.0.0.1 only | PASS |
| S10 | Axios timeout set | PASS |

### L4 Design Portal

| Terület | Állapot |
|---|---|
| OWASP A01 (Access Control) | Minden route `ProtectedRoute`-tal védett |
| OWASP A03 (Injection) | `react-hook-form` + `zod` validáció |
| No `dangerouslySetInnerHTML` | PASS |
| No hardcoded secrets | PASS |
| Chat max length 500 char | PASS (prompt injection védelem) |
| JWT expiry check | Rehydration-kor `isTokenExpired()` |
| 401 auto-logout + redirect | PASS |
| `npm audit` | 0 high/critical |

---

## Manuális tesztelés (2026-03-31)

Részletes teszt napló: [TEST_LOG.md](../TEST_LOG.md)

### Talált hibák (manuális teszt során)

| # | Hely | Hiba | Javítás | Státusz |
|---|---|---|---|---|
| 1 | Design Portal — `LoginPage.tsx` | Jelszó validáció min(6) — `admin` jelszó nem engedett | `min(6)` → `min(4)` | JAVÍTVA |
| 2 | Design Portal — `auth.service.ts` | Hibás login URL: `/api/auth/token` → 404, mert a base URL `/bff` + `/api/auth/token` = `/bff/api/auth/token` | Javítva: `/auth/token` | JAVÍTVA |
| 3 | Design Portal — Dashboard | Kártyák nem jelennek meg | Vizsgálat szükséges (Kernel `/api/dashboard/stats` nem létezik) | NYITOTT |
| 4 | Design Portal — Tenants | Újonnan létrehozott tenant nem jelenik meg a listában | Vizsgálat szükséges | NYITOTT |

---

## Ismert limitációk

| Terület | Leírás | Érintett réteg |
|---|---|---|
| Dev-only auth | Orchestrator bármilyen username/password-ot elfogad — produkciós IdP (Auth0/Keycloak) szükséges | L3 |
| Dashboard stats | `/api/dashboard/stats` végpont nem létezik a Kernelben — Dashboard 502-t kap | L2 + L4 |
| OpenAI provider | `getLlmProvider()` throws "not yet implemented" | L3 |
| SSE streaming | `/bff/chat` full JSON válasz — streaming nincs | L3 |
| SQLite dev DB | Kernel dev módban SQLite-ot használ — EF migráció kell első indításkor | L2 |
| Per-tenant rate limiting | IP-alapú — JWT claims granularitás nincs | L2 + L3 |
| CI/CD pipeline | Nincs — manuális deploy | L2 + L3 |
| Kubernetes | Docker Compose-ig kész — K8s manifesztek hiányoznak | L2 |
| `npm audit` moderate | 4 moderate (esbuild/vite dev dependency) — nem érint produkciós kódot | L4 |

---

## Technológiai stack összesítés

| Réteg | Runtime | Framework | DB | Auth | Tesztek |
|---|---|---|---|---|---|
| L2 | .NET 8 | ASP.NET Core Minimal API | PostgreSQL (EF Core 8) | JWT HS256 RBAC | xUnit v3, Moq, WebAppFactory |
| L3 | Node.js 20 | Express 4 | — | JWT verify (same key) | Vitest |
| L4 | — | React 18, Vite, Tailwind | — | Zustand + JWT | Vitest, Testing Library |
