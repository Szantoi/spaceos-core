# SpaceOS — Kódbázis összesített állapotleírás

**Utolsó frissítés:** 2026-04-28 — **~5009 teszt** · 6 LIVE service · 5 domain · Lynis ~85 · Cabinet 0.3 (719) · Cutting Phase 5 (719)
**Környezet:** VPS prod (109.122.222.198) — nginx (HTTPS) → Orchestrator → Kernel
**Archívum:** Korábbi részletes sprint-napló → [`docs/codebase-history/`](codebase-history/)

---
s
## Rendszer architektúra

```
Browser  https://joinerytech.hu / portal.joinerytech.hu / eszkozok.joinerytech.hu
  │
  ▼
L5  Nginx       (TLS 1.3 · HSTS · CSP · rate limit)         port 443
  ▼
L4  Portals     Design Portal · Doorstar Portal · FreeTier    static (nginx)
  ▼
L3  Orchestrator (Node.js 22 · Express · TS)                  port 3000 (PM2)
  │  /bff/api/* → Kernel · /bff/joinery/* → Joinery · /bff/cutting/* → Cutting
  ▼
L2  Modules     Kernel · Joinery · Cutting · Inventory · Procurement · FreeTier · PartnerTier
  ▼
L1  PostgreSQL 16 (port 5433) · Redis 7.4 · MinIO (WORM)
  ▼
External        Keycloak 24.0 (IdP) · Nesting.Algorithms (NuGet) · Cabinet (NuGet)
```

---

## Service-ek

| Service | Port | Tesztek | Státusz | Utolsó változás |
|---|---|---|---|---|
| **Kernel** | 5000 | **1161** | DEPLOYED | KERNEL-104 email-hash lookup |
| **Orchestrator** | 3000 | **227** | DEPLOYED | ORCH-084 Cutting ingest route |
| **Joinery** | 5002 | **389** | DEPLOYED | Phase 3 MinIO PublicEndpoint |
| **Abstractions** | 5003 | **81** | DEPLOYED | BFS cycle detection |
| **Inventory** | 5004 | **164** | DEPLOYED | Reservation + Offcut batch |
| **Cutting** | 5005 | **719** | DEPLOYED | Phase 3+4+5 ✅ · Execution + Analytics (OEE, k-anon, P4-4/P4-9 debt) |
| **Procurement** | 5006 | **53** | DEPLOYED | Address field migration |
| **FreeTier** | 5010 | **179** | LIVE | Brevo+Turnstile production-ready |
| **PartnerTier** | 5011 | **232** | DEPLOYED | MVP: embed + lead + commission + GDPR |

## Portals

| Domain | App | Tesztek | Repo |
|---|---|---|---|
| **joinerytech.hu** | Design Portal (Turborepo) | **323** | `design-portal` |
| **portal.joinerytech.hu** | Doorstar Portal | **99** | `spaceos-doorstar-portal` |
| **asztalostech.hu** | Design Portal (HU brand) | = joinerytech.hu | = |
| **eszkozok.joinerytech.hu** | FreeTier nesting kalkulátor | **75** | `spaceos-freetier-portal` |
| **freetier.joinerytech.hu** | FreeTier API | (API, no UI) | `spaceos-freetier-api` |

## NuGet Libraries

| Csomag | Tesztek | Verzió | Leírás |
|---|---|---|---|
| **Cabinet** (10 csomag) | **719** | 0.3.0 | Asztalosipari domain motor — Federation + TenantStandard + Channel<T> RuleEngine |
| **Contracts** | **57** | 1.3.0 | Modul-közi interfészek + DTO-k |
| **Nesting.Algorithms** | **32** | 1.1.0 | FFDH + Guillotine nesting |

## E2E + Egyéb

| Komponens | Tesztek |
|---|---|
| **E2E** | **277** (59 fájl, Vitest) |
| Reservation Contracts | 21 |

---

## Összesített tesztszám: ~5009

```
Kernel 1161 + Orchestrator 227 + Portal 323 + Doorstar 99 + Joinery 389 +
Abstractions 81 + Cutting 719 + Inventory 164 + Procurement 53 + Contracts 57 +
Nesting 32 + Reservation 21 + E2E 277 + FreeTier API 179 + FreeTier Portal 75 +
Cabinet 719 + PartnerTier 232 = 5009
```

---

## VPS Security (Lynis ~85)

| Batch | Tartalom | Státusz |
|---|---|---|
| Batch 1 | Docker bind fix + Keycloak loopback + SSH hardening + apt upgrade | ✅ |
| Batch 2 | PG chmod + Redis CONFIG rename + protocol disable + fail2ban + umask | ✅ |
| Batch 3 | auditd + needrestart + rkhunter + sysstat | ✅ |
| spaceos_schema_owner | FreeTier RLS FORCE fix | ✅ |
| CabinetBilder | Keycloak client (Device Code Flow, 6 mapper) | ✅ |

---

## Lezárt nagy mérföldkövek

| Mérföldkő | Dátum | Tesztek |
|---|---|---|
| Soft Launch GO (Doorstar Kft.) | 2026-04-20 | ~3028 |
| FreeTier LIVE (eszkozok.joinerytech.hu) | 2026-04-23 | ~3565 |
| Joinery Phase 3 VALIDATED | 2026-04-24 | ~3600 |
| Cabinet 0.1 COMPLETE | 2026-04-25 | 301 |
| Cutting Phase 4 Execution | 2026-04-27 | 496 |
| Cabinet 0.2 COMPLETE | 2026-04-26 | 518 |
| Growth Strategy v1 — 100% COMPLETE | 2026-04-27 | ~4688 |
| PartnerTier DEPLOYED | 2026-04-27 | 232 |
| Cabinet 0.3 COMPLETE | 2026-04-28 | 719 |
| **Cutting Phase 5 Analytics COMPLETE** | **2026-04-28** | **719 (Cutting total)** |

---

## Operátori teendők

| # | Feladat | Ki |
|---|---|---|
| Brevo API key regisztráció | Gábor | [brevo.com](https://brevo.com) → "Transactional" → API key |
| Turnstile site key + secret | Gábor | [Cloudflare Turnstile](https://dash.cloudflare.com) |
| PartnerTier nginx vhost | INFRA task (ha publikus domain kell) |

---

## Archívum

| Fájl | Tartalom |
|---|---|
| [`codebase-history/Codebase_Status_2026-04-27_full.md`](codebase-history/Codebase_Status_2026-04-27_full.md) | Teljes sprint-napló 2026-03-31 → 2026-04-27 (1476 sor) |
| [`docs/tasks/archive/`](tasks/archive/) | Lezárt tervdokok és task fájlok |
| [`docs/mailbox/`](mailbox/) | Teljes terminál kommunikáció (inbox/outbox/archive) |
| [`docs/knowledge/`](knowledge/) | Szintetizált tudásbázis (18 fájl) |
