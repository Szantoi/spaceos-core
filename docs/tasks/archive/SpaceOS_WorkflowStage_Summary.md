# SpaceOS — Workflow Stage Architecture
## Üzleti összefoglaló

---

## Mi ez egyszerűen?

A SpaceOS egy olyan rendszer, ahol az asztalosipari cégek a teljes munkafolyamatukat kezelik — az árajánlattól a kész termék kiszállításáig. De nem minden cég dolgozik ugyanúgy. Van aki csak gyárt, van aki értékesít is, van aki tervez, van aki felmér, van aki beépít. És ami még fontosabb: **ugyanaz a cég az egyik projektnél máshogy dolgozik, mint a másiknál.**

Ezt a problémát oldjuk meg a Workflow Stage Architecture-ral.

---

## Az alapötlet

Képzeld el a munkafolyamatot úgy, mint egy **vonatot, ami állomásokon halad át.** Minden állomás egy önálló lépés, ahol valami történik a megrendeléssel:

```
[Értékesítés] → [Felmérés] → [Tervezés] → [Gyártás] → [Beépítés]
```

De nem minden vonat áll meg minden állomáson:

```
Doorstar standard rendelés:
  [Értékesítés] ──────────────────────→ [Gyártás]
  (a felmérés kimarad, standard méretek)

Doorstar felmérős rendelés:
  [Értékesítés] → [Felmérés] ────────→ [Gyártás]
  (helyszíni mérés kell a pontos adatokhoz)

Egyedi asztalos:
  [Értékesítés] → [Felmérés] → [Tervezés] → [Gyártás] → [Beépítés]
  (minden lépés kell, mert egyedi bútor)
```

**A rendszer nem tudja előre, milyen állomások lesznek.** Egy új cég hozzáadhat sajátot (pl. "Minőségellenőrzés", "Logisztika", "Tanúsítványozás"), és az automatikusan bekapcsolódik a rendszerbe.

---

## Miért fontos ez?

### 1. Minden cég más

A Doorstar-nál a gyártás a lényeg — standard ajtókat csinálnak, a megrendelő megadja a méretet, és kész. Egy egyedi bútorasztalos viszont felmér, tervez, modellezi 3D-ben, és csak utána gyárt. Ha a rendszer mindkettőt ki akarja szolgálni, nem lehet egy fix folyamatba beégetni a lépéseket.

### 2. A lépések között "vágás" van

Minden lépés egy önálló egység, ami **kész adatcsomagot ad át** a következőnek. Az értékesítés ad egy elfogadott ajánlatot. A felmérés ad pontos méreteket és helyszíni adatokat. A tervezés ad végleges modellt. A gyártás ezekből dolgozik — nem kérdez vissza, kész adatokat kap.

Ez azért fontos, mert:
- **Más cég is végezheti a következő lépést.** A felmérést csinálhatja egy külső felmérő cég, a gyártást egy másik gyár. A rendszer ugyanúgy kezeli a cégen belüli és a cégek közötti átadást.
- **Visszakövethetőség.** Minden átadás le van mentve — melyik ajánlat alapján gyártottak, milyen felmért méretek alapján számolták ki a szabászlistát. Ha gond van, vissza lehet nézni.
- **Módosítás kezelése.** Ha a felmérés után kiderül, hogy más méret kell, egy új, módosított ajánlat készül (v2). A régi is megmarad referenciának.

### 3. Egy cégen belül is változhat

A Doorstar-nál a megrendelések 80%-a standard — nem kell felmérés. De 20%-ban kell. Nem két különböző rendszer kell ehhez, hanem egy, ahol a folyamat konfigurálható: a felmérés lépés "opcionális".

---

## Mit jelent ez a gyakorlatban?

### A Doorstar példája

**Standard rendelés (felmérés nélkül):**
1. A megrendelő szól: "5 db FAF_T ajtó, 800×2040"
2. Az értékesítő rögzíti a rendszerben → árajánlat készül automatikusan
3. A megrendelő elfogadja → a rendszer átadja az adatokat a gyártásnak
4. A gyártásilap (PDF) automatikusan elkészül a pontos szabászlistával
5. Gyártás indul

**Felmérős rendelés:**
1. A megrendelő szól: "Ajtókat kérek a lakásba"
2. Az értékesítő rögzíti → becsült árajánlat készül
3. A megrendelő elfogadja → felmérés indul
4. A felmérő kimegy, rögzíti a helyiségeket, falnyílásokat, pontos méreteket
5. Ha az árak változtak → módosított ajánlat készül → megrendelő elfogadja
6. A rendszer átadja a pontos adatokat a gyártásnak
7. Gyártásilap a felmért méretek alapján

### Egy bútorasztalos példája

1. Értékesítés: ügyfél egyedi konyhaszekrényt kér
2. Felmérés: helyszíni mérés, fényképek, akadályok rögzítése
3. Tervezés: 3D modell készítés, anyagválasztás, ügyfél jóváhagyás
4. Gyártás: szabászlista, CNC program, élzárás, összeszerelés
5. Beépítés: helyszíni beszerelés, átvétel

Ugyanaz a rendszer, más konfiguráció.

---

## Mi a különbség a "mit gyárt" és a "hogyan dolgozik" között?

A rendszer két dolgot kezel külön:

**Milyen terméket gyárt a cég?**
- Ajtó, szekrény, ablak — ez a `EnabledModules` (már létezik)
- Egy cég gyárthat ajtót ÉS szekrényt is

**Milyen lépéseken megy át a munka?**
- Értékesítés, felmérés, tervezés, gyártás, beépítés — ez a `StageChain` (új)
- Ugyanaz a cég más lépéseket használhat más projektekhez

**Példa:** A Doorstar ajtókat gyárt (`EnabledModules: [door]`), és a munkafolyamata: értékesítés → (opcionális felmérés) → gyártás. Egy bútorasztalos ajtókat ÉS szekrényeket gyárt (`EnabledModules: [door, cabinet]`), és a munkafolyamata: értékesítés → felmérés → tervezés → gyártás → beépítés.

---

## Mi épül most, és mi később?

| Sorrend | Mi épül | Miért fontos | Mikor |
|---------|---------|-------------|-------|
| **1** | A "váltórendszer" (Stage Registry) | Ez az alap — enélkül a lépések nem köthetők össze | **Most** (~8 nap) |
| **2** | Gyártásilap PDF (Joinery v2) | Ez az, amit a Doorstar ténylegesen kinyomtat és a műhelybe visz | Ezután (~16 nap) |
| **3** | Értékesítési modul (Sales) | Árajánlat, elfogadás, ügyfélkezelés — minden innen indul | Tervezés → implementáció |
| **4** | Felmérési modul (Survey) | Helyszíni mérés, pontos adatok, térbeli elhelyezés | Tervezés → implementáció |
| **5** | Portál felület a folyamat követéséhez | A felhasználó látja hol tart a megrendelés | Portál fejlesztés |
| Később | Tervezés modul | Egyedi modellezés, 3D | Ha van rá ügyfél |
| Később | Beépítés modul | Helyszíni szerelés kezelése | Ha van rá ügyfél |

---

## Az építkezés logikája

Először a **közlekedési rendszert** építjük meg (váltók, sínek, állomás-struktúra), és utána a **konkrét állomásokat** (értékesítés, felmérés). Ez azért fontos, mert:

- Ha a közlekedési rendszer jó, bármilyen új állomás hozzáadható anélkül, hogy az alaprendszert módosítanánk
- Ha az alaprendszer nincs meg, minden új lépést kézzel kell összedrótozni — és az nem skálázódik

A gyártás (Joinery v1 + Abstractions) már kész — ez az első állomás ami működik. A Stage Registry köti össze a többivel.

---

*SpaceOS — Workflow Stage Architecture · Üzleti összefoglaló · 2026-04-10*
