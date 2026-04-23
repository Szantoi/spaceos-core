# SpaceOS Sprint 5 — Doorstar Production Sprint

**Dátum:** 2026-04-17  
**Státusz:** ACTIVE  
**Előfeltétel:** Sprint 4 CLOSED_DONE · E2E 233/233 · Soft Launch GO ✅

---

## Kontextus

A Doorstar Q2 Soft Launch gate teljesült (233/233). A rendszer működik end-to-end.
Sprint 5 célja: **valódi Doorstar felhasználók számára élesíthető állapot** — branding, PDF export,
OpenAPI infrastructure, és production user onboarding.

A backlog JOINERY-V2 (PDF Gyártásilap) most feloldódott: "Doorstar pilot live" blokkoló teljesült.

---

## Feladatok (párhuzamosan futtatható)

### JOINERY-015 — PDF Gyártásilap export (JOINERY-V2 első fázis)

**Terminál:** joinery  
**Prioritás:** critical  
**Blokkoló:** —

A Doorstar számára a legfontosabb termelési adat: a gyártásilap PDF, amit a műhelyben használnak.
JOINERY-V2 backlog első fázisa: az elkészült rendelésből PDF export.

**Elvárt:**
- `GET /api/orders/{id}/manufacturing-sheet` → PDF response (`application/pdf`)
- Fejléc: ügyfél neve, cím, rendelés dátuma, szállítási határidő
- Tételek táblázat: ajtószám, típus, méretek, anyag, felület, megjegyzés
- Összesítő: darabszám, anyagnormák
- Tesztszám: ≥ 230 (jelenlegi 219 + min. 11 új)

---

### PORTAL-012 — Doorstar design tokens + brand rendszer

**Terminál:** portal (spaceos-doorstar-portal)  
**Prioritás:** high  
**Blokkoló:** —

A portal jelenleg nincs branding. Sprint 5 után Doorstar-specifikus design tokenek élnek.

**Elvárt:**
- Tailwind `theme.extend.colors`: `primary` (#3B5AD8), `secondary` (#1E293B), `accent` (#F59E0B)
- Typography: Inter font (Google Fonts vagy lokális)
- Komponensek frissítve (gombok, badge-ek, nav) az új tokenekkel
- Favicon + Doorstar logó placeholder (SVG, ha nincs végleges: "D" monogram)
- Tesztszám: ≥ 306 (nincs regresszió)

---

### KERNEL-087 — OpenAPI build-time JSON generálás

**Terminál:** kernel  
**Prioritás:** high  
**Blokkoló:** —

A Swagger spec jelenleg csak Development módban érhető el. Build-time generálással
a spec verziókövetett lesz, és TypeScript client generálás alapja lehet (ADR-07).

**Elvárt:**
- `dotnet swagger tofile` integrálva a publish folyamatba
- Output: `publish/openapi-v1.json`
- Nginx vagy statikus endpoint: `/openapi/v1.json` → 200 Production módban is
- Tesztszám: ≥ 1122 (nincs regresszió)

---

### INFRA-150 — Doorstar production user onboarding

**Terminál:** infra  
**Prioritás:** high  
**Blokkoló:** —

Valódi Doorstar felhasználók Keycloak-ban. A Soft Launch GO után az első lépés
tényleges accountok létrehozása a megfelelő szerepkörökkel.

**Elvárt:**
- Keycloak `spaceos` realm: legalább 1 `Admin` + 1 `Designer` szerepkörű Doorstar user
- Userek a `doorstar-kft` group-ba tagolva (tid claim automatikusan jön)
- Realm export frissítve (`infra/keycloak/realm-export.json`)
- Dokumentáció: user credentialok biztonságos átadási módja (nem gitben!)

---

## Cross-project sorrend

```
JOINERY-015  ─────────────────────────────► DONE
PORTAL-012   ─────────────────────────────► DONE
KERNEL-087   ─────────────────────────────► DONE
INFRA-150    ─────────────────────────────► DONE
                                              │
                                              ▼
                                        E2E smoke rerun (opcionális)
```

Mind a 4 feladat párhuzamosan futhat — nincsenek egymástól függő deliverable-ök.

---

## Build gate-ek

| Terminál | Gate |
|---|---|
| Joinery | `dotnet test` ≥ 230 pass, 0 fail |
| Portal | `pnpm test` ≥ 306 pass, 0 fail · `pnpm build` 0 error |
| Kernel | `dotnet test` ≥ 1122 pass, 0 fail · spec file generálódik |
| Infra | Keycloak UI-on userök láthatók · `tid` claim a tokenben |

---

## DONE feltételek (sprint lezáráshoz)

- [ ] JOINERY-015: PDF endpoint él, ≥ 230 teszt
- [ ] PORTAL-012: brand tokenek alkalmazva, ≥ 306 teszt
- [ ] KERNEL-087: `/openapi/v1.json` Production módban is 200
- [ ] INFRA-150: Doorstar userök Keycloak-ban, realm-export frissítve

---

## Backlog — következő sprintbe

| ID | Feladat | Miért vár? |
|---|---|---|
| ESCROW-GA | S3/Azure WORM Object Lock | Infrastruktúra döntés szükséges |
| JOINERY-V2 fázis 2 | Hardverlista PDF, Anyagnorma export | JOINERY-015 DONE után |
| API TypeScript client | NSwag/openapi-typescript generálás | KERNEL-087 DONE után |
| Project aggregate | DDD design: FlowEpic elég? Vagy új aggregate? | Design session kell |
| PM/Workshop szerepkörök | Keycloak új role-ok + Kernel policy | Doorstar igény pontosítása |
| CABINET-V1 | Modules.Cabinet v1 | Joinery v2 DoD után |
