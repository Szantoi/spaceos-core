# SpaceOS — ADR Catalogue

> Architektúrális döntési rekordok az üzenetek alapján. Forrás: MSG-K021, MSG-K031, MSG-K054-DONE, MSG-KC01-RESP, MSG-KERNEL-081-DONE, MSG-KERNEL-082-DONE.

---

## ADR-005 — Advisory Lock Single-Instance Constraint

**Kérdés:** Hogyan garantáljuk, hogy audit event write-ok sorosan történjenek (nincs PreviousHash duplikátum)?

**Döntés:** `pg_advisory_xact_lock(bigint)` MD5 int64 kulccsal, tranzakción belül.

**Alternatíva:** Optimistic concurrency (version field) — de az audit chain hash-lánc szekvenciális írást igényel.

**Implementáció:** `PostgresAdvisoryAuditWriteLock.cs` — MD5 int64 key (nem `hashtext()` int4). [MSG-K031 Track B]

---

## ADR-008 — SpatialContractDto ElementType absent

**Kérdés:** Miért nincs `ElementType` property a `SpatialContractDto`-ban?

**Döntés:** Szándékosan kihagyva. Az ElementType gyártói belső adat — megrendelő ne lássa.

**Referencia:** Need-to-Know RBAC (5 Golden Rule #4). [MSG-K025 SEC-P3A-07]

---

## ADR-023 — CreateStageHandoffCommandHandler Infrastructure rétegben

**Kérdés:** Miért van a `CreateStageHandoffCommandHandler` az Infrastructure rétegben és nem az Application rétegben (CQRS konvenció)?

**Döntés:** Infrastructure rétegben marad.

**Indoklás:** A handler `pg_advisory_xact_lock(bigint)` közvetlen `AppDbContext` hozzáférést igényel az idempotency biztosításához. Az Application réteg nem tud közvetlen DB lock-ot kiadni — az abstrakciókon (repository interfészeken) keresztül nem megvalósítható.

**Elfogadott arch kivétel:** Ez a handler egyedi — a többi CQRS handler Application rétegben van.

[MSG-K054-DONE]

---

## ADR-024 — ES256 → Keycloak JWKS váltás

**Kérdés:** Saját ES256 keypair menedzsment (LocalEcKeyProvider) vs Keycloak JWKS végpont?

**Döntés:** Keycloak JWKS-alapú validációra váltás (Keycloak IdP Integration Sprint).

**Alternatíva:** Saját kulcsmenedzsment (már implementálva Phase 1.5-ben) — de duplikált auth infra.

**Implementáció:**
- Törölt: `ISigningKeyProvider`, `LocalEcKeyProvider`, `ConfigureJwtBearerOptions`, `JwtAccessTokenIssuer`, `RefreshTokenCommand/Handler/Validator`
- Bevezetett: `options.Authority = KC_REALM_URL` → automatikus JWKS fetch + validáció
- `JwksHealthCheck` (`/health/ready`) — Keycloak elérhetőség monitoring

[MSG-KC01-RESP]

---

## ADR-025 — ProviderStub vs ProviderHttpAdapter döntési fa

**Kérdés:** Mikor melyik IInventoryProvider implementációt regisztrálni?

**Döntés:** Stub → HttpAdapter evolutív upgrade, nem egyidejű.

| Fázis | Implementáció | Mikor |
|-------|---------------|-------|
| Fejlesztés (unit teszt) | `InventoryProviderStub` | teszt konfigurációban |
| Integráció előtt | `InventoryProviderStub` (prod DI-ben) | TILOS — production bug forrása |
| Production (HTTP service fut) | `InventoryProviderHttpAdapter` | `IConfiguration` konfigált BaseUrl-lel |

**Kulcsdöntés:** Stub soha NEM kerülhet production DI container-be. [MSG-CUTTING-010-DONE]

---

## ADR-026 — DbConnectionInterceptor vs SaveChangesInterceptor

**Kérdés:** Melyik interceptor típus alkalmas GUC beállításra?

**Döntés:**
- **DbConnectionInterceptor** (Kernel minta): Connection open-kor beállít, close-kor resetel — SESSION szintű GUC
- **SaveChangesInterceptor** (egyes modulok): Csak write-path-en fut — read-path nem védett!
- **DbCommandInterceptor** (Abstractions M02 fix): Read query-k előtt is beállít — defense-in-depth

**Ajánlás:** Kernel `DbConnectionInterceptor` minta a legjobb (connection affinity garantált). Az Abstractions `DbCommandInterceptor` fallback ha `DbConnectionInterceptor` nem implementálható.

[MSG-ABSTRACTIONS-007-DONE M02, MSG-K021 T-04]

---

## ADR-027 — DenyWebRequestSentinel bevezetése

**Kérdés:** Hogyan kezeljük a "web request de nincs `tid` claim" esetet az EF query filterben?

**Döntés:** Sentinel UUID (`000...002`) visszaadása null helyett — mindig üres eredményt ad.

**Alternatíva:** 401 dobása a filter szintjén — de ez cross-cutting concern az adatrétegben.

[MSG-KERNEL-081-DONE]

---

## ADR-028 — Keycloak Script Mapper double serialization (BE-01)

**Kérdés:** A `spaceos_tenants` claim értéke JSON array legyen vagy string?

**Döntés:** JSON string (`JSON.stringify(tenants)`) — double-serialized.

**Technikai ok:** Keycloak Script Mapper JS API korlátozások — natív JSON array claim beállítása nem megbízható az OIDC token-ben. A string formátum garantáltan megőrződik.

**Következmény:** Backend-en double-deserialization szükséges: JSON string → JSON parse → array.

[MSG-INFRA-KC01-DONE BE-01 note, MSG-INFRA-060-DONE]

---

## ADR-029 — OpenConnectionAsync explicit affinity (Internal endpoints)

**Kérdés:** Hogyan biztosítsuk a GUC és az adatlekérés ugyanazon connection-en futását Internal (service-to-service) endpointokon?

**Döntés:** Explicit `OpenConnectionAsync` + `try/finally` close minta.

**Miért kell:** Az Internal endpoint-okon nincs JWT auth pipeline → a `TenantSessionInterceptor` (ConnectionOpened hook) nem fut automatikusan → GUC üres marad.

[MSG-CUTTING-015-DONE, JOINERY-014 pattern]

---

## ADR-030 — GenesisHash deploy-invariáns kezelése

**Kérdés:** Ha az audit chain első hash-e (`GenesisHash`) futás közben számítódik, hogyan garantáljuk, hogy deploy után ne változzon?

**Döntés:** Env var-ban rögzíteni az első deploy utáni értéket.

**Alternatíva:** DB-ben tárolni — de startup sorrendtől függő lehet.

[MSG-INFRA-096 pattern]

---

## ADR-031 — SourceChannel enum helye: Shared namespace

**Kérdés:** A `SourceChannel` enum a `Cutting/Enums/` vagy a `Shared/` namespace alá kerüljön?

**Döntés:** `SpaceOS.Modules.Contracts.Shared` namespace.

**Indoklás:** FreeTier és PartnerTier a Kernel szinten is használhatja (audit event, RBAC), nem Cutting-specifikus fogalom.

**Implementáció:** `SpaceOS.Modules.Contracts/Shared/SourceChannel.cs` — `Direct=0, FreeTier=1, PartnerTier=2`

[Contracts Architecture v4.2, D-31]

---

## ADR-032 — SubmitAnonymousSheetAsync Default Interface Method (DIM)

**Kérdés:** Hogyan adjunk hozzá `SubmitAnonymousSheetAsync`-t az `ICuttingProvider`-hez anélkül, hogy megtörjük a meglévő implementációkat?

**Döntés:** Default Interface Method (DIM) — a default implementáció `NotSupportedException`-t dob.

**Alternatíva:** Breaking change (új metódus, minden impl frissítendő) — elutasítva.

**Következmény:** Consumer MUST check `ProviderCapability.CuttingAnonymous` flag mielőtt hívja. Ha az implementáció explicit megvalósítja, a DIM felülíródik.

[Contracts Architecture v4.2, D-32, D-33]

---

## ADR-033 — CuttingAnonymous flag pozíció: 1 << 12

**Kérdés:** Melyik bit pozícióba kerüljön a `ProviderCapability.CuttingAnonymous` flag?

**Döntés:** `1 << 12` — a következő szabad pozíció.

**Indoklás:** `[Flags]` enum szekvenciális, nem szabad hézagot hagyni a meglévő 12 flag (1<<0..1<<11) után.

[Contracts Architecture v4.2, D-34]

---

## ADR-034 — AnonymousSheetRequest: wrapper, nem önálló flat DTO

**Kérdés:** Az `AnonymousSheetRequest` önálló flat DTO legyen vagy wrappelje a `SubmitCuttingSheetRequest`-et?

**Döntés:** Wrapper — extra mezők: `Source (SourceChannel)`, `PartnerId`, `BrandingContextId`.

**Indoklás:** DRY — a cutting sheet input nem duplikált.

[Contracts Architecture v4.2, D-35]

---

## ADR-035 — SpaceOS.Nesting.Algorithms: önálló NuGet, Contracts-tól független

**Kérdések:**
1. A nesting algoritmus `SpaceOS.Nesting` vagy `SpaceOS.Nesting.Algorithms` névvel jelenjen meg?
2. Az `INestingStrategy` interface a Nesting NuGet-ben vagy a Contracts NuGet-ben legyen?

**Döntések:**
1. `SpaceOS.Nesting.Algorithms` — specifikusabb, a `SpaceOS.Nesting` namespace az egész domain-nek fenntartva.
2. Nesting NuGet-ben — a Contracts NuGet közös modul szerződés; a nesting algoritmus implementációs részlet, nem minden modul fogyasztja.

**Következmény:** A Cutting modul mapperrel köti össze a két NuGet-et (`CuttingLine` → `NestingPart`, stb.).

[Contracts Architecture v4.2, D-36, D-37]

---

## ADR-036 — CuttingPlanStatus: typed enum + DB migration (Opció C)

**Kérdés:** Hogyan váltsunk `CuttingPlan.Status string` mezőről typed enum-ra meglévő deployed adat mellett?

**Opciók:**
- A) Clean migration (UPDATE + enum) — production adatokat módosít, E2E törnek
- B) Két párhuzamos mező — két igazságforrás, zavaros
- **C) Enum + CASE konverziós migration** — `ALTER COLUMN TYPE + USING CASE`

**Döntés:** Opció C — `CuttingPlanStatus { Draft=0, Published=1, Frozen=2, Closed=3 }`.

**Backward-compat aliasok:** `"Approved"` → `Published`, `"inprogress"` → `Frozen` (command handler szinten).

**Megjegyzés:** A migration manuálisan lett megírva (`dotnet ef` tool nem volt elérhető). VPS-en `dotnet ef database update` kötelező az élesítéshez.

[Cutting Planning Architecture v4, Section 2.3, MSG-CUTTING-031-DONE]

---

## ADR-037 — EnableRetryOnFailure eltávolítása explicit transaction-t használó DbContext-ekből

**Kérdés:** Hogyan oldjuk fel az `InvalidOperationException: OnFirstExecution + CurrentTransaction != null` hibát?

**Döntés:** `EnableRetryOnFailure` eltávolítása minden olyan `DbContext`-ből, amely explicit tranzakciót használ (`AppDbContext`, `AuditDbContext`, `ModulesDbContext`).

**Kivétel:** `HashSinkDbContext` — nincs user transaction, a retry stratégia helyes marad.

**Indoklás:** A `NpgsqlRetryingExecutionStrategy.OnFirstExecution()` hibát dob, ha a connection-ön már aktív tranzakció van — ez pontosan akkor fordul elő, amikor a modulok explicit tranzakciót nyitnak.

[MSG-KERNEL-100-DONE, KERNEL-090/091/093/099]
