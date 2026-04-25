# SpaceOS Cabinet 0.1 — Core Foundation
## Architektúra-tervdokumentum v4 (IMPLEMENTÁCIÓRA KÉSZ)

> **Verzió:** v4.0 — 2026-04-25 (senior-backend review applied — final)
> **Státusz:** ✅ **IMPLEMENTÁCIÓRA KÉSZ** — minden finding lezárva, Claude Code agent #1 részére átadható
> **Pipeline:** v1.1 → v2 (persistence/contract review) → v3 (security review) → **v4** (backend review) → IMPLEMENTÁCIÓRA KÉSZ
> **Előzmény dokumentumok:** `SpaceOS_Cabinet_Core_Session_20260425.docx` (16 axióma + 18 OD)
> **Scope:** A1–A11 axiómák — Geometry, Skeleton refactor, Machining, Construction, Semantics, Advisory, TenantStandard port (interface-only)
> **NuGet kibocsátási cél:** `SpaceOS.Cabinet.* 0.1.0` (release), `0.1.0-alpha.N` (munkaverziók)
> **Repo:** `spaceos-modules-cabinet` (VPS authoritative)
> **Becsült effort:** ~12 nap (v1) + 3.75 (v2) + 3.5 (v3) + **2.5 (v4)** = **~21.75 fejlesztői nap**
>
> **v1.0 → v1.1 patch:** runtime cél-framework `net8.0` → `net8.0;net10.0` multi-target.
> **v1.1 → v2 review:** 10 finding (DB-CAB-1..10) — schema versioning SemVer-string, migration policy, multi-target API stability, snapshot determinism, FIFO event ordering, cache thread-safety.
> **v2 → v3 review:** 10 finding (SEC-CAB-1..10) — 1 CRITICAL (NaN/Infinity), 5 HIGH (cross-tenant, dimension, DOS, post-deserialize), 3 MEDIUM, 1 LOW.
> **v3 → v4 review:** 8 finding (BE-CAB-1..8) — algoritmikus optimalizáció policy (O(N²) elfogadva, dokumentálva), thread-safety contract, deprecation lifecycle, allocation profiling, paralellizmus-elhalasztás.

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|---|---|---|---|
| v1 (DRAFT) | — | bázis dokumentum | 12 nap |
| v1 → v1.1 patch | runtime cél-framework hiányos | multi-target `net8.0;net10.0` | +0 nap (build-konfig) |
| v1.1 → v2 (database-designer + database-schema-designer) | 0 CRITICAL · 4 HIGH · 4 MEDIUM · 2 LOW = 10 finding | Schema-version SemVer-string, migration policy, multi-target API stability, snapshot determinism | +3.75 nap |
| v2 → v3 (senior-security) | 1 CRITICAL · 5 HIGH · 3 MEDIUM · 1 LOW = 10 finding | NaN/Infinity guard, cross-tenant Part isolation, dimension overflow, DOS limits, post-deserialize validation | +3.5 nap |
| **v3 → v4** (senior-backend) | **0 CRITICAL · 3 HIGH · 4 MEDIUM · 1 LOW = 8 finding** | Algoritmikus komplexitás policy, thread-safety contract, deprecation lifecycle, parallelism deferral | **+2.5 nap** |
| **Összesen** | **28 finding** | | **21.75 fejlesztői nap** |

A v2 finding-ek listája: §13. A v3 finding-ek listája: §14. A v4 finding-ek listája: §15.

---

## 2. Kontextus és scope

### 2.1 Mit csinál a Cabinet 0.1

A `SpaceOS.Cabinet.*` egy **platform-független domain motor** az asztalosipari termékek (szekrény, ajtó, ablak) parametrikus modellezésére. A Cabinet 0.1 release a foundation:

- **Skeleton** — parametrikus szekrény-aggregate, BaseCuboid + Part-ok + Connection-ök
- **Geometry** — affin mátrixok és reference frame-ek, platform-független
- **Machining** — megmunkálás-katalógus VO-k, vasalat-binding
- **Construction** — szabálymotor (32mm system mint default)
- **Semantics** — gravitációs + topológiai inferencia szerepekre (bal oldal, polc, hátfal)
- **Advisory** — figyelmeztetések, sosem blokkolás (A11)

### 2.2 Mit nem csinál a Cabinet 0.1

| Téma | Hová tartozik |
|---|---|
| Catalog (CatalogEntry, lifecycle FSM) | Cabinet 0.2 |
| Assembly Documentation (AssemblyStep, ExplodedView) | Cabinet 0.2 |
| FlowEpic Scope-bővítés (MicroAssembly) | Cabinet 0.2 |
| TenantStandard implementáció (aggregate) | Cabinet 0.2 |
| Marketplace BillOfServices | Cabinet 0.x későbbi |
| Persistence (DWG XRecord, SQL) | **Adapter-rétegek** (cabinetbilder-autocad) |
| AutoCAD-specifikus Brep műveletek | **cabinetbilder-autocad** |
| HTTP API, REST | **Adapter-rétegek** |

### 2.3 Architektúra alapaxiómák (rögzítve)

A Cabinet 0.1 a következő axiómákat implementálja:

| Axióma | Tartalom | Implementáció |
|---|---|---|
| **A1** | Affin mátrix mindenhol | `AffineTransform` VO |
| **A2** | Két reference frame | `PartFrame` + `AssemblyFrame` |
| **A3** | BaseCuboid mint gyökér | `BaseCuboid` aggregate-belső entity |
| **A4** | Hátfal mint kubus-derivált | `BaseCuboid.BackPanel` slot |
| **A5** | Default joint = face-edge butt | `Connection.JointType = FaceEdgeButt` default |
| **A6** | Megmunkálás 3-féle Subject-tel | `MachiningFeature.Subject` enum |
| **A7** | Szemantikus név derivált | `SemanticInferenceService` |
| **A8** | Platform-független Core | NuGet csomagok, semmilyen UI/CAD függőség |
| **A9** | TenantStandard | csak `ITenantStandardProvider` interface |
| **A10** | Szelektív újraszámítás | `DependencyGraph` (Kahn topo-sort) |
| **A11** | Warning, sosem blokk | `DesignAdvisory` + `Severity` enum |

A12–A16 (Catalog, Assembly, FlowEpic, Marketplace) → Cabinet 0.2.

---

## 3. NuGet csomagok és belső dependency graph

### 3.1 Csomagok

| Csomag | Tartalom | Cél-framework |
|---|---|---|
| **SpaceOS.Cabinet.Geometry** | AffineTransform, PartFrame, AssemblyFrame, DimensionVector, GravityVector | `netstandard2.1` |
| **SpaceOS.Cabinet.Abstractions** | Port-interfészek: ITenantStandardProvider, IGeometryProjector, IPartCatalog | `netstandard2.1` |
| **SpaceOS.Cabinet.Domain** | Skeleton, BaseCuboid, Part, Connection (aggregate root + entities) | `net8.0;net10.0` |
| **SpaceOS.Cabinet.Machining** | MachiningFeature, MachiningSubject, HardwareReference (VO-k) | `net8.0;net10.0` |
| **SpaceOS.Cabinet.Construction** | ConstructionRule, ConstructionRuleEngine, default rule-ok, DesignAdvisory | `net8.0;net10.0` |
| **SpaceOS.Cabinet.Semantics** | SemanticInferenceService, PartRole, GravityVector | `net8.0;net10.0` |
| **SpaceOS.Cabinet** *(meta)* | Üres csomag, függőségként az összeset behúzza | `net8.0;net10.0` |

A `Geometry` és `Abstractions` szándékosan `netstandard2.1` — ezek csomagok bekerülhetnek mobil app-okba, WebAssembly környezetbe, és minden `net*` runtime-ba. A többi csomag **multi-target** (`net8.0;net10.0`) — szervert (.NET 8 LTS) és AutoCAD 2027 plugin (.NET 10) is ki kell szolgálnia. Részletesen: §3.4.

#### Naming convention (DB-CAB-5)

A SpaceOS Cabinet ekosystem **minden** NuGet csomagja a következő mintát követi:

```
SpaceOS.Cabinet.{ComponentName}[.{SubComponentName}]

Példák:
  SpaceOS.Cabinet.Geometry           ← Cabinet 0.1 scope
  SpaceOS.Cabinet.Domain
  SpaceOS.Cabinet.Catalog            ← Cabinet 0.2-ben jön
  SpaceOS.Cabinet.Assembly           ← Cabinet 0.2-ben jön
  SpaceOS.Cabinet.Adapters.AutoCAD   ← jövőbeli, ha külön ki akarjuk emelni
```

**Szabályok:**
- A `SpaceOS.Cabinet` prefix kötelező — minden cabinet ekosystem csomag-azonosító
- A `{ComponentName}` PascalCase, egyetlen szó vagy szóösszetétel
- A namespace **ugyanaz** mint a NuGet package név (`namespace SpaceOS.Cabinet.Geometry`)
- Belső subkomponens csak akkor kap saját csomagot, ha **független release-ciklusra van szüksége** vagy **eltérő cél-framework-re**
- Pre-release-suffix mindig SemVer minta szerint: `0.1.0-alpha.5`, `0.1.0-rc.1`, `0.1.0`

### 3.2 Dependency graph

```
SpaceOS.Cabinet.Abstractions (port-ok, netstandard2.1)
            ▲
            │
SpaceOS.Cabinet.Geometry (netstandard2.1)
            ▲
            │
SpaceOS.Cabinet.Domain ────► SpaceOS.Cabinet.Machining
            ▲                          ▲
            │                          │
SpaceOS.Cabinet.Semantics              │
            ▲                          │
            └────► SpaceOS.Cabinet.Construction ◄──── SpaceOS.Cabinet.Abstractions
                          ▲
                          │
                  SpaceOS.Cabinet (meta)
```

**Szabályok:**
- `Abstractions` semmitől nem függ (csak BCL)
- `Geometry` semmitől nem függ (csak BCL)
- `Domain` függ: `Abstractions` + `Geometry`
- `Machining` függ: `Domain` + `Geometry`
- `Semantics` függ: `Domain` + `Geometry`
- `Construction` függ: `Domain` + `Machining` + `Semantics` + `Abstractions`
- Cirkuláris függés tilos

### 3.3 Approved package-ek (Cabinet 0.1)

A SpaceOS Kernel approved listájához igazodva:
- **MediatR** — domain event dispatcher
- **Ardalis.Result** — `Result<T>` minden public API-n
- **FluentValidation** — VO validáció (esetenként)

**Ami NINCS:** EF Core, Ardalis.Specification (nincs DB), MediatR-handler-ek (nincs CQRS-orchestration ezen a szinten — az adapter-réteg dolga). A Domain 100% POCO marad.

### 3.4 Runtime cél-framework stratégia

Két fogyasztó-konfiguráció van, eltérő .NET-verzió-igénnyel:

| Fogyasztó | Runtime | Indok |
|---|---|---|
| SpaceOS Kernel + Orchestrator + Portal (jövőbeli szerver-oldali integráció) | **.NET 8 LTS** | Production-grade LTS. A meglévő stack ezen fut |
| CabinetBilder Adapter.AutoCAD (Doorstar pilot, Windows) | **.NET 10** | AutoCAD 2027 kötelezően .NET 10-et igényel |

A Modules.Cabinet csomagjainak **mindkét fogyasztót ki kell szolgálnia** ugyanabból a NuGet artifact-ból. A megoldás: **multi-targeting** a `net8.0;net10.0` cél-framework listával.

```xml
<!-- Példa: SpaceOS.Cabinet.Domain.csproj -->
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

A NuGet csomag tartalmaz egy `lib/net8.0/` és egy `lib/net10.0/` mappát. A fogyasztó projekt automatikusan az illeszkedő DLL-t választja.

**Build-szabályok:**
- A VPS build-environment-en **`.NET 10 SDK`** szükséges (`dotnet --list-sdks` jelzi). A `.NET 10 SDK` képes `net8.0` cél-frameworkre is buildelni.
- A `.NET 8 runtime` és `.NET 10 SDK` parallel-installable; a Kernel/Orchestrator/Portal továbbra is `.NET 8`-on fut a VPS-en.
- `#if NET10_OR_GREATER` direktívák kerülendők — a domain kód nyelvi/runtime feature-tekintetében C# 12 (`net8.0`) sufficient. Ha mégis valahol kell platform-specifikus elágazás, az **finding**-ként kezelendő a v3/v4 review-ban.

#### Build reproducibility (v2 — DB-CAB-1)

A `spaceos-modules-cabinet` repo **gyökerében** kötelező egy `global.json` fájl, amely pinneli az SDK-verziót:

```json
{
  "sdk": {
    "version": "10.0.203",
    "rollForward": "latestFeature"
  }
}
```

**Indok:** a `dotnet --version` output a fejlesztőgépen, CI-ben és VPS-en **ugyanaz** legyen. A `latestFeature` engedi a minor frissítéseket (10.0.204), de major (11.x) ugrásokat nem — ez biztosítja, hogy egy év múlva is ugyanaz az SDK buildeli a kódot.

#### Multi-target API stability (v2 — DB-CAB-4)

A `net8.0` és `net10.0` build-ek **ugyanazt a public API-t** kell hogy adják. Konkrétan:

- **Tilos** `#if NET10_OR_GREATER` direktíva a public felületen (pl. egy method egyik build-ben létezik, másikban nem)
- **Engedett** `#if` direktíva **belső implementáció-szinten**, ha mind a két ágon ugyanazt az interfészt valósítja meg
- A NuGet csomag mindkét `lib/` mappájában az XML doc fájl (`SpaceOS.Cabinet.Domain.xml`) **azonos** kell legyen
- Smoke teszt: egy üres `net8.0` console app build-elhető-e a Modules.Cabinet csomag használatával — minden public API-t

#### Snapshot determinism cross-runtime (v2 — DB-CAB-8)

A `SkeletonSnapshot.ToJson()` ugyanazt a byte-sorozatot kell hogy adja `net8.0`-on és `net10.0`-on **ugyanazon input** mellett. A `System.Text.Json` viselkedése elvileg konzisztens, de a `JsonSerializerOptions` default-jai változhatnak verziók között.

**Védőszabály:** a Modules.Cabinet a saját `JsonSerializerOptions`-t építi fel, **explicit minden tulajdonságra**:

```csharp
internal static class CabinetJsonOptions
{
    public static readonly JsonSerializerOptions Strict = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = false,
        WriteIndented = false,
        AllowTrailingCommas = false,
        NumberHandling = JsonNumberHandling.Strict,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
        // Type-stable converters
    };
}
```

**Test gate:** a Cabinet 0.1 test suite tartalmaz egy "cross-runtime determinism" tesztet:
1. Generál egy fix `SkeletonSnapshot`-ot `net8.0`-on, kiír egy fájlba
2. Generál ugyanazt `net10.0`-on, kiír egy másik fájlba
3. Byte-pontosan egyeznie kell

A CI mindkét framework-re külön step-ben futtatja és összehasonlítja.

**Cél-framework döntési mátrix:**

| Csomag | `net8.0;net10.0` | `netstandard2.1` | Indok |
|---|---|---|---|
| Geometry | — | ✓ | Mobile + WebAssembly + minden `net*` kompat |
| Abstractions | — | ✓ | Port-interfészek, max kompat |
| Domain | ✓ | — | Modern C# nyelvi feature-ök, runtime-specifikus optimalizációk lehetségesek |
| Machining | ✓ | — | Modern C# |
| Construction | ✓ | — | Modern C#, MediatR (`net8.0`+) |
| Semantics | ✓ | — | Modern C# |
| Cabinet (meta) | ✓ | — | Mindent behúz |

**Fogyasztói példa:**

```xml
<!-- cabinetbilder-autocad/src/Adapter.AutoCAD/Adapter.AutoCAD.csproj -->
<TargetFramework>net10.0-windows</TargetFramework>

<ItemGroup>
  <PackageReference Include="SpaceOS.Cabinet.Domain" Version="0.1.0" />
  <!-- A NuGet automatikusan a lib/net10.0/ mappát választja -->
</ItemGroup>
```

```xml
<!-- (jövőbeli) spaceos-portal-server/src/Server.csproj -->
<TargetFramework>net8.0</TargetFramework>

<ItemGroup>
  <PackageReference Include="SpaceOS.Cabinet.Domain" Version="0.1.0" />
  <!-- A NuGet automatikusan a lib/net8.0/ mappát választja -->
</ItemGroup>
```



---

## 4. Domain modell (részletesen)

### 4.1 Geometry namespace (`SpaceOS.Cabinet.Geometry`)

A teljes namespace **stateless value object-eket** tartalmaz. Minden típus `readonly record struct` vagy `sealed record`, immutable.

#### 4.1.1 `AffineTransform` (A1)

```csharp
namespace SpaceOS.Cabinet.Geometry;

public readonly record struct AffineTransform
{
    // 4×4 mátrix, sor-major reprezentáció
    private readonly double[] _m;

    public static AffineTransform Identity { get; }

    // FACTORY METHODS — minden ad vissza Result<T>-t
    public static Result<AffineTransform> Translation(Vector3 offset);
    public static Result<AffineTransform> Rotation(Vector3 axis, double radians);
    public static Result<AffineTransform> Scaling(double sx, double sy, double sz);
    public static Result<AffineTransform> Compose(AffineTransform a, AffineTransform b);

    // ÉRVÉNYESSÉG-ELLENŐRZÉS (SEC-CAB-2)
    /// <summary>
    /// True ha a mátrix összes eleme véges (nem NaN, nem Infinity, nem -Infinity).
    /// Minden factory method automatikusan validál.
    /// </summary>
    public bool IsValid();

    public Result<Vector3> ApplyTo(Vector3 point);                 // Result.Error ha self !IsValid vagy point !IsValid
    public Result<Vector3> ApplyToDirection(Vector3 direction);    // translation-mentes
    public Result<AffineTransform> Inverse();                      // Result.Error ha det ≈ 0

    public Vector3 BasisX();  // 1. oszlop — szálirány (A2)
    public Vector3 BasisY();  // 2. oszlop — szélesség (A2)
    public Vector3 BasisZ();  // 3. oszlop — normál (A2)
    public Vector3 Origin();  // 4. oszlop — datum (A2)

    // Egyenlőség epsilon-toleranciával
    public bool IsApproximatelyEqualTo(AffineTransform other, double epsilon = 1e-9);
}
```

**SEC-CAB-2 (CRITICAL): NaN/Infinity védelem**

Egy hibás vagy malicious input (pl. nullával osztás eredménye, JSON-ban literal `"NaN"`) az affine mátrixba kerülve **az egész aggregate-et korruptált állapotba viszi**. A `Vector3.ApplyTo(point)` `NaN`-t ad, ami tovább terjed minden geometriai számolásra.

**Védelem:**
- Minden `AffineTransform` factory method (`Translation`, `Rotation`, `Scaling`, `Compose`) `Result<AffineTransform>`-et ad vissza, és belsőleg validál: minden mátrix-elem `double.IsFinite()` (azaz nem NaN, nem ±∞)
- Ha valamelyik input `Vector3` érvénytelen (NaN/Inf komponens), a factory `Result.Error` az "Invalid input"-tal
- A `CabinetJsonOptions.Strict` deserialize-kor `JsonNumberHandling.Strict` — a `"NaN"` literal-t **elutasítja** (System.Text.Json default `Strict`-ben sem fogadja, ezt explicit megerősítjük)
- Minden `ApplyTo`/`ApplyToDirection` is `Result<Vector3>`-at ad — ha valamiért NaN keletkezik a műveletben, az error-csatornán jelez
- `Inverse()` `Result.Error`-t ad ha a determináns ≤ epsilon (singular mátrix)

**Tervezési döntés:** ez a `Result<T>` propagálás **fokozza** az API verbosity-jét, de a security-prioritás magasabb. Az adapter-réteg (`cabinetbilder-autocad`) tudja "egyetlen `.Value` access"-szel kezelni amikor biztosan tudja, hogy az input érvényes.

#### 4.1.2 `Vector3`

```csharp
public readonly record struct Vector3(double X, double Y, double Z)
{
    public static Vector3 Zero { get; }
    public static Vector3 UnitX { get; }
    public static Vector3 UnitY { get; }
    public static Vector3 UnitZ { get; }

    public bool IsValid();              // SEC-CAB-2: minden komponens IsFinite()
    public double Length();
    public Vector3 Normalized();
    public double Dot(Vector3 other);
    public Vector3 Cross(Vector3 other);

    /// <summary>
    /// Egyenlőség konfigurálható epsilon-toleranciával (BE-CAB-7).
    /// Default: GeometryConstants.DefaultEpsilon = 1e-9
    /// </summary>
    public bool IsApproximatelyEqualTo(Vector3 other, double epsilon = GeometryConstants.DefaultEpsilon);

    // Operators
    public static Vector3 operator +(Vector3 a, Vector3 b);
    public static Vector3 operator -(Vector3 a, Vector3 b);
    public static Vector3 operator *(Vector3 v, double scalar);
}

/// <summary>
/// Központi konfigurálási pont a numerikus epsilon-okra (BE-CAB-7).
/// Tenant-szintű override Cabinet 0.2-ben jön (TenantStandard kibővítve).
/// </summary>
public static class GeometryConstants
{
    public const double DefaultEpsilon = 1e-9;       // általános numerikus precízió
    public const double AngularEpsilon = 1e-7;       // rotációs összehasonlítások (radián)
    public const double DimensionEpsilon = 1e-3;     // mm-szintű (1 micron) — méret-egyenlőség
}
```

**BE-CAB-7 (MEDIUM): Konfigurálható epsilon**

A v3-ban a hardcoded `1e-9` minden helyen ugyanaz. Cabinet 0.1-ben ez az **alapértelmezett**, de:

- Az `IsApproximatelyEqualTo(other, epsilon)` overload eseti override-ot enged
- A `GeometryConstants` osztály **központosított konfigurálási pont** — minden epsilon egy helyen
- Tenant-szintű override **Cabinet 0.2**-be kerül a `TenantStandard` kibővítésével

A három különálló epsilon (`Default`, `Angular`, `Dimension`) különböző felhasználási kontextusokra való:
- `DefaultEpsilon (1e-9)`: pontos numerikus egyenlőség (skalárok, irányvektorok)
- `AngularEpsilon (1e-7)`: szögek összehasonlítása (`cos(α) - cos(β)`)
- `DimensionEpsilon (1e-3)`: 1 mikron pontosság — méret-egyenlőség valós szekrényeken (sub-mm pontosság)

#### 4.1.3 `DimensionVector`

```csharp
// Part dimensions in PartFrame (A2)
public readonly record struct PartDimension(double Length, double Width, double Thickness)
{
    public static PartDimension FromMillimeters(double l, double w, double t);
    public Vector3 ToVector() => new(Length, Width, Thickness);

    /// <summary>
    /// Validációk: minden tagnak > 0 ÉS finite ÉS belül legyen a max-tartományban.
    /// SEC-CAB-3: extrém méret-támadás védelme.
    /// </summary>
    public static Result<PartDimension> Create(double length, double width, double thickness);

    // Limits (SEC-CAB-3) — túllépés Result.Error
    public const double MaxLength = 6000.0;        // 6 m maximum part-hossz (= max szekrény-magasság)
    public const double MaxWidth = 3000.0;         // 3 m maximum part-szélesség
    public const double MaxThickness = 100.0;      // 10 cm maximum lap-vastagság
    public const double MinDimension = 0.1;        // 0.1 mm minimum minden dimenzió
}

// Assembly dimensions in AssemblyFrame (A2)
public readonly record struct AssemblyDimension(double Width, double Height, double Depth)
{
    public static AssemblyDimension FromMillimeters(double w, double h, double d);
    public Vector3 ToVector() => new(Width, Depth, Height);

    /// <summary>
    /// Validációk: minden tagnak > 0 ÉS finite ÉS belül legyen a max-tartományban.
    /// SEC-CAB-3: extrém méret-támadás védelme.
    /// </summary>
    public static Result<AssemblyDimension> Create(double width, double height, double depth);

    // Limits (SEC-CAB-3)
    public const double MaxWidth = 6000.0;         // 6 m maximum szekrény-szélesség (gardrób-falsor)
    public const double MaxHeight = 6000.0;        // 6 m maximum magasság
    public const double MaxDepth = 1500.0;         // 1.5 m maximum mélység
    public const double MinDimension = 50.0;       // 5 cm minimum bármilyen dimenzió (értelmes lower bound)
}
```

**SEC-CAB-3 (HIGH): Méret-overflow védelem**

Az `AffineTransform`-on `double` aritmetikán keresztül egy 1e300 mm méret nem int-overflow-ba esne (mert `double`), de **`+Infinity`**-vé válna, és további számolásokon át NaN-okat generálna. Ez akkor is veszélyes, ha a NaN-guard (SEC-CAB-2) megvan: az `Inverse()` egy `+Infinity` mátrixra "rosszul jól-definiált" nullmátrixot adhat, ami egy aggregálódó értékre később NaN-t ad.

**Védelem:**
- A `PartDimension.Create()` és `AssemblyDimension.Create()` factory-k validálják:
  - Minden tag `double.IsFinite()` (NaN, ±∞ kizárva)
  - Minden tag `> MinDimension`
  - Minden tag `< MaxXxx` (a fenti tábla szerint)
- Ha bármelyik feltétel sérül → `Result.Error("Dimension out of range: ...")`
- A `Skeleton.ResizeAssembly(newDim)` az `AssemblyDimension.Create()`-t használja, így automatikusan védett

**Konfiguráció:** ezeket a `MaxXxx` értékeket a TenantStandard felülbírálhatja egyedi tenantra (pl. egy ipari gardróbsor-gyártó nagyobb max-értéket akarhat). A v1 ezt nem implementálja — v3 finding hozzáteszi a TenantStandard-ot mint forrás-portot, de az _alapértelmezett_ limit minden tenantnál ugyanaz.

#### 4.1.4 `PartFrame` és `AssemblyFrame` (A2)

```csharp
// PartFrame: a lap saját rendszere
//   X = szálirány (Length tengely)
//   Y = szélesség (Width tengely)
//   Z = vastagodás (-Z irányban, A2 axióma)
public sealed record PartFrame(AffineTransform LocalToAssembly)
{
    public PartDimension Dimension { get; init; }

    public Vector3 GrainDirectionInAssembly() => LocalToAssembly.BasisX();
    public Vector3 NormalInAssembly() => LocalToAssembly.BasisZ();
    public Vector3 DatumInAssembly() => LocalToAssembly.Origin();  // Front-Bottom-Left sarok
}

// AssemblyFrame: a szekrény saját rendszere
//   X = Width
//   Y = Depth (mert a +Z fel-irányt akarjuk megőrizni)
//   Z = Height (gravitáció ellen, +Z = fel)
public sealed record AssemblyFrame
{
    public AssemblyDimension Dimension { get; init; }
    public AffineTransform AssemblyToWorld { get; init; }

    public static readonly Vector3 GravityDirection = new(0, 0, -1);  // (A7 alap)
}
```

#### 4.1.5 `GravityVector`

```csharp
// Konstans v1-ben, interface-ezhető Cabinet 0.x-ben (OD-RFR-08)
public static class GravityVector
{
    public static readonly Vector3 Default = new(0, 0, -1);
}
```

### 4.2 Skeleton namespace (`SpaceOS.Cabinet.Domain`)

A `Skeleton` aggregate root. **Platform-független** — a CabinetBilder Adapter.AutoCAD perzisztálja DWG XRecord-on át.

#### 4.2.1 `Skeleton` aggregate root

```csharp
namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Skeleton aggregate — parametrikus szekrény-modell.
///
/// THREAD-SAFETY (BE-CAB-2):
///   READ operations (Parts, Connections, ToSnapshot, ValidateDesign): lockless, thread-safe
///   WRITE operations (AddPart, RemovePart, ResizeAssembly, AddConnection): NOT thread-safe
///   A fogyasztó-réteg felelős, hogy egy Skeleton-instance-on
///   egyszerre csak EGY szál hívjon mutáló method-ot.
///
///   AutoCAD adapter konvenciója: minden Skeleton-mutáció a UI thread-en történik.
///   (AutoCAD document lock egyébként is serializálja.)
/// </summary>
public sealed class Skeleton
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid Version { get; private set; }                       // v2 — DB-CAB-6: cache key
    public long LastSequenceNumber { get; private set; }            // v3 — SEC-CAB-10: event sequence integrity
    public AssemblyDimension Dimension { get; private set; }
    public BaseCuboid BaseCuboid { get; private set; }

    private readonly List<Part> _parts = new();
    public IReadOnlyList<Part> Parts => _parts;

    private readonly List<Connection> _connections = new();
    public IReadOnlyList<Connection> Connections => _connections;

    // Domain events (SEC-CAB-10: SequenceNumber tracking)
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    // SEC-CAB-5: DOS védelem
    public const int MaxPartsPerSkeleton = 500;
    public const int MaxConnectionsPerSkeleton = 2000;
    public const int MaxMachiningsPerPart = 100;

    // BE-CAB-6: domain events memory cap
    public const int MaxUnflushedEvents = 1000;

    public Result<Part> AddPart(/* ... */);                          // Result.Error ha _domainEvents.Count > MaxUnflushedEvents
    public Result RemovePart(Guid partId);
    public Result<Connection> AddConnection(/* ... */);
    public Result ResizeAssembly(AssemblyDimension newDimension);   // A10: szelektív invalidation

    public IReadOnlyList<IDomainEvent> PopDomainEvents();
}
```

**Invariánsok:**
- Minden `Part` az `AssemblyFrame`-ben él (transform-ja `LocalToAssembly`)
- Minden `Connection` két érvényes `Part`-ot köt össze (ID-feloldás kötelező)
- A `BaseCuboid`-on nem csinálható tetszőleges mutáció — csak az aggregate-on át (A3)
- `Parts.Count <= MaxPartsPerSkeleton` (SEC-CAB-5)
- `Connections.Count <= MaxConnectionsPerSkeleton` (SEC-CAB-5)
- Minden Part `Machinings.Count <= MaxMachiningsPerPart` (SEC-CAB-5)
- **`_domainEvents.Count <= MaxUnflushedEvents` (BE-CAB-6)** — túllépés `Result.Error("Domain events not flushed — call PopDomainEvents() first")`

**SEC-CAB-5 (HIGH): DOS védelem extrém Part-szám esetén** — lásd §7.2 algoritmikus komplexitás.

**BE-CAB-6 (MEDIUM): Domain events memory cap**

Egy hosszú session során (1000+ mutáció) a `_domainEvents` listája végtelenül nőhet, ha a fogyasztó-réteg elfelejti a `PopDomainEvents()` hívást. A `MaxUnflushedEvents = 1000` egy biztonsági limit:

- Ha a fogyasztó **rendszeres** flush-ot tesz (minden mutáció után), akkor ez sose lép be
- Ha valamiért elmarad a flush, az `AddPart`/`AddConnection`/`ResizeAssembly` `Result.Error`-t ad vissza, és a fogyasztó-réteg észreveszi a bug-ot
- Ez **nem lassít** a normál működésben — csak biztosítja, hogy ne nőjön végtelenül a memóriafogyasztás

#### 4.2.2 `BaseCuboid` (A3, A4)

A `BaseCuboid` egy aggregate-belső entity, ami a szekrény gyökere. Mindig pontosan 4 + opcionális 1 Part-ot tartalmaz:

```csharp
public sealed class BaseCuboid
{
    public Part LeftSide { get; }
    public Part RightSide { get; }
    public Part Bottom { get; }
    public Part Top { get; }              // FullTop vagy CrossRailPair (Phase 0.2)
    public Part? BackPanel { get; }       // opcionális — A4 (statikai elem, kubus-derivált)

    // BaseCuboid sose áll össze tisztán a kívülről — Skeleton.Initialize() építi
    internal static BaseCuboid CreateDefault(AssemblyDimension dim, ITenantStandardProvider tenantStandard);
}
```

**A4 invariáns:** ha `BackPanel != null`, akkor a `BackPanel.Frame.Dimension` és `BackPanel.Frame.LocalToAssembly` derivált a `BaseCuboid` 4 fő-Part-jából — nem szabadon szerkeszthető. A user a `Skeleton.SetBackPanelMode(BackPanelMode mode)` aggregate-method-on át változtatja, ami invalidálja és újraszámolja.

#### 4.2.3 `Part`

```csharp
public sealed class Part
{
    public Guid Id { get; private set; }
    public Guid SkeletonId { get; private set; }            // SEC-CAB-1: immutable, csak factory-ból

    public PartFrame Frame { get; private set; }            // A1, A2
    public string MaterialReference { get; private set; }   // Catalog reference (Cabinet 0.2-ben aggregate, most string)
    public string PartCatalogReference { get; private set; }

    public PartRole? AssignedRole { get; private set; }     // A12: user-felülbírált szemantikus szerep, null = derivált

    private readonly List<MachiningFeature> _machinings = new();
    public IReadOnlyList<MachiningFeature> Machinings => _machinings;

    // SEC-CAB-1: Part csak `internal Part(...)` ctor-ral hozható létre — public Create nincs
    // A Skeleton.AddPart() factory adja át a SkeletonId-t — sosem külső input
    internal static Part Create(Guid skeletonId, /* params */);

    // Aggregate-method-ek a Skeleton-ból hívva
    internal Result UpdateFrame(PartFrame newFrame);
    internal Result AddMachining(MachiningFeature feature);
    internal Result RemoveMachining(Guid featureId);
    internal Result AssignRole(PartRole role);
    internal Result ClearAssignedRole();
}
```

**SEC-CAB-1 (HIGH): Cross-tenant Part-isolation**

Egy potenciális támadás-vektor: a fogyasztó (pl. AutoCAD adapter) **manuálisan** létrehoz egy Part-ot egy `SkeletonId`-vel, ami **másik tenant**-é. Ha a Part valahogy bekerül egy idegen Skeleton-ba (pl. a JSON-deserialize-cláson át), az cross-tenant adat-szivárgást okozhat.

**Védelem:**
- A `Part` ctor `internal` (csak az `SpaceOS.Cabinet.Domain` assembly-n belülről hívható)
- Nincs public `Part.Create(skeletonId, ...)` — a Part-okat **mindig a `Skeleton.AddPart()`** hozza létre, és a `SkeletonId`-t implicit a `this.Id`-vel adja át
- A `Skeleton.AddPart()` belsőleg validálja, hogy a Part új (nincs duplikált ID-s Part)
- Deserializációkor (`Skeleton.FromSnapshot()`) a snapshot **minden Part-jára** ellenőrizzük, hogy a `Part.SkeletonId == this.Id`. Ha nem, `Result.Error("Cross-tenant Part detected")`

#### 4.2.4 `Connection` (A5, A6)

```csharp
public sealed class Connection
{
    public Guid Id { get; private set; }
    public Guid SkeletonId { get; private set; }

    public Guid ParentPartId { get; private set; }
    public Guid ChildPartId { get; private set; }

    public JointType JointType { get; private set; }       // A5 default = FaceEdgeButt
    public ConnectionGeometry Geometry { get; private set; }  // melyik él, melyik felület csatlakozik

    private readonly List<MachiningFeature> _machinings = new();  // A6: Subject = Connection
    public IReadOnlyList<MachiningFeature> Machinings => _machinings;

    internal Result SetJointType(JointType jointType);
    internal Result AddMachining(MachiningFeature feature);
}

public enum JointType
{
    FaceEdgeButt,        // default — A5
    Dado,                // horony (lap szélességében)
    Groove,              // horony (lap hosszában)
    Rabbet,              // falc
    Miter,               // gérvágás
    TongueGroove,        // csap+horony
    Pocket,              // zseb
    Dowel,               // tipli
    Mitered,             // gér + falc kombináció
    Offset               // virtuális (csak méret-számítás, nincs fizikai illesztés)
}

public sealed record ConnectionGeometry(
    PartFace ParentFace,           // melyik felülete a parent-nek érintett
    PartEdge ChildEdge,             // melyik éle a child-nek érintett
    double EdgeOffset);             // él-mentén pozicionálás
```

### 4.3 Machining namespace (`SpaceOS.Cabinet.Machining`)

A megmunkálás-katalógus VO-k. **A6 axióma**: Subject = Plane | Edge | Connection.

```csharp
namespace SpaceOS.Cabinet.Machining;

public sealed record MachiningFeature(
    Guid Id,
    MachiningSubject Subject,       // A6: hová alkalmazódik
    MachiningOperation Operation,   // mit csinál a CNC
    MachiningParameters Parameters,
    HardwareReference? Hardware);   // opcionális: vasalat-binding (A6)

public abstract record MachiningSubject;
public sealed record PlaneSubject(Guid PartId, PartFace Face) : MachiningSubject;
public sealed record EdgeSubject(Guid PartId, PartEdge Edge) : MachiningSubject;
public sealed record ConnectionSubject(Guid ConnectionId) : MachiningSubject;

public enum MachiningOperation
{
    Drill,        // furat (kör)
    Groove,       // horony marás
    Rabbet,       // falc marás
    Pocket,       // zseb marás
    Profile,      // élprofil marás
    EdgeBand,     // élzárás (szabász-utáni)
    Cut,          // szabás
    Chamfer,      // letörés
    Round         // lekerekítés
}

public sealed record MachiningParameters(
    double? Depth,
    double? Width,
    double? Diameter,
    double? Length,
    Vector3? Direction,
    AffineTransform? Placement);    // pozicionálás a Subject-en belül

public sealed record HardwareReference(
    string CatalogId,                // pl. "hafele.dowel.8x30"
    string CatalogType);             // "Dowel", "Hinge", "Drawer-slide", ...
```

**Példa — köldökcsap-páros egy Connection-ön:**

```csharp
// Egy face-edge butt csatlakozás köldökcsappal
var connection = skeleton.AddConnection(/*...*/, JointType.FaceEdgeButt);

connection.AddMachining(new MachiningFeature(
    Id: Guid.NewGuid(),
    Subject: new ConnectionSubject(connection.Id),
    Operation: MachiningOperation.Drill,
    Parameters: new MachiningParameters(Depth: 12, Diameter: 8),
    Hardware: new HardwareReference("hafele.dowel.8x30", "Dowel")
));
// Az adapter-réteg (cabinetbilder-autocad) ezt 2 furatra fordítja:
// 1. Parent edge-én (12mm mély, 8mm átmérőjű)
// 2. Child face-én (12mm mély, 8mm átmérőjű) ugyanazon pozícióban
```

### 4.4 Construction namespace (`SpaceOS.Cabinet.Construction`)

A szabálymotor + default rule-ok + Advisory-rendszer (A11).

#### 4.4.1 `ConstructionRule` interface

```csharp
namespace SpaceOS.Cabinet.Construction;

public interface IConstructionRule
{
    string RuleId { get; }
    string Description { get; }

    // Egy rule lehet:
    //   1. Generator: új MachiningFeature-ket vagy Part-okat hoz létre
    //   2. Validator: csak figyelmeztet (Advisory) — A11
    //   3. Both
    ConstructionRuleResult Apply(Skeleton skeleton, IConstructionContext context);
}

public sealed record ConstructionRuleResult(
    IReadOnlyList<MachiningFeature> GeneratedMachinings,
    IReadOnlyList<DesignAdvisory> Advisories);

public interface IConstructionContext
{
    ITenantStandardProvider TenantStandard { get; }
    AssemblyDimension AssemblyDimension { get; }
    // ... egyebek
}
```

#### 4.4.2 `ConstructionRuleEngine` domain service

```csharp
public sealed class ConstructionRuleEngine
{
    private readonly IReadOnlyList<IConstructionRule> _rules;

    public ConstructionRuleEngine(IEnumerable<IConstructionRule> rules);

    public Result<EngineResult> ApplyAll(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken = default);    // SEC-CAB-4: DOS védelem
}

public sealed record EngineResult(
    IReadOnlyList<MachiningFeature> AllGeneratedMachinings,
    IReadOnlyList<DesignAdvisory> AllAdvisories);
```

A motor sorba lefuttatja az összes regisztrált szabályt, és az eredményeket aggregálja. **A11 garancia:** az Engine soha nem dob exception-t domain-szabálysértésért — minden problémát `DesignAdvisory`-ként jelez.

**SEC-CAB-4 (HIGH): ConstructionRule DOS védelem**

Egy malicious vagy hibás szabály végtelen ciklusba kerülhet, vagy óriási mennyiségű MachiningFeature-t generálhat, ami a memóriát megzabálja.

**Védelem:**
- **Per-rule timeout:** minden `IConstructionRule.Apply()` hívást a motor egy `CancellationToken` keretében hív, default timeout **5 másodperc**. Ha a rule nem fejezi be ennyi idő alatt → `Result.Error("Rule '{ruleId}' timed out")`
- **Output-cap:** ha egy rule egynél több MAX_MACHININGS_PER_RULE = 1000 új MachiningFeature-t generál, az engine az 1000. után stop és `Result.Error`
- **Engine-szintű timeout:** a teljes `ApplyAll` futása max **30 másodperc**. Ezen túl `Result.Error`

```csharp
public interface IConstructionRule
{
    string RuleId { get; }
    string Description { get; }

    /// <summary>
    /// SEC-CAB-4: a rule köteles a CancellationToken-t periodikusan ellenőrizni.
    /// </summary>
    ConstructionRuleResult Apply(Skeleton skeleton, IConstructionContext context, CancellationToken cancellationToken);
}
```

**SEC-CAB-8 (MEDIUM): Rule registration validation**

Egy rule, ami `Apply()`-ban `null`-t ad vissza, megakaszthatja az egész engine-t. **Védelem:**
- Ha `Apply()` `null`-t ad → engine `DesignAdvisory.Critical("Rule '{ruleId}' returned null — disabled")`
- Ha `Apply()` exception-t dob → engine `DesignAdvisory.Critical("Rule '{ruleId}' threw: {exception}")`
- Ezek a rule-ok a session során **automatikusan letiltódnak** (de a többi rule fut)

#### 4.4.3 Default rule-ok (Cabinet 0.1)

| Rule ID | Tartalom | Severity |
|---|---|---|
| `R-32mm-LineBore` | Vertikális Part-okra 32mm raster soros furat (5mm Ø), kezdő furat 38mm | — |
| `R-Default-Joint` | Connection-ön nincs explicit JointType → FaceEdgeButt | — |
| `R-BackPanel-Hidden` | Hátfal nem látszik → Groove (depth=8mm, width=panel+0.2mm clearance) | — |
| `R-BackPanel-Visible` | Hátfal látszik → Rabbet (depth=panel+0.5mm, width=6–9mm) | — |
| `R-EdgeBand-FrontVisible` | Front-felé néző él automatikusan élzárva | — |
| `R-EdgeBand-Hidden` | Hátsó és belső élek nem élzártak | — |
| `R-Setback-15mm` | Hátfal-zóna 15mm setback a hátéltől | — |
| `R-Material-Default` | Alapanyag-default a TenantStandard-ból | — |
| `R-Stiffener-Tall` | 2000mm magasság feletti szekrény vízszintes összekötő nélkül → Warning | **Warning** |
| `R-Shelf-Sag` | 800mm-nél hosszabb polc 18mm-es lapból dado-rögzítéssel → Info | **Info** |

#### 4.4.4 `DesignAdvisory` (A11)

```csharp
public sealed record DesignAdvisory(
    string RuleId,                  // "R-Stiffener-Tall"
    AdvisorySeverity Severity,
    string Subject,                 // "Skeleton" | "Part:{guid}" | "Connection:{guid}"
    string Message,                 // human-readable Hungarian — SABLON, NEM tartalmaz tenant-specifikus értéket (SEC-CAB-9)
    string? SuggestedAction);       // pl. "Add a horizontal CrossRail at mid-height"

public enum AdvisorySeverity
{
    Info,        // jó-tudni
    Warning,     // figyelmet érdemel
    Critical     // erősen ajánlott orvosolni — DE NEM BLOKKOL (A11)
}
```

**A11 garancia:** sem `Warning`, sem `Critical` nem akadályozza a Skeleton-mentést, BOM-derivációt vagy CNC-export-ot. Csak feedback.

**SEC-CAB-9 (MEDIUM): Advisory message privacy**

A `DesignAdvisory.Message` szabad-szöveges. Ha egy `IConstructionRule` a `TenantStandard`-ből szám-szerű küszöböket vagy árazási adatokat (pl. "Material X exceeds budget at $999/m²") beleír a Message-be, akkor a snapshot-export (pl. AutoCAD log fájl, vagy bug-report) **érzékeny adatot szivároghat**.

**Védelem (kötelező szabály):**
- A `DesignAdvisory.Message` mező **sablonos**, **nem tartalmazhat számszerű adatot a TenantStandard-ből**
- Helyes példák:
  - ✅ `"Material exceeds tenant pricing threshold"`
  - ✅ `"Skeleton height above tall-cabinet limit, stiffener recommended"`
- Helytelen példák:
  - ❌ `"Material at $999.50/m² exceeds the $750 limit"`
  - ❌ `"Cabinet 2150mm tall exceeds tenant max 2000mm"`
- Számszerű adat csak a `Subject` referenciába mehet (pl. `"Part:abc123"` — ezt a fogyasztó tudja feloldani saját kontextusában)
- A `SuggestedAction` is sablonos, nem tartalmaz pénz-számot vagy tenant-belső konstanst

**Test gate:** unit teszt minden default rule-ra, ami ellenőrzi, hogy a generált Advisory.Message regex `[\$€£]\d` match-mentes (= nincs pénz-szám).

### 4.5 Semantics namespace (`SpaceOS.Cabinet.Semantics`)

A szemantikus inferencia (A7, A12).

```csharp
namespace SpaceOS.Cabinet.Semantics;

public sealed class SemanticInferenceService
{
    public PartRole InferRole(Part part, Skeleton skeleton);
    public IReadOnlyDictionary<Guid, PartRole> InferAll(Skeleton skeleton);
}

public enum PartRole
{
    LeftSide,
    RightSide,
    VerticalDivider,
    Bottom,
    Top,
    Shelf,
    BackPanel,
    Front,           // ajtó, fiók
    CrossRail,       // vízszintes összekötő (A12)
    CornerBrace,
    Unknown
}
```

#### 4.5.1 Inferencia szabályrendszer

A `SemanticInferenceService` a következő gravitációs + topológiai feltételeket ellenőrzi (Mathematical Furniture Theory §5):

| Feltétel | Eredmény |
|---|---|
| `n⃗ · g⃗ ≈ 0` AND `Datum.X ≈ 0` AND nincs `Part` balra | `LeftSide` |
| `n⃗ · g⃗ ≈ 0` AND `Datum.X ≈ AssemblyDimension.Width` AND nincs `Part` jobbra | `RightSide` |
| `n⃗ · g⃗ ≈ 0` AND `0 < Datum.X < Width` (közbülső) | `VerticalDivider` |
| `n⃗ · g⃗ ≠ 0` AND `Datum.Z ≈ 0` | `Bottom` |
| `n⃗ · g⃗ ≠ 0` AND `Datum.Z ≈ AssemblyDimension.Height − Thickness` | `Top` |
| `n⃗ · g⃗ ≠ 0` AND `0 < Datum.Z < Height` | `Shelf` (vagy `CrossRail` — A12) |
| `n⃗ · g⃗ ≈ 0` AND `Datum.Y ≈ AssemblyDimension.Depth` | `BackPanel` |
| `n⃗ · g⃗ ≈ 0` AND `Datum.Y ≈ 0` | `Front` |

**A12 különlegesség:** ha egy Part horizontálisnak tűnik (`n⃗ · g⃗ ≠ 0`) és belül van (`0 < Datum.Z < Height`), akkor a `Shelf` vs `CrossRail` döntést **a katalógus + user override** dönti el (`Part.AssignedRole`-on át). Az inferencia javasol, a user megerősíti.

#### 4.5.2 Cache + invalidation (v2 — DB-CAB-6)

```csharp
public interface ISemanticInferenceCache
{
    PartRole? TryGet(Guid skeletonVersion, Guid partId);
    void Set(Guid skeletonVersion, Guid partId, PartRole role);
    void InvalidateSkeleton(Guid skeletonId);
}
```

A `Skeleton` minden mutáció után `Version` bump-ot csinál (Guid-alapú monoton verzió). A cache `(SkeletonVersion, PartId)` kulcson hash-el. Mutation-on a régi version-ek invalidálódnak.

**Thread-safety követelmény (DB-CAB-6):**

A default implementáció `ConcurrentDictionary<(Guid, Guid), PartRole>` alapú, **lockless**. A `InvalidateSkeleton(skeletonId)` művelet **NEM** azonnali bulk-delete — ehelyett:

1. A `Skeleton.Version` minden mutációkor új `Guid`-et kap (monoton, nincs ütközés)
2. A cache-ben a régi version-okhoz tartozó entry-k **árván maradnak**
3. Az árva entry-k **automatikusan kiöregednek** a `MaxCacheSize` (default: 10 000) elérésekor — LRU eviction
4. A `TryGet(currentVersion, partId)` **csak** a current version kulcsával keres — régi entry-k láthatatlanok

**Indok:** a hagyományos "invalidate all entries for skeleton X" művelet egy multi-thread olvasási környezetben race condition-t okozhat (egy olvasó még a régi entry-t kapja, miközben az invalidator dolgozik). A version-bump megoldás **lockless korrekt**, mert a kulcs eleve nem matchel.

**Korlát:** ha egy `Skeleton` 10 000+ mutáción megy át egyetlen process futás alatt, az árva entry-k nyomják a memóriát az LRU-eviction-ig. Ez gyakorlati korlát: egy normál CAD-session 100-300 mutáción megy át, biztonsági margón belül.

### 4.6 Advisory infrastruktúra

Lásd 4.4.4 — `DesignAdvisory` + `AdvisorySeverity`. Ezeket a `ConstructionRuleEngine` aggregálja és visszaadja. A `Skeleton`-on egy `ValidateDesign()` method elérhető, ami a teljes Advisory-listát visszaadja:

```csharp
public sealed class Skeleton
{
    // ...
    public IReadOnlyList<DesignAdvisory> ValidateDesign(IConstructionContext context);
}
```

**A11 garancia:** a `Skeleton`-nak van `Validate()`-je, de **nincs olyan mutáció, ami `Critical` advisory miatt failelne**. A user-é a döntés.

### 4.7 TenantStandard port (csak interface, A9 részben)

```csharp
namespace SpaceOS.Cabinet.Abstractions;

public interface ITenantStandardProvider
{
    Guid TenantId { get; }

    // Anyag-default
    string DefaultCarcassMaterial { get; }    // pl. "lamiboard-18mm-white"
    double DefaultCarcassThickness { get; }   // 18.0
    string DefaultBackPanelMaterial { get; }   // "hdf-5mm"
    double DefaultBackPanelThickness { get; }  // 5.0

    // Konstrukciós konvenciók
    BackPanelAttachmentDefault BackPanelAttachment { get; }   // {Stumpf, Rabbet, Groove}
    TopType TopType { get; }                                   // {FullTop, CrossRailPair}

    // 32mm raster ki/be
    bool LineBoreEnabled { get; }
    double LineBoreFirstHoleOffset { get; }   // 38.0
    double LineBoreSpacing { get; }            // 32.0
    double LineBoreDiameter { get; }           // 5.0

    // Konstrukciós szabály-küszöbök
    double TallCabinetHeightThreshold { get; }    // 2000.0
    double LongShelfThreshold { get; }            // 800.0

    // ConstructionRule felülbírálások (rule-id → severity override)
    IReadOnlyDictionary<string, AdvisorySeverity> RuleSeverityOverrides { get; }
}

public enum BackPanelAttachmentDefault { Stumpf, Rabbet, Groove }
public enum TopType { FullTop, CrossRailPair }
```

A `Cabinet 0.1`-ben ezt **csak interface-ként** szállítjuk. Az `cabinetbilder-autocad` adapter implementál egy `InMemoryTenantStandardProvider`-t, ami statikus default-okkal indul (Doorstar default-set). A teljes `TenantStandard` aggregate (per-tenant tárolás, override-lánc, mutation) **Cabinet 0.2** scope.

---

## 5. Public API surface

A NuGet csomag publikus felülete. Ezek azok a típusok, amelyekre az adapter-rétegek (cabinetbilder-autocad, jövőbeli web/mobil) hivatkoznak.

### 5.1 Aggregate-belépési pontok

```csharp
// Új Skeleton létrehozása alapértelmezett BaseCuboid-dal
Skeleton skeleton = Skeleton.Create(
    tenantId: tenantId,
    dimension: AssemblyDimension.FromMillimeters(800, 720, 580),
    tenantStandard: standardProvider);

// Skeleton átméretezése — A10 szelektív invalidation
Result resizeResult = skeleton.ResizeAssembly(
    AssemblyDimension.FromMillimeters(900, 720, 580));

// Konstrukciós szabályok alkalmazása
EngineResult engineResult = constructionEngine.ApplyAll(skeleton, context);

// Advisory-k lekérdezése (sosem blokkol — A11)
IReadOnlyList<DesignAdvisory> advisories = skeleton.ValidateDesign(context);

// Szemantikus szerepek
IReadOnlyDictionary<Guid, PartRole> roles = inferenceService.InferAll(skeleton);
```

### 5.2 Read-model nézet

A `Skeleton`-on van egy snapshot-szerű read-model bővítés, amit az adapter-rétegek könnyen szerializálhatnak:

```csharp
public sealed record SkeletonSnapshot(
    Guid Id,
    Guid TenantId,
    Guid Version,
    AssemblyDimension Dimension,
    IReadOnlyList<PartSnapshot> Parts,
    IReadOnlyList<ConnectionSnapshot> Connections,
    IReadOnlyList<MachiningFeatureSnapshot> Machinings);

// Skeleton.ToSnapshot() — explicit DTO-ra konverzió szerializációhoz
```

**Tervezési döntés:** a `Skeleton.ToSnapshot()` az **egyetlen szerializációs kapu**. Sose `JsonSerializer.Serialize(skeleton)` — az aggregate private setter-eket lát, és a JSON üres `{}`-t adna (Kernel `BuildBackend` golden rule mintát követjük).

### 5.3 Domain events (v2 — DB-CAB-7: FIFO garancia)

```csharp
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
    long SequenceNumber { get; }    // ← v2: explicit FIFO ordering
}

public sealed record SkeletonCreated(Guid SkeletonId, Guid TenantId, DateTime OccurredAt, long SequenceNumber) : IDomainEvent;
public sealed record SkeletonResized(Guid SkeletonId, AssemblyDimension OldDim, AssemblyDimension NewDim, DateTime OccurredAt, long SequenceNumber) : IDomainEvent;
public sealed record PartAdded(Guid SkeletonId, Guid PartId, DateTime OccurredAt, long SequenceNumber) : IDomainEvent;
public sealed record PartRemoved(Guid SkeletonId, Guid PartId, DateTime OccurredAt, long SequenceNumber) : IDomainEvent;
public sealed record ConnectionAdded(Guid SkeletonId, Guid ConnectionId, DateTime OccurredAt, long SequenceNumber) : IDomainEvent;
public sealed record MachiningFeatureAdded(Guid SkeletonId, MachiningSubject Subject, Guid FeatureId, DateTime OccurredAt, long SequenceNumber) : IDomainEvent;
```

**FIFO garancia (DB-CAB-7):**
- Az aggregate-en belüli minden új event egy **monoton növekvő `SequenceNumber`**-t kap (per-aggregate, induló érték `0`)
- A `PopDomainEvents()` **`IReadOnlyList<IDomainEvent>`-et ad vissza, garantáltan SequenceNumber szerinti rendezésben**
- Az adapter-réteg (cabinetbilder-autocad outbox-ja, jövőbeli web-server BFF-je) a SequenceNumber-t használhatja replay-oláshoz

**Indok:** a `DateTime.UtcNow` **nem garantáltan monoton** — ugyanazon tick-ben két event ugyanazt a timestamp-et kaphatja, és a sorrend nem deterministic. A `SequenceNumber` aggregate-szintű, kötelezően incremental, és garantálja, hogy az event-feldolgozó **ugyanazt a sorrendet** látja, mint az aggregate-mutáció történt.

Az adapter-rétegek `PopDomainEvents()`-szel kiveszik az event-eket és továbbítják (pl. AutoCAD adapter audit-log-ba, vagy a SpaceOs Bridge outbox-ba).

---

## 6. Persistence contract (DDL helyett)

A Cabinet 0.1 `Modules.Cabinet` **nem perzisztál**. De van egy szigorú szerződés a szerializációra.

### 6.1 SkeletonSnapshot szerializációs stabilitás (v2 frissítés — DB-CAB-2)

A `SkeletonSnapshot` rekord-struktúrája **schema-versioned SemVer-string-gel**, nem int-tel:

```csharp
public sealed record SkeletonSnapshot
{
    /// <summary>
    /// Schema version in SemVer "major.minor" format.
    /// Cabinet 0.1.x release-ek: "0.1"
    /// Cabinet 0.2.x release-ek: "0.2"
    /// Patch verziók (0.1.5) NEM emelik — csak minor breaking change.
    /// </summary>
    public string SchemaVersion { get; init; } = "0.1";
    public Guid Id { get; init; }
    // ... többi mező
}
```

**Indok (DB-CAB-2):** az int-alapú versioning (`1`, `2`, `3`) nem skálázódik. Ha Cabinet 0.1.7-ben breaking change van, ami nem indokolja a 0.2 minor-bump-ot, az int 2 lenne — de az ütközik a 0.2 jövőbeli release-szel. A SemVer-string viszont egyértelmű: a `"0.1"` minden 0.1.x release-re érvényes, és kompatibilis a NuGet csomag-verzióval.

**Validation rule:** a `SchemaVersion` regex-szel ellenőrzött: `^\d+\.\d+$`. Pre-release suffix (alpha, rc) **nem** kerül ide — az a NuGet csomag-verzió része, nem a snapshot-séma.

### 6.2 Adapter-szerződések

| Adapter | Mit szerializál | Mibe |
|---|---|---|
| `cabinetbilder-autocad` | `SkeletonSnapshot` | DWG XRecord (JSON encoded) |
| (jövő) `cabinet-web` | `SkeletonSnapshot` | PostgreSQL JSONB column |
| (jövő) `cabinet-mobile` | `SkeletonSnapshot` | SQLite JSON column |

Mindegyik **read-back kompatibilis** kell legyen — a Cabinet 0.1 typed reader képes Cabinet 0.1.x snapshot-okat reading-elni.

### 6.3 Szerializációs guard-ok

```csharp
public sealed class SkeletonSnapshot
{
    /// <summary>
    /// Helper: kötelezően használandó szerializációhoz.
    /// Mindig a CabinetJsonOptions.Strict-et használja (DB-CAB-8).
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null);

    /// <summary>
    /// Helper: kötelezően használandó deszerializációhoz, version-check-kel ÉS invariáns-validáció.
    /// SEC-CAB-6: a deszerializált snapshot-on automatikusan futtatja a domain-invariánsokat.
    /// </summary>
    public static Result<SkeletonSnapshot> FromJson(string json, JsonSerializerOptions? options = null);
}

public static class SkeletonReconstruction
{
    /// <summary>
    /// SkeletonSnapshot → Skeleton aggregate-tel rekonstrukció + invariáns-validáció.
    /// Ez a method a Skeleton-aggregate-on EGYEDÜLI módja a deserializált állapot újraépítésének.
    /// </summary>
    public static Result<Skeleton> FromSnapshot(SkeletonSnapshot snapshot);
}
```

**Invariáns:** `FromJson` viselkedése `SchemaVersion` szerint:
- `"0.1"` → sikeres parse (Cabinet 0.1.x natív formátum)
- `"0.0"` vagy bármi nem érvényes formátum → `Result.Error("Invalid schema version: ...")`
- `"0.2"`, `"0.3"`, ... (jövőbeli, magasabb minor) → `Result.Error("Schema version 0.2 not supported by Cabinet 0.1.x reader. Upgrade required.")`
- `"1.0"`, ... (jövőbeli, magasabb major) → `Result.Error("Schema version 1.0 incompatible with Cabinet 0.1.x reader.")`

Soha nem feltételez. Soha nem próbál "best effort" parse-olni.

**SEC-CAB-6 (HIGH): Post-deserialize invariant validation**

Egy ártatlan-nak tűnő JSON (helyes SchemaVersion = "0.1") tartalmazhat **rosszindulatú belső adatot**: extrém Part-szám (10 000 db), NaN affine mátrix, cross-tenant Part-ok, ciklikus Connection-DAG, stb. A típus-szintű parse (`JsonSerializer.Deserialize<SkeletonSnapshot>`) ezt **nem** szűri ki.

**Védelem:**
- A `SkeletonSnapshot.FromJson()` után **kötelező** futtatni a `SkeletonReconstruction.FromSnapshot()`-ot
- A `FromSnapshot()` minden invariánst lefuttat:
  - `SkeletonSnapshot.SchemaVersion` formátum-validáció (DB-CAB-2)
  - `Parts.Count <= MaxPartsPerSkeleton` (SEC-CAB-5)
  - `Connections.Count <= MaxConnectionsPerSkeleton` (SEC-CAB-5)
  - Minden `Part.SkeletonId == this.Id` (SEC-CAB-1)
  - Minden `Part.Frame.LocalToAssembly.IsValid()` (SEC-CAB-2)
  - Minden `PartDimension`/`AssemblyDimension` `MaxXxx` keretben (SEC-CAB-3)
  - Minden `Connection.ParentPartId` és `ChildPartId` érvényes Part
  - Connection-DAG nem ciklikus (SEC-CAB-7)
  - `LastSequenceNumber` konzisztens a domain events SequenceNumber-jeivel (SEC-CAB-10)
- Bármelyik invariáns sérül → `Result.Error("Invariant violated: ...")` és a Skeleton **nem** rekonstruálódik

A `FromJson()` és `FromSnapshot()` **explicit külön lépések**, hogy a fogyasztó tudja, mikor "csak parse-ol" és mikor "validál is". A kettő együtthasználata a csak-helyes minta:

```csharp
var jsonResult = SkeletonSnapshot.FromJson(jsonString);
if (!jsonResult.IsSuccess) return jsonResult.ToResult();

var skeletonResult = SkeletonReconstruction.FromSnapshot(jsonResult.Value);
if (!skeletonResult.IsSuccess) return skeletonResult.ToResult();

var skeleton = skeletonResult.Value;  // ← csak ITT biztos, hogy minden invariáns ok
```

### 6.4 Migration policy (v2 — DB-CAB-3)

A Cabinet 0.x sorozatban a **schema-version migration** kötelezően dokumentált. A Cabinet 0.2-ben jönnie kell egy `ISnapshotMigrator` portnak:

```csharp
namespace SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Snapshot migration port — Cabinet 0.2-ben implementáció.
/// Cabinet 0.1-ben CSAK az interface kerül kibocsátásra (forward-compat).
/// </summary>
public interface ISnapshotMigrator
{
    /// <summary>
    /// True ha a forrás-verzióból tud upgrade-elni a cél-verzióra.
    /// </summary>
    bool CanMigrate(string fromVersion, string toVersion);

    /// <summary>
    /// Egy korábbi snapshot JSON-t felhúz a jelenlegi verzióra.
    /// Result.Error ha nem támogatott útvonal.
    /// </summary>
    Result<string> Migrate(string snapshotJson, string targetVersion);
}
```

**Migration-szabályok:**
- **Forward-only**: Cabinet 0.2 tud `0.1 → 0.2` upgrade-et. **Nem tud** `0.2 → 0.1` downgrade-et.
- **Single-step**: minden migration egy minor lépés. `0.1 → 0.3` upgrade kétlépéses (`0.1 → 0.2 → 0.3`), nem direkt.
- **Lossless**: a migration nem dobhat el adatot. Ha az új minor verzió nem tud reprezentálni egy régi mezőt, a migration **fail-elnie** kell, nem csendben elnyelni.
- **Test gate** Cabinet 0.2-ben: minden 0.1.x snapshot-formátum-variáns (legalább 5 reprezentatív minta) sikeresen upgrade-elődik.

**Cabinet 0.1 felelőssége:**
- Az `ISnapshotMigrator` interface kibocsátása az `Abstractions` csomagban (nincs implementáció)
- A `SkeletonSnapshot.SchemaVersion` mező létezése — a jövőbeli migrator ezen tud diszpacselni
- Egy "reference snapshot" minta kibocsátása a Cabinet 0.1 test suite-ben (`docs/sample-snapshots/0.1.json`), amit Cabinet 0.2 migration-test-je használhat

### 6.5 Csomag-verzió ↔ Schema-verzió kapcsolat

| NuGet csomag-verzió | Snapshot SchemaVersion | Megjegyzés |
|---|---|---|
| `SpaceOS.Cabinet.Domain 0.1.0` | `"0.1"` | Initial release |
| `SpaceOS.Cabinet.Domain 0.1.1` | `"0.1"` | Patch — bug fix, nem breaking change |
| `SpaceOS.Cabinet.Domain 0.1.5` | `"0.1"` | Patch — új optional mező OK, ha `default value` |
| `SpaceOS.Cabinet.Domain 0.2.0` | `"0.2"` | Minor — breaking change → új schema-version |
| `SpaceOS.Cabinet.Domain 0.2.1` | `"0.2"` | Patch a 0.2 sorozaton |
| `SpaceOS.Cabinet.Domain 1.0.0` | `"1.0"` | Major — fundamentális breaking change |

**A csomag-verzió többet mond, mint a schema-version:** a 0.1.0 → 0.1.5 patch-sorozatban is történhet **public API kiegészítés** (új method, új optional property), ami **nem töri** a fogyasztót. A schema-version csak akkor változik, ha a JSON-séma kompatibilitása megtörik.

---

## 7. Algoritmusok

### 7.1 DependencyGraph + szelektív újraszámítás (A10)

A `Skeleton.ResizeAssembly(newDim)` után **csak az érintett Part-okat** és Machining-eket számítjuk újra. Ez egy `DependencyGraph` Kahn-féle topo-rendezésén alapul.

```csharp
// Belső, nem public:
internal sealed class DependencyGraph
{
    // Csomópont = (Part vagy Connection vagy Machining)
    // Él = "X függ Y-tól, ha Y változik, X-et újraszámolni kell"

    /// <summary>
    /// Kahn topo-sort. Result.Error ha cikluss-detection (SEC-CAB-7).
    /// </summary>
    public Result<IReadOnlyList<Guid>> AffectedBy(AssemblyDimensionChange change);
}

public enum AssemblyDimensionChange { WidthChanged, HeightChanged, DepthChanged, Multiple }
```

**SEC-CAB-7 (MEDIUM): Cycle detection a Kahn topo-sort-ban**

A klasszikus Kahn-algoritmus feltételezi, hogy a függőségek DAG-ot alkotnak. Egy malicious rule-set vagy snapshot ciklust hozhat létre (A függ B-től, B függ A-tól) → infinite loop a topo-sortban.

**Védelem:**
- Kahn-algoritmus akkor termiszik le, amikor a "no incoming edges" csomópont-halmaz üres
- Ha a feldolgozott csomópont-szám **kisebb**, mint a teljes csomópont-szám → **cycle detected**
- Ekkor `Result.Error("Cycle detected in dependency graph: ...")` és a `ResizeAssembly` `Result.Error`-t ad vissza
- Az adapter-réteg ezt a hibát egy `Critical` Advisory-ként mutatja a usernek (A11 nem sérül, mert a műveletet vissza-bukás-szal oldjuk meg, nem az aggregate-ben hagyott invalid állapottal)

**Példa — fogyasztó kódja:**

```csharp
var resizeResult = skeleton.ResizeAssembly(newDim);
if (!resizeResult.IsSuccess)
{
    // SEC-CAB-7: lehet cycle detection error
    advisories.Add(DesignAdvisory.Critical(
        "Engine",
        "Skeleton",
        $"Cannot resize: {resizeResult.Errors.First()}",
        "Inspect Connection topology"));
}
```

**Példa — `Width` növelése esetén invalidálódó halmaz:**

| Part / Connection | Invalidálódik? | Indok |
|---|---|---|
| BaseCuboid.LeftSide | Nem | Width-független |
| BaseCuboid.RightSide | Igen — datum.X | Új pozíció |
| BaseCuboid.Bottom | Igen — Length | Hossz nő |
| BaseCuboid.Top | Igen — Length | Hossz nő |
| BaseCuboid.BackPanel | Igen — Width | Hossz nő |
| Belső Shelf-ek | Igen — Length | Hossz nő |
| LineBore furatok a Side-okon | Nem | Z-koordináta változatlan |
| Hátfal-Connection-ön a Groove | Igen — Geometry | Új pozíció |

A `DependencyGraph` ezt a topológiát képezi le **deklarált függőségekből**, amiket a szabályok (`IConstructionRule`) explicit megadnak.

### 7.2 Szemantikus inferencia algoritmus (A7)

```
function InferRole(part, skeleton):
    n = part.Frame.NormalInAssembly()
    g = GravityVector.Default
    datum = part.Frame.DatumInAssembly()
    dim = skeleton.Dimension

    if abs(dot(n, g)) < epsilon:           # vertikális
        if abs(datum.X) < epsilon:
            if no part exists at smaller X: return LeftSide
            else: return VerticalDivider
        elif abs(datum.X - dim.Width) < epsilon: return RightSide
        elif abs(datum.Y - dim.Depth) < epsilon: return BackPanel
        elif abs(datum.Y) < epsilon: return Front
        else: return VerticalDivider

    elif abs(dot(n, g) - 1) < epsilon:     # horizontális (felfelé)
        if abs(datum.Z) < epsilon: return Bottom
        elif abs(datum.Z - (dim.Height - part.Thickness)) < epsilon: return Top
        else:
            # A12: Shelf vs CrossRail — user-bírált a katalógus default mellett
            if part.AssignedRole != null: return part.AssignedRole
            else: return Shelf  // default

    return Unknown
```

#### Algoritmikus komplexitás (v4 — BE-CAB-1)

A `SemanticInferenceService.InferAll(skeleton)` a legrosszabb esetben **O(N²)** komplexitású — minden Part-ra `InferRole`-t hív, ami `O(N)` topológiai vizsgálatokat tesz ("nincs Part balra/jobbra").

**Becsült futási idő (modern CPU):**

| Part-szám | Művelet | Idő |
|---|---|---|
| 50 (átlagos szekrény) | 2 500 | < 5 ms |
| 100 (gardrób) | 10 000 | ~10 ms |
| 500 (max — SEC-CAB-5) | 250 000 | ~50–100 ms |

**Cabinet 0.1 döntés (BE-CAB-1):** **az O(N²) elfogadott, NEM optimalizáljuk**. Indok:

1. A `MaxPartsPerSkeleton = 500` (SEC-CAB-5) felső korlát biztosítja, hogy a futási idő **mindig 100ms alatt** marad
2. Az `InferAll` cache-elt — ugyanazon `Skeleton.Version`-re egyszer fut (DB-CAB-6)
3. Egy átlagos szekrény 50 Part-os, ahol az algoritmus < 5ms

**Optimalizációs lehetőség Cabinet 0.2-re (parking finding):**

Spatial indexing (X-koordináta szerinti rendezett lista) → **O(N log N)** lehetséges. De **ne implementáljuk preventív módon** — csak ha a Cabinet 0.2 méréseiben kimutatható a perf-igény (premature optimization elv).

### 7.3 ConstructionRuleEngine.ApplyAll (A11)

```
function ApplyAll(skeleton, context):
    allMachinings = []
    allAdvisories = []

    foreach rule in rules:
        try:
            result = rule.Apply(skeleton, context)
            allMachinings.AddRange(result.GeneratedMachinings)
            allAdvisories.AddRange(result.Advisories)
        catch ex:
            # A11: a rule sosem fail-eli az engine-t
            allAdvisories.Add(new DesignAdvisory(
                rule.RuleId,
                Severity.Critical,
                "engine",
                $"Rule '{rule.RuleId}' threw: {ex.Message}",
                "Disable this rule or fix it"))

    return new EngineResult(allMachinings, allAdvisories)
```

---

## 8. Validáció és invariánsok

### 8.1 VO szintű validáció

| VO | Invariáns | Implementáció |
|---|---|---|
| `PartDimension` | minden tag > 0 | `Result<PartDimension> Create(...)` factory-ban |
| `AssemblyDimension` | minden tag > 0 | factory-ban |
| `AffineTransform` | invertálható (det ≠ 0) | `Inverse()` `Result`-ot ad |
| `JointType` | enum-érték érvényes | C# enum natívan |
| `Vector3` | nincs invariáns (lehet (0,0,0)) | — |

### 8.2 Aggregate-szintű invariánsok (Skeleton)

| Invariáns | Mikor ellenőrizve |
|---|---|
| BaseCuboid mindig 4 vagy 5 Part (BackPanel opcionális) | minden mutáció után |
| `Connection.ParentPartId` és `ChildPartId` érvényes Part-ID | `AddConnection` |
| Egy Part nem hivatkozhat saját magára Connection-ben | `AddConnection` |
| `Skeleton.Version` monoton nő | `PopDomainEvents` után |
| `MachiningFeature.Subject` érvényes Part vagy Connection | `AddMachining` |

### 8.3 Mit NEM ellenőrzünk Cabinet 0.1-ben

- **Geometriai ütközés** Part-ok között → Cabinet 0.x későbbi (BVH-on át)
- **Anyag-katalógus referenciák** valódisága → adapter-réteg dolga
- **Hardware-referenciák** valódisága → adapter-réteg dolga (Catalog Cabinet 0.2-ben)
- **CNC-megvalósíthatóság** (pl. szerszám-átmérő, gép-méretek) → CAM-pipeline (külön, később)

---

## 9. Definition of Done

### 9.1 Migration gates (repo-szintű)

- [ ] `spaceos-modules-cabinet` repo létrehozva, GitHub-on private
- [ ] Skeleton solution: `SpaceOS.Cabinet.sln` 7 NuGet projekttel
- [ ] CLAUDE.md kitöltve a repo gyökerében
- [ ] **`global.json` fájl a repo gyökerében (DB-CAB-1):** `version: "10.0.203"`, `rollForward: "latestFeature"`
- [ ] GitHub Actions CI/CD: build + test minden push-ra; pack + publish tag-re (`v*.*.*`)
- [ ] **VPS build-environment .NET 10 SDK telepítve** (`dotnet --list-sdks` mutatja a `10.0.x`-et a meglévő `8.0.x` mellett) ✓ (verifikálva 2026-04-25)
- [ ] **GitHub Actions runner image-en .NET 10 SDK** (`dotnet-version: 10.0.x` a workflow-ban)
- [ ] `nuget.config` Windows-on a GitHub Packages feed-re (cabinetbilder-autocad oldal)

### 9.2 Domain gates

- [ ] `SpaceOS.Cabinet.Geometry` csomag implementálva: `AffineTransform`, `Vector3`, `PartDimension`, `AssemblyDimension`, `PartFrame`, `AssemblyFrame`, `GravityVector`
- [ ] `SpaceOS.Cabinet.Domain` csomag: `Skeleton`, `BaseCuboid`, `Part`, `Connection`, `JointType`, `ConnectionGeometry`, domain events
- [ ] `SpaceOS.Cabinet.Machining` csomag: `MachiningFeature`, `MachiningSubject` (3 leszármazott), `MachiningOperation` enum, `MachiningParameters`, `HardwareReference`
- [ ] `SpaceOS.Cabinet.Construction` csomag: `IConstructionRule`, `ConstructionRuleEngine`, 10 default rule, `DesignAdvisory`, `AdvisorySeverity`
- [ ] `SpaceOS.Cabinet.Semantics` csomag: `SemanticInferenceService`, `PartRole`
- [ ] `SpaceOS.Cabinet.Abstractions` csomag: `ITenantStandardProvider`, `IGeometryProjector` (port), `IPartCatalog` (port)
- [ ] `SpaceOS.Cabinet` meta-package: dependency az összesre

### 9.3 API stabilitás gates

- [ ] Minden public method `Result<T>` vagy `Result` return-rel
- [ ] Nincs public setter aggregate-eken (Skeleton, Part, Connection)
- [ ] `Skeleton.ToSnapshot()` az egyetlen szerializációs kapu
- [ ] **`SkeletonSnapshot.SchemaVersion = "0.1"` (SemVer-string, DB-CAB-2)** minden szerializált payload-on
- [ ] `SkeletonSnapshot.FromJson()` ismeretlen verzióra `Result.Error` (regex `^\d+\.\d+$` validation, DB-CAB-2)
- [ ] **`ISnapshotMigrator` interface kibocsátva (DB-CAB-3)** az `Abstractions` csomagban (Cabinet 0.1-ben implementáció nincs)
- [ ] **Reference snapshot `docs/sample-snapshots/0.1.json` (DB-CAB-3)** committed — Cabinet 0.2 migration-test használja majd
- [ ] **Domain események `SequenceNumber` mezővel (DB-CAB-7)** — FIFO garancia
- [ ] `PopDomainEvents()` SequenceNumber szerint rendezetten ad vissza
- [ ] Public API XML doc komment minden exportált típuson

### 9.4 Construction Rule gates

- [ ] 10 default rule mindegyike implementálva és tesztelve (lásd 4.4.3 tábla)
- [ ] `R-Stiffener-Tall` és `R-Shelf-Sag` Advisory-t generál, NEM blokkol (A11)
- [ ] `ConstructionRuleEngine.ApplyAll` exception-mentes (rule exceptions Advisory-vé alakulnak)
- [ ] Rule registration DI-on át: `IServiceCollection.AddConstructionRule<TRule>()`

### 9.5 Geometry gates

- [ ] `AffineTransform` 4×4 double pontossággal
- [ ] `AffineTransform.Inverse()` invariáns: `A.Compose(A.Inverse()) ≈ Identity`
- [ ] `IsApproximatelyEqualTo` minden numerikus VO-n, `epsilon = 1e-9` default
- [ ] `PartFrame.GrainDirectionInAssembly()` és társai a basis-vektorokat helyesen extraktálják

### 9.6 Semantics gates

- [ ] `SemanticInferenceService.InferRole(part, skeleton)` implementálva mind a 8 PartRole-ra (A12 Shelf/CrossRail user-override-dal)
- [ ] **Cache `(SkeletonVersion, PartId)` kulcson, lockless ConcurrentDictionary alapú (DB-CAB-6)**
- [ ] **`InvalidateSkeleton(id)` NEM eager-delete** — a version-bump miatt árva entry-k LRU-eviction-nel törlődnek
- [ ] **`MaxCacheSize` konfigurálható, default 10 000 (DB-CAB-6)** — túlcsordulás esetén LRU eviction
- [ ] Inference algoritmusban: gravitáció + topológia + datum-pozíció — nem stored flag (A7)
- [ ] **Concurrent stress test (DB-CAB-6):** 8 párhuzamos szál olvasja a cache-t miközben mutáció zajlik — race condition nélkül

### 9.7 Test gates

- [ ] **Új tesztek minimum:** 230 db (Cabinet 0.1 új scope, +30 v2 review delta-tól)
  - Geometry: 50 (mátrix-műveletek, basis-vektor extraktálás)
  - Skeleton: 60 (aggregate mutations, invariánsok, BaseCuboid)
  - Machining: 25 (Feature creation, Subject típusok)
  - Construction: 40 (10 rule × ~4 forgatókönyv)
  - Semantics: 25 (8 PartRole × topológiai esetek + cache)
  - **Persistence: 15 (DB-CAB-2, -3, -8 hatások)** — schema-version validation, migration interface, JSON determinism
  - **Concurrency: 10 (DB-CAB-6, -7)** — cache stress, event ordering
  - **Sample snapshots: 5 (DB-CAB-10)** — kicsi, közepes, nagy, hátfal-nélküli, gérvágott szekrény mintái
- [ ] xUnit v3 alapú (a CabinetBilder MSTest-jét NEM örökli — itt új repo, modern test-stack)
- [ ] **Tesztek mind `net8.0`-on, mind `net10.0`-on lefutnak** (multi-target test projekt: `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`)
- [ ] CI workflow külön step-ben futtatja `dotnet test -f net8.0` és `dotnet test -f net10.0`
- [ ] **Cross-runtime determinism teszt (DB-CAB-8):** `SkeletonSnapshot.ToJson()` ugyanaz mindkét framework-en byte-pontosan
- [ ] **Smoke fogyasztás teszt (DB-CAB-4):** üres `net8.0` console app build-elhető a Modules.Cabinet csomag minden public API-jával
- [ ] 0 build warning
- [ ] 0 build error
- [ ] `dotnet list package --vulnerable` → 0 high/critical

### 9.8 NuGet gates

- [ ] Verzió `0.1.0-alpha.1` az első tag-eléskor
- [ ] Minden csomag kibocsát `.snupkg` (symbol package) is — debug-elhetőség
- [ ] Csomag-leírások (`<Description>`) kitöltve minden `.csproj`-ban
- [ ] LICENSE fájl: a SpaceOS standard (proprietary)
- [ ] README a NuGet listing-hez minden csomagban
- [ ] **Multi-target verifikáció:** `unzip -l SpaceOS.Cabinet.Domain.0.1.0.nupkg | grep lib/` mutatja **mind** a `lib/net8.0/` és `lib/net10.0/` mappákat (és a `Geometry`/`Abstractions` esetén `lib/netstandard2.1/`-t)
- [ ] **Smoke-fogyasztási teszt:** egy üres `net8.0` console app `dotnet add package SpaceOS.Cabinet.Domain` után le tudja fordítani a `Skeleton.Create(...)` hívást — ezzel verifikáljuk, hogy nincs `net10.0`-only API véletlen public felületen

### 9.9 Összesített

- [ ] cabinetbilder-autocad **NEM TÖRÉS** — a meglévő 99 teszt zöld (a Cabinet 0.1 NuGet-ekre még nem migrálódott a CabinetBilder, ez a Cabinet 0.1 vége utáni 1.5 napos integrációs feladat)
- [ ] `dotnet test` minden új tesztet zöldre futtatja **mindkét cél-frameworken** (`net8.0` és `net10.0`)
- [ ] `dotnet build -c Release` 0 warning **mindkét cél-frameworken**
- [ ] `dotnet pack -c Release` 7 NuGet csomagot legenerál, mindegyik mind a multi-target lib-mappákat tartalmazza
- [ ] Mindegyik csomag mérete < 500 KB (a multi-target sem növeli aránytalanul — elsősorban metaadat különbözik)
- [ ] GitHub Actions sikeres tag-publikálás teszt-elve egy `v0.1.0-alpha.0` tag-gel
- [ ] Smoke-fogyasztási teszt (lásd 9.8) `net8.0` környezetben sikeres

### 9.10 Security gates (v3 — DEPLOYMENT BLOCKERS)

Ezek a checkbox-ok **deployment-blokkolók**: amíg nem mind ✓, addig a Cabinet 0.1.0 nem release-elhető production-ra.

- [ ] **SEC-CAB-1:** `Part` ctor `internal`, factory csak `Skeleton.AddPart()`, `FromSnapshot()` cross-tenant Part-ot detektál
- [ ] **SEC-CAB-2:** `AffineTransform.IsValid()` és minden Vector3 művelet véges-érték védelemmel; `CabinetJsonOptions.Strict` `"NaN"` literal-t elutasít
- [ ] **SEC-CAB-3:** `PartDimension.Create()` és `AssemblyDimension.Create()` `MaxXxx` keretek (6m × 6m × 1.5m max) ellenőrzéssel
- [ ] **SEC-CAB-4:** `ConstructionRuleEngine.ApplyAll()` `CancellationToken` keretben, per-rule 5s + engine 30s timeout + max 1000 generated machinings/rule
- [ ] **SEC-CAB-5:** `Skeleton.MaxPartsPerSkeleton = 500`, `MaxConnectionsPerSkeleton = 2000`, `MaxMachiningsPerPart = 100` enforced
- [ ] **SEC-CAB-6:** `SkeletonReconstruction.FromSnapshot()` minden invariánst lefuttat (Parts.Count, Connections, AffineTransform.IsValid, dimensions)
- [ ] **SEC-CAB-7:** `DependencyGraph.AffectedBy()` Kahn termináció-detection (cycle → Result.Error)
- [ ] **SEC-CAB-8:** Engine null-Apply és exception-Apply Advisory.Critical-ként kezeli, rule disabled
- [ ] **SEC-CAB-9:** Default rule-ok Advisory.Message regex `[\$€£]\d` match-mentes (= nincs pénz-szám szivárgás)
- [ ] **SEC-CAB-10:** `SkeletonSnapshot.LastSequenceNumber` mező + `FromSnapshot()` konzisztencia-ellenőrzés
- [ ] **Security tesztek:** minimum 35 új teszt (10 finding × ~3-4 forgatókönyv)
- [ ] **Threat model dokumentum:** `docs/security/threat-model-cabinet-0.1.md` committed (STRIDE keret-rendszerrel)
- [ ] **`dotnet list package --vulnerable`** és **`dotnet list package --deprecated`** mindkettő üres a release tag-eléskor

---

## 10. Security adósság státusz

A v3 security review-ban minden eredeti SEC-CAB tétel lezárult vagy bővült. Az új adósság-státusz:

| ID | Tétel | v1 állapot | v3 állapot |
|---|---|---|---|
| SEC-CAB-1 | Cross-tenant Part isolation | ⚠️ TenantId mező létezik, de Part.SkeletonId mutability gyenge | ✅ `internal Part(...)` ctor, `FromSnapshot` validáció |
| SEC-CAB-2 | NaN/Infinity propagation | ⚠️ TBD | ✅ `IsValid()` minden VO-n + factory `Result<T>` |
| SEC-CAB-3 | Dimension overflow | ⚠️ TBD | ✅ `MaxXxx` konstansok + `Create()` validáció |
| SEC-CAB-4 | ConstructionRule DOS | ⚠️ TBD | ✅ Per-rule timeout 5s + engine 30s + max-output 1000 |
| SEC-CAB-5 | Inference DOS extrém Part-szám | ⚠️ TBD | ✅ `MaxPartsPerSkeleton = 500` |
| SEC-CAB-6 | Schema-version mismatch attack | ⚠️ Result-error path | ✅ `FromSnapshot()` minden invariánst validál |
| SEC-CAB-7 | DependencyGraph cycle | — | ✅ Kahn termináció-detection |
| SEC-CAB-8 | Rule registration / null-handling | — | ✅ Engine fog null-t és exception-t Advisory-vé alakítva |
| SEC-CAB-9 | Advisory message privacy | — | ✅ Sablonos Message-szabály + regex teszt |
| SEC-CAB-10 | Domain events sequence integrity | — | ✅ `LastSequenceNumber` mező a snapshot-on |

**Maradó security-adósság a Cabinet 0.1 után:** nincs blokkoló. A v3 alapos review minden ismert támadás-vektort lefedett.

---

## 11. Mi jön utána (roadmap)

| Lépés | Tartalom | Becsült effort |
|---|---|---|
| ~~v2 review~~ | ~~Persistence/serialization contract review~~ | ✅ 0.5 nap (DONE) |
| ~~v3 review~~ | ~~Security review (sub-senior-security)~~ | ✅ 0.5 nap (DONE) |
| ~~v4 review~~ | ~~Backend review (sub-senior-backend)~~ | ✅ 0.5 nap (DONE) |
| **v4 IMPLEMENTÁCIÓRA KÉSZ** ◀️ | Final document, Claude Code handoff | — JELENLEG ITT VAGYUNK |
| **Implementáció (Claude Code agent #1, VPS)** | A v4 dokumentum alapján | **~21.75 nap** |
| **CabinetBilder integráció** | Cabinet 0.1 NuGet → cabinetbilder-autocad migráció | ~1.5 nap |
| **Cabinet 0.2 design session** | Catalog + Assembly + FlowEpic-bővítés (A12–A16) | TBD |
| **Cabinet 0.2 implementáció** | A12–A16 axiómák | ~11.5 nap (becsült, review-delta nélkül) |

### 11.1 Cabinet 0.x deprecation lifecycle (BE-CAB-3)

A Cabinet 0.x release sorozatban a public API breaking change-ek kezelése:

| Verzió-bump | Megengedett változások | Deprecation kötelezettség |
|---|---|---|
| **Patch** (0.1.5) | Bugfix, **NINCS** public API változás | — |
| **Minor** (0.2.0) | Új feature, új public API. Régi API `[Obsolete]` jelölhető | Deprecated API él **legalább 1 minor verzión át**, utána eltávolítható |
| **Major** (1.0.0) | Breaking change megengedett | Deprecation lifecycle nem kötelező 1.0-ban |

**Példa:**

```csharp
// Cabinet 0.1.0
public Skeleton CreateSkeleton(Guid tenantId, AssemblyDimension dim);

// Cabinet 0.2.0 — új factory, régi deprecated
[Obsolete("Use Skeleton.Create(tenantId, dim, tenantStandardProvider) instead. Removed in Cabinet 0.3.")]
public Skeleton CreateSkeleton(Guid tenantId, AssemblyDimension dim);

public Skeleton CreateSkeleton(Guid tenantId, AssemblyDimension dim, ITenantStandardProvider standard);

// Cabinet 0.3.0 — régi eltávolítva
public Skeleton CreateSkeleton(Guid tenantId, AssemblyDimension dim, ITenantStandardProvider standard);
```

A részletes szabályrendszer Cabinet 0.1 release-szel együtt committed: `docs/deprecation-policy.md` (BE-CAB-8).

### 11.2 Parallelism: elhalasztva Cabinet 0.2-re (BE-CAB-5)

Felmerült, hogy a `ConstructionRuleEngine.ApplyAll` futtassa a rule-okat parallel. **Cabinet 0.1 döntés: NEM**. Indok:

- A rule-ok közötti függőségek (egy rule generálta MachiningFeature alapján egy másik dönt) Cabinet 0.1-ben **nincsenek explicit dokumentáltak**
- Egy paralellizált engine subtle race condition-okat hoz, amik nehéz reprodukálni
- A 30s engine-timeout (SEC-CAB-4) elég sok rule-ra (50 rule × 0.5s = 25s)

**Cabinet 0.2-ben:** ha bevezetünk explicit "no dependency" kontraktust a rule-ok közt, akkor megnyitható a paralellizmus. A v3 §12 nyitott kérdésként ezt **Cabinet 0.2 design-ra** delegáljuk.

---

## 12. Nyitott kérdések — MIND LEZÁRVA

A v4 review minden korábbi nyitott kérdést kezelt. A részletes lezárások:

| Kérdés | Lezárta | Hol |
|---|---|---|
| `Vector3` egyenlőség epsilon konfigurálható-e? | BE-CAB-7 | §4.1.2 — `GeometryConstants` osztály |
| `AffineTransform` asszociativitás-tesztek epsilon? | BE-CAB-7 | `GeometryConstants.DefaultEpsilon = 1e-9` |
| Skeleton thread-safety policy? | BE-CAB-2 | §4.2.1 — read lockless, write single-thread |
| Domain events sorrend backend-perf? | BE-CAB-6 | §4.2.1 — MaxUnflushedEvents cap |
| API breaking change policy / deprecation lifecycle? | BE-CAB-3, BE-CAB-8 | §11.1 + `docs/deprecation-policy.md` |
| `SemanticInferenceService.InferAll` algoritmikus optimalizáció? | BE-CAB-1 | §7.2 — O(N²) elfogadva, MaxParts 500 keret |
| `ConstructionRuleEngine` paralellizmus? | BE-CAB-5 | §11.2 — Cabinet 0.2-re elhalasztva |
| `MaxPartsPerSkeleton = 500` tenant-szintű override? | BE-CAB-1 | TenantStandard-ben Cabinet 0.2-ben |
| ~~SkeletonSnapshot.SchemaVersion formátum~~ | DB-CAB-2 | §6.1 |
| ~~Multi-tenant szivárgás~~ | SEC-CAB-1 | §4.2.3 |
| ~~ConstructionRule infinite loop~~ | SEC-CAB-4 | §4.4.2 |
| ~~Inference cache memória-felhasználás~~ | SEC-CAB-5 | §4.2.1 |

A Cabinet 0.1 implementációhoz nincs nyitott architektúra-kérdés.

---

## 13. v2 review finding tábla (database-designer + database-schema-designer)

A v2 review során 10 finding született. Mindegyik **be van építve** a v2 dokumentumba — a finding-ID-k a megfelelő szakaszokon belül vannak hivatkozva.

### 13.1 Cumulative summary

| Severity | Count | Effort delta |
|---|---|---|
| 🔴 CRITICAL | 0 | — |
| 🟠 HIGH | 4 | +1.5 nap |
| 🟡 MEDIUM | 4 | +1.5 nap |
| 🟢 LOW | 2 | +0.75 nap |
| **Összesen** | **10** | **+3.75 nap** |

### 13.2 Egyedi finding-ek

| ID | Súly | Terület | Probléma | v2 javítás | Hol |
|---|---|---|---|---|---|
| **DB-CAB-1** | 🟠 HIGH | Build reproducibility | A `spaceos-modules-cabinet` repo-ban nincs `global.json`; a fejlesztőgép, CI és VPS különböző SDK-verziókkal buildelhet, ami nem-determinisztikus build-eredményt ad. | `global.json` SDK pinneléssel (`10.0.203` + `latestFeature`) | §3.4 |
| **DB-CAB-2** | 🟠 HIGH | Schema versioning | A v1-ben `SchemaVersion` int (1, 2, 3...) — nem skálázódik, ütközik a NuGet csomag-verzióval, nehéz kommunikálni. | SemVer-string (`"0.1"`, `"0.2"`) regex-validált formátum | §6.1 |
| **DB-CAB-3** | 🟠 HIGH | Migration policy | A v1 csak megemlíti, hogy "Cabinet 0.2-ben jön migráció", de nincs explicit interface vagy szabály-rendszer. | `ISnapshotMigrator` interface kibocsátva Cabinet 0.1-ben (csak interface), forward-only/single-step/lossless szabályok dokumentáltan | §6.4 |
| **DB-CAB-4** | 🟠 HIGH | Multi-target API stability | Lehetséges, hogy `net8.0` és `net10.0` build-ek **különböző public API-t** adjanak (pl. `#if NET10_OR_GREATER` direktíva). | Tilos `#if` a public felületen, smoke-teszt egy `net8.0` console app-pal a CI-ben, identikus XML doc fájl mindkét lib-mappában | §3.4 |
| **DB-CAB-5** | 🟡 MEDIUM | Naming convention | A NuGet csomag-naming pattern nincs explicit dokumentálva. | `SpaceOS.Cabinet.{ComponentName}[.{Sub}]` minta, namespace = package név | §3.1 |
| **DB-CAB-6** | 🟡 MEDIUM | Cache thread-safety | `SemanticInferenceCache.InvalidateSkeleton(id)` race condition forrás multi-thread olvasási környezetben. | Lockless `ConcurrentDictionary` + version-bump (régi entry-k láthatatlanok) + LRU eviction | §4.5.2 |
| **DB-CAB-7** | 🟡 MEDIUM | Event ordering | `IDomainEvent` interface-en csak `OccurredAt: DateTime` van, ami **nem garantáltan monoton**. | Új `SequenceNumber: long` mező az `IDomainEvent`-en, aggregate-szintű monoton incremental | §5.3 |
| **DB-CAB-8** | 🟡 MEDIUM | Snapshot determinism | `SkeletonSnapshot.ToJson()` `net8.0`-on és `net10.0`-on potenciálisan különböző byte-output-ot adhat. | Saját `CabinetJsonOptions.Strict` minden tulajdonsággal explicit, cross-runtime determinism teszt a CI-ben | §3.4 |
| **DB-CAB-9** | 🟢 LOW | Documentation | XML doc nincs explicit `<example>` runnable kód követelmény. | Minden public type-on legalább 1 `<example>` blokk runnable kóddal | §9.3 |
| **DB-CAB-10** | 🟢 LOW | Sample data | Hiányzik egy "katalógus" reprezentatív mintákkal a Cabinet 0.2 migration-test-hez. | `docs/sample-snapshots/` — 5 reprezentatív minta committed | §6.4, §9.7 |

### 13.3 Effort breakdown

| Finding | Effort |
|---|---|
| DB-CAB-1 (global.json) | 0.25 nap |
| DB-CAB-2 (SemVer-string) | 0.5 nap |
| DB-CAB-3 (ISnapshotMigrator) | 0.5 nap |
| DB-CAB-4 (API stability smoke-teszt) | 0.25 nap |
| DB-CAB-5 (naming convention dokumentáció) | 0.25 nap |
| DB-CAB-6 (cache thread-safety + stress teszt) | 0.5 nap |
| DB-CAB-7 (SequenceNumber + FIFO teszt) | 0.25 nap |
| DB-CAB-8 (CabinetJsonOptions + determinism teszt) | 0.5 nap |
| DB-CAB-9 (XML example blokkok) | 0.25 nap |
| DB-CAB-10 (sample snapshots) | 0.5 nap |
| **Összesen** | **3.75 nap** |

---

## 14. v3 review finding tábla (senior-security)

A v3 security review során 10 finding született, **STRIDE keret-rendszerben** azonosítva (Spoofing, Tampering, Repudiation, Information disclosure, Denial of service, Elevation of privilege). Mindegyik **be van építve** a v3 dokumentumba — a finding-ID-k a megfelelő szakaszokon belül vannak hivatkozva.

### 14.1 Cumulative summary

| Severity | Count | Effort delta |
|---|---|---|
| 🔴 CRITICAL | 1 | +0.5 nap |
| 🟠 HIGH | 5 | +1.75 nap |
| 🟡 MEDIUM | 3 | +0.75 nap |
| 🟢 LOW | 1 | +0.5 nap |
| **Összesen** | **10** | **+3.5 nap** |

### 14.2 STRIDE-mapping

| STRIDE kategória | Érintett finding-ek | Lefedés |
|---|---|---|
| **S — Spoofing** | SEC-CAB-1 | Cross-tenant Part isolation |
| **T — Tampering** | SEC-CAB-2, SEC-CAB-6 | NaN/Infinity védelem; post-deserialize validáció |
| **R — Repudiation** | SEC-CAB-10 | Domain events sequence integrity (DB-CAB-7-re épít) |
| **I — Information disclosure** | SEC-CAB-9 | Advisory message privacy |
| **D — Denial of service** | SEC-CAB-3, SEC-CAB-4, SEC-CAB-5, SEC-CAB-7 | Dimension overflow, rule engine timeout, parts cap, cycle detection |
| **E — Elevation of privilege** | SEC-CAB-8 | Rule registration null-handling |

A class-library kontextusban az "Elevation of privilege" kategória korlátozott jelentőségű (nincs auth/permission system) — itt a "rendszer-szintű elromlás" pótolja (egy rosszul-viselkedő rule megakaszt mindenkit).

### 14.3 Egyedi finding-ek

| ID | Súly | STRIDE | Terület | Probléma | v3 javítás | Hol |
|---|---|---|---|---|---|---|
| **SEC-CAB-1** | 🟠 HIGH | S | Cross-tenant isolation | A `Part` ctor public, `Part.SkeletonId` mezőt kívülről be lehet állítani — egy Part létrehozható egy másik tenant skeleton-ID-jével, és valahogy beillesztve cross-tenant adatszivárgást okozhat. | `internal Part(...)` ctor (csak `SpaceOS.Cabinet.Domain` assembly-n belül); `Skeleton.AddPart()` az egyetlen factory; `FromSnapshot()` validáció: minden `Part.SkeletonId == this.Id` | §4.2.3 |
| **SEC-CAB-2** | 🔴 **CRITICAL** | T | NaN/Infinity propagation | A `AffineTransform`-ban `double.NaN` vagy `±Infinity` érték (rosszindulatú JSON, hibás számítás) végigterjed a teljes pipeline-on, korruptált aggregate-állapotot generálva. Az invariáns-tesztek később detektálják, de a már létrehozott objektumok inkonzisztensek. | `AffineTransform.IsValid()` minden VO-n; minden factory method `Result<T>`-et ad vissza belső validációval; `CabinetJsonOptions.Strict` `JsonNumberHandling.Strict` (NaN literal elutasítva); `ApplyTo`/`ApplyToDirection`/`Inverse` is `Result<Vector3>`/`Result<AffineTransform>` | §4.1.1 |
| **SEC-CAB-3** | 🟠 HIGH | D | Dimension overflow | A `PartDimension` és `AssemblyDimension` `double` mezőket fogad el felső korlát nélkül. Egy 1e300 mm érték `+Infinity`-t generál a mátrix-számolásokban, ami NaN-okat hoz létre, ami megint korrupcióhoz vezet. | `MaxLength = 6000`, `MaxWidth = 3000/6000`, `MaxThickness = 100`, `MaxHeight = 6000`, `MaxDepth = 1500`, `MinDimension = 0.1/50`; `Create()` factory `Result.Error` ha kívül esik | §4.1.3 |
| **SEC-CAB-4** | 🟠 HIGH | D | ConstructionRule DOS | Egy malicious vagy hibás `IConstructionRule.Apply()` infinite loop-ba kerülhet, vagy óriási mennyiségű MachiningFeature-t generálhat memória-kimerülésig. | Per-rule `CancellationToken` 5s timeout; engine `ApplyAll` 30s timeout; max 1000 generated MachiningFeature/rule (cap), túllépés `Result.Error`; max 50 rule egyszerre | §4.4.2 |
| **SEC-CAB-5** | 🟠 HIGH | D | Inference DOS extrém Part-szám | `SemanticInferenceService.InferAll()` O(N²) komplexitású — 10 000 Part-os skeleton percekig számol, DOS. A `MaxPartsPerSkeleton` keret nélkül a védelem hiányzik. | `MaxPartsPerSkeleton = 500`, `MaxConnectionsPerSkeleton = 2000`, `MaxMachiningsPerPart = 100`; `Skeleton.AddPart()` `Result.Error` ha túllépi | §4.2.1 |
| **SEC-CAB-6** | 🟠 HIGH | T | Post-deserialize validation | A `SkeletonSnapshot.FromJson()` v2-ben már fail-el ismeretlen `SchemaVersion`-ra. DE: ha a JSON ártatlan-nak tűnik (helyes SchemaVersion), a belső adat lehet rosszindulatú (extrém Part-szám, NaN affine, cross-tenant Part-ok). A típus-szintű parse ezt nem szűri. | `SkeletonReconstruction.FromSnapshot()` mint kötelező lépés a `FromJson()` után; minden invariánst lefuttat: PartCount, ConnectionCount, AffineTransform.IsValid, dimensions, Part.SkeletonId, DAG-ciklus, LastSequenceNumber konzisztencia | §6.3 |
| **SEC-CAB-7** | 🟡 MEDIUM | D | DependencyGraph cycle | A `DependencyGraph` Kahn topo-sortja **feltételezi** DAG-ot. Egy malicious rule-set vagy snapshot ciklust hozhat létre → infinite loop a sorter-ben. | Kahn-termináció-detection: ha feldolgozott csomópont-szám < total → Result.Error("Cycle detected"). A `ResizeAssembly()` ezt error-csatornán továbbítja | §7.1 |
| **SEC-CAB-8** | 🟡 MEDIUM | E | Rule registration / null-handling | Egy `IConstructionRule.Apply()` `null`-t ad vissza vagy exception-t dob → engine megakad, az összes többi rule nem fut. | Engine fog null-t ÉS exception-t, mindkettőt `DesignAdvisory.Critical("Rule '{ruleId}' returned null/threw: ...")` formába alakít, és az adott rule-t **disabled** állapotba teszi a session-re — a többi rule fut tovább | §4.4.2 |
| **SEC-CAB-9** | 🟡 MEDIUM | I | Advisory message privacy | A `DesignAdvisory.Message` szabad-szöveges. Egy rule beleírhat számszerű érzékeny adatot a TenantStandard-ből (árazási küszöb, mennyiségi limit), ami snapshot-export-on át adatszivárgást okozhat. | Kötelező sablonos szöveg-minta a Message-ben — nincs számszerű adat. Test gate: regex `[\$€£]\d` match-mentes minden default rule output-jában | §4.4.4 |
| **SEC-CAB-10** | 🟢 LOW | R | Event sequence integrity | A `SkeletonSnapshot` deserialize-cláson át a `_currentSequenceNumber` mező manipulálható → sequence-collision a domain events-ben → repudiation lehetőség. | `SkeletonSnapshot.LastSequenceNumber: long` explicit mező; `FromSnapshot()` ellenőrzi: minden domain event SequenceNumber ≤ LastSequenceNumber, és nincs duplikátum | §4.2.1 |

### 14.4 Effort breakdown

| Finding | Súly | Effort |
|---|---|---|
| SEC-CAB-1 (Part isolation) | HIGH | 0.25 nap |
| SEC-CAB-2 (NaN/Infinity guard + tests) | **CRITICAL** | 0.5 nap |
| SEC-CAB-3 (dimension limits + tests) | HIGH | 0.25 nap |
| SEC-CAB-4 (rule timeout + max-output cap) | HIGH | 0.5 nap |
| SEC-CAB-5 (parts cap + tests) | HIGH | 0.25 nap |
| SEC-CAB-6 (FromSnapshot invariáns-validáció) | HIGH | 0.5 nap |
| SEC-CAB-7 (cycle detection) | MEDIUM | 0.25 nap |
| SEC-CAB-8 (null/exception handling) | MEDIUM | 0.25 nap |
| SEC-CAB-9 (Advisory privacy + regex teszt) | MEDIUM | 0.25 nap |
| SEC-CAB-10 (LastSequenceNumber + tests) | LOW | 0.5 nap |
| **Összesen** | | **3.5 nap** |

### 14.5 Threat model dokumentum

A Cabinet 0.1 release-szel együtt kötelezően committed:

```
docs/security/threat-model-cabinet-0.1.md
```

Ez a doc tartalmazza:
1. **Asset list** — Skeleton aggregate, Part-ok, Connection-ök, MachiningFeature-ek, ConstructionRule-ok, TenantStandard adat
2. **STRIDE-mátrix** — minden asset minden STRIDE-kategóriában megvizsgálva
3. **Mitigation-mapping** — minden azonosított threat → SEC-CAB-N finding hivatkozás
4. **Maradék kockázat** — mit nem fed le a Cabinet 0.1 (pl. side-channel attacks, supply-chain via NuGet)

A threat model nem statikus — Cabinet 0.2 design-ja előtt felülvizsgálandó.

---

## 15. v4 review finding tábla (senior-backend)

A v4 backend review során 8 finding született — a perspektíva: **algoritmikus komplexitás, thread safety, API stability, allocation patterns**. Mindegyik **be van építve** a v4 dokumentumba.

### 15.1 Cumulative summary

| Severity | Count | Effort delta |
|---|---|---|
| 🔴 CRITICAL | 0 | — |
| 🟠 HIGH | 3 | +1.0 nap |
| 🟡 MEDIUM | 4 | +1.25 nap |
| 🟢 LOW | 1 | +0.25 nap |
| **Összesen** | **8** | **+2.5 nap** |

### 15.2 Egyedi finding-ek

| ID | Súly | Terület | Probléma | v4 javítás | Hol |
|---|---|---|---|---|---|
| **BE-CAB-1** | 🟠 HIGH | Algoritmikus komplexitás | `SemanticInferenceService.InferAll(skeleton)` O(N²) komplexitású. 10 000 Part-os skeleton percekig számolódik (DOS-veszély). | Nem optimalizáljuk Cabinet 0.1-ben — a `MaxPartsPerSkeleton = 500` (SEC-CAB-5) felső korlát biztosítja, hogy a futási idő < 100ms. Spatial indexing (O(N log N)) Cabinet 0.2 parking finding. | §7.2 |
| **BE-CAB-2** | 🟠 HIGH | Thread-safety contract | A `Skeleton` aggregate concurrent használata nincs explicit dokumentálva. A fogyasztó-réteg nem tudja, hogy a `Parts.Count` thread-safe-e mutáció közben. | Explicit XML doc a Skeleton-on: **READ lockless**, **WRITE single-thread**. AutoCAD adapter konvenciója: minden mutáció a UI thread-en. Concurrent stress-test a CI-ben. | §4.2.1 |
| **BE-CAB-3** | 🟠 HIGH | API stability + deprecation | A Cabinet 0.x sorozatban a public API breaking change-ek kezelése nincs definiálva. | Patch-ben tilos public API változás. Minor-ben deprecated jelölés `[Obsolete]`-szel + min. 1 minor verziónyi tartózkodás. Major-ben breaking OK. `docs/deprecation-policy.md` committed (BE-CAB-8). | §11.1 |
| **BE-CAB-4** | 🟡 MEDIUM | Allocation hot path | `AffineTransform` 128 byte-os struct, `ResizeAssembly()` 2500+ allokációt csinálhat. A `Result<AffineTransform>` boxing-ot okozhat (Result generic class). | Cabinet 0.1-ben **NEM** optimalizáljuk preventív módon (premature optimization). Profile-vezérelt: ha Cabinet 0.1 teszt-szuit BenchmarkDotNet-ben kimutatja, akkor Cabinet 0.2-re intern `Span<AffineTransform>` API. | §15.3 (külön szakasz) |
| **BE-CAB-5** | 🟡 MEDIUM | Engine parallelism | `ConstructionRuleEngine.ApplyAll` rule-okat sorosan futtat. 50 rule × 0.5s = 25s, közelíti a 30s-os engine timeout-ot. Paralellizmus segíthetne. | Cabinet 0.1-ben **NEM** paralellizálunk: rule-ok közötti függőségek nem dokumentáltak, race condition-ok kockázata túl nagy. Cabinet 0.2-ben jön explicit "no dependency" rule-kontraktus, és arra alapozva paralellizmus. | §11.2 |
| **BE-CAB-6** | 🟡 MEDIUM | Domain events memory growth | Hosszú session-ben (1000+ mutáció) a `_domainEvents` listája végtelenül nőhet, ha a fogyasztó elfelejti `PopDomainEvents()`-et. | `MaxUnflushedEvents = 1000` cap; túllépés `Result.Error("Domain events not flushed")`. A fogyasztó észreveszi a missing flush-ot. | §4.2.1 |
| **BE-CAB-7** | 🟡 MEDIUM | Epsilon configurability | A `1e-9` epsilon hardcoded minden helyen. Egyes domain (precíziós optikai gyártás) finomabb, mások (gyors prototípus) durvább epsilon-t akarhatnak. | `GeometryConstants.DefaultEpsilon = 1e-9`, `AngularEpsilon = 1e-7`, `DimensionEpsilon = 1e-3` — central config. Eseti override `IsApproximatelyEqualTo(other, epsilon)` overload-on át. Tenant-szintű override Cabinet 0.2-ben. | §4.1.2 |
| **BE-CAB-8** | 🟢 LOW | Deprecation policy doc | A BE-CAB-3 részletes szabályait egy committed dokumentum kell, hogy hordozza. | `docs/deprecation-policy.md` committed Cabinet 0.1 release-szel; tartalmazza: SemVer rules, [Obsolete] usage pattern, removal timing, fogyasztói példák. | §11.1 |

### 15.3 Allocation pattern megjegyzés (BE-CAB-4)

A v4 review felmerült: a `Result<T>` használata a Geometry namespace forró úton (hot path) boxing-allocation-okat okozhat. Cabinet 0.1-ben **nem optimalizálunk preventív módon**. Indok:

1. **Premature optimization elv:** soha nem optimalizálj mérés nélkül
2. A `MaxPartsPerSkeleton = 500` keret garantálja, hogy a hot path nem extrém
3. Modern .NET 8/10 garbage collector (regiongc) jól kezeli a kis-méretű record allokációkat

**Test gate Cabinet 0.1-ben:** BenchmarkDotNet alapú perf-teszt egy reprezentatív Skeleton (50 Part) `ResizeAssembly()` műveletére. Ha az allokáció **< 1 MB / művelet**, akkor a current minta megtartható. Cabinet 0.2-ben felülvizsgálandó, ha a perf-szám romlik.

### 15.4 Effort breakdown

| Finding | Effort |
|---|---|
| BE-CAB-1 (algorithmic complexity policy + perf-teszt) | 0.5 nap |
| BE-CAB-2 (thread-safety doc + concurrent stress-teszt) | 0.5 nap |
| BE-CAB-3 (API stability decision doc) | 0.25 nap |
| BE-CAB-4 (allocation BenchmarkDotNet teszt) | 0.5 nap |
| BE-CAB-5 (parallelism deferral doc) | 0.0 nap (csak parking finding) |
| BE-CAB-6 (event count cap + tests) | 0.25 nap |
| BE-CAB-7 (GeometryConstants + epsilon overload + tests) | 0.25 nap |
| BE-CAB-8 (deprecation-policy.md) | 0.25 nap |
| **Összesen** | **2.5 nap** |

---

## 16. Claude Code implementációs csomag

Ez a szekció a Claude Code agent #1 (VPS) számára készült — a v4 dokumentum alapján a teljes Cabinet 0.1 implementáció vezérlési terve.

### 16.1 Végrehajtási sorrend (Track-bontás)

A 21.75 nap **párhuzamosítható track-ekre** van bontva. A track-ek függőségi sorrendben:

| Nap | Feladat | Track | Függőség |
|---|---|---|---|
| 1 | Repo skeleton: `dotnet new sln`, `global.json`, 7 csomag-projekt skeleton, .gitignore, CLAUDE.md, GitHub Actions workflow | **A — Infra** | — |
| 1.5 | NuGet csomag-konfigurációk (Description, package metadata mindegyikben, multi-target) | A | Nap 1 |
| 2 | `Geometry` csomag: `Vector3`, `AffineTransform`, `IsValid()`, factory `Result<T>`, `GeometryConstants` | **B — Foundation** | Nap 1 |
| 3 | `Geometry` csomag: `PartFrame`, `AssemblyFrame`, `PartDimension`, `AssemblyDimension` MaxXxx limit-ekkel | B | Nap 2 |
| 3.5 | `Geometry` csomag: 50 unit teszt mindkét cél-frameworken | B | Nap 3 |
| 4 | `Abstractions` csomag: `ITenantStandardProvider`, `ISnapshotMigrator`, `IGeometryProjector`, `IPartCatalog` interface-ek | **C — Ports** | — |
| 4.5 | `Domain` csomag: `IDomainEvent` + `SequenceNumber`, `Skeleton` aggregate skeleton, `BaseCuboid` | **D — Domain** | Nap 3 (B), Nap 4 (C) |
| 5 | `Domain` csomag: `Part`, `Connection` aggregate-belső entity, internal ctor SEC-CAB-1 mintával | D | Nap 4.5 |
| 6 | `Domain` csomag: `Skeleton.AddPart`, `RemovePart`, `AddConnection`, `ResizeAssembly` mutator-ok + invariáns-validáció | D | Nap 5 |
| 7 | `Domain` csomag: `SkeletonSnapshot`, `ToJson`, `FromJson` SchemaVersion-ellenőrzéssel; `SkeletonReconstruction.FromSnapshot()` | D | Nap 6 |
| 7.5 | `Domain` csomag: 60 unit teszt aggregate-mutációkra, invariánsokra, multi-tenant isolation-re | D | Nap 7 |
| 8 | `Machining` csomag: `MachiningFeature`, `MachiningSubject` (3 leszármazott), `HardwareReference` | **E — Machining** | Nap 5 |
| 8.5 | `Machining` csomag: 25 unit teszt | E | Nap 8 |
| 9 | `Construction` csomag: `IConstructionRule` interface, `ConstructionRuleEngine` skeleton, `DesignAdvisory`, `AdvisorySeverity` | **F — Construction** | Nap 4 (C), Nap 8 (E) |
| 9.5 | `Construction` csomag: 10 default rule (R-32mm-LineBore, R-Default-Joint, R-BackPanel-Hidden/Visible, R-EdgeBand-*, R-Setback, R-Material-Default, R-Stiffener-Tall, R-Shelf-Sag) | F | Nap 9 |
| 10 | `Construction` csomag: Engine timeout (CancellationToken 5s/30s), null-handling, exception-handling, max-output cap | F | Nap 9.5 |
| 11 | `Construction` csomag: 40 unit teszt (10 rule × 4 forgatókönyv) + Advisory message regex teszt | F | Nap 10 |
| 12 | `Semantics` csomag: `SemanticInferenceService`, `PartRole` enum, gravity + topology algoritmus (BE-CAB-1: O(N²) elfogadva) | **G — Semantics** | Nap 7.5 (D) |
| 12.5 | `Semantics` csomag: lockless `ConcurrentDictionary` cache + LRU eviction (DB-CAB-6) | G | Nap 12 |
| 13 | `Semantics` csomag: 25 unit teszt + concurrent stress teszt | G | Nap 12.5 |
| 14 | `Domain` csomag: `DependencyGraph` Kahn topo-sort + cycle detection (SEC-CAB-7) | D | Nap 7.5 |
| 14.5 | `Domain` csomag: 15 persistence + 10 concurrency teszt (DB-CAB-2, -3, -8 + DB-CAB-6, -7) | D | Nap 14 |
| 15 | `Domain` csomag: `docs/sample-snapshots/` 5 reprezentatív minta (DB-CAB-10) | D | Nap 14.5 |
| 16 | Cross-cutting: `CabinetJsonOptions.Strict` (DB-CAB-8) + cross-runtime determinism teszt | **H — Cross-cutting** | Nap 7 |
| 16.5 | Cross-cutting: smoke-fogyasztási teszt (`net8.0` console app) (DB-CAB-4) | H | Nap 16 |
| 17 | Cross-cutting: `docs/security/threat-model-cabinet-0.1.md` (SEC-CAB review-ból) | H | Nap 16.5 |
| 17.5 | Cross-cutting: `docs/deprecation-policy.md` (BE-CAB-8) | H | Nap 17 |
| 18 | Performance: BenchmarkDotNet teszt 50-Part Skeleton ResizeAssembly-re (BE-CAB-4) | H | Nap 17.5 |
| 19 | `Cabinet (meta)` package: dependencies finalize, README each package | A | All |
| 20 | Multi-target verifikáció: `unzip -l *.nupkg` lib/net8.0 + lib/net10.0 minden csomagban | A | Nap 19 |
| 20.5 | GitHub Actions tag-publikálás teszt: `v0.1.0-alpha.0` tag | A | Nap 20 |
| 21 | Final integration test: minden csomag konzisztens — 230+ teszt zöld | All | All |
| 21.75 | Cabinet 0.1.0-alpha.1 release tag; NuGet publish GitHub Packages-re | A | Nap 21 |

A track-ek B (Geometry), C (Ports), D (Domain), E (Machining), F (Construction), G (Semantics) **több ponton paralellizálhatók** ha több agent dolgozik. Egy agent-tel a fenti soros sorrend optimális.

### 16.2 Agent utasítás

```
Implementáld a SpaceOS Cabinet 0.1 — Core Foundation v4 dokumentum szerint
a `spaceos-modules-cabinet` repo-ban.

Track A (Infra): repo setup + GitHub Actions
Track B (Foundation): Geometry namespace
Track C (Ports): Abstractions namespace
Track D (Domain): Skeleton aggregate + persistence
Track E (Machining): MachiningFeature + Subject
Track F (Construction): RuleEngine + 10 default rule
Track G (Semantics): InferenceService + cache
Track H (Cross-cutting): determinism, threat-model, deprecation policy, perf

DoD: §9.1–§9.10 minden checkbox ✓
Security gates: §9.10 mindegyik (deployment blockers)
Finding-ek: 28 db (10 DB-CAB + 10 SEC-CAB + 8 BE-CAB) — minden javítás §13–§15-ben részletezve

Minden feladat után:
  cd ~/dev/spaceos/spaceos-modules-cabinet
  dotnet build -c Release
  dotnet test -f net8.0
  dotnet test -f net10.0
  git status
  git commit -m "..."
  git push

Aggregate-szintű invariáns-tesztet külön végezz végén:
  - Cross-tenant Part isolation (SEC-CAB-1)
  - NaN/Infinity guard minden VO-n (SEC-CAB-2)
  - Dimension limits (SEC-CAB-3)
  - DOS védelem (SEC-CAB-4, -5)
  - FromSnapshot validation (SEC-CAB-6)
  - DependencyGraph cycle detection (SEC-CAB-7)
  - Engine null/exception handling (SEC-CAB-8)
  - Advisory message privacy (SEC-CAB-9)
  - Domain events sequence integrity (SEC-CAB-10)

Kérdés esetén állj meg és kérdezd a humán architekt-et.
Ne improvizálj a public API-on — ha hiányzik valami a dokumentumból, kérdezd.
```

### 16.3 Kockázatok és mitigációk

| Kockázat | Valószínűség | Hatás | Mitigáció |
|---|---|---|---|
| Multi-target build subtle különbségek (DB-CAB-4) | Közepes | Magas — fogyasztó breakage | Smoke teszt CI-ben minden release előtt; `#if NET10_OR_GREATER` ban a public felületen |
| `JsonSerializer` viselkedés-változás .NET 10 minor-frissítéskor | Alacsony | Magas — snapshot-inkompatibilitás | `CabinetJsonOptions.Strict` minden tulajdonság explicit; cross-runtime determinism teszt |
| Agent rosszul interpretálja a "thread-safe read" kontraktust | Közepes | Közepes — race condition production-ben | Concurrent stress-teszt a CI-ben; XML doc explicit minden public method-on |
| ConstructionRule timeout 5s elégtelennek bizonyul realisztikus rule-okra | Alacsony | Közepes — false positive Result.Error | Cabinet 0.1-ben fix. Ha a Doorstar UAT során kiderül probléma, Cabinet 0.1.x patch-ben emelhető |
| `MaxPartsPerSkeleton = 500` túl szigorú gardróbsor-méretre | Alacsony | Alacsony — Result.Error a tervezőnek | TenantStandard-override Cabinet 0.2-ben |
| Multi-tenant Part-szivárgás (SEC-CAB-1) szándékos megkerülése | Alacsony | Magas — adatszivárgás | `FromSnapshot()` invariáns-teszt minden Part.SkeletonId-ra |
| Az 21.75 nap becslés valójában 25-30 nap | Közepes | Közepes | Track-bontás engedi a paralellizmust; több agent-szál esetén 14 nap |
| AutoCAD 2027 .NET 10-igénye további constraintet ad később | Alacsony | Alacsony — multi-target már megoldja | A v4 már lefedte (DB-CAB-4) |

### 16.4 Cabinet 0.1 → CabinetBilder integráció (post-v4)

A Cabinet 0.1.0 release után a `cabinetbilder-autocad` repo-ban (Windows agent #2) ~1.5 napos integráció:

| Lépés | Tartalom | Effort |
|---|---|---|
| 1 | `cd cabinetbilder-autocad && dotnet add package SpaceOS.Cabinet.Domain --version 0.1.0` (és minden további csomag) | 0.25 nap |
| 2 | A meglévő `CabinetBilder.Core.Skeleton` namespace-eket átírni `using SpaceOS.Cabinet.Domain.Skeleton;`-re | 0.25 nap |
| 3 | Adapter.AutoCAD Brep-pipeline: a meglévő `Skeleton.cs` Brep-rendelést átalakítani az új Modules.Cabinet domain types-ra | 0.5 nap |
| 4 | Properties Palette WPF UI: data-binding az új Skeleton aggregate-en át | 0.25 nap |
| 5 | AutoCAD UAT: a meglévő DWG-k betöltődnek-e, a properties módosulnak-e, a BOM generálódik-e | 0.25 nap |

A Cabinet 0.1 schema-version `"0.1"` az új JSON-snapshot formátum. A meglévő DWG-k XRecord-ja (régi CabinetBilder.Core.Skeleton format) **migration-igényes** — de **NEM** törlési task. A migrátor egy 1-szeri konvertáló futtatás, ami a meglévő DWG-ket az új Skeleton.FromSnapshot()-tal olvashatóvá teszi. Ez a Cabinet 0.1.0 NuGet publish után 1-2 hét, nem blokkolja a Cabinet 0.1 release-t.

---

*SpaceOS · Cabinet 0.1 — Core Foundation · v4.0 IMPLEMENTÁCIÓRA KÉSZ · 2026-04-25*
*Státusz: ✅ IMPLEMENTÁCIÓRA KÉSZ — 28 finding beépítve (10 DB-CAB + 10 SEC-CAB + 8 BE-CAB), Claude Code agent #1 részére átadható*
*Kumulált effort: ~21.75 fejlesztői nap*
*v1.0 → v1.1: runtime cél-framework multi-target (net8.0;net10.0)*
*v1.1 → v2: database-designer + database-schema-designer review — 10 finding (4 HIGH + 4 MEDIUM + 2 LOW)*
*v2 → v3: senior-security review — 10 finding (1 CRITICAL + 5 HIGH + 3 MEDIUM + 1 LOW)*
*v3 → v4: senior-backend review — 8 finding (3 HIGH + 4 MEDIUM + 1 LOW)*
