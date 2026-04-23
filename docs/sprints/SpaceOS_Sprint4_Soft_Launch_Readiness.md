# SpaceOS Sprint 4 — Soft Launch Readiness

**Státusz:** ACTIVE  
**Létrehozva:** 2026-04-17  
**Cél:** Tech debt tisztítás + audit chain fix + E2E regresszió kizárás → Q2 Soft Launch GO

---

## Kontextus

Sprint 3 CLOSED_DONE: teljes üzleti logika UI live (portal.joinerytech.hu).
233/233 E2E zöld. Összesített tesztszám: 2526.

Sprint 4 célja: az utolsó tech debt-ek kiiktatása és éles indulás előtti validáció.

---

## Feladatok (cross-project sorrend)

### 1. KERNEL-070 — Audit chain hash mismatch vizsgálat
**Terminál:** KERNEL  
**Prioritás:** P0 — escrow/WORM előfeltétel  
**Feladat:** A `verify-chain isValid=false` concurrent writes után fellépő bug diagnosztikája és javítása.
Az advisory lock race condition azonosított, de fix még nem deployed.

### 2. PROCUREMENT-007 — /healthz endpoint
**Terminál:** PROCUREMENT  
**Prioritás:** P1 — monitoring feltétel  
**Feladat:** `GET /healthz` AllowAnonymous health endpoint hozzáadása (Kernel minta: KERNEL-080).

### 3. PORTAL-011 — api-client tsconfig fix
**Terminál:** PORTAL (FE)  
**Prioritás:** P2 — CI/CD feltétel  
**Feladat:** `packages/@spaceos/api-client/tsconfig.json` → `"types": ["vite/client"]` hozzáadás.
`turbo build` 0 error legyen.

### 4. ORCH-081 — Rate limit vizsgálat
**Terminál:** ORCH  
**Prioritás:** P1 — prod viselkedés  
**Feladat:** `27-rate-limit` E2E teszt "GET requests are not rate limited" bukása után:
diagnosztika, hogy a GET route-ok is rate limitáltak-e az ORCH-077..080 után.
Ha igen: konfigurációs fix (GET route-ok mentesítése).

### 5. INFRA deploy-ok
**Terminál:** INFRA  
**Feladat:** KERNEL-070 + PROCUREMENT-007 kód kész után deploy a VPS-re.

### 6. E2E-049 — Full rerun post-Sprint 3 + Sprint 4
**Terminál:** E2E  
**Prioritás:** P0  
**Feladat:** Teljes 233+ teszt rerun, beleértve a Sprint 3 UI deploy-t és Sprint 4 fixeket.
Minden zöld = Soft Launch GO.

---

## Sorrend

```
Kernel-070 ──→ INFRA deploy ──→ E2E-049
Procurement-007 ──→ INFRA deploy ─┘
Portal-011 (párhuzamos) ─────────┘
Orch-081 (párhuzamos) ──→ ha fix kell → INFRA deploy ─┘
```

---

## Sikerkritérium

- 0 nyitott P0/P1 tech debt
- Audit chain mismatch megoldva VAGY dokumentáltan elfogadott kockázat
- 233+ E2E zöld
- `turbo build` clean
- Minden service /healthz 200
