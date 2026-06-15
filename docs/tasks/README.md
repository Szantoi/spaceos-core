# SpaceOS — Feladatok (Root nézet)

## Státuszok

| Mappa | Jelentés |
|---|---|
| `new/` | Beérkezett, root még nem döntött / nem adta ki |
| `active/` | Ki van adva terminálnak vagy operátornak, vár |
| `archive/` | DONE + elfogadott, lezárt |

---

## Jelenlegi állapot (2026-06-16 — FE-048 DONE ✅ · 489 FE teszt · MfgPrep+Supervisor live · FE-049 + FE2-001/002 aktív · Marveen migráció indul)

### Architektúra

```
/opt/spaceos/
  backend/         ← 8 futó service + 3 NuGet lib + Orchestrator (AI gateway)
  frontend/        ← joinerytech-portal (247 teszt, dist deployed)
  docs/            ← dokumentáció
  keycloak/        ← IdP (Doorstar userek konfigurálva)
  tools/           ← dispatcher
```

### Lezárt milestoneok

- Soft Launch (Doorstar) ✅ 2026-04-20
- Bugfix Sprint ✅ (E2E 266/266)
- Growth Strategy v1 Sessions 1-5 ✅
- FreeTier API ✅ (176 teszt)
- Cabinet 0.1–0.3 ✅ (755 teszt, 10 NuGet)
- Cutting Phase 3–6 ✅ (931 teszt)
- PartnerTier MVP ✅ (232 teszt)
- Manufacturing Phase 1 ✅ (250 teszt)
- Portal World (5 world) + FE-026 mock data ✅ (242 teszt)
- **FE-027–035** ✅ — Portal újraépítés, auth, API integrációk, 247/247 teszt
- **Cabinet 0.3** ✅ — Track A+B+C lezárva, v0.3.0-alpha.1 (755 teszt)
- **NAGY ÁTALAKÍTÁS** ✅ — mappa restructure, Orchestrator = AI gateway only (121 teszt)
- **Site live** ✅ — https://joinerytech.hu elérhető (2026-04-30)
- **Doorstar seed** ✅ — tenant, facility, 5 workstation, 5 template, 9 KC user (2026-05-27)
- **Keycloak SMTP** ✅ — Brevo, noreply@joinerytech.hu, domain hitelesítve, reset password on (2026-05-27)
- **Orchestrator → systemd** ✅ — PM2 root helyett spaceos user, AI gateway only (2026-05-27)
- **Identity modul v1** ✅ — 63 teszt, 5 track, Clean Architecture + Outbox + Redis (2026-05-27)
- **P0-1 JWT RS256** ✅ — Cutting+Inventory+Procurement Authority+ValidateIssuer fix (2026-05-27)
- **Identity DEPLOYED** ✅ — 5008, systemd, nginx `/identity/`, spaceos_identity DB, KC client (2026-05-27)
- **FE-037 Identity UsersPanel** ✅ — kétoszlop, SlideOver részletek + meghívás, 258 teszt (2026-05-28)
- **ADR-039 Kernel** ✅ — `GET /api/internal/tenants/{id}` 1186 teszt, commit `c70a359` (2026-05-28)
- **ADR-039 Joinery** ✅ — `POST /joinery/internal/orders/from-quote` 420 teszt, commit `da7199f` (2026-05-28)
- **Sales modul v4** ✅ — 102 teszt, commit `2ab1586`, Track A–G kész, VPS deploy szükséges (2026-05-28)
- **FE-037** ✅ — Identity UsersPanel, 258 teszt (2026-05-28)
- **FE-038** ✅ — Sales Phase 1 lista bekötés + CreateCustomer, 271 teszt (2026-05-28)
- **FE-039** ✅ — Sales Phase 2 SlideOver-ök (Quote/Customer/Create) + useSalesDetail, 304 teszt (2026-05-28)
- **FULL DEPLOY** ✅ — Kernel M-0031 + Joinery J-003 + Sales S-001/002/003 + FE · minden smoke teszt zöld (2026-05-28)
- **Procurement v2** ✅ — 136 teszt · PurchaseRequisition+SupplierInvoice+ThreeWayMatch+PriceList · commit `26a05d1` (2026-05-29)
- **Procurement v2 deploy bug #1–2** ✅ — SqlQueryRaw AS "Value" alias + Worker BYPASSRLS factory · commit `96a51e3` (2026-05-29)
- **Procurement v2 deploy bug #3** ✅ — from-reorder-alert GUC fix (INSERT RETURNING RLS) · commit `c5f1292` (2026-05-29)
- **Procurement v2 DEPLOYED** ✅ — 136 teszt · live on 5006 · smoke test OK (2026-05-29)
- **FE-041** ✅ — Procurement v2 UI · RequisitionPanel+InvoicePanel+PriceListPanel · SoD · 3WM · 360 teszt (2026-05-29)
- **FE-042** ✅ — WorkflowPage NewOrderDrawer POST · flowEpicId prop · DetailPanel "Rendelés indítása" · 370 teszt (2026-05-29)
- **FE-043** ✅ — Settings TemplatesPanel · lista + SlideOver + mock fallback · 381 teszt (2026-05-29)
- **FE-044** ✅ — DesignPage paraméter wizard · ApiParamWizard + calculate + cutting-list preview · 391 teszt (2026-05-29)
- **FE-040** ✅ — Procurement v1 kiegészítések · PO detail + Delivery drawer + Supplier SlideOver + New PO · 330 teszt (2026-05-29)
- **FE-045** ✅ — Mobil menü · hamburger + slide-in drawer · scroll lock · close-on-nav · 391 teszt (2026-06-15)
- **Prototípus LIVE** ✅ — https://joinerytech.hu/proto/ · teljes UI spec · 27 világ · design reference (2026-06-15)
- **FE-046** ✅ — CRM világ (LeadPipeline+Kanban+OppList+Forecast) + Finance világ (ki/bejövő számlák+kifizetések) · 422 teszt · commit `9070dc0` (2026-06-15)
- **FE-047** ✅ — Projektek világ (Kanban+Lista+SlideOver) + Logisztika világ (fuvarok+stepper+FSM) · 455 teszt · commit `89da031` (2026-06-15)
- **FE-048** ✅ — MfgPrep világ (ReleaseQueue+Datasheets+SlideOver) + Supervisor világ (FloorView+AlertPanel+DayPlan) · 489 teszt · commit `e86bede` (2026-06-16)

### Backend tesztek: ~3902 | Frontend: 489

### 🔵 Active

| Task | Terminál | Leírás |
|---|---|---|
| MSG-FE-049 | FE-A | Törzsadatok + Kereskedelem + Belső tér világ |
| MSG-FE2-001 | FE-B | HR + Kontrolling világ (fut) |
| MSG-FE2-002 | FE-B | Raktár (bővített) + Reklamáció/Szerviz világ |

### 🟡 New

| Fájl | Megjegyzés |
|---|---|
| `joinerytech/` | Prototípus forrás (27 világ) — FE terminálok ide hivatkoznak |
| `bakery-project.md` | Pékség vertikál tervdok — következő üzleti vonal |
| `FE_Design_Requirements_2026.md` | Design sprint ✅ lezárva (2026-05-29) |

### Következő lépések (FE backlog — prioritás szerint)

| # | Feladat | Prototípus fájlok | Terminál |
|---|---|---|---|
| FE-046 | CRM + Finance világ | page-crm.jsx, page-finance.jsx | ✅ DONE |
| FE2-001 | HR + Kontrolling világ | page-hr.jsx, page-controlling.jsx | FE-B 🔵 |
| FE-047 | Projektek + Logisztika világ | page-projects.jsx, page-logistics.jsx | ✅ DONE |
| FE2-002 | Raktár (bővített) + Reklamáció | page-warehouse-2.jsx, page-service.jsx | FE-B (következő) |
| FE-048 | Gyártás-előkészítés (mfgprep) + Supervisor világ | page-mfg-prep.jsx, page-supervisor.jsx | ✅ DONE |
| FE2-003 | AI munkaterület + Dokumentumtár | page-ai.jsx, page-docs.jsx | FE-B |

### Többterminál-struktúra (2026-06-15 óta)

| Terminál | tmux session | Mailbox | Fókusz |
|---|---|---|---|
| FE-A | `fe-a` | `docs/mailbox/fe/` | CRM / Finance / Projektek / MfgPrep |
| FE-B | `fe-b` | `docs/mailbox/fe2/` | HR / Kontrolling / Raktár / AI |

### Monitoring

```bash
bash /opt/spaceos/scripts/fe-status.sh
```

Megmutatja: tmux session-ök, aktív task-ok, világ-implementáltság, következő lépések.
