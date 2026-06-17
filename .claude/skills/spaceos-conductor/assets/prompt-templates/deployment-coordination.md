# Prompt template — Deployment koordináció

> Használd ezt a template-et amikor a felhasználó **nem új tervezést** akar, hanem **deployment / release / launch koordinációt**. Pl. Doorstar Soft Launch, új modul VPS-re telepítése, multi-modul release.

---

## Mikor deployment koordináció

Tipikus jellemzők:

- **Nincs új scope** — a kód már kész vagy közel kész, **most hogyan élesítjük**
- **Több szereplő bevonása** — VPS deploy + Windows AutoCAD plugin + Doorstar end-user + nginx config + Keycloak
- **Sorrend kérdés** — mit először, mit utoljára, hogy ne törjön semmi
- **Roll-back tervezés** — ha a deployment hibát kap, hogyan vissza
- **UAT-koreográfia** — milyen user testing kell az élesindítás előtt
- **Monitoring** — élesindítás utáni mit figyelünk, milyen metrikákkal

## A kitöltendő mezők

| Mező | Forrás |
|---|---|
| `{{DEPLOYMENT_NÉV}}` | Felhasználó megadja (pl. "Doorstar Soft Launch", "Cabinet 0.1 NuGet release") |
| `{{ÉRINTETT_KOMPONENSEK}}` | Felhasználó megadja vagy kontextusból (pl. "Cabinet 0.1 NuGet, CabinetBilder integráció, Keycloak setup") |
| `{{DEPLOYMENT_FÁZISÁBAN}}` | "tervezés alatt", "kész de nem deployed", "deployed de nem aktivált", stb. |
| `{{ÉRINTETT_KÖRNYEZETEK}}` | VPS, Windows-os Doorstar gép, esetleges third-party (nginx, Keycloak, Cloudflare) |
| `{{KOCKÁZAT_PROFIL}}` | Magas (production-ügyfél), közepes (saját staging), alacsony (csak fejlesztői ellenőrzés) |
| `{{KÖTELEZŐ_KÉRDÉSEK}}` | 2-3 deployment-specifikus kulcsdöntés |

## A prompt sablon

```
Deployment koordináció: {{DEPLOYMENT_NÉV}}.

Ez **nem új tervezés** — a kód készen van vagy közel kész, az élesindítás koreográfiáját és koordinációját kell tisztáznunk.

A project knowledge-ben fent van:
- Aktuális Codebase Status — mi a jelenlegi LIVE állapot, mi nincs még élesben
- {{ÉRINTETT_KOMPONENSEK}} v4 dokumentumai — a kód mire épül
- (Ha aktuális:) SETUP_VPS.md, SETUP_WINDOWS.md — a setup-runbook-ok
- (Ha aktuális:) SpaceOS_VPS_Infrastructure_Runbook_v1.md

**A deployment állapota:** {{DEPLOYMENT_FÁZISÁBAN}}

**Érintett komponensek:**

{{ÉRINTETT_KOMPONENSEK}}

**Érintett környezetek:**

{{ÉRINTETT_KÖRNYEZETEK}}

**Kockázat-profil:** {{KOCKÁZAT_PROFIL}}

---

A koordináció **NEM** v1→v4 architektúra-pipeline. Nincs new design dokumentum. A kimenet egy:

1. **Deployment runbook** (markdown) — sorrend-listán szerepli, mit kell csinálni, milyen sorrendben, milyen ellenőrző parancsokkal
2. **Roll-back plan** — minden lépés hogyan vissza
3. **Smoke-test checklist** — élesindítás után mit tesztelünk percen belül
4. **UAT checklist** (ha relevant) — felhasználói tesztelés szempontjai
5. **Monitoring plan** — első 24 / 72 órában mit figyelünk

A runbook **konkrét parancsokkal** tartalmazza a lépéseket — ne legyen elvontat, hanem `cd /opt/spaceos && ...` szintű utasításokkal.

**A Cabinet 0.1 v4 dokumentum** §16 (Claude Code implementációs csomag) mintát követjük a runbook-stílusra. Az adott deployment-re szabottan.

Kérlek készítsd el a deployment runbook v1-et `SpaceOS_{{DEPLOYMENT_NÉV_FÁJLNÉV}}_Runbook_v1.md` néven. Ha egy review-iteráció kell, azt v2-vé tesszük (de ez **nem** ugyanaz mint az architektúra-review pipeline — csak egy review-pass).

Mielőtt elkezded, néhány kulcsfontosságú döntést szeretnék tisztázni:

{{KÖTELEZŐ_KÉRDÉSEK}}

Egy mondatos válasz mindháromra, és indul a runbook draft.
```

## A `{{KÖTELEZŐ_KÉRDÉSEK}}` deployment-specifikus archetípusai

A 3 kérdés **mindig** ezen a 3 dimenzión menjen át:

### 1. Sorrend és time-window kérdés

> "Milyen **sorrendben** indítjuk élesbe a komponenseket? Van **fix időablak** (pl. szombat éjjel 2-4 óra között, alacsony forgalom)?"

Ez azért fontos, mert sok deployment-bug az **élesítés sorrendje** miatt kerül felszínre.

### 2. Roll-back határ kérdés

> "Mi a **roll-back határ**? Ha a deployment 30 percen belül problémát mutat, vissza-állunk-e? Ha 24 órán belül, a downtime-ot meg-engedjük? Mi az **észlelési kritérium**, hogy 'baj van'?"

Ez egy nehéz kérdés, de **kötelező** előre eldönteni — élesindítás közben nincs idő rá.

### 3. UAT és visszacsatolás-loop kérdés

> "Ki teszteli **első körben** a deployment-et? Doorstar tényleges felhasználó, vagy csak Te magad? Ha végfelhasználó, **milyen kommunikációs csatornán** kapjuk vissza a feedback-et (Slack, telefon, dedikált email)?"

A UAT a soft launch szíve — ha nincs jó feedback-csatorna, az élesindítás nem sokat ér.

## Példa kitöltött prompt — Doorstar Soft Launch

Ha a felhasználó: "Indítjuk a Doorstar Soft Launch-ot. Itt minden összejön: Cabinet 0.1, CabinetBilder, Keycloak, end-user."

Akkor a kitöltés:

- `{{DEPLOYMENT_NÉV}}` = "Doorstar Soft Launch"
- `{{ÉRINTETT_KOMPONENSEK}}` = "Cabinet 0.1 NuGet (LIVE), CabinetBilder.Adapter.AutoCAD (Windows), Keycloak Doorstar realm, Modules.Joinery (LIVE), portal.joinerytech.hu Doorstar tenant skin"
- `{{DEPLOYMENT_FÁZISÁBAN}}` = "Cabinet 0.1 LIVE, CabinetBilder integráció kell hozzá (~1.5 nap), Keycloak finalizálás, Doorstar onboarding"
- `{{ÉRINTETT_KÖRNYEZETEK}}` = "VPS prod (joinerytech.hu) + Doorstar Windows fejlesztő-gépek (AutoCAD 2027) + Keycloak"
- `{{KOCKÁZAT_PROFIL}}` = "**Magas** — első éles ügyfél, hibanél a hitelességi tét óriási"
- `{{KÖTELEZŐ_KÉRDÉSEK}}`:
  > 1. **Sorrend és time-window:** A Doorstar Soft Launch "egy estés" élesítés (mind együtt), vagy fokozatos (pl. először Cabinet 0.1 NuGet release, hét múlva CabinetBilder integráció, hét múlva Keycloak migration, hét múlva tényleges Doorstar használat)? A fokozatos csökkenti a kockázatot, de hosszabb. Doorstar el-tudja-e fogadni egy 4-6 hetes ramp-up-ot?
  > 2. **Roll-back határ:** Ha élesindítás után az első hét Doorstar-tervezésnél bug merül fel (pl. egy szekrény Brep-rendelés hibázik), a roll-back **csak a Cabinet 0.1 NuGet csomagra**, vagy **az egész CabinetBilder plugin**-re? Mi az amit Doorstar **mindenképpen kell hogy működjön** (pl. a meglévő DWG-k nyithatók legyenek), ami nem törhetik meg semmi áron?
  > 3. **UAT és feedback-csatorna:** A Doorstar oldali tester(ek) ki(k)? Ha az ügyfél maga, milyen pre-launch felkészítés (egy 2 órás betanítás videó-conferencia)? Ha hozzád érkezik bug-jelentés, milyen reakcióidő (24 óra? Munkaidőben azonnal?)? Mi az **eskalációs csatorna** ha valami kritikus?

## Egy fontos megjegyzés

A deployment-koordinációs runbook **nem helyettesíti** a Claude Code agent munkáját. A runbook **a humán** számára készül — **ki** csinálja, **mit**, **mikor**, **mi a fallback**. A konkrét kód-deployment-et a Claude Code agent fogja kivitelezni a runbook alapján.

A deployment-koordináció **mindig human-in-the-loop**:
- A runbook minden ön-állóan futtatható lépésénél a humán **megerősíti hogy mehet**
- A runbook minden roll-back döntésénél a humán **dönt** (nem az agent)
- A runbook minden user-facing communication-nél a humán **ellenőrzi a wording-et**

Ez **különbözik** a tervező-pipeline-tól, ahol az agent autonómabb. Itt a humán autonómia **védőelv**.
