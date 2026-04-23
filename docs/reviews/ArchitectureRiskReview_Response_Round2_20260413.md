# SpaceOS — Architektúra Kockázatelemzés Válasz, 2. kör

**Keletkezés:** 2026-04-13  
**Válaszol:** Architect (SpaceOS)  
**Forrás:** `ArchitectureRiskReview_Round2_20260413.md` (Devil's Advocate analízis, 2. kör)  
**Előzmények:** 1. kör + válasz (`ArchitectureRiskReview_Response_20260413.md`)  
**Státusz:** LEZÁRVA — akciók rögzítve

---

## Előszó

A 2. kör lényegesen erősebb, mint az 1. volt. A 6 új kockázatból 4 helyes és cselekvést igényel (#7, #9, #10, #12), 1 releváns de rossz időzítéssel (#8), és 1 félreérti a célt (#11). Az 1. körnél a precision 33% volt a KRITIKUS besorolásra — itt ~67%.

---

## Összesítő

| # | Kockázat | DA értékelés | Architekt értékelés | Indoklás |
|---|---|---|---|---|
| 7 | Escrow PSD2/MNB engedélyezés | 🔴 KRITIKUS | 🟡 KÖZEPES — **helyes kérdés, de a modell más** | SpaceOS nem tartja a pénzt — "soft escrow" state machine |
| 8 | Graph Engine IFC hiánya (NIH) | 🔴 MAGAS | 🟡 KÖZEPES — **helyes, de rossz időzítéssel** | IFC bridge tervezve (Phase D, ~10 nap), de nem 2026-os prioritás |
| 9 | HU KKV digitális érettség / CAC | 🔴 MAGAS | 🔴 MAGAS — **teljes egyetértés** | A legvalósabb üzleti kockázat |
| 10 | Anthropic API cost + EU residency | 🟡 KOCKÁZAT | 🟡 KOCKÁZAT — **egyetértés, mitigáció részben megvan** | ILlmProvider absztrakció + Gemini production; EU residency valós kérdés |
| 11 | Multi-brand prematur optimalizáció | 🟡 KOCKÁZAT | 🟢 ALACSONY — **félreérti a célt** | Nem 2 brand 1 ügyfélnek, hanem 2 piac 2 nyelven |
| 12 | Single design partner dependency | 🔴 MAGAS | 🔴 MAGAS — **teljes egyetértés** | Legsúlyosabb üzleti kockázat a Doorstar-on túl |

---

## 7. Escrow PSD2/MNB engedélyezés — helyes kérdés, a modell más

**DA állítás:** Ha a SpaceOS ténylegesen megkapja a megrendelő pénzét → MNB engedélyköteles (125k–350k EUR induló tőke, 6–12 hónap).

**Ez helyes lenne, ha a SpaceOS pénzforgalmi szolgáltató lenne. De nem az.**

### A SpaceOS Escrow modell — "Soft Escrow" state machine

A SpaceOS Escrow funkciója **nem pénzforgalmi szolgáltatás**, hanem egy auditált állapotgép, amely az alábbi workflow-t biztosítja:

```
Megrendelő ──(1. megrendel)──→ FlowEpic BACKLOG_READY
    │
Gyártó ──(2. gyárt)──→ FlowEpic IN_DEV → REVIEW → ACCEPTED
    │
SpaceOS ──(3. igazol)──→ AggregateSnapshot + ProofHash (bizonyíték rögzítés)
    │
SpaceOS ──(4. értesít)──→ "A munka elfogadva — fizess a gyártónak"
    │
Megrendelő ──(5. utal)──→ Közvetlenül a gyártó bankszámlájára
    │
Gyártó ──(6. visszaigazol)──→ FlowEpic CLOSED_DONE
```

**A pénz soha nem megy át a SpaceOS számlán.** A SpaceOS:
- Rögzíti a teljesítés bizonyítékát (ProofHash + WORM + Snapshot)
- Értesíti a feleket a milestone elfogadásáról
- Timestamp-eli az eseményeket (jövőben OpenTimestamps / TSA)
- Vitarendezéshez bizonyítékot szolgáltat

Ez a modell a PSD2 Art. 3(b) "commercial agent exemption" hatálya alá eshet, **de a DA helyes abban, hogy ez jogi tisztázást igényel**.

### DA források értékelése

| Forrás | Kommentár |
|---|---|
| 2009. évi LXXXV. törvény | Releváns — de a SpaceOS nem végez "átutalás"-t és nem "fogad pénzösszeget" |
| PSD2 Art. 3(b) exemption | A DA helyesen hivatkozza az EBA Opinion-t — marketplace esetén nem alkalmazható. DE: a SpaceOS nem marketplace (nem két fél közötti tranzakciót közvetít), hanem B2B project management tool |
| EBA-Op-2019-04 | Ez marketplace modellekre vonatkozik (Uber, Airbnb) — a SpaceOS workflow management, nem marketplace |
| Stripe Connect / MangoPay | Ezek ténylegesen tartják a pénzt — a SpaceOS nem |

### Amit elfogadok: jogi tisztázás kötelező

Annak ellenére, hogy a "soft escrow" modell valószínűleg nem igényel MNB engedélyt, a jogi bizonytalanság valós. A SpaceOS "escrow" marketing-nyelven használja a fogalmat, miközben jogilag más történik.

**Szükséges akció:**

| # | Akció | Határidő | Prioritás |
|---|---|---|---|
| 7.1 | Jogi vélemény kérése: a "soft escrow" state machine modell pénzforgalmi szolgáltatásnak minősül-e a Pft. szerint | Escrow GA sprint előtt | **P0** |
| 7.2 | Az "escrow" szó marketing-használatának felülvizsgálata — ha jogilag nem escrow, ne nevezzük annak | Q3 2026 | P1 |
| 7.3 | Ha jogi vélemény alapján PSP szükséges: Barion Escrow / MangoPay integráció értékelése | Jogi vélemény után | Feltételes |

### DA nyitott kérdéseire válasz

| Kérdés | Válasz |
|---|---|
| Mi az Escrow modellje? | Soft escrow — state machine + bizonyíték rögzítés. Pénz nem megy át SpaceOS-en. |
| PSP partner költsége? | Ha szükséges, Barion ~0.5–1.5% / tranzakció. Ez az ARPU-ból levonandó. |
| Escrow GA sprint mikor? | Backlogban, Doorstar pilot live után. A jogi vélemény a sprint indulási feltétele. |

---

## 8. Graph Engine IFC szabvány — helyes, de rossz időzítéssel

**DA állítás:** A házilag épített Graph Engine NIH (Not Invented Here) szindróma — IFC szabvány létezik, xBIM Toolkit elérhető.

**Ez a leginkább árnyalt pont a 2. körben.** A DA egyszerre van igaza és téved.

### Miben van igaza a DA-nak

Az IFC interoperabilitás **a DACH piac belépési feltétele**. Ez dokumentált és tervezett:

- `SpaceOS_Product_Configuration_Engine_Vision_v1.md` — Phase D: "IFC Bridge (xbim .NET), Revit kompatibilitás, BIM metadata mapping" — **~10 nap becsült effort**
- `SpaceOS_Modules_Abstractions_Architecture_v4.md` — Roadmap #4: "IFC Bridge (Revit import/export)" — **P1 Horizon 2**
- Import/Export tábla explicit tartalmazza: IFC 2×3 / 4 import, IFC export Revit-hez

### Miben téved a DA

A Graph Engine **nem az IFC újrafeltalálása**. Más problémát old meg:

| Szempont | IFC (ISO 16739) | SpaceOS Graph Engine |
|---|---|---|
| Cél | Épületmodellek cseréje alkalmazások között | Parametrikus termék-konfiguráció → CNC deriváció |
| Adatmodell | Épületelemek (IfcWall, IfcDoor, IfcSlab) | Gyártási komponensek (ComponentSlot, SlotConnection, CalculationRule) |
| Számítási modell | Nincs — geometriai leírás, nem szabályalapú | Kahn's BFS topologikus rendezés + paraméter-propagáció |
| CNC deriváció | Nem támogatja | `DeriveCncPlan()` + `DeriveProcessPlan()` — a core use case |
| Ki használja | Építészek, BIM koordinátorok | Gyártók, műhelyvezetők |

Az IFC egy **exchange format**, nem egy konfigurátor motor. Az xBIM Toolkit-et arra használjuk majd, amire való: **import/export bridge**, nem a Graph Engine helyettesítőjeként.

### A CNC output formátumról

A DA kérdezi: "A `DeriveCncPlan()` output szabványos (STEP, DXF, G-code) vagy proprietáris JSON?"

Jelenlegi állapot: **strukturált JSON** — `CncInstructionDto` (ComponentName, Operation, Position, Diameter, Depth, Angle). Ez szándékos:

1. A JSON a **köztes formátum** — post-processor specifikus G-code-ot generálni ebből triviális (Phase F: "CNC Post-processor — G-code generálás, gépspecifikus" ~8 nap)
2. A Doorstar-nál **nincs CNC gép** — manuális műhelyen dolgoznak, szabászlistát kapnak PDF-ben
3. A G-code/DXF/STEP export a 2027 roadmap része (Horizon 2)

### Szükséges akció

| # | Akció | Határidő | Prioritás |
|---|---|---|---|
| 8.1 | IFC Bridge (Phase D) scope és effort finomítása | DACH demo előtt (2027 Q1) | P1 |
| 8.2 | xBIM Toolkit PoC (import/export egy egyszerű ajtóval) | 2026 Q4 | P2 |
| 8.3 | CNC Post-processor (Phase F) — G-code output Homag-hoz | Első CNC-s ügyfél előtt | P2 |

A DA által javasolt "xBIM Toolkit wrapper az `IParametricProduct` alatt" nem helyes architektúra — az IFC bridge **az output réteg**, nem az engine maga. A helyes integráció:

```
Graph Engine (Modules.Abstractions)
    → DeriveCncPlan() → CncInstructionDto (JSON)
    → Phase F: PostProcessor → G-code / DXF
    → Phase D: IFC Bridge (xBIM) → IFC export Revit/ArchiCAD-nak
```

---

## 9. HU KKV digitális érettség / CAC — teljes egyetértés

**DA állítás:** A magyar faiparos KKV-k digitális érettsége alacsony, a CAC validálatlan, a TAM szám nem pipeline.

**🔴 Ez a 2. kör legfontosabb pontja. Teljes mértékben egyetértek.**

### Amit a DA helyesen azonosít

| Tény | Következmény |
|---|---|
| DESI 2023: HU 22. hely, KKV digital intensity 52% | A célcsoport aktívan nem keres SaaS megoldást |
| <10 fős vállalkozások felhő SaaS használata: 27% | Outbound sales szükséges, PLG nem működik |
| Construction SaaS CAC break-even: ~30 hónap | Az ARPU-nak ehhez igazodnia kell |
| McKinsey: 2. legkevésbé digitalizált szektor | Early adopter ≠ representative customer |

### A helyzet őszinte értékelése

| Kérdés | Jelenlegi állapot |
|---|---|
| Validált WTP (Willingness To Pay) Doorstar-on kívül | ❌ Nincs — informális beszélgetések vannak, formális WTP interjú nincs |
| Becsült CAC | ❌ Nincs mérve — outbound sales motion nincs definiálva |
| 10+ szándéknyilatkozat | ❌ Nincs — Doorstar az egyetlen formális partner |
| Sales motion terv | ❌ Nincs dokumentálva |
| ARPU target | ❌ Nincs validálva piaci adatokkal |

### A Doorstar mint reference: erős, de nem elég

A Doorstar validálja, hogy a **probléma valós** (Viber + Excel koordináció = fájdalompont). De egyetlen customer validálja a problémát, nem a megoldást és nem az árazást. A DA Steve Blank hivatkozása helyes: minimum 3–5 párhuzamos design partner szükséges.

### Szükséges akció

| # | Akció | Határidő | Prioritás |
|---|---|---|---|
| 9.1 | 10 non-Doorstar céggel strukturált WTP interjú (nem értékesítés, hanem validáció) | **2026 Q3 előtt** | **P0** |
| 9.2 | CAC modell: outbound vs. referral vs. szakmai szövetségi partnerség értékelése | 2026 Q3 | P1 |
| 9.3 | ARPU target meghatározása a CAC break-even alapján (30 hónapos ciklus → ARPU minimum) | 2026 Q3 | P1 |
| 9.4 | Sales motion terv dokumentálása (ki, hogyan, milyen csatornán) | 2026 Q3 | P1 |

### Reális piaci megközelítés

A DA helyesen jelzi, hogy PLG nem működik Viber-es cégeknél. A reális sales motion:

| Csatorna | Hatékonyság | Költség |
|---|---|---|
| Szakmai szövetség (Magyar Bútor- és Faipari Szövetség) | Magas — hitelesség + reach | Alacsony — tagdíj + előadás |
| Doorstar referral (elégedett ügyfél ajánl) | Nagyon magas — peer trust | Közel nulla |
| Helyi iparági események (BüsiFa, WoodTech expo) | Közepes — demo lehetőség | Közepes — stand + utazás |
| Outbound cold sales | Alacsony — bizalom hiánya | Magas — idő + személyzet |

---

## 10. Anthropic API cost + EU data residency — egyetértés, mitigáció részben megvan

**DA állítás:** API cost skálázódás + GDPR Art. 44–49 EU data residency problémás.

### Cost értékelés: a DA számítása korrekt, de van kontextus

A DA $0.05–0.08 / session becslése reális Claude Sonnet 4-re. De a jelenlegi production config: **Gemini 2.0 Flash** — nem Claude.

| Provider | Becsült cost / session | Jelenlegi státusz |
|---|---|---|
| Claude Sonnet 4 | $0.05–0.08 | Nem használt production-ben |
| Gemini 2.0 Flash | $0.003–0.008 | **DEPLOYED** — VPS live |
| Claude Haiku | $0.01–0.02 | Alternatíva, ha Gemini nem elég |

Az `ILlmProvider` interfész (Orchestrator) **pontosan erre az esetre van**: provider switch konfigurációs kérdés, nem architektúra-változás.

```typescript
// Orchestrator — meglévő absztrakció
interface ILlmProvider {
  complete(messages: Message[], tools: Tool[]): Promise<LlmResponse>;
}
// Implementációk: AnthropicProvider, OpenAiCompatibleProvider (Gemini)
```

### EU data residency: valós kérdés

A DA helyes: Anthropic szerverei USA-ban vannak. A Gemini szintén (default). De:

| Provider | EU hosting opció | Státusz |
|---|---|---|
| Anthropic (Claude) | EU beta (2026 Q1) | Nem production-ready |
| Google Vertex AI (Gemini) | `europe-west4` (Netherlands) | **Elérhető** |
| Azure OpenAI (GPT-4) | `swedencentral` | Elérhető |

A jelenlegi Gemini 2.0 Flash Vertex AI-on **EU régióban futtatható** — ez a legrövidebb út az EU data residency-hez.

### Milyen adat kerül az LLM promptba?

| Adattípus | Megy az LLM-be? | GDPR kategória |
|---|---|---|
| Ajtó méretek (Sz×M×V) | Igen | Nem személyes adat |
| Anyagtípus, szín | Igen | Nem személyes adat |
| Megrendelő neve, címe | **Nem** — a tool calling paraméterekben nem szerepel | PII — nem küldött |
| Árak, BOM | **Nem** — a C# Driver számolja, nem az LLM | Üzleti titok — nem küldött |

A SpaceOS architektúra eleve védi a PII-t: az LLM kizárólag konfigurációs paramétereket kap (Golden Rule #1), nem ügyfél-adatokat.

### Szükséges akció

| # | Akció | Határidő | Prioritás |
|---|---|---|---|
| 10.1 | Gemini 2.0 Flash → Vertex AI EU régió (`europe-west4`) átállás értékelése | Q2 Soft Launch előtt | P1 |
| 10.2 | GDPR Art. 28 Data Processing Addendum ellenőrzése (Google Vertex AI) | Q2 Soft Launch előtt | P1 |
| 10.3 | LLM prompt audit: PII tartalom dokumentálása (megerősíteni, hogy nincs) | Q2 Soft Launch előtt | P1 |
| 10.4 | Cost monitoring dashboard (API hívások / session / tenant) | 10+ ügyfélnél | P2 |

---

## 11. Multi-brand prematur optimalizáció — a DA félreérti a célt

**DA állítás:** Két brand 1 ügyfélnél = prematur scaling. Conway's Law: 1 fős csapat + 2 brand = mindkettő szenved.

**A DA itt a brand-et "termék duplikáció"-ként értelmezi. De a SpaceOS-ben a két brand két különböző piacot és nyelvet jelent — nem egy ügyfélnek szól.**

### Ami a két brand valójában

| Brand | Nyelv | Célpiac | Domain |
|---|---|---|---|
| **JoineryTech** (.hu) | Angol + magyar | Nemzetközi (DACH Horizon 2) + HU tech-savvy | joinerytech.hu |
| **AszalosTech** (.hu) | Magyar | Magyar KKV-k, akik magyarul keresnek | asztalostech.hu |

Ez **nem prematur optimalizáció**, hanem lokalizációs stratégia:
- A magyar asztalos "asztalos szoftver"-re keres Google-ön, nem "joinery software"-re
- Az AszalosTech brand a magyar SEO és a Doorstar-típusú ügyfélkör belépési pontja
- A JoineryTech a DACH/international piac brand-je

### A technikai overhead értékelése

A DA aggodalmát ("Turborepo build idő, SEO duplikálás, tesztek kétszeres futtatása") nézzük meg a tényeknél:

| Overhead | Valóság |
|---|---|
| Turborepo build idő | A brand system `brand-tokens` + `overrides.ts` — ~50 sor kód. A build idő nem duplázódik. |
| SEO duplikálás | Két domain = két SEO stratégia — ez szándékos, nem duplikáció. `hreflang` tagek kezelik. |
| Tesztek kétszeres futtatása | A tesztek brand-agnosztikusak — a 291 Portal teszt egyszer fut, mindkét brand-re érvényes. |
| Branding asset-ek | Logo + színek + font = 3 fájl. Ez a Turborepo `brand-tokens` package — triviális overhead. |

### Amit elfogadok

Az asztalostech.hu domain **jelenleg nincs aktívan marketelve**. Ha 2026 Q4-re nincs organikus forgalma, a brand freeze indokolt — de nem a technikai overhead miatt, hanem a marketing effort hiánya miatt.

### Szükséges akció

| # | Akció | Határidő | Prioritás |
|---|---|---|---|
| 11.1 | AszalosTech SEO baseline mérés (Google Search Console regisztráció) | Q2 Soft Launch után | P3 |
| 11.2 | Ha 2026 Q4-re nincs organikus forgalom: marketing döntés (nem technikai — a kód marad) | 2026 Q4 | P3 |

---

## 12. Single design partner dependency — teljes egyetértés

**DA állítás:** Egyetlen ügyfél köré épülő platform = "consultware trap". Doorstar kiesése = product death.

**🔴 Ez a #9-cel együtt a két legfontosabb üzleti kockázat. Teljes mértékben egyetértek.**

### A helyzet őszinte értékelése

| Kockázat | Valószínűség | Hatás |
|---|---|---|
| Doorstar-specifikus feature torzulás | 🟡 Közepes | A Graph Engine + Modules.Abstractions eleve generikus — a Doorstar a `FafTTemplateSeeder` seed data, nem hardkódolt logika |
| Exclusivity trap | 🟢 Alacsony | Nincs ilyen szerződés |
| Churn = product death | 🔴 Magas hatás, ha bekövetkezik | Reference customer nélkül az értékesítés lényegesen nehezebb |
| Generalizálás blokkolása | 🟢 Alacsony | ADR-014 (Product Graph Engine) explicit az offset-tábla modellről gráf modellre váltásról szól — ez az abstrakció megvan |

### Mitigiáló architektúra-döntések (ami már megvan)

A SpaceOS architektúra **eleve véd a consultware trap ellen**, de ez nem jelenti, hogy a kockázat nulla:

| Döntés | Hogyan véd |
|---|---|
| `Modules.Abstractions` (generikus Graph Engine) | A Doorstar-specifikus logika = seed data (`FafTTemplateSeeder`), nem kód. Új ügyfél = új seed, nem refactor. |
| `enabledModules` tenant config | Minden ügyfél saját modul-készletet kap. Doorstar = `['door']`, új ügyfél = `['cabinet']` stb. |
| `TenantType` enum (ADR-018: 6 aktor-típus) | A platform nem "ajtógyártó szoftver", hanem faiparos ökoszisztéma — struktúra kész a bővítésre. |
| Polyrepo izolál | `spaceos-modules-joinery` ≠ `spaceos-kernel`. Doorstar-specifikus kód izolált. |

### Amit a kód nem old meg: üzleti diverzifikáció

Az architektúra kész a skálázásra, de **jelenleg egyetlen ügyfél validálja az egészet**. Ez a #9-es kockázattal kombinálva:

- 0 validált WTP a Doorstar-on kívül
- 0 LOI (Letter of Intent)
- 0 párhuzamos design partner

### Szükséges akció

| # | Akció | Határidő | Prioritás |
|---|---|---|---|
| 12.1 | **2 párhuzamos design partner azonosítása** (nem fizető — LOI szintű commitment) | **2026 Q2 Soft Launch előtt** | **P0** |
| 12.2 | Design partner kritériumok definiálása (más vertical? más méret? más régió?) | 2026 Q2 | P0 |
| 12.3 | Doorstar szerződés felülvizsgálata: exclusivity clause kizárása | Azonnal | P0 |
| 12.4 | Risk plan Doorstar drop-out esetre (mit mutatunk befektetőnek / 2. ügyfélnek) | 2026 Q3 | P1 |

### A DA hivatkozásainak értékelése

| Forrás | Kommentár |
|---|---|
| Steve Blank — 3–5 design partner | Helyes — de a SpaceOS open-source core + seed data modell csökkenti a torzulás kockázatát |
| Geoffrey Moore — Crossing the Chasm | A "bowling alley" stratégia releváns: Doorstar = head pin, 2. ügyfél kell a lánc indulásához |
| SaaS Capital — single-customer 67% fail | Ijesztő statisztika, és nem figyelmen kívül hagyható |
| Bessemer — >40% revenue dependency | Ha Doorstar az egyetlen fizető ügyfél = 100% revenue dependency. Ez a definíció szerint red flag. |

---

## Összevont akció-lista (1. + 2. kör)

### P0 — Blokkoló akciók

| # | Kockázat | Akció | Határidő |
|---|---|---|---|
| 3.1 | Single VPS | RTO/RPO definiálás | Q3 2026 előtt |
| 3.2 | Single VPS | PostgreSQL streaming replication | Q3 2026 előtt |
| 3.3 | Single VPS | pg_dump + tesztelt restore | Q3 2026 előtt |
| 7.1 | Escrow PSD2 | Jogi vélemény: soft escrow → Pft. minősítés | Escrow GA előtt |
| 9.1 | CAC validáció | 10 non-Doorstar WTP interjú | Q3 2026 előtt |
| 12.1 | Single partner | 2 párhuzamos design partner LOI | Q2 2026 előtt |
| 12.3 | Single partner | Doorstar exclusivity kizárás | Azonnal |

### P1 — Magas prioritás

| # | Kockázat | Akció | Határidő |
|---|---|---|---|
| 1 | LLM accuracy | E2E 37-tools.chain.test.ts mérés | Következő E2E batch |
| 4 | SHA-256 jogi | OpenTimestamps integráció | Escrow GA sprint |
| 7.2 | Escrow naming | "Escrow" marketing-szó felülvizsgálata | Q3 2026 |
| 8.1 | IFC bridge | Phase D scope finomítás | DACH demo előtt (2027 Q1) |
| 9.2 | CAC modell | Outbound vs. referral értékelés | Q3 2026 |
| 9.3 | ARPU | Target meghatározás CAC break-even alapján | Q3 2026 |
| 10.1 | EU residency | Gemini Vertex AI EU régió értékelés | Q2 Soft Launch előtt |
| 10.2 | GDPR DPA | Google Vertex AI DPA ellenőrzés | Q2 Soft Launch előtt |
| 10.3 | LLM PII audit | Prompt tartalmi dokumentálás | Q2 Soft Launch előtt |
| 12.4 | Risk plan | Doorstar drop-out forgatókönyv | Q3 2026 |

### P2–P3 — Közepes / alacsony prioritás

| # | Kockázat | Akció | Határidő |
|---|---|---|---|
| 2 | Mixed stack | .NET Anthropic SDK figyelés | Folyamatos |
| 3.4 | Single VPS | Incident response playbook | Q3 2026 |
| 5 | Keycloak | realm-export.json git auto-commit | Következő infra task |
| 8.2 | IFC PoC | xBIM Toolkit egyszerű ajtó PoC | Q4 2026 |
| 10.4 | API cost | Cost monitoring dashboard | 10+ ügyfélnél |
| 11.1 | Multi-brand | AszalosTech SEO baseline | Q2 Soft Launch után |

---

## Módszertani megjegyzés

A 2. kör lényegesen erősebb, mint az 1. volt. Az üzleti kockázatok (#9, #12) a legkritikusabbak — ezek nem architektúrával, hanem piaci validációval oldhatók meg. Az architektúra kész a skálázásra (Graph Engine generikus, enabledModules, TenantType enum), de **az architektúra nem tudja kompenzálni az ügyfelek hiányát**.

A két kör összesített tanulsága: a technikai kockázatok (1. kör) kezeltek vagy túlbecsültek. Az üzleti kockázatok (2. kör) valósak és cselekvést igényelnek. Ez a helyes prioritási sorrend:

**2026 Q2–Q3 fókusz: üzleti validáció > infrastruktúra > feature fejlesztés**

---

*Ez a dokumentum a `ArchitectureRiskReview_Round2_20260413.md` formális válasza.*  
*Mindhárom dokumentum (DA 1. kör, válasz 1. kör, DA 2. kör, válasz 2. kör) a Project Knowledge részét képezi.*
