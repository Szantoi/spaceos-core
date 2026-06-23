---
domain: manufacturing
segment: kernel-memory
type: endpoint_gap
priority: medium
created: 2026-06-16
---

# WorkStation Trust Provisioning Endpoint

## Mit old meg

Manufacturing context: egy új gépet (WorkStation — labeling station, CNC, laser cutter) berakják a szalonba. Eddig az E4 auth csak API user-ek JWT-t kezelnek. WorkStation (device) előtt nincs olyan kernel endpoint, mely:
1. Device identity-t regisztrál (MAC, serial number, certificate)
2. Tenant-hoz köti (csak az adott cég munka olvasható)
3. Offline JWT-t generál (shop floor gyak offline)

Ma: WorkStation TenantId denormalizáció (E4 T4), de **nincs provisioning** — device blind joins, vagy hardcoded tenant key-vel (security risk).

## Jelenlegi állapot

- **E4 ITenantResolver** — HTTP JWT claims-ből olvassa a tid; WorkStation-ön offline nincs claim
- **E4 T4 WorkStation.TenantId** — DB column meglétezik, de API-n nincs endpoint mely kitölti
- **Security Sprint RS256** (MSG-K014) — asymmetric JWT, de device key provisioning nincs definiálva

## Bekötési lehetőség

1. **POST /api/workstations/provision** — idempotent: `{ tenantId, deviceId, deviceType, serialNumber }`
   - Returns: offline JWT + public key + refresh schedule
   - Requires: TenantAdmin role + ApiKey authentication (first-time setup)

2. **WorkStation domain entity** — extend E4 denormalizáció
   - Add: `OfflineJwtSecret` (RSA private key, encrypted at rest)
   - Add: `LastSyncedAt`, `Status` (Provisioned|Revoked|Expired)

3. **WorkStationProvisioningHandler** — command:
   - Verify tenant admin claim
   - Generate RSA key pair (device keeps private, kernel stores public in audit)
   - Create WorkStation + audit event (`WorkStationProvisioned`)
   - Return offline JWT template + key rotation schedule

4. **WorkStationRevokeCommand** — POST `/api/workstations/{id}/revoke`
   - Immediate: offline JWT invalidity (public key removed)
   - Log: `WorkStationRevoked` audit event (forensics)

## Iparági relevancia

- **Doorstar workshop** — 5+ labeling/cutting stations, minden offline-first capability
- **Manufacturing IoT** — device trust bootstrap az iparban standard (OPC-UA, MQTT mutual TLS)
- **Soft Launch blockers** — device provisioning a shop floor onboarding-nak alapja

## Érintett modulok

- Kernel Domain: `WorkStation` aggregate extend (OfflineJwtSecret, Status)
- Kernel API: POST `/api/workstations/provision`, POST `/{id}/revoke`
- Kernel Infrastructure: RSA key pair generation, encrypted storage
- E4 update: ITenantResolver offline fallback (local key loading)
- Integration tests: provisioning → offline JWT verify → revoke chain

## Blocker

None — E4 auth infra + E6 DB + E10 OpenAPI ready.

## Megjegyzés

Ez ne lehessen a REST API-ból közvetlenül — csak **ServiceAccount** + **TenantAdmin** össze.
QR-code alapú device pairing (opcional Phase 2) vagy manual key scanning.
