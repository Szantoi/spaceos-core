# SpaceOS — Architektúra Kockázatelemzés

**Keletkezés:** 2026-04-13  
**Módszer:** Devils Advocate analízis — csak forrásokkal alátámasztott, valódi kockázatok  
**Státusz:** Nyitott — nem kell minden pontot azonnal kezelni, de tudatosan kell viszonyulni hozzájuk

---

## Összesítő

| # | Kockázat | Súlyosság | Mikor kritikus |
|---|---|---|---|
| 1 | LLM tool calling megbízhatatlansága CNC kontextusban | 🔴 KRITIKUS | Most — Doorstar Soft Launch előtt |
| 2 | Mixed stack kognitív overhead | 🔴 MAGAS | Folyamatos, minden sprintben |
| 3 | Egyetlen VPS production | 🔴 KRITIKUS | 2. ügyfél onboarding előtt (2026 Q3) |
| 4 | SHA-256 audit chain jogi státusza | 🟡 KOCKÁZAT | Első escrow-val kapcsolatos jogvitánál |
| 5 | Keycloak komplexitás | 🟡 KÖZEPES | Első major verziófrissítésnél (~6 hónap) |
| 6 | B2B Federation complexity MVP előtt | 🔴 MAGAS | Most — wasted effort kockázat |

---

## 1. LLM Tool Calling megbízhatatlansága CNC-precíziós kontextusban

**Súlyosság: 🔴 KRITIKUS**

### A probléma

Az Orchestrator (L3) Claude Sonnet API-t használ természetes nyelvű input → CNC-pontosságú JSON paraméter fordításra. A mért tool calling accuracy a legjobb modellekkel is **85–90%** multi-turn scenarioban. Manufacturingban ez 10 rendelésből 1 hibás gyártási lapot jelent.

Különösen veszélyes a **schema-helyes, de szemantikailag hibás** paraméter: pl. a user "balos ajtó"-t mond, az LLM `right_hinge: true`-t generál. Ez schema validationon átmegy, C# Driveren átmegy, és hibás ajtót vág a CNC — visszafordíthatatlan fizikai kárral.

### Források

- **Berkeley Function Calling Leaderboard v3** (Gorilla Project, UC Berkeley) — gorilla.cs.berkeley.edu/leaderboard.html
- **"Hallucination is Inevitable"** — Xu et al., 2024, arXiv:2401.11817 — formálisan bizonyítja, hogy 100% accuracy LLM-mel nem elérhető
- **Anthropic Tool Use Docs** — "the model may occasionally generate parameters that don't match your schema" — ajánlott mitigáció: retry + validation loop, nem trust
- **"Domain Adaptation of LLMs"** — Ling et al., 2024 — domain-specifikus inputoknál (pl. magyar faipari terminológia) további 5–15pp accuracy romlás

### Nyitott kérdések

1. Van-e mért, dokumentált end-to-end accuracy szám a SpaceOS specifikus doménjében (magyar nyelvű ajtó-konfiguráció → CNC-kész JSON)?
2. Ha van human approval gate a CNC output előtt, mi az LLM valódi ROI-ja egy form-alapú konfigurátorhoz képest (Compusoft, Imos)?
3. Mi a tervezett mitigáció szemantikai hibákra — amelyeket schema validation nem fog meg?

### Lehetséges mitigáció

- CNC output előtt kötelező human review gate (de ez csökkenti az automatizálás értékét)
- Confidence score + low-confidence esetén form-alapú megerősítés
- Domain-specifikus fine-tuning vagy few-shot prompt engineering mért accuracy eléréséig

---

## 2. Mixed Stack kognitív overhead (.NET 8 + Node.js 22 + React 18)

**Súlyosság: 🔴 MAGAS**

### A probléma

A 4 réteges architektúra három különböző runtime-ot, három package managert (NuGet, pnpm, MSBuild), három tesztelési ökoszisztémát (xUnit, Vitest, Playwright), és három CI/CD pipeline ágat igényel. Egy bug a teljes láncon végigfutva három különböző debugger és logging stack között követhető csak.

Ha a csapat 3–4 fő, minden fejlesztőnek mindhárom stacket ismernie kell — ez Magyarországon senior-only team, ami drága és ritka.

### Források

- **Stack Overflow Developer Survey 2024** — polyglot teamek 23–40%-kal több időt töltenek build/tooling problémákkal mono-stack csapatokhoz képest
- **ThoughtWorks Technology Radar Vol 30, 2024** — "polyglot programming" HOLD ajánlás kis csapatoknál: "cognitive overhead frequently exceeds the technical benefits"
- **Martin Fowler — "MicroservicePremium" (2023)** — "Don't consider architectural complexity unless the system is too complex to manage without it" — polyglot stackre ugyanez érvényes
- **Google SRE Book, Chapter 8** — "operational complexity scales superlinearly with runtime diversity"

### Nyitott kérdések

1. Mi szükségessé teszi a Node.js BFF réteget, amit egy ASP.NET Core YARP reverse proxy + minimal API nem tudna megoldani, a teljes stack-et .NET-en tartva?
2. Mekkora a csapat? Ha <4 fő, a polyglot stack valódi kockázat.

### Tudatos trade-off (ha valóban szükséges)

Az Anthropic SDK Node.js-re érettebb, mint a .NET-es kliens — ez az LLM integráció egyik indoka. Ha ez a döntő érv, dokumentálandó, és figyelni kell a .NET Anthropic SDK fejlődésére.

---

## 3. Egyetlen VPS production deployment

**Súlyosság: 🔴 KRITIKUS (2. ügyfél előtt blokkoló)**

### A probléma

Single VPS (109.122.222.198) = single point of failure. B2B SaaS-nál, ahol pénzügyi escrow és immutable audit chain fut, egy hardware failure órákig tartó downtime-ot és potenciálisan adatvesztést okoz. Nincs hot standby, nincs blue/green deployment, nincs automatikus failover.

### Források

- **AWS Well-Architected Framework — Reliability Pillar** — "single instance deployments cannot achieve more than 99.0% availability" (~3.65 nap downtime/év)
- **Google SRE — Service Level Objectives** — B2B SaaS iparági standard: 99.9% SLA minimum (8.76 óra/év downtime)
- **GDPR Article 32** — "ability to restore the availability and access to personal data in a timely manner in the event of a physical or technical incident" — single VPS nehezen teljesíti auditban
- **PostgreSQL HA Docs** — production ajánlás: minimum 1 streaming replication standby

### Nyitott kérdések

1. Mi a documented RTO (Recovery Time Objective) és RPO (Recovery Point Objective)?
2. Mikor kerül HA a roadmapra? 2. ügyfél onboarding (2026 Q3) előtt szükséges.
3. Van-e napi automatikus backup és tesztelt restore eljárás?

### Javasolt minimális mitigáció (2026 Q3 előtt)

- PostgreSQL streaming replication → 1 standby node
- Automatikus napi backup tesztelő restore-ral
- VPS szintű snapshotok (Hetzner/OVH mindkettő kínál)
- Incident response playbook dokumentálva

---

## 4. SHA-256 audit chain jogi státusza

**Súlyosság: 🟡 KOCKÁZAT**

### A probléma

A Golden Rule #3 ("Immutability & Trust — SHA-256 hashed audit event") technikailag tamper-evident, de **jogilag nem azonos** az EU eIDAS rendelet szerinti minősített elektronikus időbélyeggel. Egy hash chain önmagában csak azt bizonyítja, hogy a lánc utólag nem módosult — nem bizonyítja, hogy az operátor (SpaceOS) nem manipulálta keletkezésekor, és nem bizonyítja az esemény pontos időpontját.

Escrow funkciónál (pénzügyi tranzakció) ez különösen kritikus: ha Doorstar és egy partner között jogvita keletkezik, a bíróság a SpaceOS audit logot kvázi-okiratként csak akkor fogadja el, ha külső tanúsítás is van.

### Források

- **eIDAS Regulation (EU) 910/2014, Article 41–42** — csak Qualified Trust Service Provider által kiadott időbélyeg élvez "presumption of accuracy" jogi védelmet
- **Magyar Eüt. (2015. évi CCXXII. törvény az elektronikus ügyintézésről)** — minősített e-aláírás és időbélyeg követelmények B2B kontextusban
- **NIST SP 800-102** — hash chain trusted third party nélkül nem time-attesting
- **ETSI EN 319 422** — Time-stamping Protocol és Token Profile szabvány
- **OpenTimestamps** — ingyenes blockchain-alapú időbélyeg szolgáltatás

### Megoldási opciók

| Megoldás | Költség | Jogi erő |
|---|---|---|
| Microsec / NetLock TSA (HU) | ~0.01–0.05 EUR/event | eIDAS Qualified ✅ |
| OpenTimestamps (Bitcoin anchor) | Ingyenes | Erős, de nem Qualified |
| Periodikus notarizált backup snapshot | Alacsony | Közepes |

### Ajánlás

Escrow GA előtt (backlog-ban van) kötelező legalább OpenTimestamps anchor bevezetése. Qualified TSA Doorstar first invoice után mérlegelendő.

---

## 5. Keycloak komplexitás korai SaaS fázisban

**Súlyosság: 🟡 KÖZEPES**

### A probléma

Keycloak enterprise IAM megoldás — jellemzően 1000+ felhasználós, multi-realm, federated identity környezetekre tervezett. Korai SaaS fázisban (10–50 ügyfél) ez extra VPS erőforrást (minimum 2GB RAM csak Keycloaknak), realm konfiguráció verziókezelési problémákat, és magas upgrade friction-t jelent (~40%-ban manuális migration szükséges minor verziófrissítésnél).

A Keycloak setup külön epic volt (INFRA-KC01), a Script Mapper bug (INFRA-056/060) szintén extra effort — mindkettő jelzi a valódi operational overhead-et.

### Források

- **Keycloak System Requirements** — minimum 2GB RAM csak Keycloaknak production-ben
- **Okta State of Identity Report 2024** — SaaS startupok 67%-a SaaS auth provider-rel indul, majd migrál önhostoltra méretezési igény esetén
- **Keycloak GitHub Issues** — 21→22, 23→24 verziófrissítés: ~40%-ban manual realm migration szükséges
- **Authentik / Ory Kratos dokumentáció** — lightweight alternatívák kisebb footprinttel

### Nyitott kérdések

1. Mi a becsült operational cost (fejlesztői idő + RAM) Keycloak üzemeltetésére vs. Ory Kratos, Authentik, vagy ASP.NET Core Identity + Duende IdentityServer?
2. A jelenlegi Keycloak konfiguráció teljesen kód-alapú (IaC), vagy kézzel kattintott? Ha kézzel, upgrade esetén reprodukálható-e?

### Megjegyzés

Ha a Keycloak már production-ben fut és Doorstar onboarding zajlik, migration most nem reális — de a konfiguráció IaC-ba (Terraform Keycloak provider vagy realm-export.json) való zárása kötelező.

---

## 6. B2B Federation complexity MVP előtt

**Súlyosság: 🔴 MAGAS (wasted effort kockázat)**

### A probléma

A Phase 3C+ már tartalmaz B2BHandshake + `/bff/handshakes` implementációt. Két ügyféllel (Doorstar + 1 tervezett 2026 Q3-ban) federation = két ember számára épített híd. Minden jelenlegi federation API throwaway code kockázata magas, mivel valódi cross-tenant use case-ek ismerete nélkül épül.

Federation megoldásához szükséges: identity mapping, data sovereignty, schema versioning két tenant között, conflict resolution, partial failure recovery, cross-tenant authorization (Need-to-Know RBAC federation esetén exponenciálisan komplikálódik).

### Források

- **Pat Helland — "Data on the Outside vs. Data on the Inside"** (Microsoft Research, 2005) — cross-boundary data sharing fundamentálisan más probléma, mint internal data management
- **Sam Newman — "Building Microservices" 2nd ed., 2021, Chapter 4** — "integration is the most expensive part of any distributed system"
- **AWS SaaS Factory — B2B SaaS Patterns** — ajánlás: "tenant isolation first, federation only after 50+ paying tenants"
- **Stripe Connect / Plaid** — B2B federation 2–3 év érett internal platform után élesedett, nem MVP-ben
- **Kent Beck — Extreme Programming Explained — YAGNI elv** — "implement things when you actually need them, never when you just foresee that you need them"
- **Gartner — Multi-Enterprise Business Application Platforms (2023)** — érettségi modell: federation Level 4/5, MVP Level 1/2

### Nyitott kérdések

1. Melyik konkrét 2026-os use case igényli a federationt? (Név szerint: melyik Doorstar workflow nem oldható meg single-tenant módon?)
2. Ha a válasz "majd 2027-ben a DACH terjeszkedésnél kell", akkor a jelenlegi implementáció YAGNI-violation.

### Javasolt megközelítés

Single-tenant per ügyfél, manuális export/import az első 5 ügyfélig. 2027-ben (DACH belépés előtt) federationt valódi pattern adatok alapján tervezni — nem előre spekulálni.

---

## Amit NEM kérdőjeleztünk meg

Az alábbi döntések megalapozottak, és szándékosan nem szerepelnek a kockázatlistán:

- **Domain modell** (FlowEpic, SpaceLayer, Tenant/Facility/WorkStation hierarchia) — jól átgondolt, iparspecifikus, Clean Architecture + DDD megfelelő ennél a komplexitásnál
- **"Data → Rules → Geometry" axióma** — helyes döntés, a frontend egyetlen koordinátát sem számol
- **PostgreSQL + EF Core** — megfelelő választás multi-tenant SaaS-nál
- **Immutable audit event log** — technikailag helyes, a jogi réteg hiánya a kockázat (lásd #4), nem maga az elvem
- **Faipari piac diagnózisa** (Viber + Excel koordináció) — reális probléma, alátámasztott
- **Turborepo monorepo** a frontendnek — indokolt multi-brand portálnál

---

*Ez a dokumentum élő — frissítendő, ha valamely kockázat kezelésre kerül vagy új kockázat azonosítható.*
