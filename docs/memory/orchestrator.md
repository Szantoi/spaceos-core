# ORCHESTRATOR Memory

Utolsó frissítés: 2026-06-21

## Aktuális állapot
- MSG-ORCH-006 DONE: Joinery + Cutting API routing verified
- Port 3000, PM2-ben fut, 121/121 teszt zöld
- Proxy routing működik: /api/orders/:id/material-req, /api/orders/:id/hardware-list, /api/cutting/plans (GET+POST)

## Fontos kontextus
- `proxy.route.ts` tartalmazza az összes backend proxy route-ot
- `app.use('/api', proxyRouter)` mount pont az index.ts-ben (60. sor)
- Backend szolgáltatások portjai: Joinery:5002, Identity:5003, Cutting:5004
- .env.example frissítve az IDENTITY_BASE_URL és CUTTING_BASE_URL pontos portjaival

## Következő lépések
- Várakozás következő inbox üzenetre

## Megoldott problémák
- **Proxy routing setup**: A kért 4 route már létezett a codebase-ben, csak ellenőrizve és tesztelve
- **.env.example konzisztencia**: Frissítve hogy CUTTING_BASE_URL=5004 (nem 5005)

## Session tapasztalatok
- PM2 alatt fut az Orchestrator root-ként, ezért sudo -u root -i pm2 restart kell
- Build után mindig restart szükséges a változások érvényesítéséhez
- Backend 404 response normális ha nincs adat - a routing működik ha 404-et kapunk (nem 502)
