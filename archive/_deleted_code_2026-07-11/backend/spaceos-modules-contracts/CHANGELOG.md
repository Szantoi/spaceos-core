# Changelog — SpaceOS.Modules.Contracts

All notable changes to this package are documented here.
Versioning follows [SemVer](https://semver.org/): MAJOR = breaking interface change · MINOR = new method · PATCH = DTO field / doc.

---

## [1.3.0] — 2026-04-20

### Added — Growth Strategy Extension Points (FreeTier/PartnerTier)

**`SourceChannel`** — new enum (`Shared/SourceChannel.cs`):
- `Direct = 0` — authenticated tenant user via Portal or BFF.
- `FreeTier = 1` — anonymous workspace, rate-limited by IP/session.
- `Partner = 2` — B2B2C embedded flow via partner integration.
- `Api = 3` — programmatic ERP/API integration (future).

**`ProviderCapability`**:
- `CuttingAnonymous = 1 << 12` — provider supports anonymous/unauthenticated cutting sheet submission.

**`AnonymousSheetRequest`** — new DTO (`Cutting/Requests/AnonymousSheetRequest.cs`):
- Wraps `SubmitCuttingSheetRequest` with channel metadata (`Source`, `PartnerId`, `BrandingContextId`, `SessionFingerprint`).
- SEC-10: Lines ≤ 50 (server-side enforced). SEC-11: Partner source requires registered PartnerId.

**`ICuttingProvider`** — 1 new Default Interface Method:
- `SubmitAnonymousSheetAsync(AnonymousSheetRequest, CancellationToken)` — throws `NotSupportedException` by default; providers opt-in by overriding.

### Security
- SEC-05: Consumer MUST check `CuttingAnonymous` capability flag before calling `SubmitAnonymousSheetAsync`.
- SEC-07: `SessionFingerprint` is IP hash only — NOT stored in audit trail, max 128 chars, no PII.
- SEC-10: Anonymous sheet line limit (50) enforced server-side.
- SEC-11: Partner channel requires registered PartnerId UUID; 403 on unknown.

---

## [1.2.0] — 2026-04-18

### Added — Reservation API (Track A · v4.1 Amendment)

**`ProviderCapability`**
- `InventoryReservation = 1 << 11` — new capability flag for soft stock reservation.

**`IInventoryProvider`** — 3 new methods (additive, backward-compatible):
- `ReserveAsync(ReserveStockRequest, CancellationToken)` — soft reservation with TTL, idempotent on active correlations.
- `ReleaseReservationAsync(Guid correlationId, string? reason, CancellationToken)` — explicit release; no-op on terminal states.
- `GetReservationsAsync(ReservationFilter, CancellationToken)` — filtered query with DoS guard.

**New types:**
- `Inventory/Enums/ReservationStatus` — `Active`, `Released`, `Expired`, `Consumed`.
- `Inventory/Requests/ReserveStockRequest` + `ReserveItemRequest` — input DTOs (no TenantId, SEC-01).
- `Inventory/DTOs/ReservationDto` + `ReservationItemDto` + `ReservationFilter` — output / query DTOs.
- `Inventory/Events/StockReserved` · `ReservationReleased` · `ReservationExpired` · `ReservationConsumed` — all carry SEC-03 XML doc warning.
- `Inventory/Validation/ConsumerContextJsonSchema` — shared XSS + PII regex patterns (SEC-07, SEC-09).

### Security
- SEC-03: All 4 reservation events carry XML doc: `Consumers MUST verify Event.TenantId matches their JWT TenantId`.
- SEC-07 / SEC-09: `ConsumerContextJsonSchema` provides `XssPattern` and `PiiPattern` regex for 3-layer content validation.
- SEC-13: `ConsumerModule` allowlist enforced by implementation (registered via `IModuleRegistry`).

---

## [1.0.0] — 2026-04-15

### Added — Initial release

- `IModuleProvider` — base interface with `ProviderName`, `Capabilities`, `HealthCheckAsync`.
- `ProviderCapability` — `[Flags]` enum with 11 values (bits 0–10).
- `ModuleEvent` — abstract base record with auto-generated `EventId` (CD-03).
- `ICuttingProvider` — 6 methods (Submit, Get, GetBySource, Nesting, Execution, Waste).
- `IInventoryProvider` — 6 methods (GetStock, GetOffcuts, RecordConsumption, RecordOffcut, RecordInbound, GetTrend).
- `IProcurementProvider` — 5 methods (CreatePO, GetPO, GetPrices, RecordDelivery, GetRating).
- 48+ DTOs, 8 enums, 11 domain events across Cutting, Inventory, Procurement domains.
