---
id: MSG-K027
from: architect
to: kernel
type: task
status: UNREAD
priority: P2
sprint: "Sprint D · Phase 3A Cleanup"
ref: MSG-K025-RESPONSE
---

# Phase 3A — Minor fix-ek (MSG-K025-RESPONSE nyitott pontjai)

A Phase 3A review két nem-blokkoló eltérést azonosított. Kérlek javítsd ki őket a Phase 3B munka megkezdése előtt.

---

## Fix 1 — `DimensionVector` + `Point3D`: `sealed record` → `readonly record struct`

**Érintett fájlok:**
- `SpaceOS.Kernel.Domain/ValueObjects/DimensionVector.cs`
- `SpaceOS.Kernel.Domain/ValueObjects/Point3D.cs`

**Jelenlegi állapot:** `sealed record` (heap-allokált class)
**Elvárt:** `readonly record struct` (stack-allokált, CLAUDE.md konvenció — mint `GridCell`)

**Csere menete:**
```csharp
// ELŐTTE:
public sealed record DimensionVector(int WidthMm, int HeightMm, int DepthMm);

// UTÁNA:
public readonly record struct DimensionVector(int WidthMm, int HeightMm, int DepthMm);
```

```csharp
// ELŐTTE:
public sealed record Point3D(int X, int Y, int Z);

// UTÁNA:
public readonly record struct Point3D(int X, int Y, int Z);
```

**Ellenőrzések a csere után:**
- [ ] `dotnet build` — 0 error (a `readonly record struct` nem nullable, EF Core `OwnsOne` konfigurációban ez érinthet valamit)
- [ ] EF Core `OwnsOne` konfiguráció: `PhysicalSpaceConfiguration` — `OwnsOne(e => e.Dimensions)` és `OwnsOne(e => e.Origin)` változatlanul működik-e? Ha nem: `builder.ComplexProperty()` lehet szükséges .NET 8-ban struct value object-ekhez.
- [ ] `dotnet test` — 814 pass, 0 fail

**Fontos:** Ha az EF Core konfiguráció törne a struct típus miatt, jelezd visszajelzésben — a fix módosítható (pl. `ComplexProperty` használata `OwnsOne` helyett).

---

## Fix 2 — CS nullable warnings `TenantSessionInterceptorTests.cs`-ben

**Érintett fájl:** `SpaceOS.Kernel.Tests/...TenantSessionInterceptorTests.cs`

**Warningok:**
```
CS8764(227,48): Nullability of return type doesn't match overridden member
CS8764(260,49): Nullability of return type doesn't match overridden member
CS8765(312,50): Nullability of type of parameter 'value' doesn't match overridden member
CS8765(314,50): Nullability of type of parameter 'value' doesn't match overridden member
CS8602(294,14): Dereference of a possibly null reference
```

**Javítás iránya:**
- CS8764/CS8765: az override metódusokon a nullable annotáció egyezzen az alap interfész/osztály deklarációjával (pl. `string?` → `string` vagy fordítva, ahogy az alap definiálja)
- CS8602: null-check hozzáadása a dereferencia előtt, vagy `!` null-forgiving operator ha biztos a nem-null érték

**Ellenőrzés:**
```bash
dotnet build 2>&1 | grep -v xUnit1051
```
→ 0 warning (xUnit1051 kivételével)

---

## Elvárt outbox üzenet

`type: response`, `ref: MSG-K027`:
- Fix 1: `readonly record struct` csere sikeres? + EF Core `OwnsOne` érintett?
- Fix 2: nullable warnings javítva?
- `dotnet build` output (warning count)
- `dotnet test` eredmény (pass/fail)
