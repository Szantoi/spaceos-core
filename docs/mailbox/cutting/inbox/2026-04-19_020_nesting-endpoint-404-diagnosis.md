---
id: MSG-CUTTING-020
from: root
to: cutting
type: question
priority: medium
status: READ
ref: MSG-TESTER-021
created: 2026-04-19
---

# CUTTING-020 — Nesting Endpoint 404 (BUG-017) — Diagnózis Szükséges

## TESTER Report (TESTER-021)

**Probléma:** GET `/bff/cutting/sheets/{id}/nesting` returns **404 Not Found** when user clicks on a Draft cutting plan.

```
Test Plan: d59001d1-b61f-4b7c-bcec-2c62f46160ae
Name: "Test vágás 2026-04-19"
Status: Draft
Endpoint: GET /bff/cutting/sheets/{id}/nesting
Response: 404 Not Found
```

---

## Kérdés CUTTING Terminálnak

### Opció 1: **404 Expected** (Draft → No nesting yet)

Ez szándékos design: nesting sheet csak Finalized/Published terveknél létezik.

**Indokás:**
- Nesting csak ready-to-manufacture terveknél értelmes
- Draft terveknél nincs nesting data
- 404 helyes response

**Teendő:** Nincs backend fix szükséges. Frontend UX fejlesztés (MSG-PORTAL-012).

---

### Opció 2: **404 Unexpected** (Backend Bug)

Terv ID vagy sheet ID eltérés DB-ben, vagy RLS/permission hiba.

**Indokás:**
- Test-admin Draft terveinek nesting sheet-ét nem látja/nem létezik
- Backend bug valamilyen nesting sheet creation/retrieval során

**Teendő:** Diagnózis szükséges:
1. Terv ID (`d59001d1-b61f-4b7c-bcec-2c62f46160ae`) vizsgálata
2. Van-e nesting sheet a DB-ben ennek a tervnek?
3. RLS/permission filter blokkolja-e az access-t?

---

## DoD (Opció választás alapján)

**Opció 1 (404 expected):**
- [ ] Dokumentáció: nesting endpoint spec (csak finalized terveknél)
- [ ] Outbox: MSG-CUTTING-020-DONE (Design confirmed)

**Opció 2 (Backend bug):**
- [ ] Diagnózis: test plan DB record + nesting sheet ellenőrzése
- [ ] Fix (ha szükséges): permission, RLS, vagy data integrity
- [ ] Outbox: MSG-CUTTING-020-DONE (bug fixed vagy design clarification)

---

## Frontend UX fejlesztés (Mindkét opcióban)

Lásd: MSG-PORTAL-012 — Loading spinner + error message

---

**Skill:** `/spaceos-cutting` — database diagnózis, permission audit

**Status:** UNREAD — CUTTING terminál válasza szükséges
