# E2E Terminál

> End-to-End tesztek — Playwright, integrációs tesztek

## Gyors Info

| | |
|---|---|
| **Terminál** | e2e |
| **Port** | - |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/e2e/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/e2e/` |
| **Memory** | `/opt/spaceos/docs/memory/e2e.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/e2e.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/e2e/inbox/

# 3. Tesztek futtatása
cd /opt/spaceos/e2e
npm test
```

## Build & Test Parancsok

```bash
# Install
npm install
npx playwright install

# Összes teszt
npm test

# Specifikus teszt
npx playwright test auth.spec.ts

# UI módban
npx playwright test --ui

# Report generálás
npx playwright show-report
```

## Teszt Struktúra

```
e2e/
├── tests/
│   ├── auth/           ← Authentikáció tesztek
│   ├── tenant/         ← Tenant CRUD tesztek
│   ├── joinery/        ← Joinery flow tesztek
│   └── smoke/          ← Smoke tesztek
├── fixtures/           ← Test fixtures
├── pages/              ← Page Object Model
└── playwright.config.ts
```

## Teszt Minták

- **probe-and-skip** — ha service nem elérhető, skip (nem fail)
- **401/200 minta** — auth előtt 401, auth után 200
- **test isolation** — minden teszt független

## Környezet

```bash
# .env
BASE_URL=http://localhost:5173
API_URL=http://localhost:3000
KEYCLOAK_URL=...
```

## DONE Outbox Sablon

```yaml
---
id: MSG-E2E-NNN-DONE
from: e2e
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-E2E-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit teszteltem, hány teszt.

## Teszt eredmények
Passed/Failed/Skipped számok.

## Report
Ha releváns, report elérhetősége.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/e2e/CLAUDE.md`
- Testing Patterns: `/opt/spaceos/docs/knowledge/patterns/TESTING_PATTERNS.md`
