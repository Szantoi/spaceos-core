---
id: MSG-KERNEL-063
from: root
to: kernel
type: task
priority: critical
status: DONE
ref: MSG-E2E-008-DONE
created: 2026-04-13
---

# MSG-KERNEL-063 — ClaimsTenantResolver fallback fix (8dd0bd7 regresszió)

## Kontextus

Az MSG-KERNEL-062-DONE commit `8dd0bd7` **súlyos regressziót okoz** a VPS-en:

| Metrika | Baseline (c62f1d7) | Regresszió (8dd0bd7) |
|---|---|---|
| E2E pass | 147 | 119 |
| E2E fail | 4 | **23** |
| E2E skip | 0 | 9 |

**Tünetek:**
- POST create endpointok (`/api/flow-epics`, `/api/workstations`, `/api/space-layers`) → **500** (előtte 201)
- GET list endpointok → **üres** (tenant filter rossz tenantId-t ad)
- 12 test fájl érintett

**Root cause hipotézis:**

`ClaimsTenantResolver.TryResolve()` most a `spaceos_tenants` claim-et olvassa elsőként. De a **tesztek `test-admin` JWT token-je valószínűleg NEM tartalmaz `spaceos_tenants` claim-et** (csak `groups` / `tid` claim-et) — ilyenkor a resolver vagy `null`-t ad vissza, vagy kivételt dob, ami:
- `TenantSessionInterceptor`-ban: `UnauthorizedAccessException` → 500
- Global query filter-ben: `null` TenantId → üres lista

---

## Feladat — Graceful fallback implementálása

A `ClaimsTenantResolver.TryResolve()` metódusban a következő logika szükséges:

```csharp
// Helyes prioritás sorrend:
// 1. spaceos_tenants claim (double-deserialized JSON array) — Keycloak Script Mapper
// 2. tid claim (standard Keycloak tenant claim)
// 3. groups claim (fallback)
// Ha egyik sem → return false (ne throw-oljon)

public bool TryResolve(ClaimsPrincipal user, out Guid tenantId)
{
    tenantId = Guid.Empty;

    // 1. spaceos_tenants (double-serialized JSON array)
    var spaceosTenantsClaim = user.FindFirst("spaceos_tenants")?.Value;
    if (!string.IsNullOrEmpty(spaceosTenantsClaim))
    {
        try
        {
            // double-deserialized: "[{\"tenant_id\":\"...\"}]"
            using var doc = JsonDocument.Parse(spaceosTenantsClaim);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    if (element.TryGetProperty("tenant_id", out var idEl)
                        && Guid.TryParse(idEl.GetString(), out var g)
                        && g != Guid.Empty)
                    {
                        tenantId = g;
                        return true;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // malformed — fall through to next option
        }
    }

    // 2. tid claim
    var tidClaim = user.FindFirst("tid")?.Value;
    if (Guid.TryParse(tidClaim, out var tidGuid) && tidGuid != Guid.Empty)
    {
        tenantId = tidGuid;
        return true;
    }

    // 3. groups (ha egyéb tenant-claim formátum van)
    // ... (ha az eredeti kódban volt groups-alapú fallback, tartsuk meg)

    return false;  // NE THROW-OLJON
}
```

**Kritikus:** ha a fallback mind hiányzik → `return false`, a hívó kód kezelje (pl. 401 vagy 403), ne throw `InvalidOperationException` vagy `UnauthorizedAccessException`.

---

## Debug lépések (VPS loggal)

Ha a root cause nem egyértelmű, ellenőrizd a VPS journal-t egy failed POST-nál:

```bash
journalctl -u spaceos-kernel -n 100 | grep -A5 "Exception\|500\|tenant"
```

Ez megmutatja, hogy a `ClaimsTenantResolver` milyen hibát dob (`JsonException`? `NullReferenceException`? `UnauthorizedAccessException`?).

---

## Definition of Done

- [ ] `ClaimsTenantResolver.TryResolve()` graceful fallback: spaceos_tenants → tid → groups → `return false`
- [ ] POST create endpointok nem dobnak 500-at ha `spaceos_tenants` hiányzik
- [ ] GET list endpointok a helyes tenantId-vel szűrnek
- [ ] A korábban ismert 4 fail is javítva marad (ha volt közöttük spaceos_tenants-dependens teszt)
- [ ] Meglévő **1075 teszt zöld**
- [ ] Commit + push

## Visszajelzés

Outboxba: `MSG-KERNEL-063-DONE`

## Blokkoló hatás

A VPS-en ideiglenes rollback zajlik (MSG-INFRA-064) — a `c62f1d7` binary fut addig, amíg ez a fix nincs kész. Utána INFRA újra deployol.
