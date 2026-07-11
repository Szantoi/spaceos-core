# Integration Check: SSOT State Management & Hierarchy Refactor

## Érintett Szabványok
- **`Project_Folder_Structure_Standard.md`**: Ez a terv alapvetően erre a standardra épít. A cél a fizikai és virtuális (DB) hierarchia szinkronizálása.
- **`Plans_Discovery_Framework_Standard.md`**: Az integráció során biztosítani kell, hogy a Discovery fázisok (Explorer -> Integrator) is követhetőek legyenek az API-n keresztül.
- **`Clean Architecture & DDD Standards`**: Az új entitások (Milestone, Epic) bevezetésekor szigorúan követni kell a Core/Infra/Api szétválasztást.

## Érintett Komponensek
- **`JoineryTech.Flow.Api`**: Új végpontok, modellek és MCP eszközök szükségesek.
- **`JoineryTech.Flow.Web`**: Új dashboard nézetek létrehozása.
- **`Agent System Runner Scrips`**: A `.ps1` rutinok frissítése szükséges lehet, hogy validálják az API elérhetőségét.

## Kockázatos pontok (Hol törhet el valami?)
- **Markdown vs. DB Szinkronizáció**: Ha egy Agent manuálisan szerkeszt egy Markdown fájlt, de nem hívja meg az API-t, inkonzisztencia lép fel. Meg kell határozni, melyik az "Ultimate SSOT".
- **Migráció**: A meglévő projektek és taskok betöltése az új rendszerbe (Backfilling) adatvesztéssel vagy duplikációval járhat.
- **Frontend Build**: A `JoineryTech.Flow.Web` build logok (`build_error.log`) arra utalnak, hogy jelenleg is vannak TypeScript/TSConfig problémák a frontendben. Ezeket orvosolni kell, mielőtt az új dashboardot integráljuk.
