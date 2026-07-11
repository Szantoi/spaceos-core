# Keresztértékelés — Planner-B értékeli Planner-A tervét

## 1. Miben erősebb Planner-A terve?

**Kockázatkezelés és dependency management.** A függőségi gráf (`P1→P2→P3→P4`) világos, minden lépés előkészíti a következőt. Az offline cache (P2) **előtt** megköveteli a Joinery API stabilizálását (P1) — ez okos, mert így tudjuk mi legyen cache-elhető.

**Backend inventárja precíz.** Pontosan megmondja mi létezik (`✅ GET /joinery/api/designs`) és mi hiányzik (`POST /kernel/api/audit/buffer`). Ez konkrét action itemeket ad a Backend/Kernel termináloknak.

**Realistic sprint becslések.** 7-10 sprint ~14-20 hét — konzervatív, de becsületes. Az én 3 hetes tervem agresszív, az övé teljesíthető.

## 2. Miben gyengébb?

**Nincs early customer feedback loop.** P1 (Joinery API) csak akkor ad üzleti értéket, ha P2 (offline) is kész — de ez 3-5 sprint. Doorstar-nak HÓNAPOKAT kell várnia demonstrálható értékre.

**Mobile UI (P4) túl későn.** A műhelyben dolgozók **MOST** mobillal dolgoznak, nem desktop-pal. P4-et utolsónak tolni = ők 6 hónapig nem használhatják a rendszert.

## 3. Mit átvennék?

**Backend inventory technikát.** A "meglévő ✅ / hiányzik ❌" táblázat remek. Integrálnám a saját tervembe is, hogy világos legyen melyik endpoint mock, melyik éles.

**Service Worker + Dexie.js stack.** Planner-A technikai választása helyes — Workbox battle-tested, nem kell újra feltalálni.

## 4. Egyetértés (konsenzus magja)

✅ **Joinery API integráció az első feature** — mindketten ezt priorizáljuk  
✅ **Offline capability kritikus** — gyári realitás  
✅ **React Query + localStorage draft mentés** — modern state management

## 5. Nézeteltérés

**Fokozatos vs. end-to-end.**  
Planner-A: "Előbb az API, utána a UI, utána a mobil."  
Én: "Egy teljes vertical slice (ajánlat→gyártási lap) 1 hét alatt, mock backend-del is."

**Indoklás:** Doorstar soft launch **Q2 2026** — már itt vagyunk. Nem engedhetjük meg a 10 sprintet. A **walking skeleton filozófia** (amit Root hangsúlyoz) azt jelenti: vékony, de TELJES pipeline első. Planner-A terve horizontális rétegeket épít — én vertikális értékláncot.

**Javaslat:** Kombináljuk: **P1 (Joinery) legyen vertical slice** — egyszerű konfigurátor → BOM → work order PDF, 2 sprint alatt. Mobil UI (P4) párhuzamosan induljon.
