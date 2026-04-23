# SpaceOS Sprint 6 — ESCROW + TypeScript Client + Joinery V2

**Dátum:** 2026-04-17  
**Státusz:** ACTIVE  
**Előfeltétel:** Sprint 5 CLOSED_DONE + INFRA-151 deploy ✅

---

## Kontextus

Sprint 5 teljesen deployed. Három backlog item most unblockolt:
- **JOINERY-V2 fázis 2** — PDF export folytatása (hardverlista, anyagnorma)
- **TypeScript API client** — KERNEL-087 DONE után a `/openapi/v1.json` elérhető → generálás lehetséges
- **ESCROW-GA** — "Doorstar pilot live" feltétel teljesült → S3/WORM audit log most kiadható

A **Project aggregate** design session döntésre vár — Sprint 6-ba nem kerül be.

---

## Feladatok

### Track A — Párhuzamos (JOINERY + PORTAL)

#### JOINERY-016 — JOINERY-V2 fázis 2: Hardverlista + Anyagnorma PDF

**Terminál:** joinery  
**Prioritás:** high

A meglévő JSON endpointok (`/hardware-list`, `/material-req`) PDF változatai.
Sprint 5 QuestPDF pattern folytatása.

**Elvárt:**
- `GET /api/orders/{id}/hardware-list-pdf` → `200 application/pdf`
  - Hardverlista: ajtóvasalat, pántok, zárak, fogantyúk tételenként
- `GET /api/orders/{id}/material-req-pdf` → `200 application/pdf`
  - Anyagnorma: nyersanyag, lap, él, felület összesítve
- Cache-Control: private, no-store (mint a manufacturing-sheet)
- Tesztszám: ≥ 245 (jelenlegi 231 + min. +14 új)

---

#### PORTAL-013 — TypeScript API client generálás

**Terminál:** portal (design-portal monorepo)  
**Prioritás:** high

A `/openapi/v1.json` spec alapján auto-generált TypeScript típusok és fetch client
a `packages/@spaceos/api-client` package-be. Ezután a Doorstar Portal importálhatja
a típusokat ahelyett, hogy kézzel definiálná.

**Elvárt:**
- `packages/@spaceos/api-client/src/generated/` — auto-generált fájlok
- Generáló script: `pnpm run generate:api` (package.json-ban)
- Spec forrás: `docs/openapi/kernel-v1.json` (gitben lévő, stabil)
- Ajánlott tool: `openapi-typescript` + `openapi-fetch` (MIT, lightweight)
- A `@spaceos/api-client` exportálja a típusokat: `export type { paths, components }`
- `turbo build` továbbra is zöld
- Tesztszám: ≥ 306 (nincs regresszió)

---

### Track B — Szekvenciális (INFRA → KERNEL)

#### INFRA-152 — MinIO setup VPS-en

**Terminál:** infra  
**Prioritás:** high  
**Blokkoló:** —

MinIO object storage telepítése a VPS-re az ESCROW-GA alapjaként.
Az audit eventek WORM tárolásához szükséges.

**Elvárt:**
- MinIO standalone deployed (`/opt/minio/` vagy systemd service)
- Port: `9000` (API) + `9001` (Console) — loopback-only
- Bucket: `spaceos-audit-worm` Object Lock engedélyezve
- Access/Secret key: `/etc/spaceos/minio.env`-ben
- `curl http://127.0.0.1:9000/minio/health/live` → 200
- OUTBOX DONE: endpoint URL + bucket neve + access key (secret NEM gitbe)

---

#### KERNEL-088 — ESCROW-GA: Audit event WORM streaming (INFRA-152 DONE után)

**Terminál:** kernel  
**Prioritás:** critical  
**Blokkoló:** INFRA-152 DONE

Az audit chain eseményeket az immutable MinIO bucket-be is írja a Kernel,
megvalósítva a WORM (Write Once Read Many) escrow követelményt.

**Elvárt:**
- `IAuditEscrowWriter` interfész (Domain/Infrastructure)
- `MinioAuditEscrowWriter` implementáció: minden `AuditEvent` → MinIO object
  - Object key: `{tenantId}/{year}/{month}/{eventId}.json`
  - Object metadata: `audit-chain-hash`, `created-at`
- Background: az audit write pipeline részeként fut (nem külön scheduled job)
- Idempotens: ha az object már létezik, nem ír felül (PUT with `if-none-match: *`)
- Tesztszám: ≥ 1135 (jelenlegi 1122 + min. +13 új)

---

## Cross-project sorrend

```
JOINERY-016  ─────────────────────────────────► DONE
PORTAL-013   ─────────────────────────────────► DONE
INFRA-152    ──────────────► DONE
                               │
                               ▼
                           KERNEL-088 ─────────► DONE
                                                   │
                                                   ▼
                                             INFRA deploy
                                             (KERNEL + JOINERY)
```

---

## Build gate-ek

| Terminál | Gate |
|---|---|
| Joinery | `dotnet test` ≥ 245 pass, 0 fail |
| Portal | `pnpm build` 0 error · `pnpm test` ≥ 306 pass |
| Infra (MinIO) | `curl` health 200 · bucket Object Lock ON |
| Kernel | `dotnet test` ≥ 1135 pass, 0 fail |

---

## DONE feltételek (sprint lezáráshoz)

- [ ] JOINERY-016: hardverlista + anyagnorma PDF endpointok, ≥ 245 teszt
- [ ] PORTAL-013: `@spaceos/api-client` generált típusok, `pnpm run generate:api` fut
- [ ] INFRA-152: MinIO live, `spaceos-audit-worm` bucket Object Lock ON
- [ ] KERNEL-088: `MinioAuditEscrowWriter` deployed, WORM write verified
- [ ] INFRA deploy: Kernel + Joinery VPS-en

---

## Backlog — következő sprintbe

| ID | Feladat | Miért vár? |
|---|---|---|
| Project aggregate | DDD design session: FlowEpic elég? | Döntés szükséges |
| PM/Workshop szerepkörök | Keycloak + Kernel policy | Doorstar igény pontosítása |
| CABINET-V1 | Modules.Cabinet v1 | Joinery v2 DoD után |
| ABSTRACTIONS-C | Phase C-Geometry | — |
