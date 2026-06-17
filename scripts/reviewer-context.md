# SpaceOS Reviewer Kontextus

Ez a fájl a SpaceOS automatizált reviewer ügynökök számára készült.
Minden review előtt be kell tölteni — ez adja a projekt-tudást és minőségi elvárásokat.

---

## Mi a SpaceOS?

A SpaceOS a **magyar faipar digitális gerince** — egy iparspecifikus SaaS platform,
amely az ajtógyártókat, szekrénygyártókat, lapszabászokat, kereskedőket és beszerelőket
egyetlen összekapcsolt ökoszisztémába szervezi. Az első éles ügyfél a **Doorstar Kft.**
(ajtógyártó), live: https://joinerytech.hu.

**Miért épül:** A faiparos KKV-k 90%+ ma Viber + Excel + telefon alapon koordinál.
Nincs rájuk szabott, megfizethető digitális megoldás.

---

## Rendszer felépítése

```
L4  JoineryTech Portal   React 18 + Vite — brand UI, 27 világ
L3  Orchestrator (BFF)   Node.js 22 — AI gateway
L2  Modules              .NET 8 — Joinery, Inventory, Procurement, Sales, Identity, stb.
L1  Kernel               .NET 8 + PostgreSQL — auth, audit, FSM, tenant
```

---

## 5 Golden Rule — minden döntésnél kötelező

1. **Data → Rules → Geometry** — frontend csak megjelenít, a C# Driver számol, LLM csak paramétereket ad
2. **Modular Monolith** — Kernel interfészen keresztül dolgozik, nem tudja mi az asztalos
3. **Immutability & Trust** — nincs UPDATE CAD adatokon, minden SHA-256 hashed audit eventtel
4. **Need-to-Know RBAC** — megrendelő nem látja a gyártó belső anyaglistáját
5. **Walking Skeleton First** — E2E pipeline előbb, matematika utóbb mélyül

---

## Aktuális sprint fókusz

**Slice 1 — Mock-mentes integráció (ez a jelenlegi sprint):**
- 19 page, backend már létezik → mock adatokat le kell cserélni real API hívásokra
- Ha egy endpoint nem létezik a backendben → `EndpointPending` banner (nem mock fallback!)
- Mock adat (`window.sim`, statikus tömbök) **nem maradhat** ha van backend endpoint

**Slice 2 — Jövő (még nem indult):**
- 5 új backend modul: CRM / Finance / Project / Maintenance / HR
- Addig: a hiányzó endpointok `EndpointPending` bannerrel jelzett állapotban maradnak

**FE domain felosztás:**
- `FE-CORE` (fe terminál): Dashboard, Orders, Workflow, Settings, Analytics, Production, Design, MfgPrep, Quality
- `FE-SALES` (fe2 terminál): Sales, CRM, Finance, Projects
- `FE-OPS` (fe2 terminál): Inventory, Procurement, Logistics, Warehouse
- `FE-PEOPLE` (fe2 terminál): HR, Kontrolling

---

## Minőségi elvárások — Frontend (React/TypeScript)

### Mock-mentesség (kritikus)
- ❌ TILOS: `import { ORDERS } from '../mock/...'` ha a backendben van endpoint
- ❌ TILOS: `|| mockData` fallback ha az endpoint létezik
- ✅ KÖTELEZŐ: `EndpointPending` komponens ha az endpoint **nem létezik** a backendben
- ✅ KÖTELEZŐ: real `fetch`/`useQuery` hívás ha az endpoint létezik

### API hívások
- ✅ Loading state kezelve (`isLoading`, skeleton, spinner)
- ✅ Error state kezelve (toast vagy inline error message)
- ✅ Üres lista kezelve (placeholder, "Nincs adat" üzenet)
- ❌ TILOS: `console.log` production kódban
- ❌ TILOS: hardcoded URL-ek (használj `config.ts` vagy env változókat)

### Tesztek
- ✅ Build zöld (`pnpm build` 0 hiba)
- ✅ Tesztek zöldek (`pnpm test --run` minden teszt pass)
- ✅ Tesztszám nem csökkent szignifikánsan (max -5 tolerált refactor esetén)
- ✅ EndpointPending banner-ek tesztelve (a banner megjelenik-e)
- ❌ TILOS: mock-specifikus tesztek törlése anélkül hogy EndpointPending tesztek felváltják őket

### Komponens minőség
- ✅ Egy komponens = egy felelősség
- ✅ Props típusai definiálva (TypeScript interface)
- ✅ Shared logika hookba kiemelve (pl. `useSalesData`)
- ❌ TILOS: copy-paste logika 3+ komponensben

---

## Minőségi elvárások — Backend (.NET 8)

### Clean Architecture (kötelező)
- Domain → Application → Infrastructure → Presentation rétegek
- Domain nem hivatkozhat Infrastructure-re
- EF Core csak Infrastructure rétegben

### Tesztek
- ✅ Unit + Integration tesztek (Testcontainers PostgreSQL)
- ✅ Happy path + legalább 1 edge case minden endpointhoz
- ✅ RLS tesztelve: cross-tenant izolácó

### Biztonság
- ✅ JWT RS256 validáció minden védett endpointon
- ✅ TenantId a JWT claimből, nem URL paraméterből
- ✅ BYPASSRLS factory (Worker services) vs. user connection (API)
- ❌ TILOS: `SELECT *` production query-kben (explicit oszlopok)

### Migrációk
- ✅ `dotnet ef migrations add` — manuális SQL script tilos
- ✅ Idempotens migráció (újrafuttatható)

---

## Minőségi elvárások — Nexus / Datahaven / Resonance

A `nexus` terminál az agent infrastruktúrán dolgozik (nem SpaceOS termék kód).
Különböző elvárások vonatkoznak rá:

### Datahaven (McpServer + Marvin + ChromaDB)
- ✅ Izolált — a meglévő bash pipeline (nightwatch/reviewer/pipeline) NEM szakad meg
- ✅ Fokozatos felváltás — egy szkriptet vált egyszerre, nem mindent
- ✅ Fallback létezik — ha Datahaven leáll, a bash pipeline fut tovább
- ✅ `.env.example` naprakész — minden új env var dokumentálva
- ✅ ChromaDB kapcsolat tesztelve (MemoryVectorStore fallback működik)
- ❌ TILOS: SpaceOS termék kód módosítása (Kernel, FE, Joinery stb.)

### Resonance (Daemon definíciók, skill-ek, role-ok)
- ✅ Daemon template-t követ (`daemons/templates/daemon.template.md`)
- ✅ Minden skill v1→v4 pipeline-t dokumentál
- ✅ DWI workflow fázisok követve
- ❌ TILOS: SpaceOS-specifikus kód a Resonance-ban (általános marad)

---

## Review elvek

**Konstruktív kritika:** A reviewer célja a minőség javítása, nem a terminál blokkolása.
- APPROVE ha a munka jó, de van javaslat → adjuk meg a javaslatot, ne blokkoljunk
- REJECT csak ha valódi hiba van (mock leak, sikertelen build, hiányzó DoD pont)
- A REJECT-nek mindig konkrét, javítható lista legyen — "nem elég jó" nem elfogadható feedback

**Amit NEM kell vizsgálni:**
- Kód formázás (Prettier/ESLint kezeli)
- Elnevezési konvenciók ha következetesek
- Teljesítmény optimalizáció ami nincs a taskban
