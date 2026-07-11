---
id: goal-mcp-maintenance
title: "Goals: MCP System Maintenance"
type: goal
project: mcp-maintenance
---

# 🎯 Célkitűzés: MCP System Maintenance

Az MCP (Model Context Protocol) szerver az Agent rendszer központja. A cél a rendszer stabilitásának, biztonságának (RBAC), és karbantarthatóságának hosszútávú biztosítása.

## 🚀 Üzleti és Technológiai Célok

1. **Jogosultság-kezelés (RBAC) konzisztenciája**: Minden agent role kizárólag a domainjének és feladatkörének megfelelő eszközökhöz férhet hozzá (Fail Closed elv).
2. **Kód Higiénia és Stabilitás**: A szerver gyökérkönyvtárának tisztán tartása, teszt/mock maradványok eliminálása, használaton kívüli kódok és felesleges fájlok törlése (Dead Code Elimination).
3. **Teszt Lefedettség Biztosítása**: Az e2e és API teszteknek fedniük kell a szerepkörök jogosultsági mátrixát.

## 📦 Scope

**In Scope:**
- Schema YAML fájlok frissítése (`mcp_tool_permissions`).
- Szerver gyökér kitakarítása és `.gitignore` frissítése.
- Statikus kódelemzés (unused exports, unused files) és takarítás a `src/agent-system/server/` alatt.
- Program/Milestone/Epic adminisztráció kialakítása a dokumentációs standardok szerint.

**Out of Scope:**
- Új MCP toolok írása.
- A teljes rendszer TypeScript strict módra állítása.

## 🏆 Sikerkritériumok

- [x] Minden schema YAML tartalmaz `mcp_tool_permissions` definíciót.
- [x] Lefutnak az RBAC E2E tesztek (Playwright).
- [x] Nincs teszt maradvány a szerver gyökerében.
- [x] A statikus kódanalízis (pl. `ts-prune`) igazolja, hogy nincs felgyülemlett halott kód a projektben.
