---
description: how to run Discovery Phase 2 – Ideate with ADR drafting
---

# Discovery – Fázis 2: Ideate / Ötletelés és ADR Vázlat (The Framer 📐 + Explorer 🧭)

**Szerepkör:** The Framer (vezető) + The Explorer (támogató)
**Módszertan:** Double Diamond – 2. gyémánt, Divergens fázis → **Megoldástér feltárása**
**Elhelyezés:** `[DISCOVERY_ROOT]/02_ideate/`
**Forrás:** `Plans_Discovery_Framework_Standard.md` — Fázis 2

> 🧭 **Fontos:** Az ADR-ek itt születnek, de `draft` státuszban maradnak egészen addig, amíg a `04_test-and-learn` fázis meg nem erősíti a döntést. **Véglegesíteni (accepted) itt tilos.**

---

## Lépések

### 1. Input beolvasása

```
01_define/hypotheses/hyp-XXX.md          ← Hipotézis
01_define/constraints/scope.md           ← In/Out Scope határok
01_define/success-criteria/              ← Sikerkritériumok
```

### 2. Ötletek szabad gyűjtése

Az ötletek **szabadon futnak** – nincs helyes vagy helytelen ötlet ebben az alfázisban:

```
02_ideate/concepts/<ötlet-slug>.md    ← Minden alternatív megközelítés külön fájlba
```

Minden concept fájl tartalmazza:
- Az ötlet leírása (mit és hogyan)
- Előnyök
- Hátrányok / kockázatok
- Becsült komplexitás (S/M/L)

### 3. Döntési napló (Decision Log) elkészítése

Miután az ötletek összegyűltek, dokumentáld a konvergencia folyamatát:

```
02_ideate/decision-log/decision-<NNN>.md
```

Tartalom:
- Milyen alternatívákat értékeltünk?
- Miért választottuk azt, amit?
- Miért vetettük el a többit?

> Ez a fájl védi meg a csapatot a „miért is döntöttünk így?" kérdéstől 6 hónappal később.

### 4. ADR vázlat létrehozása (ha szoftverdöntés születik)

```
02_ideate/adrs/ADR-<NNN>-<döntés-slug>.md
```

**ADR sablon (status: `draft`):**

```markdown
---
id: ADR-NNN
status: draft
date: YYYY-MM-DD
validated-by:      # kitöltendő a 04_test-and-learn után
---
# ADR-NNN: [Döntés rövid neve]

## Kontextus
[Miért kell dönteni? Mi a helyzet?]

## A javasolt döntés
[Mit javaslunk? — még nem elfogadott!]

## Következmények
**Pozitív:** ...
**Negatív/Trade-off:** ...

## Elvetett alternatívák
- [Alt 1] — Miért nem ezt?
- [Alt 2] — Miért nem ezt?
```

> ⚠️ **ADR nem törölhető!** Megbukott ADR-ek státusza `rejected` lesz, de a fájl megmarad az archívumban. Ha egy jobb döntés váltja fel: `superseded`, `superseded-by: ADR-YYY`.

### 5. Integration Check – Rendszerszintű Hatáselemzés

Minden ötlethez és ADR vázlathoz végezz hatáselemzést:

- [ ] Milyen meglévő szabványt érint? (`src/agent-system/database/standards/`)
- [ ] Milyen meglévő komponenst/folyamatot érint?
- [ ] Hol törhet el valami a meglévő architektúrában?

### 6. Definition of Done – Fázis 2

- [ ] Legalább 2 alternatíva dokumentálva `concepts/`-ban
- [ ] `decision-log/` tartalmazza a konvergencia indoklását
- [ ] ADR vázlat elkészítve (`status: draft`) ha szoftverdöntés érintett
- [ ] Integration Check elvégezve

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Ötlet fájlok | `02_ideate/concepts/<ötlet>.md` |
| Döntési napló | `02_ideate/decision-log/decision-<NNN>.md` |
| ADR vázlat | `02_ideate/adrs/ADR-<NNN>-<slug>.md` (status: `draft`) |

## Következő fázis

→ **Fázis 3:** `discovery-prototype.md` (The Experimenter 🧪)
