---
id: IDEA-20260622-002
title: "2. Assembly terv vs. aktuális gyártás — Real-time eltérés jelzés"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. Assembly terv vs. aktuális gyártás — Real-time eltérés jelzés

**Komponens:** `assembly.jsx`
**Típus:** ui-component + state-management
**Prioritás:** high

Az assembly terv-vs-aktuális nézeten belül egy **eltérés-indikátor** (piros/sárga badge) a soronként megjelölt hibákhoz: „-2 darab", „+1 óra munkaórák", „anyag helyettesítve". Kattintásra egy **inline popover** mutatja az okot (lista aláírásával) és gyors-akciót (jóváhagyás/módosítás). State: `app-store.jsx`-ben egy `discrepancies[]` array (típus: qty | time | material).

**Kapcsolódó fájlok:**
- `assembly.jsx`
- `app-store.jsx`
- `app-main.jsx` (context)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
