# FE Terminál (Frontend / Portal)

> React 18 frontend — JoineryTech Design Portal

## Gyors Info

| | |
|---|---|
| **Terminál** | fe |
| **Port** | 5173 (dev), 80/443 (prod) |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/frontend/joinerytech-portal/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/fe/` |
| **Memory** | `/opt/spaceos/docs/memory/fe.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/fe.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/fe/inbox/

# 3. Build és teszt
cd /opt/spaceos/frontend/joinerytech-portal
npm run build
npm test
```

## Build & Test Parancsok

```bash
# Install
npm install

# Dev szerver
npm run dev

# Build
npm run build

# Tesztek
npm test

# Lint
npm run lint

# Type check
npm run type-check
```

## Architektúra

```
src/
├── components/          ← React komponensek
│   ├── ui/              ← shadcn/ui primitívek
│   ├── forms/           ← Form komponensek
│   └── layout/          ← Layout komponensek
├── pages/               ← Route oldalak
├── hooks/               ← Custom React hooks
├── services/            ← API hívások
├── store/               ← State management
├── types/               ← TypeScript típusok
└── utils/               ← Utility függvények
```

## Tech Stack

- **React 18** + TypeScript
- **Vite** — build tool
- **TailwindCSS** — styling
- **shadcn/ui** — UI komponensek
- **React Query** — data fetching
- **React Router** — routing

## Környezeti Változók

```bash
# .env
VITE_API_URL=http://localhost:3000
VITE_KEYCLOAK_URL=...
VITE_KEYCLOAK_REALM=...
VITE_KEYCLOAK_CLIENT_ID=...
```

## DONE Outbox Sablon

```yaml
---
id: MSG-FE-NNN-DONE
from: fe
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-FE-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Tesztek
npm test eredmény.

## Build
npm run build sikeres.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/frontend/joinerytech-portal/CLAUDE.md`
- Knowledge: `/opt/spaceos/docs/knowledge/context/PORTAL_CONTEXT.md`
