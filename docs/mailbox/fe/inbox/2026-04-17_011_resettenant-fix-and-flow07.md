---
id: MSG-FE-011
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-FE-010-DONE
created: 2026-04-17
---

# FE-011 — resetTenant DELETE→POST fix + Flow 07 (Supplier) E2E spec

## Kontextus

FE-010 DONE-ban két nyitott pont maradt:

1. **resetTenant.ts HTTP metódus bug** — a helper `DELETE`-et küld a BFF-nek, de az endpoint `POST`.
   Ez az összes E2E teszt reset lépését töri production-on.
2. **Flow 07 (Supplier)** — az E2E spec fájl hiányzik (FE-010-ben halasztva).

## Feladat

### 1. resetTenant.ts fix — DELETE → POST

Fájl: `tests/e2e/helpers/resetTenant.ts`

```typescript
// ELŐTTE (hibás):
method: 'DELETE'

// UTÁNA (helyes):
method: 'POST'
```

A BFF reset endpoint: `POST /bff/test/tenants/{id}/reset?confirm=true`  
(Lásd CLAUDE.md — ⚠️ Ismert bug szekció)

### 2. Flow 07 — Supplier E2E spec

Fájl: `tests/e2e/flows/07-supplier-flow.spec.ts`

**Feladat:** Implementáld a tényleges E2E teszteket (ne `test.skip`). A Supplier seed már működik
(`doorstar-cutting-ready-v1` → `suppliers: 1`).

**Scope:**
- Supplier adatok megjelenítése az UI-on (ha van `/suppliers` route) — ha nincs UI: `test.skip`
  és CONTRACT_ISSUES.md bejegyzés
- Ha van BFF `GET /bff/procurement/*` → supplier lista lekérés tesztelése

> Ha a Supplier UI route még nem létezik (`/suppliers`), a tesztek maradhatnak `test.skip`-en,
> de a spec fájlnak léteznie kell a helyes describe blokkokkal és skip megjegyzésekkel.

### 3. Build + Test

```bash
pnpm build        # 0 TS error
pnpm test         # ≥90 pass (unit + contract)
pnpm lint         # 0 error
pnpm typecheck    # 0 error
```

## DoD

- [ ] `resetTenant.ts` → `method: 'POST'`
- [ ] `tests/e2e/flows/07-supplier-flow.spec.ts` létezik (valódi teszt vagy indokolt skip)
- [ ] `pnpm build && pnpm test && pnpm lint && pnpm typecheck` — mind zöld
- [ ] Ha Supplier UI route hiányzik: `CONTRACT_ISSUES.md` bejegyzés + outbox-ban jelzés
- [ ] OUTBOX DONE üzenet (`MSG-FE-011-DONE`)

## Eszközök

`/spaceos-terminal` skill
