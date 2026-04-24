---
id: MSG-ORCH-083
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: SpaceOS_Joinery_Phase3_Architecture_v1.md
created: 2026-04-24
---

# ORCH-083 — Joinery Phase 3 BFF routes (4 új proxy route)

> **Tervdok:** `docs/architecture/SpaceOS_Joinery_Phase3_Architecture_v1.md` Section 3
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** JOINERY-054 ✅ + INFRA-055 ✅ (Joinery API LIVE, nginx MinIO proxy OK)
> **Használhatsz sub-agent-eket** ha szükséges

---

## 4 új BFF route

A Doorstar Portal a `/bff/joinery/` route-on éri el a Joinery API-t. 4 új route kell a Phase 3 batch + anyaglista funkciókhoz.

### Route definíciók

```
POST   /bff/joinery/gyartasilap/batch              → POST   http://127.0.0.1:5002/api/gyartasilap/batch
GET    /bff/joinery/gyartasilap/batch/:id           → GET    http://127.0.0.1:5002/api/gyartasilap/batch/:id
GET    /bff/joinery/gyartasilap/batch/:id/download  → GET    http://127.0.0.1:5002/api/gyartasilap/batch/:id/download
POST   /bff/joinery/anyaglista                      → POST   http://127.0.0.1:5002/api/anyaglista
```

### Megjegyzések

1. **JWT forwarding:** A BFF a Bearer tokent forward-olja a Joinery API-nak — a Joinery `ManufacturerOnly` policy ellenőrzi
2. **Batch download (GET .../download):** A Joinery API 302-vel redirect-ol a presigned MinIO URL-re — a BFF ezt transparent forward-olja (ne interceptálja a redirect-et!)
3. **Anyaglista (POST):** Szinkron PDF generálás — a response `application/pdf` binary, a BFF ezt pass-through-olja
4. **Timeout:** A batch download timeout-ja 10s elég (presigned URL redirect gyors); az anyaglista generálás <1s

### Implementáció

Valószínűleg a meglévő `/bff/joinery/` route group-ba kell hozzáadni. Ellenőrizd:
```bash
grep -r "joinery\|gyartasilap\|5002" src/ --include="*.ts" | grep -v node_modules | head -20
```

## Tesztek (+4)

1. `POST /bff/joinery/gyartasilap/batch` → proxy forward + JWT
2. `GET /bff/joinery/gyartasilap/batch/:id` → proxy forward
3. `GET /bff/joinery/gyartasilap/batch/:id/download` → 302 redirect transparent forward
4. `POST /bff/joinery/anyaglista` → proxy forward + PDF response

## Definition of Done

- [ ] 4 BFF route regisztrálva
- [ ] JWT forward + ManufacturerOnly auth delegálva
- [ ] Batch download 302 redirect transparent
- [ ] `npm run build` 0 error
- [ ] `npm run test` ≥ 223 pass (219 előző + min 4 új)
- [ ] Outbox DONE
