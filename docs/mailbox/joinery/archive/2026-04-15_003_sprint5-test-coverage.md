---
id: MSG-JOINERY-003
from: root
to: joinery
type: task
priority: high
status: READ
ref: R-19
created: 2026-04-15
---

# MSG-JOINERY-003 — Sprint 5: Test Coverage — PDF golden-file + Contracts integráció

## Háttér

Devils-advocate audit (2026-04-15) két magas prioritású gap-et azonosított (R-19):

1. A gyártásilap PDF **jogi/operációs dokumentum** — silent field loss nem derül ki jelenleg
2. Az `ICuttingProvider` / `IInventoryProvider` / `IProcurementProvider` mock-ok az implementáló feltételezéseit tesztelik, nem a valódi contract surface-t

## Feladat

### 1. PDF golden-file teszt

3 kanonikus megrendelésre:
- Generáld le a PDF-et
- PdfPig-gel extract-old a szöveget
- Assert: minden kötelező mező jelen van (rendelésszám, méret, anyag, élzárás, vasalat, dátum)
- Assert: magyar karakterek helyes kódolása (ő, ű, á, é — PDF encoding edge case)

```csharp
[Theory]
[InlineData("order_standard_door")]
[InlineData("order_oversized_door")]
[InlineData("order_minimal_door")]
public void GenerateGyartasilap_ContainsAllRequiredFields(string orderFixture)
```

### 2. Dimension/material validáció edge case-ek

```csharp
[Theory]
[InlineData(0, "zero width")]
[InlineData(-100, "negative width")]
[InlineData(99999, "exceeds max press size")]
public void CreateDoor_WithInvalidDimension_ThrowsValidationException(int width, string desc)
```

### 3. ICuttingProvider hibakezelés

Mi történik ha a provider:
- 500-at ad vissza
- Timeout-ol (408)
- Részleges adatot küld

Elvárás: Joinery nem korrupt adatot tárol, hanem explicit hibát dob.

### 4. RLS negatív teszt

```csharp
// Tenant A door config nem elérhető Tenant B session-ből
JoineryRepository_CrossTenant_ReturnsEmpty()
```

## DoD

- [ ] PDF golden-file teszt 3 fixture-re zöld + magyar karakter teszt zöld
- [ ] Dimension validáció `[Theory]` tesztek zöldek (min. 5 edge case)
- [ ] ICuttingProvider hibakezelés 3 eset tesztelve
- [ ] RLS negatív teszt zöld
- [ ] Tesztszám ≥ 109
- [ ] DONE outbox: új tesztszám + összefoglaló

