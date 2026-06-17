# Prompt template — Új modul tervezés indítás

> Használd ezt a template-et amikor a felhasználó **teljesen új modul** tervezését akarja indítani (pl. új Modules.Window, új Modules.Glass, stb.).
> Ne használd verzió-bumphoz (Cabinet 0.2, Cutting Phase 4) — arra a `module-version-bump.md` template van.

---

## A kitöltendő mezők

A prompt-generálás előtt **kérdezd meg vagy keresd ki a project knowledge-ből**:

| Mező | Forrás |
|---|---|
| `{{MODUL_NÉV}}` | Felhasználó megadja (pl. "Modules.Window") |
| `{{MODUL_DOMAIN}}` | Felhasználó megadja (pl. "ablakgyártás") |
| `{{SCOPE_RÖVID}}` | Felhasználó megadja (pl. "alapszintű ablak-tervezés és BOM derivation") |
| `{{PRECEDENS_DOC}}` | Project knowledge-ből: a legfrissebb v4 modul-architektúra dokumentum |
| `{{KAPCSOLÓDÓ_VISION_DOC}}` | Project knowledge-ből: van-e Modules.{X}_Vision_v1.md? |
| `{{BECSÜLT_BÁZIS_EFFORT}}` | Felhasználó megadja vagy a vision-doc-ból olvasod |
| `{{KÖTELEZŐ_KÉRDÉSEK}}` | 2-3 specifikus döntés amit a v1 előtt tisztáznunk kell — ezeket te találd ki a domain alapján |

## A prompt sablon

```
{{MODUL_NÉV}} design session indítása.

Új modul tervezése a SpaceOS ekosystem-be: {{MODUL_DOMAIN}}.

A project knowledge-ben fent van:
- {{PRECEDENS_DOC}} — mint precedens-példa v4 dokumentum-stílushoz
{{HA_VAN_VISION:- {{KAPCSOLÓDÓ_VISION_DOC}} — a modul hosszú-távú vízió}}
- SpaceOS_Master_Manifesto.docx — T1-T6 tenetek
- Mathematical_Furniture_Theory.docx (ha geometriai vonatkozású)
- Aktuális Codebase Status — jelenlegi platform-állapot

**{{MODUL_NÉV}} scope (első iteráció):**

{{SCOPE_RÖVID}}

**Becsült bázis-effort:** ~{{BECSÜLT_BÁZIS_EFFORT}} nap, a v1→v4 review pipeline-on átfutva várhatóan ~{{BECSÜLT_BÁZIS_EFFORT_PLUSZ_5}} nap.

**Kötelező inputok a tervezéshez:**

- A {{PRECEDENS_DOC}} mint **strukturális precedens** — ugyanazt követjük (kumulált finding összesítő, NuGet csomag-bontás, runtime cél-framework `net8.0;net10.0` multi-target ha .NET-es modul, public API surface, persistence contract, DoD security gates §9.10, threat model)
{{HA_VAN_VISION:- A {{KAPCSOLÓDÓ_VISION_DOC}} mint **scope-iránytű** — fázisok és horizontok ott körvonalazva}}
- Cross-module integráció: a {{MODUL_NÉV}} **nem szigetelt**, integrálódnia kell a Kernel-hez (FlowEpic, AggregateSnapshot, B2BHandshake) és potenciálisan más L2 modulokhoz (Inventory, Cutting, Procurement)
- A SpaceOS T1-T6 tenetek (Manifesto-ból) — minden architektúra-döntés kompatibilis kell legyen velük

**Várhatóan új csomagok:**

- `SpaceOS.{{MODUL_NÉV_PASCAL}}.Domain` — aggregate-ek, value object-ek
- `SpaceOS.{{MODUL_NÉV_PASCAL}}.Application` (vagy hasonló) — CQRS handlers ha alkalmazható
- `SpaceOS.{{MODUL_NÉV_PASCAL}}.Infrastructure` — EF Core, ha perzisztál
- Esetleg `SpaceOS.{{MODUL_NÉV_PASCAL}}.Abstractions` — port-interfészek

**Multi-target:** ha a modul várhatóan AutoCAD adapterre integrálódik, akkor `net8.0;net10.0`. Ha tisztán szerver-oldali, akkor `net8.0`.

**Schema-version:** új modul → új schema-version namespace, a `SpaceOS.Cabinet.*` `"0.1"` mintát követve.

Kérlek indítsd a `spaceos-arch-planner` skillt, és kezdj egy v1 draft-ot `SpaceOS_{{MODUL_NÉV_PASCAL}}_v1_Architecture_v1.md` néven (vagy a felhasználóval egyezett verzió-jelölő szerint). Dokumentum-konvenció: magyar próza + angol kód/identifier-ek, ugyanaz mint a {{PRECEDENS_DOC_RÖVID}}-ben.

Mielőtt v1-et generálsz, néhány kulcsfontosságú döntést szeretnék tisztázni:

{{KÖTELEZŐ_KÉRDÉSEK}}

Egy mondatos válasz mindháromra, és indul a v1 draft.
```

## Hogyan állítsd elő a `{{KÖTELEZŐ_KÉRDÉSEK}}` blokkot

A 2-3 kérdés a modul-domain alapján változó. Példa-archetípusok:

- **Domain-modell-kérdés**: az aggregate-szerkezet konkrét döntése (pl. "egy aggregate-be vagy szétbontva?")
- **Cross-module-integráció-kérdés**: melyik más modullal hogyan kommunikál (pl. "a Modules.X használja-e a Modules.Inventory port-jait?")
- **Persistence-stratégia-kérdés**: új DB-schema vagy meglévő bővítése (pl. "új `spaceos_window` schema vagy `spaceos_joinery`-be integrált?")
- **Multi-tenancy-kérdés**: tenant-scope minden adatra vagy bizonyos szabványok globalízálhatók (pl. "WindowProfile tenant-private vagy shared catalog?")

A 3 kérdés legyen **konkrét és releváns** a modulra — ne generikus.

## Példa kitöltött prompt

Ha a felhasználó: "Indítok új design-t a Modules.Window-ra. Alap ablakgyártás, Doorstar mintára."

Akkor a kitöltés:
- `{{MODUL_NÉV}}` = "Modules.Window"
- `{{MODUL_DOMAIN}}` = "ablakgyártás"
- `{{SCOPE_RÖVID}}` = "alapszintű ablak-tervezés (keret + tok + üvegezés), BOM derivation, Doorstar-szerű B2B Handshake támogatás"
- `{{PRECEDENS_DOC}}` = "SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md"
- `{{KAPCSOLÓDÓ_VISION_DOC}}` = nincs (Modules.Window még nincs vision-doc)
- `{{BECSÜLT_BÁZIS_EFFORT}}` = 14
- `{{KÖTELEZŐ_KÉRDÉSEK}}`:
  > 1. **Aggregate-design:** egy `Window` aggregate vagy szétbontott `WindowFrame` + `WindowGlazing` + `WindowHardware` aggregate-ek? A Cabinet 0.1 mintában a `Skeleton` egy aggregate, sok aggregate-belső entity-vel — ezt követjük, vagy ablaknál más struktúra indokolt?
  > 2. **Cross-module-integráció:** a Modules.Window használja-e a Modules.Cutting-et az üvegezés-szabászathoz, vagy saját `IGlassCuttingProvider` portot definiál? A Cutting Phase 3 már LIVE — érdemes integrálni, de felmerülhet hogy az üveg specifikus.
  > 3. **Persistence:** új `spaceos_window` PostgreSQL schema, vagy a meglévő `spaceos_joinery`-ba integrált (ha az ablak a joinery része)? A B2B Handshake szempontjából melyik tisztábban kezeli a "tenant ablakot rendel"-t?
