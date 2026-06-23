---
id: IDEA-20260622-004
title: "3. Beszállítói portál — PO-nyugta + ASN nyomkövetés (QR-kód aláírás)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Beszállítói portál — PO-nyugta + ASN nyomkövetés (QR-kód aláírás)

**Komponens:** `page-supplier.jsx` / Supplier Cockpit
**Típus:** ui-component + state-management
**Prioritás:** high

A beszállítói önkiszolgáló portál (Falco Sopron tesztfiók) "Feladás" (`markPOShipped`) lépésénél megjelenik egy **QR-kód generátor** az ASN (Advance Shipping Notice) számmal, amely az asztalos/bútoripari fuvarlevélre nyomtatható. Az inbound csomagvétel (`receiveGoods`) során a portál-üzemeltető **mobilos kamera-szkennerrel** azonosítja az ASN-t, valós idejű visszaigazolást küld a beszállítónak, és az `po.status` → `"received"` lépés véglegesül. Egyszerű offline QR lib (nincs kamera API, szimulált szkenner).

**Kapcsolódó fájlok:**
- `page-supplier.jsx` (ASN QR-gen + mock scanner input)
- `app-store.jsx` (acknowledgeSupplierShipment action)
- `data-po.js` (ASN → QR formatter)

---
*Automatikusan generálva a JoineryTech prototípusból*
