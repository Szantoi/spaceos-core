# SpaceOS — Architektúra Kockázatelemzés, 2. kör

**Keletkezés:** 2026-04-13  
**Módszer:** Devils Advocate analízis, 2. kör — az 1. kör ismert kockázataitól eltérő, új területek  
**Előzmény:** `ArchitectureRiskReview_20260413.md` + `ArchitectureRiskReview_Response_20260413.md`  
**Státusz:** Nyitott

---

## Összesítő

| # | Kockázat | Súlyosság | Mikor blokkoló |
|---|---|---|---|
| 7 | Escrow pénzforgalmi engedélyezés (MNB/PSD2) | 🔴 KRITIKUS | 2026 Q2, első valós tranzakció |
| 8 | Graph Engine IFC szabvány hiánya (NIH) | 🔴 MAGAS | 2027 DACH belépéskor |
| 9 | HU faipari KKV digitális érettség / CAC validáció | 🔴 MAGAS | 2026 Q4 |
| 10 | Anthropic API cost + EU data residency | 🟡 KOCKÁZAT | 20+ ügyfélnél |
| 11 | Multi-brand prematur optimalizáció | 🟡 KOCKÁZAT | Nem blokkoló, hidden tax |
| 12 | Single design partner (Doorstar) dependency | 🔴 MAGAS | Már most |

---

## 7. Escrow funkció pénzforgalmi engedélyezési kötelezettsége (MNB/PSD2)

**Súlyosság: 🔴 KRITIKUS — 2026 Q2 Soft Launch előtt tisztázandó**

### A probléma

Ha a SpaceOS Escrow funkció keretében **ténylegesen megkapja** a megrendelő pénzét, majd továbbítja a gyártónak — ez pénzforgalmi szolgáltatás a Pft. és PSD2 szerint. Ez **MNB engedélyköteles tevékenység**, minimum 125 000 EUR induló tőkével (kis pénzforgalmi intézmény) vagy 350 000 EUR teljes pénzforgalmi intézménynél. Engedélyezési idő: 6–12 hónap.

Ha a SpaceOS **nem tartja a pénzt** (csak értesít, hogy "az ügyfél utaljon a gyártónak") — akkor nincs PSD2-kötelezettség, de akkor **nincs valódi escrow védelem sem**, és az üzleti modell tranzakciós díj-komponense megkérdőjelezhető: hogyan veszi be a SpaceOS a jutalékot, ha nem látja a pénzmozgást?

### Források

- 2009. évi LXXXV. törvény a pénzforgalmi szolgáltatás nyújtásáról — 2. § (1): "átutalás" és "pénzösszeg fogadása" definíciók
- PSD2 Directive (EU) 2015/2366, Annex I — különösen Point 3–4 (payment transactions)
- MNB Pénzforgalmi intézmény engedélyezési útmutató — mnb.hu/felugyelet/engedelyezes
- EBA Opinion EBA-Op-2019-04, March 2019 — a "Commercial Agent Exemption" (PSD2 Art. 3(b)) **marketplace escrow esetén nem alkalmazható**, mert a platform mindkét fél nevében jár el egyszerre
- Stripe Connect, MangoPay, Lemonway — mind teljes pénzforgalmi intézményi licenszzel operálnak

### Nyitott kérdések

1. Technikailag mi az Escrow modellje: a pénz átmegy-e a SpaceOS számlán, vagy csak state machine (értesítés, hogy "utalj")?
2. Ha licensed PSP partneren (Barion Escrow, MangoPay) keresztül menne — ez az üzleti modell (tranzakciós díj) mekkora részét adja át a PSP-nek?
3. Az Escrow GA sprint mikor indul? Ha 2026 Q3 előtt — kötelező jogi tisztázás.

### Lehetséges megközelítések

| Opció | Leírás | Következmény |
|---|---|---|
| Licensed PSP partner (MangoPay/Barion) | Ők tartják a pénzt, SpaceOS csak az értesítési logikát futtatja | PSP elvesz 0.5–1.5% / tranzakció, de nincs MNB engedély szükség |
| Saját pénzforgalmi engedély | 6–12 hónap, 125k–350k EUR tőke | Csak 2027-re reális |
| "Soft escrow" (state machine, nincs pénztar tás) | Jogilag biztonságos, de üzletileg gyengébb | Tranzakciós díj modell átgondolandó |

---

## 8. Graph Engine IFC szabvány hiánya (NIH szindróma)

**Súlyosság: 🔴 MAGAS — 2027 DACH belépésnél blokkoló**

### A probléma

A házilag épített `ProductTemplate` → `SlotConnection` → Kahn's BFS parametrikus motor egy **40+ éves, szabványosított** probléma újrafeltalálása. Az iparági szabvány az **IFC (Industry Foundation Classes, ISO 16739-1:2018)**, amelyre .NET-ben kész, MIT licencű implementáció létezik (**xBIM Toolkit**).

Németországban 2020 óta **kötelező IFC export** közbeszerzési építési projekteknél. DACH enterprise ügyfeleknél az IFC interoperabilitás nem opció — piacra lépési alapfeltétel.

### Források

- ISO 16739-1:2018 — Industry Foundation Classes for data sharing in construction and facility management
- BuildingSMART International — buildingsmart.org/standards/bsi-standards/industry-foundation-classes
- **xBIM Toolkit** (.NET, MIT license) — docs.xbim.net · GitHub: xBimTeam/XbimEssentials · aktív fejlesztés 2013 óta
- BMI (Bundesministerium des Innern) BIM-Leitfaden für Deutschland 2020 — IFC export kötelező közbeszerzési projekteknél
- OpenCascade Technology (LGPL) — 100+ mérnökév, constraint propagation + topological sort megoldva

### Nyitott kérdések

1. A Graph Engine output-ja exportálható-e IFC formátumba? Ha nem, a DACH piac → Revit/ArchiCAD integráció lehetetlen.
2. Miért nem xBIM Toolkit wrapper a `Modules.Abstractions` alatt, az `IParametricProduct` interfészen keresztül? Ez pontosan a Golden Rule #2 célja lett volna.
3. A jelenlegi `DeriveCncPlan()` output formátuma szabványos (STEP, DXF, G-code) vagy proprietáris JSON? Ha proprietáris, minden CNC gépgyártóval (Homag, Biesse) egyedi integráció kell.

### Megjegyzés

HU piacon (Doorstar, 2026 Q2) ez nem blokkoló — a hazai KKV-k nem várnak IFC-t. De ha a DACH roadmap komoly, az IFC réteg 2026 Q4-re szükséges, mielőtt az első DACH ügyfél-demo megtörténik.

---

## 9. HU faipari KKV digitális érettség és CAC validáció

**Súlyosság: 🔴 MAGAS — 2026 Q4-re válik láthatóvá**

### A probléma

Az "1300–2500 HU KKV célpiac" TAM szám, nem validált pipeline. A magyar faiparos KKV-k digitális érettsége európai átlag alatt van, és a B2B vertical SaaS adoption construction/manufacturing szektorban drámian lassabb az SMB horizontal SaaS-nál.

### Adatok

| Metrika | Adat | Forrás |
|---|---|---|
| HU digitális érettség (EU27 rangsor) | 22. hely, KKV digital intensity 52% vs EU 69% átlag | DESI Index 2023, EC |
| HU <10 fős vállalkozások felhő SaaS használata | 27% | KSH "IKT a vállalkozásokban" 2023 |
| Faipar/fafeldolgozás szektor SaaS penetráció | <20% becsült | KSH alapján |
| Construction SaaS CAC/LTV break-even | ~30 hónap (2-3× horizontal SaaS-nál) | Procore S-1 filing 2021 |
| 1 → 10 ügyfél growth, vertical SaaS | Medián 18 hónap | SaaS Capital Benchmarks 2023 |
| McKinsey Construction digitalizáció | 2. legkevésbé digitalizált szektor globálisan | McKinsey "Digital Transformation in Construction" 2023 |

### Nyitott kérdések

1. Mi a validált Willingness To Pay a Doorstar-on kívüli 5–10 céggel? Léteznek design partnership letter-ek?
2. Mi a becsült CAC? Ha outbound sales szükséges (Viber-es cégek nem keresnek SaaS-t Google-ön), CAC ≥ 2000 EUR/ügyfél reális. Mennyi az ARPU, és mikor a break-even?
3. Van-e 10+ szándéknyilatkozat a Doorstar-on túl? Ha nincs, a TAM szám nem pipeline.
4. Mi a sales motion terve? PLG (product-led growth) nem működik, ha a célcsoport nem keres digitális megoldást. Ki végzi az outbound sales-t?

---

## 10. Anthropic API cost + EU data residency

**Súlyosság: 🟡 KOCKÁZAT — 20+ ügyfélnél és DACH enterprise-nál válik érzékennyé**

### A probléma

**Becsült cost / konfigurátor session:** $0.05–0.08 (Claude Sonnet 4, multi-turn tool calling, system prompt + schema + 3–5 turn ≈ 8000–15000 input + 1500–3000 output token)

| Ügyfélszám | Havi API cost (1100 session/ügyfél/hó) |
|---|---|
| 1 (Doorstar) | $55–88 |
| 10 | $550–880 |
| 100 | $5 500–8 800 |

A **GDPR Art. 44–49** (nemzetközi adattovábbítás) problémát jelent: Anthropic szerverei USA-ban vannak. Az EU Data Residency opció Claude-ra 2026 Q1-ben csak beta státuszú. DACH enterprise ügyfeleknél (ahol a GDPR compliance szigorúbb) ez azonnali kizáró ok lehet.

### Források

- Anthropic Pricing — anthropic.com/pricing
- a16z "The LLM API Cost Explosion" 2024 — B2B SaaS LLM API cost: medián 12–18% of revenue
- GDPR Art. 44–49 — adattovábbítás harmadik országba
- Anthropic EU Data Processing Addendum — létezik, de szerver-lokáció USA (nem EU)
- Azure OpenAI EU hosting → Claude-ra nem elérhető (Anthropic partner: Google Cloud Vertex AI)

### Nyitott kérdések

1. Van-e abstract LLM adapter az Orchestrator-ban (`ILlmProvider`), vagy Anthropic SDK közvetlen? — Ha van adapter (a kódbázis alapján van: `ILlmProvider` interfész), a provider switch megoldható.
2. A Doorstar-ügyfél adatai (ajtó méretek, megrendelő adatok, árak) belekerülnek-e az LLM promptba? Ha igen, ez az USA-ba kerülő személyes/üzleti adat.
3. Az "AI prémium" árazás validált-e? Mi a kontroll csoport (form-based konfigurátor)?

### Megjegyzés

Az `ILlmProvider` interfész megléte (Orchestrator kódbázisban dokumentált) a provider lock-in kockázatát csökkenti. A valódi probléma az EU data residency és a cost skalázódás — ezek 2027-re válik élesé.

---

## 11. Multi-brand architektúra prematur optimalizáció

**Súlyosság: 🟡 KOCKÁZAT — nem blokkoló, de hidden complexity tax**

### A probléma

Két brand (joinerytech.hu + asztalostech.hu) futtatása **1 éles ügyfélnél** prematur scaling. A multi-brand architektúra minden feature-re extra overheadet ad: Turborepo build időt, SEO duplikálást, tesztek kétszeres futtatását, branding asset-ek duplikálását.

### Források

- Startup Genome Report 2019 — prematur scaling TOP 1 B2B SaaS bukási ok (74% of failed startups)
- Shopify Plus Playbook — multi-storefront architektúra csak $1M+ GMV után ajánlott
- Basecamp "Shape Up" (Ryan Singer, 2019) — "optional flexibility is guaranteed complexity"
- Conway's Law (1968) — szervezet struktúrája tükröződik a rendszerben; 1-2 fős csapat + 2 brand = mindkettő szenved
- Intercom — 2011 → 2018-ig (kb. $30M ARR) egyetlen brand-del működött

### Nyitott kérdések

1. Mikor kap az asztalostech.hu brand első ügyfelet? Ha nem 2026 Q3 előtt, a brand layer dead code.
2. Minden E2E teszt fut mindkét brand alatt? Ha nem, az egyik brand csendben eltörhet.
3. Mi a SEO stratégia két domainre? Kétszeres content és backlink igény.

### Lehetséges mitigáció

Tenant-level customization (logo, színek, szövegek) egyetlen brand alatt az első 3–5 ügyfélig. Második brand csak a második iparági vertical (szekrény) első ügyfelénél aktiválva.

---

## 12. Single design partner (Doorstar) dependency

**Súlyosság: 🔴 MAGAS — már most cselekvést igényel**

### A probléma

Az egész platform első éles verziója egyetlen ügyfél köré épül. Ez a **"consultware trap"** klasszikus mintája: a termék lassan Doorstar-specifikus egyedi szoftverré válik, ahelyett hogy horizontálisan újrahasznosítható vertical SaaS maradna.

### Konkrét kockázatok

| Kockázat | Leírás |
|---|---|
| **Termék torzulás** | Minden Doorstar-specifikus feature (ajtó BOM template, árképzés) beépül a "standard" modulba |
| **Exclusivity trap** | Ha Doorstar kér "1 évig nem szolgáltatsz konkurens ajtógyártónak" — a 2. ügyfél terv meghal |
| **Churn = product death** | Ha Doorstar 2026 Q3-ban kiesik (tulajdonosváltás, csőd, stratégiaváltás) — a platform reference customer nélkül marad |
| **Generalizálás blokkolása** | A Doorstar-specifikus assumptions refaktorálása a 2. ügyfélig kötelez minden architektúra-érintést |

### Források

- Steve Blank — "The Four Steps to the Epiphany" (2005) — minimum 3–5 párhuzamos design partner a termék-torzulás elkerüléséhez
- Geoffrey Moore — "Crossing the Chasm" (1991) — 3+ early customer szükséges validation-hoz, 1 customer = captive
- SaaS Capital Benchmarks 2023 — 1 ügyféllel 12+ hónapig ragadt vertical SaaS cégek 67%-a nem ért el Series A-t
- Bessemer Venture Partners "State of the Cloud 2024" — "single-customer dependency >40% revenue" explicit red flag

### Nyitott kérdések

1. Milyen a Doorstar szerződéses viszonya? Van exclusivity clause? Van IP-megosztás a Doorstar-specifikus sablonokra?
2. Mi a risk plan Doorstar drop-out esetére?
3. Van-e 2–3 párhuzamos design partner (nem feltétlenül fizető ügyfél, csak LOI szintű commitment)?

### Ajánlás

2026 Q2 Soft Launch előtt **minimum 2 further LOI** (Letter of Intent) nem-Doorstar cégektől. Ezek nem kell, hogy fizetők legyenek — csak validálja, hogy a termék a Doorstar-on kívül is releváns.

---

## Amit ez a kör NEM kérdőjelez meg (folytonosság az 1. körrel)

- Domain modell és Clean Architecture — helyes, indokolt
- "Data → Rules → Geometry" axióma — az 1. körös elemzés válasza meggyőző volt
- B2B Handshake scope — nem federation, egyszerű allowlist, indokolt
- PostgreSQL + EF Core + RLS — megfelelő választás
- 1728+ teszt, 0 fail — komoly minőségi alap

---

## Akció javaslatok

| # | Kockázat | Akció | Határidő | Prioritás |
|---|---|---|---|---|
| 7 | Escrow PSD2 | Jogi tisztázás: PSP partner (MangoPay/Barion) vs. saját engedély vs. "soft escrow" | **Escrow GA sprint előtt** | **P0** |
| 8 | IFC hiány | xBIM Toolkit értékelése `IParametricProduct` alá — döntés dokumentálása | 2026 Q4 (DACH demo előtt) | P1 |
| 9 | CAC validáció | 10 non-Doorstar céginterjú WTP-re; outbound sales motion terv | **2026 Q3 előtt** | P1 |
| 10 | Anthropic EU residency | GDPR Art. 28 DPA ellenőrzése, Doorstar adatok LLM prompt-ban? | Q2 Soft Launch előtt | P1 |
| 11 | Multi-brand | Döntés: asztalostech.hu mikor kap 1. ügyfelet — ha 2026 Q4-nél később, brand freeze | 2026 Q2 | P2 |
| 12 | Single partner | 2 párhuzamos design partner LOI megszerzése | **2026 Q2 előtt** | **P0** |

---

*Ez a dokumentum a `ArchitectureRiskReview_20260413.md` második körös folytatása. Az 1. kör akció-listája a `ArchitectureRiskReview_Response_20260413.md`-ben rögzített.*
