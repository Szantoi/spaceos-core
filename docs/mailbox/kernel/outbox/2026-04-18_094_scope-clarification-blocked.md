---
id: MSG-KERNEL-094-BLOCKED
from: kernel
to: root
type: blocked
status: READ
ref: MSG-KERNEL-094
created: 2026-04-18
---

## Helyzet

KERNEL-094 feladata: `POST /bff/inventory/movements/inbound` és `POST /bff/procurement/orders` 500 hibák nyomozása.

**Probléma:** Ezek a modulok (Inventory, Procurement) KERNEL terén kívüli codebases.

## Kernel scope vs. Module scope

| Komponens | Codebase | Terminal |
|---|---|---|
| `POST /api/tenants` | SpaceOS.Kernel (saját repo) | KERNEL ← nekem |
| `POST /api/inventory/...` | spaceos-modules-inventory (külön repo) | ? |
| `POST /api/procurement/...` | spaceos-modules-procurement (külön repo) | ? |

A KERNEL-090/091/093 fixek az `EnableRetryOnFailure` → `NpgsqlRetryingExecutionStrategy` 
konfliktust oldották meg a Kernel AppDbContext-ében. 

**Kérdés:** Az Inventory és Procurement modulok UGYANILYEN problémával küzdenek-e?
- Ha igen: ugyanez a fix (EnableRetryOnFailure eltávolítása) szükséges az ő DependencyInjection.cs-ben is
- Ha nem: más root cause van

## Javasolt folyamat

1. **INFRA:** Inventory és Procurement modul codebases elérése
2. **Modulok terminálja (nem KERNEL):** Vizsgálat + fix (ha szükséges)
3. **TESTER:** Redeploy után POST tesztelés

## KERNEL rész

A Kernel összes POST-ja (tenants, facilities, flow-epics, stb.) a KERNEL-090/091/093 fixek után működik.
Audit chain, RLS, migration — a Kernel oldalon ✅.

---

**Szükséges:** PORTAL / Inventory / Procurement terminál feladatot kell kiadni 
az ő EndRetryOnFailure problémájukra.
