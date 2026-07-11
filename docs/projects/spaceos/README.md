# SpaceOS Platform

> Magyar faipar digitális gerince

## Vision

A SpaceOS egy iparspecifikus SaaS platform, amely az ajtógyártókat, szekrénygyártókat,
lapszabászokat, kereskedőket és beszerelőket egyetlen összekapcsolt ökoszisztémába szervezi.

## Goals

1. **Doorstar Soft Launch** - 2026 Q2
2. **Szabászat modul + 2. ügyfél** - 2026 Q3
3. **5+ éles ügyfél, DACH belépés** - 2027

## Architecture

```
L4  Design Portal / JoineryTech   React 18 — brand-specifikus UI-k
L3  Orchestrator (BFF)            Node.js 22 — LLM Tool Calling, API gateway
L2  Modules (Drivers)             .NET 8 — iparági üzleti logika
L1  Kernel                        .NET 8 + PostgreSQL — auth, audit, FSM, escrow
```

## 5 Golden Rules

1. **Data → Rules → Geometry** — frontend rajzol, C# Driver számol
2. **Modular Monolith** — Kernel interfészen dolgozik
3. **Immutability & Trust** — nincs UPDATE, SHA-256 audit
4. **Need-to-Know RBAC** — megrendelő nem látja a gyártó BOM-ját
5. **Walking Skeleton First** — E2E pipeline előbb

## Active Projects

- **Sales Module** - Ajánlatok, pipeline kezelés (Doorstar launch)
- **CRM Module** - Ügyfélkezelés
- **Cutting Module** - Lapszabászat, nesting optimalizáció
- **Identity Module** - SSO, Keycloak integráció
