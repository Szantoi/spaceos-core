---
domain: manufacturing
segment: kernel-memory
type: feature_gap
priority: high
created: 2026-06-16
---

# Offline Audit Trail Resilience

## Mit old meg

Manufacturing gyári körülmények: intermittent WiFi, sensor-read-ok folyamatosan. Ma az OfflineQueue (MSG-K025) az intent-eket tárja offline módban, de a domain event-ek — melyeket az E8 audit trail követne — **szerver-szintűek és online kell hogy legyenek**. Ha 30 percre leáll a WiFi, az audit event-ek gap keletkezik.

Megoldás: **Local audit event cache** a WorkStation-ön offline módban; sync-on-reconnect az audit trail-ba (append-only, chain maintained).

## Jelenlegi állapot

- **E8 Audit Log** — immutable append-only log, SHA-256 chain, TenantId scoped
- **MSG-K025 OfflineQueue** — local intent storage offline módban, HTTP sync online
- **Kernel gap** — nincs local audit event storage, offline domain events ~ lost forever

## Bekötési lehetőség

1. **AuditOfflineCache** — Infrastructure layer, SQLite/LevelDB local cache (WorkStation.OfflineStorage)
2. **DomainEventDispatcher.OnEventAsync** — offline check: `if (!HasConnectivity) { await _auditOfflineCache.AppendAsync(event); return; }`
3. **SyncAuditCacheCommand** — new API endpoint POST `/api/sync-offline-audit-events` (batched, idempotent, PreviousHash chain verification)
4. **Migration** — add `OfflineAuditEvents` table (WorkStationId, EventType, Payload, LocalHash, SyncedAt)

## Iparági relevancia

- **Doorstar Soft Launch (2026 Q2)** — gyári WiFi spotty, szenzor adatok elég fontosak hogy auditálni kell
- **Offline-first manufacturing** — NCR, Quality gate-ek offline is megtörténnek, majd sync-olódnak
- **Audit compliance** — GDPR / iparági audit: ha event gap van, compliance rejtett nyomma

## Érintett modulok

- Kernel: AuditOfflineCache, Program.cs ServiceCollection regisztráció
- Infrastructure: SQLite OfflineStorage (hasonló MSG-K025 pattern-hez)
- API: POST `/api/sync-offline-audit-events` endpoint, idempotent handler
- Integration tests: offline scenario (connectivity mock, cache fill, sync verify)

## Blocker

Nincs — E8 audit core már DONE, SQL + migration tooling kész (E6 PostgreSQL).
