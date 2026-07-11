# Planner-A Keresztértékelése

## 1. Miben erősebb Planner-B terve?

**Merészség és demonstrálhatóság.** A "teljes value chain early prototype" megközelítés ügyfélfókuszú — a Doorstar soft launch-nál valóban az számít, hogy egy megrendelést végig lehessen vinni. A real-time shop floor feed **azonnal látványos**, még mock adattal is — ez igazi sales enabler.

A technikai stack egyszerűsége (Zustand + TanStack Query vs. Redux) **pragmatikus** — ennyi feature-höz tényleg overkill lenne Redux.

## 2. Miben gyengébb?

**Kockázatkezelés hiánya.** A "hardcode 5 ajtótípus" indulás jól hangzik, de ha a parametrikus szabályok valódi komplexitása kiderül (lásd: Joinery modul C# kódbázisa), ez **technical debt lesz** amit nehéz később kicserélni.

Az offline sync konfliktuskezelés ("Last Write Wins + manual resolution") **nem skálázódik** — 10 műhelyvezető esetén ez support nightmare.

A 3 hetes timeline **túl optimista** — egyetlen blocker (pl. SSE perf probléma) és késésben vagyunk.

## 3. Mit vennék át?

**SSE-alapú shop floor live view azonnal.** Ez a feature tényleg quick win — a `useSSE` hook már létezik, mock implementáció gyors. Demo értéke óriási.

**OpenAPI → TypeScript codegen** — ezt a tiéd is használhatná. Típusbiztonság REST-el is elérhető tRPC nélkül.

## 4. Egyetértés pontok

✅ **Offline-first kritikus** — mindkét terv prioritizálja (P2 vs Week 3)  
✅ **Joinery integráció első** — azonos logika, különböző indoklás  
✅ **Walking skeleton filozófia** — mindkettő teljes loop-ot akar hamar  

## 5. Vita pontok

**Backend-first vs. Demo-first:**  
A tiéd: "Backend már kész → biztonságos integráció"  
Planner-B: "Mock-kal is működő demo → gyors feedback"

**Érv a tiédnek:** A Joinery backend **tényleg** kész van — miért építenénk mock réteget amikor production code használható? A technical debt csökkentés fontosabb mint a gyors demo.

**Offline sync:**  
A tiéd: "Last-write-wins + server timestamp authority"  
Planner-B: "Manual conflict resolution UI"

**Érv a tiédnek:** Manual resolution **nem skálázódik**. Az én megoldásom automatikus és determinisztikus — a konfliktus ritkán fordul elő production-ben ha timestamp alapú.

---

**Konklúzió:** B terve merészebb, A terve biztonságosabb. **Hibrid:** SSE shop floor (B) + production Joinery backend (A) + server-authority offline sync (A).
