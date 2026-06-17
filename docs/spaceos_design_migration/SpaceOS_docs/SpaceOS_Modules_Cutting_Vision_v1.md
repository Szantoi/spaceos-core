# SpaceOS — Modules.Cutting: A Szabászat Világa
## Domain Vision · Fogalmi váz · Rendszertérkép

> **Verzió:** v1.2 — 2026-04-12
> **Státusz:** FOGALMI VÁZ — az implementációs fázisok ebből származnak
> **Döntéshozó:** Gábor (Architect & Founder)
> **Kontextus:** Design session 2026-04-12 — domain discovery + integration architecture + module decomposition
> **Előzmények:** Modules.Abstractions (Graph Engine) DONE · Modules.Joinery v1 DONE · ADR-019 (közös vs trade-specifikus)
> **Kapcsolódó dokumentumok:** SpaceOS_Ecosystem_Module_Architecture_v1.md · SpaceOS_Product_Configuration_Engine_Vision_v1.md
> **Changelog:**
> - v1.0: Alap domain discovery — fogalomtár, három dimenzió, fázisolás
> - v1.1: Contract/Implementation szétválasztás — ICuttingProvider pattern
> - v1.2: Hárommodulos dekompozíció — Cutting + Inventory + Procurement szétválasztás

---

## 1. Mi a szabászat?

A szabászat a faiparos gyártás fizikai kapuja. Minden ami a tervezőasztalon készül — konfiguráció, számítás, gráf — itt válik anyaggá. A szabász üzem egy önálló világ: saját napi ritmusa, saját optimalizálási problémái, saját hulladéka és maradékai.

**A szabászat nem egy feature — hanem egy teljes üzleti domain.**

Egy ajtógyártó, egy szekrénygyártó és egy lapszabász szolgáltató mind ugyanazt a fundamentális problémát oldja meg: adott alkatrészeket kell kivágni adott alapanyagokból, minimális veszteséggel, maximális hatékonysággal.

---

## 2. A valóság: a szabászat nem zöldmezős

Egy faiparos cégnek **már van** szabász megoldása — vagy szoftver (OptiCut, CutRite, Holzma CADmatic), vagy Excel, vagy papír. A SpaceOS nem mondhatja: "dobd ki és használd a miénket." Azt kell mondania: **"akármit használsz, bekapcsolódik."**

```
Doorstar ma                          Doorstar + SpaceOS
───────────                          ──────────────────
Excel szabászlista                   SpaceOS kiszámolja → CuttingSheet
  ↓                                    ↓
Kézi kiosztás (papír)               Opció A: SpaceOS nesting (beépített)
  VAGY                               Opció B: OptiCut (külső szoftver)
OptiCut / CutRite szoftver            Opció C: kézi kiosztás (Portal UI)
  ↓                                    ↓
Vágás                                Végrehajtás tracking (SpaceOS VAGY külső)
```

### Az alapelv: Contract/Implementation szétválasztás

A Cutting modul **nem monolit alkalmazás**, hanem két réteg:

| Réteg | Mi | Analógia |
|-------|-----|---------|
| **Contract** | A SpaceOS által definiált szerződés: "aki szabász szolgáltatást nyújt, ezt az interface-t valósítja meg" | `ILlmProvider` az Orchestrator-ban → Gemini / OpenAI / Mock |
| **Implementation** | Egy konkrét megvalósítás (SpaceOS beépített VAGY külső adapter) | `GeminiProvider` / `OpenAIProvider` / `MockProvider` |

Ez a minta **mindhárom modulra érvényes** — Cutting, Inventory és Procurement egyaránt.

---

## 3. Hárommodulos dekompozíció

### Miért három modul?

A szabászat, a raktározás és a beszerzés három különböző felelősségi kör, három különböző ember kezében:

| Modul | Felelős személy | Kérdés amit megválaszol |
|-------|----------------|------------------------|
| **Cutting** | Szabász / gépkezelő | "Mit vágunk, hogyan, mi maradt?" |
| **Inventory** | Raktáros | "Mi van, hol van, mennyi fogyott?" |
| **Procurement** | Beszerzős | "Kitől, mennyiért, mikor rendelek?" |

A versenyeztetést nem kell a raktárosnak látnia. A nesting-et nem kell a beszerzősnek értenie. A napi szabásztervet nem kell a szállítókezelőnek ismernie.

### Aktor × Modul mátrix

| Aktor | Cutting | Inventory | Procurement |
|-------|:-:|:-:|:-:|
| **Manufacturer (Doorstar)** | ✅ | ✅ | ✅ |
| **PanelCutter (lapszabász)** | ✅ | ✅ | ✅ |
| **Cabinet maker** | ✅ | ✅ | ✅ |
| **Trader (kereskedő)** | ❌ | ✅ | ✅ |
| **Installer (beszerelő)** | ❌ | ✅ | opcionális |
| **EndCustomer** | ❌ | ❌ | ❌ |

A Trader-nek Inventory + Procurement kell, Cutting nélkül. Az Installer-nek Inventory kell (szerelvények, vasalatok), Procurement opcionális. Mindhárom önállóan is működik.

### Dependency irány (nincs kör)

```
                    ┌──────────────────────────────────┐
                    │          Modules.Cutting          │
                    │  szabászlista · nesting · vágás   │
                    └───────────────┬──────────────────┘
                                    │
                  "Felhasználtam 2 táblát"
                  "Van-e elég MDF 18mm?"
                  "Maradék keletkezett: 800×400mm"
                                    │
                                    ▼
                    ┌──────────────────────────────────┐
                    │         Modules.Inventory         │
                    │  készlet · anyagtörzs · mozgás    │
                    └───────────────┬──────────────────┘
                                    │
                  "Készletszint kritikus!"
                  "Megérkezett a szállítmány"
                                    │
                                    ▼
                    ┌──────────────────────────────────┐
                    │        Modules.Procurement        │
                    │  szállítók · rendelés · árak      │
                    └──────────────────────────────────┘
```

---

## 4. Modules.Cutting — fogalomtár

A Cutting a szabászati végrehajtás domainje. Input: alkatrészlista. Output: kivágott darabok + hulladék.

### Fogalmak

**CuttingSheet** — Egy megrendeléshez vagy tételhez tartozó szabászati adatcsomag. Tartalmazza az összes alkatrészt, CNC műveleteket és gyártási lépéseket. Immutable snapshot — ha újraszámoljuk, új CuttingSheet keletkezik, a régi megmarad (audit trail).

**CuttingLine** — Egy alkatrész a szabászlistában. Név, típus, nyers méret, szabászati méret (oversize-zal), anyag, vastagság, darabszám.

**CncInstruction** — Egy CNC megmunkálási utasítás egy alkatrészhez. Fúrás, marás, hornyolás, élzárás. A vágás utáni művelet.

**ProcessStep** — Egy gyártási lépés a teljes folyamatban. Vágás → CNC → Élzárás → Felület → Összeszerelés → Csomagolás. Becsült idő, sorrend.

**DailyCuttingPlan** — A napi szabászati terv. Összefogja az aznap elvégzendő vágásokat, csoportosítja anyagtípus szerint (minden MDF 18mm egyszerre → kevesebb gépátállás).

**CuttingBatch** — Egy köteg azonos anyagból. Egy batch = egy anyagtípusból készülő vágások csoportja. A gép egyszer áll be, és folyamatosan vágja.

**PanelAssignment** — A nesting eredménye: melyik alkatrész melyik táblára kerül. "Erre a 2800×2070 MDF 18mm táblára ezek a CuttingLine-ok mennek, ez a maradék keletkezik."

**CuttingExecution** — A vágás végrehajtásának nyomon követése. Ki végezte, mikor kezdte, mikor fejezte be. FSM: `Planned → InProgress → Completed / Failed`.

**Waste** — Hulladék. Ami a vágásból keletkezett és nem használható fel. A Cutting méri, az Inventory-nak jelenti.

**Capacity** — A szabász üzem kapacitása. Hány négyzetméter anyagot tud feldolgozni egy nap/műszak.

### Contract

```
ICuttingProvider
├── SubmitCuttingSheet(sheet)       → szabászlista regisztrálása
├── GetNestingResult(sheetId)       → tábla-kiosztás lekérdezése
├── GetExecutionStatus(sheetId)     → végrehajtás állapota
├── GetWasteReport(dateRange)       → hulladék riport
└── OnEvent(callback)               → esemény értesítések
```

### Implementációk

| Implementáció | Ki használja |
|---------------|-------------|
| **SpaceOS beépített** | Doorstar soft launch, kisebb cégek |
| **OptiCut Adapter** | Cégek akik OptiCut-ot használnak |
| **CutRite Adapter** | Cégek akik CutRite-ot használnak |
| **Manuális** | Aki papíron / Excel-ben dolgozik |

---

## 5. Modules.Inventory — fogalomtár

Az Inventory a készletgazdálkodás domainje. Tudja mi van, hol van, mennyi fogyott, mi a maradék.

### Fogalmak

**MaterialCatalog** — Az anyagok törzsadata. MDF 18mm, HDF 3mm, forgácslap 25mm, ABS él 0.8mm, stb. Sztenderd méret, egységár, szállító referencia. Ritkán változik — referencia adat.

**PanelStock** — A raktárkészlet. Hány tábla van az adott anyagból, milyen méretben, melyik raktárhelyen. Két típus:
- **Teljes tábla** — gyári méret (pl. 2800×2070mm MDF 18mm)
- **Maradék (Offcut)** — korábbi vágásból megmaradt, újrafelhasználható darab

**Offcut** — A maradék. Mérete ismert, anyaga ismert, eredete nyomon követhető (melyik vágásból keletkezett). Három sors:
```
Offcut keletkezik
    ├─→ Visszakerül a készletbe (elég nagy, felhasználható)
    ├─→ Hulladék (túl kicsi vagy sérült)
    └─→ Felhasználva egy későbbi vágásban
```

**StockMovement** — Minden készletváltozás naplója. Bevételezés (szállítótól), felhasználás (vágáskor), maradék keletkezés, selejtezés, visszavásárlás. Audit trail.

**StockLocation** — Raktárhely. Fizikai lokáció ahol az anyag tárolva van. Polc, sor, szektor.

**StockCount** — Leltár. Fizikai számlálás eredménye vs. rendszer szerinti készlet.

**ConsumptionTrend** — Fogyási trend. Napi/heti/havi anyagfelhasználás. Szezonalitás, átlag, kiugró értékek.

**ReorderThreshold** — Minimális készletszint. Ha a PanelStock ez alá csökken, jelzést küld a Procurement-nek.

### Contract

```
IInventoryProvider
├── GetStock(materialType)          → készletszint lekérdezés
├── GetOffcuts(materialType)        → felhasználható maradékok
├── RecordConsumption(items)        → felhasználás rögzítése (Cutting hívja)
├── RecordInbound(delivery)         → bevételezés (Procurement hívja)
├── RecordOffcut(offcut)            → maradék regisztrálás (Cutting hívja)
├── GetConsumptionTrend(range)      → fogyási trend
└── OnLowStock(callback)            → készletszint jelzés → Procurement felé
```

### Implementációk

| Implementáció | Ki használja |
|---------------|-------------|
| **SpaceOS beépített** | Kisebb cégek, Doorstar |
| **WMS Adapter** | Cégek akiknek raktárkezelő rendszerük van |
| **ERP Készlet Adapter** | SAP / Dynamics készletmodul integráció |

---

## 6. Modules.Procurement — fogalomtár

A Procurement a beszerzés domainje. Tudja kitől, mennyiért, mikor rendeljünk, mi érkezett meg.

### Fogalmak

**Supplier** — Szállító cég. Név, kapcsolat, szállítási feltételek, lead time, megbízhatósági rating.

**PriceList** — Árlista. Szállítónkénti anyagárak. Mennyiségi kedvezmények, érvényesség, pénznem.

**PurchaseOrder** — Beszerzési rendelés. Szállító + anyag + mennyiség + ár + várható szállítás. FSM: `Draft → Submitted → Confirmed → Shipped → Delivered / Cancelled`.

**Delivery** — Szállítás fogadás. A rendelés fizikai megérkezése. Darabszám ellenőrzés, minőség, bevételezés az Inventory-ba.

**ReorderAlert** — Rendelési jelzés. Az Inventory küldi amikor a készletszint a threshold alá csökken. A Procurement dönt: melyik szállítótól, milyen áron, milyen mennyiségben.

**SupplierRating** — Szállító értékelés. Szállítási pontosság, minőség, ár-érték arány. Historikus adat.

### Contract

```
IProcurementProvider
├── CreatePurchaseOrder(order)      → rendelés létrehozás
├── GetOrderStatus(orderId)         → rendelés állapota
├── GetSupplierPrices(material)     → árlista lekérdezés
├── RecordDelivery(delivery)        → szállítás fogadás → Inventory-ba
├── GetSupplierRating(supplierId)   → szállító értékelés
└── OnReorderAlert(callback)        → Inventory készletjelzés fogadása
```

### Implementációk

| Implementáció | Ki használja |
|---------------|-------------|
| **SpaceOS beépített** | Kisebb cégek, Doorstar |
| **EDI Adapter** | Szállítói elektronikus adatcsere |
| **ERP Beszerzés Adapter** | SAP / Dynamics beszerzési modul |

---

## 7. A három modul együttműködése

### Példa: Doorstar napi szabászat

```
1. DoorOrder kalkuláció kész
   Joinery → CuttingSheet létrejön (Cutting)
                │
2. Napi tervezés
   Cutting: "Holnap 3 MDF 18mm + 2 HDF 3mm CuttingSheet van"
                │
3. Készlet ellenőrzés
   Cutting → Inventory: "Van-e 8 tábla MDF 18mm?"
   Inventory: "5 teljes + 2 maradék (800×400, 1200×600)"
                │
4. Nem elég → rendelés
   Inventory → Procurement: "ReorderAlert: MDF 18mm, hiány 1 tábla"
   Procurement: PurchaseOrder → Szállító
                │
5. Szállítás megérkezik
   Procurement → Delivery → Inventory: bevételezés (+3 tábla)
                │
6. Nesting
   Cutting: kiosztás a 8 táblára (5 teljes + 2 maradék + 1 új)
                │
7. Vágás végrehajtás
   Cutting: CuttingExecution → Completed
                │
8. Eredmény
   Cutting → Inventory: "Felhasználtam 6 táblát, 
                          3 maradék keletkezett (900×300, 700×500, 400×200),
                          hulladék: 1.2 m²"
   Inventory: PanelStock frissítve, Offcut-ok regisztrálva
```

### Adatfolyam diagram

```
               Modules.Door / Cabinet
                       │
                CuttingSheet (számított alkatrészlista)
                       │
                       ▼
              ┌─ Cutting ─────────────────────────────────┐
              │  DailyCuttingPlan                         │
              │    ↓                                      │
              │  "Van-e elég anyag?" ──→ Inventory query  │
              │    ↓                                      │
              │  PanelAssignment (nesting)                 │
              │    ↓                                      │
              │  CuttingExecution                         │
              │    ↓                                      │
              │  Waste + Offcut eredmény ──→ Inventory    │
              └───────────────────────────────────────────┘
                                                │
              ┌─ Inventory ───────────────────────────────┐
              │  PanelStock frissítés                     │
              │  Offcut regisztrálás                      │
              │  ConsumptionTrend frissítés               │
              │    ↓                                      │
              │  "Készlet alacsony!" ──→ Procurement      │
              └───────────────────────────────────────────┘
                                                │
              ┌─ Procurement ─────────────────────────────┐
              │  PurchaseOrder → Supplier                 │
              │  Delivery fogadás → Inventory bevételezés │
              └───────────────────────────────────────────┘
```

---

## 8. Domain határok

| Kérdés | Cutting | Inventory | Procurement | Egyik sem |
|--------|:-:|:-:|:-:|:-:|
| Alkatrész kiszámítása | | | | ❌ Abstractions |
| CNC művelet meghatározása | | | | ❌ Abstractions |
| Szabászlista tárolás | ✅ | | | |
| Tábla-kiosztás (nesting) | ✅ | | | |
| Napi szabászterv | ✅ | | | |
| Vágás végrehajtás tracking | ✅ | | | |
| Hulladék mérés | ✅ | | | |
| Készletszint nyilvántartás | | ✅ | | |
| Maradék (offcut) kezelés | | ✅ | | |
| Anyagtörzs (katalógus) | | ✅ | | |
| Készletmozgás napló | | ✅ | | |
| Raktárhely kezelés | | ✅ | | |
| Leltár | | ✅ | | |
| Fogyási trend | | ✅ | | |
| Szállító kezelés | | | ✅ | |
| Árlista, versenyeztetés | | | ✅ | |
| Rendelés (PO) | | | ✅ | |
| Szállítás fogadás | | | ✅ | |
| Szállító értékelés | | | ✅ | |
| Gyártásilap PDF | | | | ❌ Portal |
| Ajtó/szekrény logika | | | | ❌ Door/Cabinet |

---

## 9. A nesting probléma

A szabászat szíve: hogyan osztjuk ki az alkatrészeket a táblákra?

### A probléma

Adott:
- N darab téglalap alakú alkatrész (CuttingLine: szélesség × magasság)
- M darab rendelkezésre álló tábla (PanelStock — Inventory-ból lekérdezve)
- Vágási rés (fűrészlap vastagság, jellemzően 4mm)
- Forgathatóság (az alkatrész elforgatható-e 90°-kal? → dekorminta függő)

Cél: minimális hulladék, maximális maradék-felhasználás.

### Bonyolítás a valóságban

| Tényező | Hatás |
|---------|-------|
| Dekorminta iránya | Nem minden alkatrész forgatható — az erezetnek egyeznie kell |
| Élzárás | Az alkatrész széleit el kell lássuk éllel — ez befolyásolja az oversize-t |
| Gépkorlátok | A gép maximális és minimális darabmérete |
| Maradék prioritás | Először a maradékot használjuk, aztán új táblát |
| Vágási sorrend | A gép fizikailag hogyan tud vágni — nem minden kiosztás vágható |

### Algoritmikus mélység

| Szint | Megközelítés | Ki oldja meg |
|-------|-------------|-------------|
| **L0 — Manuális** | A felhasználó maga osztja ki (Portal UI drag & drop) | SpaceOS Portal |
| **L1 — Greedy** | First Fit Decreasing Height — egyszerű, gyors, ~85% | SpaceOS beépített |
| **L2 — Heurisztikus** | Guillotine-cut optimalizáció — valódi szabász logika | SpaceOS beépített |
| **L3 — Külső szoftver** | OptiCut, CutRite, CADmatic — ipari megoldás | Adapter (Contract) |
| **L4 — AI-assisted** | LLM + constraint solver — komplex, multi-tábla | Horizon 3 |

A Contract biztosítja, hogy bármelyik szint használható legyen — a SpaceOS nem kényszerít implementációt.

---

## 10. A három modul helye a SpaceOS rétegekben

```
L4  Portal             PDF renderelés (tenant-specifikus layout)
                       Nesting vizualizáció · Raktár dashboard
                       Beszerzési UI · Szállítói összehasonlítás
                            │
L3  Orchestrator       Provider választás (tenant konfig alapján)
                       Adapter hívás (külső rendszerek felé)
                            │
                       ┌────┴───────────────────────────────────────────────┐
                       │  Contracts (NuGet packages)                        │
                       │  ICuttingProvider · IInventoryProvider ·           │
                       │  IProcurementProvider · DTO-k · Events · Enums    │
                       └────┬──────────────────┬───────────────────────────┘
                            │                  │
                  ┌─────────▼───────┐  ┌───────▼──────────┐
                  │ SpaceOS Built-in│  │ External Adapter  │
                  │ (saját DB)      │  │ (OptiCut / SAP /  │
                  │                 │  │  WMS / EDI)       │
                  └─────────────────┘  └──────────────────┘
                            │
L2  Trade Modules      Joinery / Cabinet / Window
                       → CuttingSheet létrehozás
                       → ICuttingProvider.SubmitCuttingSheet()
                            │
L1  Kernel             FlowEpic · Stage Registry · B2B Handshake
```

---

## 11. B2B Relativity a három modulban

```
Doorstar (Manufacturer)          LapMester Kft. (PanelCutter)
─────────────────────           ─────────────────────────────
Cutting:   PRODUCER              Cutting:   CONSUMER
  "Én számoltam,                   "Én kaptam,
   én adom fel"                     én hajtom végre"

Inventory: CONSUMER              Inventory: PRODUCER
  "Én veszem                       "Én tartom
   az anyagot"                      raktáron"

Procurement: BUYER               Procurement: SELLER
  "Én rendelek"                    "Én szállítok"
```

A Contract DTO-k a B2B Handshake közös nyelve.

---

## 12. Fázisolás — a három modul mélységi fokozatai

### Modules.Cutting fázisok

| Fázis | Tartalom | Prereq |
|-------|----------|--------|
| **Contract** | `ICuttingProvider` interface + DTO-k + Events | Abstractions DONE ✅ |
| **Core** | CuttingSheet + DailyCuttingPlan + CuttingBatch + API | Contract DONE |
| **Planning** | PanelAssignment + nesting (L1-L2) + kapacitás | Core DEPLOYED |
| **Execution** | CuttingExecution FSM + operátor tracking + Stage Registry | Planning DEPLOYED |
| **Analytics** | Waste % + kapacitás kihasználtság | Execution DEPLOYED |
| **Adapters** | OptiCut / CutRite integráció | Contract DONE |

### Modules.Inventory fázisok

| Fázis | Tartalom | Prereq |
|-------|----------|--------|
| **Contract** | `IInventoryProvider` interface + DTO-k + Events | — |
| **Core** | MaterialCatalog + PanelStock + Offcut + StockMovement + API | Contract DONE |
| **Locations** | StockLocation + raktárhely kezelés | Core DEPLOYED |
| **Counting** | StockCount + leltár workflow | Locations DEPLOYED |
| **Trends** | ConsumptionTrend + ReorderThreshold + alert | Core DEPLOYED |
| **Adapters** | WMS / ERP készlet integráció | Contract DONE |

### Modules.Procurement fázisok

| Fázis | Tartalom | Prereq |
|-------|----------|--------|
| **Contract** | `IProcurementProvider` interface + DTO-k + Events | — |
| **Core** | Supplier + PurchaseOrder + Delivery + API | Contract DONE |
| **Pricing** | PriceList + versenyeztetés + mennyiségi kedvezmény | Core DEPLOYED |
| **Rating** | SupplierRating + historikus értékelés | Core DEPLOYED |
| **Adapters** | EDI / ERP beszerzés integráció | Contract DONE |

### Sorrendiség

A három Contract egymástól függetlenül tervezhető. A Core fázisok közül az **Inventory Core az első** — mert a Cutting Core függ tőle (készlet lekérdezés).

```
Inventory Contract → Inventory Core
                          │
Cutting Contract ─────────┤
                          ▼
                    Cutting Core
                          │
Procurement Contract ─────┤
                          ▼
                    Procurement Core
```

---

## 13. Amit ez a dokumentum NEM tartalmaz

| Téma | Miért nem | Hol lesz |
|------|-----------|----------|
| Migration DDL | Implementáció | Fázis-specifikus arch-planner tervdok |
| C# domain modell részletek | Implementáció | Fázis-specifikus arch-planner tervdok |
| Nesting algoritmus specifikáció | Planning fázis scope | Cutting Planning tervdok |
| Portal UI wireframe | Prezentációs réteg | Portal design session |
| Adapter specifikáció (OptiCut API) | Partner-specifikus | Cutting Adapters tervdok |
| PDF gyártásilap layout | Tenant-specifikus prezentáció | Portal / Orchestrator réteg |

---

*SpaceOS — Modules.Cutting: A Szabászat Világa v1.2 · 2026. április · Domain Vision*
*Hárommodulos dekompozíció: Cutting + Inventory + Procurement*
*Contract/Implementation szétválasztás mindhárom modulon*
*Ez a váz — a részletek fázisonként következnek.*
