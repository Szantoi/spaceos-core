# Prompt template — Modul verzió-bump tervezés

> Használd ezt a template-et amikor a felhasználó **meglévő modul új major/minor verzióját** akarja tervezni (pl. Cabinet 0.2, Cutting Phase 4, Joinery Phase 4).
> Ne használd új modulhoz — arra a `new-module-design.md` template van.

---

## A kitöltendő mezők

| Mező | Forrás |
|---|---|
| `{{MODUL_NÉV}}` | Felhasználó megadja vagy kontextusból (pl. "Cabinet 0.2", "Cutting Phase 4") |
| `{{ELŐZŐ_VERZIÓ}}` | A jelenlegi LIVE verzió (pl. "Cabinet 0.1", "Cutting Phase 3") |
| `{{ELŐZŐ_V4_DOC}}` | Az előző verzió v4 architektúra-dokumentuma (pl. "SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md") |
| `{{ÚJ_SCOPE}}` | Mit ad hozzá az új verzió (axiómák, fázisok, feature-ök) |
| `{{ELŐZŐ_LIVE_TÉNYEK}}` | A jelenlegi LIVE állapot — tesztszám, NuGet-verzió, deployolt komponensek |
| `{{BREAKING_CHANGE_ÁLLÁSPONT}}` | A schema-version váltás breaking-e a fogyasztókra (Cabinet, AutoCAD, Joinery) |
| `{{KÖTELEZŐ_KÉRDÉSEK}}` | 2-3 kulcsfontosságú döntés amit a v1 előtt tisztáznunk kell |

## A prompt sablon

```
{{MODUL_NÉV}} design session indítása.

A {{ELŐZŐ_VERZIÓ}} készen van ({{ELŐZŐ_LIVE_TÉNYEK}}).
A project knowledge-ben fent van a {{ELŐZŐ_V4_DOC}} mint **alapdokumentum** és **precedens-példa**.

**{{MODUL_NÉV}} scope:**

{{ÚJ_SCOPE}}

**Becsült bázis-effort:** [a felhasználóval egyeztetve] nap, a v1→v4 review pipeline-on átfutva várhatóan [+5 nap] nap.

**Kötelező inputok a tervezéshez:**

- A {{ELŐZŐ_V4_DOC}} mint **alapdokumentum** — ezekre a típusokra ÉPÍT a {{MODUL_NÉV}}, nem újraírja őket
- A 16 axióma session-record (`SpaceOS_Cabinet_Core_Session_*.docx` ha Cabinet-szerű) — az új axiómák részletes leírása
- A {{ELŐZŐ_VERZIÓ}} jelenlegi LIVE állapota — a teszt-szám és a deployolt komponensek nem változhatnak {{MODUL_NÉV}} miatt
- A SpaceOS T1-T6 tenetek (Manifesto-ból) — kontinuitás

**Várhatóan új csomagok / bővítések:**

- [Új csomagok listája — pl. `SpaceOS.Cabinet.Catalog`, `SpaceOS.Cabinet.Assembly`]
- A meglévő `SpaceOS.{{MODUL_PREFIX}}.Domain` és más csomagok **bővítése** — a felhasználó válaszait alapul véve

**Schema-version váltás:**

A {{MODUL_NÉV}} release-szel a `*.SchemaVersion` `"{{ELŐZŐ_SCHEMA}}"` → `"{{ÚJ_SCHEMA}}"`. {{BREAKING_CHANGE_ÁLLÁSPONT}}

**Migration:** ha a {{ELŐZŐ_VERZIÓ}}-ben kibocsátott `ISnapshotMigrator` interface kell az első implementációt megkapnia: `SnapshotMigrator_{{ELŐZŐ_SCHEMA_RÖVID}}_to_{{ÚJ_SCHEMA_RÖVID}}`. A migration **forward-only, single-step, lossless**.

Kérlek indítsd a `spaceos-arch-planner` skillt, és kezdj egy v1 draft-ot `SpaceOS_{{MODUL_NÉV_FÁJLNÉV}}_Architecture_v1.md` néven. Dokumentum-konvenció: magyar próza + angol kód/identifier-ek, ugyanaz mint a {{ELŐZŐ_V4_DOC_RÖVID}}-ben. A multi-target stratégia ugyanaz marad ({{TARGET_FRAMEWORKS}}).

Mielőtt v1-et generálsz, néhány kulcsfontosságú döntést szeretnék tisztázni:

{{KÖTELEZŐ_KÉRDÉSEK}}

Egy mondatos válasz mindháromra, és indul a v1 draft.
```

## Példa kitöltött prompt — Cabinet 0.2

Ha a felhasználó: "Indítom Cabinet 0.2 design-t."

Akkor a kitöltés:

- `{{MODUL_NÉV}}` = "Cabinet 0.2"
- `{{ELŐZŐ_VERZIÓ}}` = "Cabinet 0.1"
- `{{ELŐZŐ_V4_DOC}}` = "SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md"
- `{{ÚJ_SCOPE}}`:
  > A12-A16 axiómák implementálása.
  >
  > - A12 — Horizontális szerep ambivalens (Shelf vs CrossRail, user override + catalog-default)
  > - A13 — Marketplace-bontás (Skeleton.DeriveBillOfServices() extension point)
  > - A14 — Assembly Documentation 4. derivált nézet (AssemblyStep, ExplodedView, HardwareCallout)
  > - A15 — Catalog mint federated, community-driven entity (CatalogEntry lifecycle FSM, Visibility)
  > - A16 — FlowEpic skálafüggetlenség (Scope enum bővítés: + MicroAssembly)
- `{{ELŐZŐ_LIVE_TÉNYEK}}` = "LIVE, 301 teszt, 7 NuGet csomag, multi-target net8.0+net10.0"
- `{{BREAKING_CHANGE_ÁLLÁSPONT}}` = "**Breaking change** — a Cabinet 0.1.x fogyasztók (CabinetBilder.Adapter.AutoCAD) nem tudják közvetlenül olvasni a 0.2 snapshot-okat. Migrációval kompatibilitás biztosítva."
- `{{KÖTELEZŐ_KÉRDÉSEK}}`:
  > 1. **A14 — Assembly Documentation:** a Modules.Abstractions-ben definiált `IAssemblyDocumentationDerivation` interface-t Cabinet 0.2 implementálja, vagy saját interface-t kapjon a Cabinet-ben? (A Cabinet 0.1 session-record OD-RFR-15 javaslata: A — Modules.Abstractions szintű, mert ajtó/ablak/szekrény mind ugyanazon az infrán fog kapni assembly dokumentációt.)
  > 2. **A15 — Catalog Visibility precedence:** a 6-rétegű resolution order (Skeleton-pin → Template-default → Tenant-private → Tenant-shared → Community → Curated) Cabinet 0.2-ben mind élesedik, vagy kezdjük csak a Private + Curated rétegekkel és a többi Cabinet 0.3-ra megy?
  > 3. **A16 — FlowEpic.Scope bővítés:** a Kernel `FlowEpic` aggregate `Scope` enum bővítése (+MicroAssembly) **breaking change** vagy **additív**? Hatás a Modules.Joinery + Cutting + más Kernel-fogyasztókra.

## Példa kitöltött prompt — Cutting Phase 4

Ha a felhasználó: "Indítom Cutting Phase 4 design-t."

Akkor a kitöltés:

- `{{MODUL_NÉV}}` = "Cutting Phase 4"
- `{{ELŐZŐ_VERZIÓ}}` = "Cutting Phase 3"
- `{{ELŐZŐ_V4_DOC}}` = "SpaceOS_Modules_Cutting_Core_Architecture_v4.md"
- `{{ÚJ_SCOPE}}` (a vision-doc fázis-listájából):
  > Phase 4 — Execution.
  >
  > - Cutting execution tracking — CuttingSheet FSM Submitted → Scheduled → InProgress → Completed/Cancelled
  > - Daily plan generation — napi szabászati ütemterv egy gép vagy gépcsoport számára
  > - Worker assignment — Phase 3 nesting publish output gép- és időponthoz rendelve
  > - Real-time progress — CuttingSheet %-on tart
  > - Completion proof — fotó, ProofHash a Kernel hash-chain-be
  > - Offcut tracking integration — végrehajtás közben keletkező offcut visszamegy az Inventory-ba
  > - Material consumption reconciliation — tervezett vs. valós anyagfogyás
- `{{ELŐZŐ_LIVE_TÉNYEK}}` = "303 teszt, order ingestion + nesting publish élesben"
- `{{KÖTELEZŐ_KÉRDÉSEK}}`:
  > 1. **Execution FSM tárolása:** A `CuttingSheet.ExecutionStatus` enum a meglévő `CuttingSheet` aggregate-be integrálódik, vagy új `CuttingExecution` aggregate születik? Az izoláció előny: Phase 3 tesztek érintetlenek; hátrány: két aggregate egy üzleti eseményt modellez.
  > 2. **Real-time progress source:** a worker mobil-/tablet-alkalmazásból küldi (websocket/SignalR), vagy időszakos REST polling? Mi a B2B Handshake-en át cross-tenant esetben a frekvencia (DOS-vektor védelem a host tenant felé)?
  > 3. **Completion proof formátum:** fotó-mp4-ProofHash kötelező minden CuttingSheet-re, vagy csak tenant-policy szerint (TenantStandard-ben konfigurálható)? GDPR: a worker arca biometrikus adat.
