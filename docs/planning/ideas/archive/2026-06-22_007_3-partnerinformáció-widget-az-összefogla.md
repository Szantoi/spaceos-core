---
id: IDEA-20260622-007
title: "3. Partnerinformáció-widget az összefoglaláskor (Handshake-bekezdésben)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Partnerinformáció-widget az összefoglaláskor (Handshake-bekezdésben)

**Komponens:** Új mini-komponens a handshake/quote-összegzésben
**Típus:** ui-component | styling
**Prioritás:** medium

Amikor egy **RFQ/PO-t szállítónak küldünk** (handshake), a képernyő alján megjelenjen egy **szürke, összecsukható kártya** ("Partner adatok"), amely mutatja:
- Szállító neve, adóazonosító, kapcsolattartó
- Utolsó szállítás dátuma és teljesítésI pontossága (%)
- Egy kicsi **"Nyitás partnerportálon"** link (a `PartnerCockpit`-re mutató modal/tab-ra)

Ez csökkenti a többszöri keresést a partner-panelen.

**Kapcsolódó fájlok:**
- app-main.jsx (handshake-summary section)
- page-partner.jsx (adatlekérdezés)
- tailwind utility: `bg-gray-100`, `border-l-4`, `transition-all`

---

---
*Automatikusan generálva a JoineryTech prototípusból*
