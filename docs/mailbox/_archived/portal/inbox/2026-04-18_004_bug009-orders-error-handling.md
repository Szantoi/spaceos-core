---
id: MSG-PORTAL-004
from: root
to: portal
type: task
priority: high
status: READ
created: 2026-04-18
---

# BUG-009 — Rendelések és Készlet UI: hiányzó error handling

## Szimptómák (manuális teszt, test-admin felhasználó)

1. **Rendelések lista** — üres, semmi nem látszik (háttérben 405 → majd 500 jön a listázó GET-re)
2. **"Rendelés rögzítése" gomb** — megnyomás után semmi nem történik; a modal nyitva marad, nincs hibaüzenet
3. **Készlet oldal** — üres; ha nincs adat a tenant alatt, `GetStock` 404-et ad → de az UI ezt nem kezeli

## Javítandó fájlok

### 1. `CreatePurchaseOrderModal.tsx` — silent error fix

Jelenleg: `useMutation` nincs `onError`-ral bekötve a modal-ban.

```tsx
// HIÁNYZIK:
const { mutate, isPending, isError, error } = useCreatePurchaseOrder();

// Form-ban:
{isError && (
  <div className="rounded border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
    Nem sikerült a rendelés rögzítése. Próbáld újra.
  </div>
)}
```

### 2. `ProcurementPage.tsx` — loading/error states

Ha a GET lista hiba → jelenleg üres div. Add hozzá:
```tsx
const { data: orders, isLoading, isError } = useProcurementOrders();

// Hibakezelés:
{isError && (
  <div className="rounded border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
    Nem sikerült betölteni a rendeléseket.
  </div>
)}
```

### 3. `InventoryPage.tsx` — 404 → üres készlet (ne hibázzon)

Ha a `useInventoryStock` hook 404-et kap (nincsen készlet a tenantnál), az `isError`-ba kerül, de ezt üres állapotként kell kezelni, nem hibaként:

```tsx
// Ha 404 → show "Nincs készletadat" üres állapot (nem piros hiba)
const isEmpty = isError || !stockData || stockData.length === 0;

{isEmpty ? (
  <div className="rounded-lg border border-dashed border-gray-300 bg-gray-50 py-12 text-center text-sm text-gray-500">
    Nincs rögzített készletadat
  </div>
) : (
  // ...táblázat
)}
```

**Megjegyzés**: A 404 → üres állapot kezelése csak az `axios` error interceptor-ban müködik ha az `apiClient` nem dob exception-t 404-re, hanem `isError: true`-t ad vissza. Ellenőrizd az `apiClient` config-ot.

## DoD

- [ ] `CreatePurchaseOrderModal` mutat hibaüzenetet POST failure esetén
- [ ] `ProcurementPage` mutat hibaüzenetet GET failure esetén
- [ ] `InventoryPage` üres állapotot mutat 404 esetén (nem piros hibát)
- [ ] `npm test` → legalább 311 zöld
- [ ] Build: INFRA deploy szükséges → jelezd

---

*Skill: `/spaceos-terminal`*
