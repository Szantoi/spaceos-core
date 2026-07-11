---
description: how to run Discovery Phase 3 – Prototype with MVE planning and escalation decision
---

# Discovery – Fázis 3: Prototype / MVE Tervezés (The Experimenter 🧪)

**Szerepkör:** The Experimenter
**Módszertan:** Double Diamond – 2. gyémánt, Konvergens fázis előkészítése
**Elhelyezés:** `Plans/Discoveries/<Program>/<Alprojekt>/03_prototype/`
**Forrás:** `Plans_Discovery_Framework_Standard.md` — Fázis 3

> 🧪 **Alapelv:** A prototípus NEM a végső termék — egy **kísérlet eszköze**. Célja bizonyítani, nem szállítani. Az MVE (Minimum Viable Experiment) a lehető legkisebb ráfordítással tesztel egy hipotézist.

> ⚠️ **Visszabonthatósági Szabály (Reversible Prototype Rule):** Minden prototípus kódnak **izoláltan és maradéktalanul visszabonthatóan** kell készülnie. A kísérlet NEM módosíthat meglévő Core/Domain entitásokat, adatbázis sémát vagy production kódot úgy, hogy az a kísérlet lezárása után is hatással legyen a rendszerre. A kísérlet végén a prototípus kód **egyetlen `git revert` vagy fájltörlés művelettel** eltávolítható kell, hogy legyen, nyom nélkül. Ha ez nem lehetséges → eszkaláció operatív projektbe (`rollback_safe: true`).

---

## Lépések

### 1. MVE vs. Operatív Projekt döntés

**Először dönts: marad-e a kísérlet Discovery módban?**

| Feltétel | MVE marad | Eszkalálni kell |
|:---------|:---------:|:---------------:|
| Egyetlen fájlban elfér, önállóan futtatható | ✅ | |
| NEM módosít meglévő production kódot | ✅ | |
| < 2 óra munkával elvégezhető | ✅ | |
| Célja **bizonyítani**, nem **szállítani** | ✅ | |
| Több meglévő fájl módosítása szükséges | | ❌ |
| Perzisztens állapot kell (DB, fájl) | | ❌ |
| Munka > 2 óra ÉS > 3 fájl | | ❌ |
| Más fejlesztők munkájától függ/befolyásolja | | ❌ |
| Production-ba kerül | | ❌ |

**Ha Eszkalálni kell:** Hozd létre az operatív projekt struktúrát `<EPIC_ROOT>` alatt, és a `state.md`-be írd be:
```yaml
mode: test-operation
rollback_safe: true
```
A `rollback_safe: true` megköveteli az eltávolítási lépések dokumentálását!

### 2. MVE Kísérlet Terv elkészítése

```
03_prototype/experiments/exp-<NNN>.md
```

**MVE sablon:**

```markdown
---
id: exp-XXX
hypothesis: hyp-XXX
type: user_test | spike | PoC | a_b_test
effort_estimate: S | M | L
---
# Kísérlet: [Rövid cím]

## Mit tesztelünk?
[Pontosan melyik hipotézis-elem kerül tesztelésre?]

## Módszer
[Hogyan mérjük? Milyen eszközzel?]

## Sikerességi küszöb
[Mikor mondjuk, hogy a hipotézis igazolódott?]

## Tervezett ráfordítás
[Max. X óra / X nap — utána STOP és értékelés]

## Integration Check — Rendszerszintű Hatáselemzés
[Milyen meglévő szabványt, rendszert vagy folyamatot érint ez az újítás?]
- **Érintett szabványok:** [...]
- **Érintett komponensek/folyamatok:** [...]
- **Hol törhet el valami a meglévő architektúrában?** [...]
```

### 3. Prototípus / PoC megvalósítása

Implementáld a legkisebb életképes kísérletet:

- **spike:** Gyors technológia-vizsgálat (pl. "Működik-e az X library erre a feladatra?")
- **PoC (Proof of Concept):** Működő, de nem production-ready kód
- **user_test:** Felhasználói viselkedés megfigyelése (mockup, wizard-of-oz)
- **a_b_test:** Két verzió összehasonlítása mérhető metrikával

```
03_prototype/prototypes/<slug>/    ← PoC kód, wireframe, mockup
```

### 4. Idő-korlát betartása

> ⏱️ **STOP szabály:** Ha a tervezett ráfordítási időt túllépte az implementáció, ÁLLJ MEG. Értékeld az eddigi eredményeket — ne folytasd automatikusan. Ha szükséges, eszkaláld az operatív projektté (1. lépés).

### 5. Definition of Done – Fázis 3

- [ ] MVE vs. Operatív döntés meghozva és dokumentálva
- [ ] `exp-XXX.md` kísérlet terv elkészítve
- [ ] Integration Check elvégezve
- [ ] PoC/prototípus megvalósítva (ha MVE mód)
- [ ] **Visszabonthatóság igazolva:** A prototípus kód izolált és egyetlen `git revert` vagy fájltörléssel nyom nélkül eltávolítható
- [ ] Időkorlát betartva (ha átlépte: eszkaláció!)

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Kísérlet terv | `03_prototype/experiments/exp-<NNN>.md` |
| Prototípus | `03_prototype/prototypes/<slug>/` |

## Következő fázis

→ **Fázis 4:** `discovery-test-and-learn.md` (The Integrator ⚖️)
