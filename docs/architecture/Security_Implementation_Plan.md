# SpaceOS Security Implementation Plan

**Forrás:** `SpaceOS_Security_Task_Register.md`
**Készült:** 2026-04-02
**Összesen:** 21 task, ~47 fejlesztői nap

---

## Sprint bontás és réteg felelősségek

### Sprint 0 — Kritikus alapok (1 hét)

| ID | Task | Réteg | Effort | Mailbox |
|---|---|---|---|---|
| P0-1 | JWT HS256 → RS256/ES256 + Key Vault | Kernel | 3 nap | MSG-K014 |
| P0-3 | Hash chain race condition → serialized write | Kernel | 2 nap | MSG-K014 |
| P0-4 | PostgreSQL audit role szeparáció | Kernel | 1 nap | MSG-K014 |
| P1-7 | Threat Model dokumentum (STRIDE) | Docs | 2 nap | MSG-D001 |

### Sprint 1 — External trust boundary (2 hét)

| ID | Task | Réteg | Effort | Mailbox |
|---|---|---|---|---|
| P0-2 | Hash chain external write-only sink | Kernel | 4 nap | MSG-K015 |
| P1-1 | TenantId HTTP header → JWT claim | Kernel | 2 nap | MSG-K015 |
| P1-2 | ExternalAuthToken → Key Vault ref | Kernel | 3 nap | MSG-K015 |
| P1-5 | IntentDataJson JSON Schema validáció | Kernel | 3 nap | MSG-K015 |

### Sprint 2 — Snapshot & identity (2 hét)

| ID | Task | Réteg | Effort | Mailbox |
|---|---|---|---|---|
| P1-3 | AggregateSnapshot entitás + tábla | Kernel | 3 nap | MSG-K016 |
| P1-4 | SnapshotService: Outbox Pattern | Kernel | 4 nap | MSG-K016 |
| P1-6 | Rate Limiting: Identity alapú + Redis | Kernel | 2 nap | MSG-K016 |
| P1-8 | ProofUrl → ProofHash + WORM storage | Kernel | 3 nap | MSG-K016 |

### Sprint 3 — Verification & monitoring (1.5 hét)

| ID | Task | Réteg | Effort | Mailbox |
|---|---|---|---|---|
| P2-1 | Chain Integrity Verifier endpoint | Kernel | 2 nap | MSG-K017 |
| P2-2 | Snapshot Query API | Kernel + Portal | 2 nap | MSG-K017 + MSG-P010 |
| P2-4 | Audit alerting + anomaly detection | Kernel | 2 nap | MSG-K017 |
| P2-5 | Genesis Hash deployment-time gen | Kernel | 1 nap | MSG-K017 |

### Sprint 4 — Compliance (1.5 hét)

| ID | Task | Réteg | Effort | Mailbox |
|---|---|---|---|---|
| P2-3 | GDPR pseudonymizáció + PII szeparáció | Kernel | 4 nap | MSG-K018 |
| P2-6 | Crypto algoritmus migration plan | Kernel | 2 nap | MSG-K018 |

### Roadmap (Q3+)

| ID | Task | Réteg | Effort |
|---|---|---|---|
| P3-1 | RFC 3161 TSA timestamping | Kernel | 5 nap |
| P4-1 | Blockchain hash anchoring (Polygon L2) | Kernel | 8 nap |
| P4-2 | Event Sourcing full átállás | Kernel | 20+ nap |

---

## Réteg összesítés

| Réteg | Taskek | Effort |
|---|---|---|
| Kernel (L2) | 19 task | ~43 nap |
| Docs | 1 task (P1-7) | 2 nap |
| Portal (L4) | 1 task (P2-2 UI) | 2 nap |
| Orchestrator (L3) | 0 task | — |
