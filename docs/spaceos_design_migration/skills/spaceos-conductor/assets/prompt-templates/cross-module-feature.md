# Prompt template — Cross-module feature tervezés

> Használd ezt a template-et amikor a felhasználó **több modulra hatást gyakorló feature-t** akar tervezni — pl. új Kernel-szintű capability, új cross-cutting concern, vagy egy feature ami 2-3 modulban egyszerre kell változtatást.
> Ne használd egyetlen modulra korlátozott feature-höz — arra a `module-version-bump.md` template van.

---

## Mikor cross-module feature ez

Tipikus jellemzők:

- A feature a **Kernel-ben** újat ad (új aggregate, új capability), és **2+ Modules** fogyasztja
- A feature **B2B Handshake-en** át zajlik (cross-tenant)
- A feature egy **közös contracts package**-en át (`SpaceOS.Modules.Contracts`) kommunikál
- Több **L4 portal**-on egyszerre kell UI-t adni
- **Új security-modell** ami minden modulra kihat
- **Új audit/compliance** követelmény ami minden modult érint

## A kitöltendő mezők

| Mező | Forrás |
|---|---|
| `{{FEATURE_NÉV}}` | Felhasználó megadja (pl. "ProofHash escrow API", "Multi-language tenant labels") |
| `{{ÉRINTETT_MODULOK}}` | Felhasználó megadja vagy kontextusból (lista) |
| `{{KERNEL_ÉRINTETTSÉG}}` | "igen" / "nem" — kell-e Kernel-szintű új aggregate / API |
| `{{SCOPE_RÖVID}}` | A feature lényege 2-3 mondatban |
| `{{KAPCSOLÓDÓ_PRECEDENS}}` | Project knowledge-ből: hasonló jellegű v4 dokumentum |
| `{{KÖTELEZŐ_KÉRDÉSEK}}` | 2-3 cross-module-specifikus kulcsdöntés |

## A prompt sablon

```
Cross-module feature design session indítása: {{FEATURE_NÉV}}.

Ez **nem** egyetlen modul belső feature-je — az érintett modulok: {{ÉRINTETT_MODULOK}}. Ezért a tervezés **cross-module-perspektívájú**: a Kernel + Contracts package + 2+ Modules egyidejű érintettsége.

A project knowledge-ben fent van:
- {{KAPCSOLÓDÓ_PRECEDENS}} — hasonló cross-module feature precedens
- A jelenlegi Codebase Status — érintett modulok aktuális állapota
- SpaceOS_Master_Manifesto.docx — T1-T6 tenetek
- (Ha aktuális:) SpaceOS_Distributed_Network.docx + SpaceOS_Federated_Data_Schema.docx — cross-tenant minták

**Feature scope:**

{{SCOPE_RÖVID}}

**Becsült bázis-effort:** [a felhasználóval egyeztetve] nap, a v1→v4 review pipeline-on átfutva várhatóan [+5-7 nap] nap. Cross-module feature **általában magasabb review-deltát ad**, mert minden érintett modulra reflektálni kell.

**Kötelező inputok a tervezéshez:**

- A **Kernel** mint stabil platform — minden cross-module feature-nek **a Kernel-ben kell horgonyoznia** (FlowEpic, AggregateSnapshot, ProofHash, B2BHandshake)
- Az **érintett modulok aktuális v4 dokumentumai** — minden érintett modulra meg kell vizsgálni a hatást
- A **`SpaceOS.Modules.Contracts`** package — ha új DTO-t / interface-t ad hozzá, ez a megosztási pont
- A **B2B Handshake protokoll** — ha cross-tenant a feature, a Handshake-en megy
- A **T1-T6 tenetek** — különösen T1 (one source of truth), T2 (federation), T3 (adat-tulajdon), T4 (kiszámítható biztonság)

**Várhatóan érintett szintek:**

| Szint | Érintettség |
|---|---|
| L1 Kernel | {{KERNEL_ÉRINTETTSÉG_RÉSZLETES}} |
| L2 Modules ({{ÉRINTETT_MODULOK}}) | mind érintett — a Contracts-on át kommunikálnak |
| L3 Orchestrator | BFF endpoint-ok új feature-höz |
| L4 Portal | UI-bővítés ha user-facing |

**Multi-modul-koordináció:**

A feature implementációja **több repo-ban** zajlik:
{{ÉRINTETT_REPÓK_LISTA}}

A tervezés **egy v4 dokumentumban** zárul, ami minden érintett repo Claude Code agent-jének átadható (külön track-ekre bontva, mint Cabinet 0.1 §16.1-ben).

Kérlek indítsd a `spaceos-arch-planner` skillt, és kezdj egy v1 draft-ot `SpaceOS_{{FEATURE_NÉV_FÁJLNÉV}}_Architecture_v1.md` néven. Dokumentum-konvenció: magyar próza + angol kód/identifier-ek.

A v1 draft-ban fontos szakaszok (a klasszikus modul-architektúra fölött):

- **Cross-module ownership map** — melyik modul mit "birtokol", és hogy a feature ezt hogyan érinti
- **Contracts evolution** — milyen új DTO/interface kerül a Contracts package-be, és milyen verzió-bumpot jelent
- **Migration koreográfia** — hogyan tudunk először Kernel-t deployolni, aztán a modulokat (vagy fordítva), úgy hogy a rendszer nem törik meg menet közben
- **Roll-back plan** — ha a feature deployment közben hibát kap, hogyan tudunk visszafordulni
- **Cross-module security gates** — ha a feature security-érintett (T4), minden modulra security review

Mielőtt v1-et generálsz, néhány kulcsfontosságú döntést szeretnék tisztázni:

{{KÖTELEZŐ_KÉRDÉSEK}}

Egy mondatos válasz mindháromra, és indul a v1 draft.
```

## A `{{KÖTELEZŐ_KÉRDÉSEK}}` cross-module-specifikus archetípusai

A 3 kérdés **mindig** ezen a 3 vágáson menjen át:

### 1. Kernel-vagy-Modules kérdés

> "A feature **Kernel-szintű** új aggregate-et igényel, vagy a meglévő Kernel-aggregate-ek elégségesek és csak a Modules változnak?"

Ez a **legfontosabb cross-module döntés**, mert:
- Kernel-bővítés → Kernel migration (DB-changes), minden modul-fogyasztóra hat
- Csak Modules-bővítés → izolált, gyorsabb iteráció, de később Kernel-refactor lehet kell

### 2. Contracts evolution kérdés

> "A feature **breaking change**-et okoz a `SpaceOS.Modules.Contracts` csomagon? Ha igen, melyik verzió-bumpot követel (1.x → 2.0)? Ha nem, additív bővítés (1.x → 1.y)?"

Ez azért fontos, mert a Contracts package-et **minden modul** fogyasztja — ha breaking, akkor egyszerre kell minden modul-deployment.

### 3. Tenant-szintű hatás kérdés

> "A feature **minden tenantra** azonnal aktiválódik, vagy **opt-in** modell (TenantStandard / EnabledModules-szerű)? Ha opt-in, milyen migration-stratégia?"

Ez a T1 (one source of truth) és T2 (federation) tenetekre érzékeny — meg kell védeni a tenant-szuverenitást.

## Példa kitöltött prompt — "Multi-language tenant labels"

Ha a felhasználó: "Indítok cross-module design-t a multi-language tenant label-ekre. Minden modul-aggregate-en kell a `labels` mező nyelv-specifikus."

Akkor a kitöltés:

- `{{FEATURE_NÉV}}` = "Multi-language Tenant Labels"
- `{{ÉRINTETT_MODULOK}}` = "Joinery, Cabinet, Cutting, Inventory, Procurement"
- `{{KERNEL_ÉRINTETTSÉG}}` = "igen — Kernel-Tenant aggregate-en kell a `SupportedLanguages` mező, és minden Modules-aggregate-en `Labels: Dictionary<lang, string>`"
- `{{SCOPE_RÖVID}}` = "Minden tenant-aggregate (Joinery DoorOrder, Cabinet Skeleton, Cutting CuttingSheet, stb.) támogasson nyelv-specifikus label-eket. A label-ek a B2BHandshake-en is áramoljanak. Default lokalizáció: hu, en. Új tenant `SupportedLanguages` listával állítja be."
- `{{KAPCSOLÓDÓ_PRECEDENS}}` = "SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md (struktúra) + SpaceOS_Federated_Data_Schema.docx (cross-tenant minta)"
- `{{KÖTELEZŐ_KÉRDÉSEK}}`:
  > 1. **Kernel-vagy-Modules:** a `SupportedLanguages` és `DefaultLanguage` mezők a Kernel `Tenant` aggregate-en (ami minden modul-fogyasztó számára közös), vagy minden modul saját TenantStandard-jában tárolja külön? Az egyik konzisztencia, a másik függetlenség.
  > 2. **Contracts evolution:** a `LabeledString` típus (nyelv-specifikus label-szerkezet) bevezetése a `SpaceOS.Modules.Contracts`-ban. Ez egy új type-bevezetés (additív, 1.x → 1.y), de a meglévő DTO-k `Title: string` mezőit `Title: LabeledString`-re cserélnünk kell — ami breaking. Hogyan?
  > 3. **Tenant-szintű hatás:** új feature **minden tenantra** automatikusan aktiválódik (default `["hu"]` `SupportedLanguages`-szel), vagy opt-in (a tenant kéri a multi-language-et explicit)? GDPR-szempont: a tenant tartalma több nyelven tárolódik akkor is ha a tenant csak egyet beszél.
