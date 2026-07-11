---
id: MSG-DESIGNER-009-DONE
from: designer
to: root
type: done
status: READ
ref: MSG-DESIGNER-007
created: 2026-06-24
content_hash: 3731e10a564e87065e74f48992a7a93b59bfc037aef1696e2b182231d1f6a557
---

# Telegram Alias Választás — Részletes Értékelés

## Választott aliasok

```
TELEGRAM_ALIASES: designer, dizájner, ux
```

---

## Teljes értékelés (UX/használhatósági szempontból)

### 1. **dizájner** ✅ VÁLASZTVA
**Előnyök:**
- Magyar felhasználóknak természetes, könnyen érthető
- Egyértelmű szerepkör azonosítás
- Könnyen gépelhető magyar billentyűzetről
- Nincs szóköz, URL-safe

**Hátrányok:**
- Nemzetközi felhasználóknak idegen lehet

**Döntés:** ✅ Választva — magyar közegben dolgozunk, elsődleges nyelv magyar

---

### 2. **ux** ✅ VÁLASZTVA
**Előnyök:**
- Rövid, tömör (2 karakter)
- Nemzetközileg ismert rövidítés (User Experience)
- Gyorsan gépelhető
- Szakmai szleng, azonnal érthető
- URL-safe, nincs speciális karakter

**Hátrányok:**
- Kissé általános (sok UX designer van a világon)

**Döntés:** ✅ Választva — tömörség, nemzetköziség, egyértelmű szakmai kontextus

---

### 3. **grafikus** ❌ NEM VÁLASZTVA
**Előnyök:**
- Magyar nyelven érthető
- Kreatív szerepkör azonosítás

**Hátrányok:**
- **Pontatlan szerepkör** — a Designer terminál UX/UI designer, nem grafikus (a grafikus inkább print, branding, vizuális identitás)
- Hosszabb szó (8 karakter vs. dizájner 8 karakter)
- Félreértésre adhat okot (inkább illusztrátor/grafikus művész asszociáció)

**Döntés:** ❌ NEM választva — **szerepkör pontatlansága miatt**

---

### 4. **artist** ❌ NEM VÁLASZTVA
**Előnyök:**
- Kreatív, művészi asszociáció
- Rövid, angol

**Hátrányok:**
- **Túl tág fogalom** — artist lehet festő, szobrász, zenész, stb.
- Magyar közegben kevésbé releváns
- Nem utal UX/UI szakmai kontextusra
- Nem engineering/product design oriented

**Döntés:** ❌ NEM választva — **túl általános, nem utal a UX/UI szerepkörre**

---

### 5. **creative** ❌ NEM VÁLASZTVA
**Előnyök:**
- Kreatív munka jelölése
- Angol, nemzetközi

**Hátrányok:**
- **Túl általános** — creative lehet copywriter, art director, videographer is
- Hosszabb szó (8 karakter)
- Nem utal specifikusan UX/UI designra
- Magyar közegben idegenebb

**Döntés:** ❌ NEM választva — **nincs egyértelmű UX/UI kontextus**

---

## Alternatív javaslatok (nem választva, de fontolóra vehetők)

| Alias | Előny | Hátrány | UX score |
|---|---|---|---|
| **ui** | Rövid (2 kar), UI Design egyértelmű | Túl általános, nincs User Experience utalás | 6/10 |
| **uiux** | Teljes szerepkör leírva | Hosszabb (4 kar), redundáns a "ux" mellett | 5/10 |
| **design** | Rövid angol, általános | Túl általános (web design, graphic design, stb.) | 4/10 |
| **terv** | Magyar, rövid | Nem utal design-ra, inkább mérnöki tervezésre | 3/10 |

---

## Végső döntés indoklása

A **dizájner** és **ux** kombináció:
1. ✅ **Magyar + nemzetközi** lefedettség
2. ✅ **Egyértelmű UX/UI szerepkör** azonosítás
3. ✅ **Rövid, gyorsan gépelhető**
4. ✅ **Nincs félreértés** más kreatív szakmákkal (grafikus, artist)
5. ✅ **Telegram @mention friendly** (nincs szóköz, speciális karakter)

---

## Használati példa Telegramban

```
@dizájner kérlek nézd meg a login flow-t
@ux milyen színsémát javasolsz a dashboard-hoz?
@designer teljes design spec kellene a CRM modulhoz
```

Mind a három alias működik, de a **dizájner** magyar, az **ux** nemzetközi és szakmai kontextusban.
