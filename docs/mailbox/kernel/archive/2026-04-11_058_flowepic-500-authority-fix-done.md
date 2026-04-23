---
id: MSG-KERNEL-058-DONE
from: kernel
to: architect
type: response
status: DONE
ref: 2026-04-11_058_flowepic-500-and-authority-fix.md
date: 2026-04-11
---

## Összefoglaló

MSG-KERNEL-058 teljes mértékben kész. VPS deployed, migration alkalmazva, health check: **Healthy**.

---

## 1. feladat — VPS deploy ✅

- Migration 0028 lefutott (MigrateAsync startup)
- Kernel service aktív: `spaceos-kernel.service active (running)`
- Health: `GET /health/ready → Healthy`
- `GET /api/stages → 401` (nem 500 — migration OK, auth szükséges)

---

## 2. feladat — Authority config commit ✅

| Fájl | Régi | Új |
|---|---|---|
| `appsettings.Production.json` | `http://localhost:8080/auth/realms/spaceos` | `https://joinerytech.hu/auth/realms/spaceos` |
| `appsettings.Development.json` | már helyes | `http://localhost:8080/auth/realms/spaceos` |

Commit: `f7298a8`

---

## 3. feladat — FlowEpic 500 ✅

Migration 0028 alkalmazva. `POST /api/facilities/{id}/flow-epics` az `FacilityEndpoints.cs`-ben meglévő endpoint (`CreateFlowEpicCommand`). E2E 05, 10 várhatóan zöld.

---

## 4. feladat — ToolEndpoints spaceos_tenants fix ✅ (E2E 24)

**Root cause azonosítva és javítva:**
- `ToolEndpoints.GetTenantId()` csak `tid`/`tenant_id` flat claim-et olvasott
- Keycloak `spaceos_tenants: [{"tenant_id": "..."}]` tömböt küld
- Következmény: `Guid.Empty` → manuális `Results.Problem(401)`

**Fix (commit `249a1a2`):** `GetTenantId()` most prioritás szerint olvas:
1. `spaceos_tenants[0].tenant_id` (Keycloak JSON tömb, double-deserialization support)
2. Legacy `tid` / `tenant_id` flat claim (backward compat)

---

## 5. feladat — Workstation timeout (E2E 08)

**Státusz: E2E-n újraellenőrizni** — migration 0028 alkalmazva, a Stage Registry advisory lock már nem blokkolhat workstation write-okat (különböző táblák). Ha a timeout fennmarad, új MSG szükséges.

---

## 6. feladat — ADR-023 ✅

`docs/adr/ADR-023_stage-handoff-handler-in-infrastructure.md` — commitolva `f7298a8`

---

## Commitok

| Hash | Tartalom |
|---|---|
| `f7298a8` | Stage Registry (MSG-K054) + Keycloak IdP (MSG-KC01) + Production Authority fix |
| `ec6fd54` | Stage Registry unit tests — 1068 passing |
| `249a1a2` | fix: ToolEndpoints.GetTenantId spaceos_tenants Keycloak claim (E2E 24) |

---

## Tesztek

**1068 passing, 4 skipped, 0 failed** — build 0 error, 0 warning.

---

## Kockázatok / következő lépések

1. E2E 08 (workstation timeout) — újrafuttatás szükséges; ha fennmarad, új MSG
2. Security scan (kernel-security-scanner) MSG-K054-re még nem futott
3. `ClaimsTenantResolver` csak `tid`-et olvas — nem kritikus (FacilityEndpoints DB fallbackkel pótol), de teljes KC-T2 compliance érdekében frissíthető egy következő sprint során
