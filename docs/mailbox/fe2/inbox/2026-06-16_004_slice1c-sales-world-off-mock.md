---
id: MSG-FE2-004
from: root
to: fe2
type: task
priority: high
status: READ
ref: MSG-ARCH-001-DONE
created: 2026-06-16
---

# FE-SALES Slice 1C — Sales world off-mock

## Kontextus

A Sales backend (5009) él és kész. A SalesPage jelenleg PARTIAL — van API hívás de mock fallback is él.  
Ez a task a Sales Reconciliation v1.1 spec **Track B (Portal)** része.

Spec fájl: `docs/tasks/new/SpaceOS_Sales_FrontOffice_Contract_Reconciliation_v1.md`

## Feladat

### 1. SalesPage teljes mock eltávolítás

- `QUOTES_FALLBACK` mock eltávolítva
- Minden Sales hook (`useSalesDetail`, `useSalesQuotes`, `useSalesCustomers`) valódi API-t hív, nincs `?? mockData` fallback
- Error state + empty state minden listán

### 2. Sales Reconciliation CI-001/002/003 workaround-ok (spec §5)

| Issue | Megoldás |
|---|---|
| CI-001: line-tree hierarchia (Quote.lines[] fa) | Megtartod a display-only kliens-oldali fa nézetet a flat persisted sorok felett — NEM perzisztálsz, NEM küldöd backendre a hierarchiát |
| CI-002: nginx upstream verify | Ellenőrizd: `VITE_SALES_URL` → `/sales` nginx route → 5009 service ✅ (ha nem megy: jelzed outboxban) |
| CI-003: lejárt ajánlat badge | `ValidUntil < now` → "Lejárt" badge kliens-oldalon számítva, backend nem adja |

### 3. SEC-FE-01: Token verify

- 0 token legyen localStorage-ben
- Token csak memory / httpOnly cookie-ban lehet
- Ha találsz localStorage.setItem('token', …) vagy hasonlót → fix kötelező, security blocker

### 4. SalesPage minőségi elvárások

- Customer létrehozás (`CreateCustomerSlideOver`) → valódi POST → lista frissül
- Quote létrehozás (`CreateQuoteSlideOver`) → valódi POST → lista frissül
- Quote FSM akciók (Send/Accept/Reject/Convert) → valódi API hívások, spinner + error kezelés
- `CustomerDetailSlideOver` → `GET/PATCH /sales/api/customers/{id}` valódi API

## DoD

- SalesPage: 0 mock import/fallback
- CI-001/002/003 kezelve
- SEC-FE-01 verifikálva (0 localStorage token)
- Build zöld, tesztek zöldek
- Outbox: `MSG-FE2-004-DONE` — CI-002 nginx ellenőrzés eredménye, SEC-FE-01 státusz

## Skill / agent

Használd a `/spaceos-terminal` skillt. Sub-agent engedélyezett.
