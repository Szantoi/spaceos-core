**SpaceOS.Kernel**

Security & Audit — Consolidated Task Register

*Minden elvégzendő feladat egy helyen — prioritás, felelős réteg, effort és függőségek szerint*

2026\. április  •  v1.0  •  Belső PM dokumentum

# **1\. Összefoglaló**

Ez a dokumentum összesíti az Audit Research (v1.0), az Ördög ügyvédje elemzés, és a Senior Security Review összes megállapítását. Minden feladat priorizálva, réteg szerint felelőshöz rendelve, effort-becsléssel és függőséggel ellátva szerepel.

| Dimenzió | Érték |
| :---- | :---- |
| Forrás dokumentumok | SpaceOS\_Audit\_Research.docx · Ördög ügyvédje analízis · Senior Security Review |
| Összes task | 21 db |
| Kritikus (P0) | 4 db — azonnali beavatkozás |
| Magas (P1) | 8 db — Sprint 1-2 |
| Közepes (P2) | 6 db — Sprint 3-4 |
| Alacsony (P3/P4) | 3 db — roadmap |
| Érintett rétegek | Domain · Application · Infrastructure · Api · DevOps |
| Teljes becsült effort | \~47 fejlesztői nap |

| Legfontosabb megállapítás:  A hash chain és a JWT secret kezelése egymástól független, de egyforma súlyú kritikus problémák. Mindkettő hamis biztonságérzetet ad: a hash chain ugyanabban a trust boundary-ban él mint az adat, a HS256 kulcs pedig env var-ból kiolvasható. Ezek P0 — blokkolják az Escrow production deployment-et. |
| :---- |

# **2\. Threat Model — Ki ellen védünk?**

A feladatok priorizálásának alapja. Jelenleg egyik threat actor sincs explicit dokumentálva a rendszerben — ez önmagában P1 feladat.

| Threat Actor | Képesség | Jelenlegi védelem | Gap |
| :---- | :---- | :---- | :---- |
| Külső hálózati támadó | HTTP kérések, brute force | JWT, rate limiting (IP) | IP-based RL megkerülhető |
| Kompromittált app process | DB write, hash újraszámítás | ❌ Nincs | Hash chain \+ DB \= same trust |
| Rosszindulatú DBA | Direkt DB módosítás | Csak app-szintű tiltás | Nincs DB-szintű WORM/role |
| Kompromittált Tenant (B2B) | Hamis JWT TenantId claim | HS256 (shared secret) | HS256 → bárki aláírhat |
| Insider (fejlesztő) | Code injection, migration | Code review (feltételezett) | Nincs formalizálva |
| Kompromittált Orchestrator | Tetszőleges IntentDataJson | ❌ Nincs schema validáció | Kernel határán nincs guard |

| P0  KRITIKUS — Azonnali beavatkozás *Production Escrow deployment előtt kötelező. Hamis biztonságérzetet okozó elemek.* |
| :---- |

### **P0-1 · JWT: HS256 → RS256/ES256 \+ Key Vault**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — JWT Secret Kezelés |
| Probléma | HS256 shared secret env var-ban tárolva. /proc/{pid}/environ, Docker inspect, crash dump mind kiszivárogtatja. Bárki aki ismeri a kulcsot tetszőleges TenantId claim-et írhat alá. |
| Megoldás | RS256 vagy ES256 asymmetric signing. Private key Azure Key Vault / AWS Secrets Manager-ben. Public key a validációhoz publikus endpoint-on. |
| Érintett réteg | SpaceOS.Kernel.Api · Infrastructure · DevOps (docker-compose, CI/CD) |
| Effort becslés | 3 fejlesztői nap |
| Függőség | Nincs — önálló task |
| Definition of Done | JWT\_SIGNING\_KEY env var eltávolítva · Key Vault integrációs teszt zöld · API.Tests JWT tesztek frissítve |

### **P0-2 · Hash Chain: External Write-Only Sink bevezetése**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — Broken Trust Boundary · Ördög ügyvédje \#1 |
| Probléma | A hash chain és az audit adatbázis ugyanabban a trust boundary-ban van. Kompromittált DB hozzáférés esetén a teljes chain újraszámítható — a verify-chain endpoint IsValid: true-t ad vissza. |
| Megoldás | Minden AuditEvent hash-t párhuzamosan egy write-only external sink-be is el kell küldeni: Azure Immutable Blob Storage, AWS S3 Object Lock (WORM), vagy append-only syslog szerver külön gépen. A verify-chain ezt a külső tanút is konzultálja. |
| Érintett réteg | Application/Audit · Infrastructure/ExternalSink · DevOps |
| Effort becslés | 4 fejlesztői nap |
| Függőség | P0-1 után (autentikált sink connection) |
| Definition of Done | Minden AuditEvent hash megjelenik a külső sink-ben · verify-chain endpoint a külső tanút is lekérdezi · Integration teszt lefedi a divergencia detekciót |

### **P0-3 · Hash Chain: Race Condition — Serialized Write**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Ördög ügyvédje \#2 — Chain Fork |
| Probléma | GetLastHashAsync \+ párhuzamos request \= két rekord ugyanazzal a PreviousHash-sel. A lánc elágazik, mindkét ág 'valid' — a chain integrity checker nem tudja eldönteni melyik helyes. |
| Megoldás | Pessimistic lock vagy serialized queue az AuditEventDispatcher-ben. PostgreSQL advisory lock (pg\_try\_advisory\_xact\_lock) per TenantId, vagy dedikált single-writer channel (Channel\<T\>) az Application rétegben. |
| Érintett réteg | Application/Audit/AuditEventDispatcher · Infrastructure |
| Effort becslés | 2 fejlesztői nap |
| Függőség | P0-2 előtt kell (a sink-be is sorrend-helyes hash kerüljön) |
| Definition of Done | Concurrent dispatch load teszt: 50 párhuzamos event → 0 fork · Chain verify zöld |

### **P0-4 · PostgreSQL: Audit tábla DB-szintű role szeparáció**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — Append-only app szintű kényszer · Ördög ügyvédje \#6 |
| Probléma | Az alkalmazás DB user-ének jelenleg nincs tiltva a DELETE/UPDATE az AuditEvents táblán. Egy kompromittált migration script vagy DBA bypass-olhatja az app-szintű védelmet. |
| Megoldás | Dedikált spaceos\_audit\_writer PostgreSQL role: csak INSERT \+ SELECT az AuditEvents táblán. Az alkalmazás connection string ezt a role-t használja az audit writer path-on. A spaceos\_app role-nak nincs joga az AuditEvents-hez UPDATE/DELETE-re. |
| Érintett réteg | Infrastructure · DevOps (docker-compose, init SQL) |
| Effort becslés | 1 fejlesztői nap |
| Függőség | Nincs |
| Definition of Done | spaceos\_audit\_writer role létezik · DELETE kísérlet permission denied · IntegrationTests ezt teszteli |

| P1  MAGAS — Sprint 1–2 *Strukturális biztonsági és trust hiányosságok. Escrow MVP előtt teljesítendő.* |
| :---- |

### **P1-1 · TenantId: HTTP header → JWT claim**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — ITenantResolver HTTP Header alapú |
| Probléma | HttpTenantResolver a TenantId-t HTTP headerből olvassa. SSRF vagy proxy esetén más Tenant nevében küldhetők kérések. A header nem hitelesített forrás. |
| Megoldás | A TenantId kizárólag a validált JWT sub/claims-ből olvasható ki. HTTP header csak hint lehet debug célra (development only). A HttpTenantResolver refaktorálása: ClaimsPrincipal alapú lookup. |
| Érintett réteg | SpaceOS.Kernel.Api · Application/ITenantResolver |
| Effort becslés | 2 fejlesztői nap |
| Függőség | P0-1 (JWT reform kész legyen előbb) |
| Definition of Done | HttpTenantResolver nem olvassa a headert · API.Tests: header manipulation nem változtatja a TenantId-t |

### **P1-2 · ExternalAuthToken: Key Vault referencia, nem plaintext DB**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — B2B ExternalAuthToken tárolása |
| Probléma | SpaceLayer.ExternalAuthToken plaintext-ben az alkalmazás adatbázisában. Backup leak, SQL injection, vagy RBAC misconfiguration esetén az összes federált kapcsolat tokenje kompromittálódik. |
| Megoldás | Az adatbázisban csak a Key Vault referencia (secret name / ARN) tárolódik. A tényleges token futásidőben kerül lekérésre. SpaceLayer entitás módosítása: ExternalAuthToken → ExternalAuthTokenRef (Key Vault pointer). |
| Érintett réteg | Domain/SpaceLayer · Infrastructure · DevOps |
| Effort becslés | 3 fejlesztői nap |
| Függőség | P0-1 (Key Vault kapcsolat megvan) |
| Definition of Done | ExternalAuthToken mező nem létezik az adatbázisban · Migration eltávolítja · IntegrationTests Key Vault mockkal fut |

### **P1-3 · AggregateSnapshot entitás \+ tábla (FlowEpic, FlowMilestone)**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Audit Research 4.1 · Security Review — Snapshot \+ Escrow nem atomiáris |
| Probléma | Nincs snapshot store — aggregate múltbeli állapota nem kérdezhető le. Az Escrow kifizetés pillanatában fennálló állapot nem bizonyítható jogilag. |
| Megoldás | AggregateSnapshot entitás létrehozása (AggregateId, AggregateType, Version, SnapshotAt, TriggerEventId, StateJson jsonb, SnapshotHash, TenantId). EF Core migration, repository interfész és implementáció. |
| Érintett réteg | Domain · Infrastructure · Migration |
| Effort becslés | 3 fejlesztői nap |
| Függőség | Nincs — domain szintű task |
| Definition of Done | Tábla létezik · Repository tesztek zöldek · 350+ teszt fennmarad |

### **P1-4 · SnapshotService: CLOSED\_DONE \+ Milestone trigger (Outbox Pattern)**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Audit Research 4.2 · Security Review — Snapshot \+ Escrow nem atomiáris |
| Probléma | A snapshot mentés és az Escrow trigger nem atomiáris. Process crash esetén a kifizetés olyan állapotra hivatkozhat, amelynek hash-e nincs rögzítve — vagy fordítva. |
| Megoldás | Outbox Pattern: a snapshot és az Escrow trigger ugyanabban a DB tranzakcióban kerül az Outbox táblába. Külön background worker dolgozza fel és küldi el. Saga kompenzáció rollback esetén. |
| Érintett réteg | Application/Snapshots · Infrastructure/Outbox |
| Effort becslés | 4 fejlesztői nap |
| Függőség | P1-3 |
| Definition of Done | CLOSED\_DONE event → Outbox bejegyzés · Outbox worker → snapshot \+ Escrow trigger atomiáris · Chaos teszt: process kill → konzisztens állapot |

### **P1-5 · IntentDataJson: Schema validáció az API határán**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — IntentDataJson schema validáció hiány |
| Probléma | SpaceLayer.IntentDataJson tetszőleges JSONB — nincs schema validáció az API határán. Kompromittált Orchestrator tetszőleges payloadot tud beküldeni, amely a Driver rétegben logikai bypass-t vagy crash-t okozhat. |
| Megoldás | TradeType-specifikus JSON Schema validáció a Kernel Api rétegben, az UpdateSpaceLayerIntentData endpoint-on. Joinery TradeType esetén Joinery schema; MEP esetén MEP schema stb. FluentValidation \+ System.Text.Json JsonDocument. |
| Érintett réteg | SpaceOS.Kernel.Api · Application/Validators |
| Effort becslés | 3 fejlesztői nap |
| Függőség | Nincs |
| Definition of Done | Invalid JSON schema → 422 Unprocessable · Minden TradeType-nak van schema · API.Tests teszteli |

### **P1-6 · Rate Limiting: Identity alapú (JWT sub \+ TenantId)**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — Rate Limiting IP alapú |
| Probléma | IP-alapú rate limiting megkerülhető IPv6 rotation-nel, residential proxy-val. B2B esetén az office IP mögött minden felhasználó egy IP-t mutat → legitimate DOS saját felhasználókra. |
| Megoldás | Sliding window rate limiting JWT sub \+ TenantId kulcs alapján. In-memory store helyett Redis (horizontális skálázáshoz). Endpoint-specifikus limitek: write műveletek szigorúbbak. Egységes 429 response RFC 7807 szerint. |
| Érintett réteg | SpaceOS.Kernel.Api · Infrastructure (Redis) · DevOps |
| Effort becslés | 2 fejlesztői nap |
| Függőség | P0-1 (JWT reform — sub claim megbízható) |
| Definition of Done | Különböző IP, azonos JWT → közös limit · Redis-ben tárolt counter · RateLimitTestFactory frissítve |

### **P1-7 · Threat Model dokumentum**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — hiányzó Threat Model |
| Probléma | Nincs formális threat model. Biztonsági döntések ad-hoc alapon születnek, nincs közös referencia arra, hogy ki ellen, milyen képességgel védekezünk. |
| Megoldás | STRIDE modell alkalmazása az 5 azonosított threat actor-ra (külső hálózati, kompromittált process, DBA, B2B tenant, insider). Minden réteghez (API, Application, Infrastructure, B2B Federation) explicit mitigáció. A dokumentum a CLAUDE.md-be kerül referenciaként. |
| Érintett réteg | Architektúra / dokumentáció |
| Effort becslés | 2 fejlesztői nap |
| Függőség | P0 taskokhoz képest párhuzamosan futtatható |
| Definition of Done | Threat Model doc létezik · STRIDE tábla minden rétegre · Elfogadott review |

### **P1-8 · ProofUrl: Content-Addressed Immutable Storage**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Ördög ügyvédje \#5 · Security Review — ProofUrl nem immutable |
| Probléma | ImplementationSummary.ProofUrl egy HTTP pointer. A mögöttes content törölhető, módosítható. Escrow vita esetén a bizonyíték eltűnhet vagy megváltozhat — az adatbázisban lévő hash-en kívül nincs kötelező kapcsolat a tényleges tartalommal. |
| Megoldás | Feltöltés S3 Object Lock (WORM) vagy Azure Immutable Blob-ba. Az adatbázisban ProofUrl helyett ProofHash (SHA-256 a feltöltött fájlból) kerül tárolásra. Az URL másodlagos — a hash authoritative. Feltöltési endpoint a Kernel-ben hash-t számít és ellenőriz. |
| Érintett réteg | Domain/ImplementationSummary · Application · Infrastructure/Storage · Api |
| Effort becslés | 3 fejlesztői nap |
| Függőség | P0-1 (autentikált storage connection) |
| Definition of Done | ProofHash mező létezik · Feltöltés → hash számítás → WORM storage · Visszaolvasáskor hash validáció |

| P2  KÖZEPES — Sprint 3–4 *Compliance, megfigyelhetőség és temporal query képességek. Escrow GA előtt teljesítendő.* |
| :---- |

### **P2-1 · Chain Integrity Verifier API endpoint**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Audit Research 3.3 |
| Megoldás | GET /api/audit-events/verify-chain?tenantId=\&from=\&to= · IsValid, FirstBrokenAt, TotalRecordsChecked · A külső sink-et is konzultálja (P0-2 után). |
| Érintett réteg | Api · Application/Audit |
| Effort becslés | 2 fejlesztői nap |
| Függőség | P0-2, P0-3 |

### **P2-2 · Snapshot Query API**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Audit Research 4.3 |
| Megoldás | GET /api/snapshots/{id}?at= · GET /api/snapshots/{id}/versions · GET /api/audit/diff?from=\&to= · Lapozható eredmény. |
| Érintett réteg | Api/Snapshots · Application/Queries |
| Effort becslés | 2 fejlesztői nap |
| Függőség | P1-3, P1-4 |

### **P2-3 · GDPR: Pseudonymizáció \+ PII szeparáció**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Audit Research 5.2 · Ördög ügyvédje \#4 |
| Megoldás | UserProfile tábla külön — audit logban csak userId GUID. AES-256 a payload PII mezőin, per-tenant kulcsrotációval. Balancing test dokumentálva a Privacy Notice-ban. |
| Érintett réteg | Domain · Infrastructure · Jogi (Privacy Notice) |
| Effort becslés | 4 fejlesztői nap |
| Függőség | P0-1 (Key Vault a kulcsokhoz) |

### **P2-4 · Audit Log: Alerting és anomaly detection**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Security Review — Audit log nincs alertelve · Audit Research 5.1 SOC 2 |
| Megoldás | Alert: ha az audit log megáll (SOC 2 CC7.2). Anomaly: szokatlanul sok CLOSED\_DONE rövid időn belül, vagy chain break detekció → PagerDuty/webhook. Prometheus metrika az audit event rate-re. |
| Érintett réteg | Infrastructure · DevOps (monitoring) |
| Effort becslés | 2 fejlesztői nap |
| Függőség | P0-2, P0-3 |

### **P2-5 · Genesis Hash: Deployment-time generálás**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Ördög ügyvédje \#7 |
| Megoldás | A genesis hash konstans eltávolítása. Első deployment-kor kriptográfiailag véletlenszerű genesis hash generálódik és Key Vault-ba kerül. Migration ellenőrzi, hogy a genesis rekord létezik-e. |
| Érintett réteg | Application/Audit · Infrastructure · DevOps |
| Effort becslés | 1 fejlesztői nap |
| Függőség | P0-1, P0-3 |

### **P2-6 · Kriptográfiai algoritmus migration plan**

| Attribútum | Érték |
| :---- | :---- |
| Forrás | Ördög ügyvédje \#8 |
| Megoldás | HashAlgorithm enum az AuditEvent-en (SHA256, SHA3\_256). Migration utility: régi chain re-hash-elés új algoritmussal, chain kontinuitás megőrzésével. Dokumentált upgrade path. |
| Érintett réteg | Domain · Application · dokumentáció |
| Effort becslés | 2 fejlesztői nap |
| Függőség | P0-2, P0-3 |

| P3 / P4  ALACSONY — Roadmap (Q3 2026 – Horizon 5\) *Jogi és enterprise-grade kiegészítések. Üzleti igény vezérelt.* |
| :---- |

| Task | Leírás | Érintett réteg | Effort | Feltétel |
| :---- | :---- | :---- | :---- | :---- |
| P3-1 · RFC 3161 TSA | Minden Milestone elfogadáskor és Escrow triggerkor DigiCert / GlobalSign EU TSA bélyeg. Jogilag hiteles harmadik fél timestamp. | Infrastructure/Timestamping | 5 nap | P1-4 (Outbox kész) |
| P4-1 · Blockchain hash anchoring | Időszakos hash anchoring Polygon L2-re. Csak ha az Escrow üzletileg igényli — a Hash chain \+ TSA önmagában elegendő. | Infrastructure/Blockchain | 8 nap | P3-1, üzleti döntés |
| P4-2 · Event Sourcing átállás | Full Event Sourcing refactor, ha audit replay SLA vagy Horizon 5 blockchain anchoring explicit üzleti igény lesz. Jelenleg a Snapshot Store az előnyök 80%-át adja. | Teljes backend | 20+ nap | Explicit üzleti igény |

# **7\. Master Task Register**

Minden task egy helyen, végrehajtási sorrendben.

| ID | Task | Réteg | Effort | Prioritás | Függőség |
| :---- | :---- | :---- | :---- | :---- | :---- |
| P0-1 | JWT HS256 → RS256/ES256 \+ Key Vault | Api · Infra · DevOps | 3 nap | **KRITIKUS** | — |
| P0-2 | Hash chain: External write-only sink | Application · Infra | 4 nap | **KRITIKUS** | P0-1 |
| P0-3 | Hash chain: Race condition → serialized write | Application · Infra | 2 nap | **KRITIKUS** | — |
| P0-4 | PostgreSQL audit role szeparáció | Infra · DevOps | 1 nap | **KRITIKUS** | — |
| P1-1 | TenantId: HTTP header → JWT claim | Api · Application | 2 nap | Magas | P0-1 |
| P1-2 | ExternalAuthToken → Key Vault ref | Domain · Infra | 3 nap | Magas | P0-1 |
| P1-3 | AggregateSnapshot entitás \+ tábla | Domain · Infra | 3 nap | Magas | — |
| P1-4 | SnapshotService: Outbox Pattern | Application · Infra | 4 nap | Magas | P1-3 |
| P1-5 | IntentDataJson JSON Schema validáció | Api · Application | 3 nap | Magas | — |
| P1-6 | Rate limiting: Identity alapú \+ Redis | Api · Infra | 2 nap | Magas | P0-1 |
| P1-7 | Threat Model dokumentum (STRIDE) | Architektúra | 2 nap | Magas | párhuzamos |
| P1-8 | ProofUrl → ProofHash \+ WORM storage | Domain · Api · Infra | 3 nap | Magas | P0-1 |
| P2-1 | Chain Integrity Verifier endpoint | Api · Application | 2 nap | Közepes | P0-2, P0-3 |
| P2-2 | Snapshot Query API | Api · Application | 2 nap | Közepes | P1-3, P1-4 |
| P2-3 | GDPR pseudonymizáció \+ PII szeparáció | Domain · Infra | 4 nap | Közepes | P0-1 |
| P2-4 | Audit alerting \+ anomaly detection | Infra · DevOps | 2 nap | Közepes | P0-2 |
| P2-5 | Genesis hash deployment-time gen. | Application · DevOps | 1 nap | Közepes | P0-1, P0-3 |
| P2-6 | Crypto algoritmus migration plan | Domain · Application | 2 nap | Közepes | P0-2 |
| P3-1 | RFC 3161 TSA timestamping | Infrastructure | 5 nap | Alacsony | P1-4 |
| P4-1 | Blockchain hash anchoring (Polygon L2) | Infrastructure | 8 nap | Roadmap | P3-1 |
| P4-2 | Event Sourcing full átállás | Teljes backend | 20+ nap | Roadmap | Üzleti igény |

| Teljes becsült effort:  P0: 10 nap · P1: 22 nap · P2: 15 nap · P3: 5 nap · P4: 28+ nap. A P0–P1 blokk (\~32 nap) az Escrow production deployment minimuma. |
| :---- |

# **8\. Ajánlott Végrehajtási Sorrend**

| Sprint | Tartalom | Eredmény |
| :---- | :---- | :---- |
| Sprint 0 (1 hét) | P0-1 · P0-3 · P0-4 (párhuzamos: P1-7 Threat Model) | JWT biztonságos · Audit chain serialized · DB role szeparált |
| Sprint 1 (2 hét) | P0-2 · P1-1 · P1-2 · P1-5 | External sink aktív · TenantId megbízható · Token vault-ban · Schema guard |
| Sprint 2 (2 hét) | P1-3 · P1-4 · P1-6 · P1-8 | Snapshot store él · Outbox atomiáris · Identity RL · Proof immutable |
| Sprint 3 (1.5 hét) | P2-1 · P2-2 · P2-4 · P2-5 | Chain verifier · Temporal API · Alerting · Genesis secure |
| Sprint 4 (1.5 hét) | P2-3 · P2-6 | GDPR compliant · Crypto upgrade path |
| Q3 2026 | P3-1 RFC 3161 TSA | Jogilag hitelesített timestamp az Escrow-hoz |
| Horizon 5 | P4-1 · P4-2 (üzleti döntés alapján) | Blockchain anchoring / Event Sourcing |

*SpaceOS — Security & Audit Task Register | 2026\. április | Architect & PM*