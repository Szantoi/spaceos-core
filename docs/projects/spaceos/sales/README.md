# Sales Module

> Értékesítési folyamatok digitalizálása

## Goal

Teljes értékesítési folyamat támogatása az ajánlattól a megrendelésig:
- Ajánlatok létrehozása, szerkesztése
- PDF export, email küldés
- Ügyfél jóváhagyás workflow
- Pipeline vizualizáció, forecast

## Target Customer

**Doorstar Kft.** - Első production ügyfél (Soft Launch Q2 2026)

## Epics

### 1. Ajánlat Kezelés (offer)
Ajánlatok CRUD, PDF generálás, ügyfél portál

### 2. Pipeline (pipeline)
Sales pipeline nézet, stage-ek, forecast

## Technical Stack

- **Backend:** Kernel + Sales Driver (.NET 8)
- **BFF:** Orchestrator (Node.js)
- **Frontend:** JoineryTech Portal (React 18)

## Dependencies

- Kernel auth + RBAC
- Joinery product catalog (BOM-hoz)
