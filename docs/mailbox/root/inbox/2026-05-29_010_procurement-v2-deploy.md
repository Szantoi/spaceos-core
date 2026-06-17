---
id: MSG-ROOT-010
from: root
to: operator
type: task
priority: high
status: READ
ref: MSG-PROCUREMENT-012-DONE
created: 2026-05-29
---

# Procurement v2 — VPS deploy (PR-M1..M8 + service restart)

A Procurement v2 implementáció kész (136 teszt, commit `26a05d1`).
A `ManualMigrations/` mappában lévő SQL fájlok még nem futottak a VPS-en.

**Repo:** `/opt/spaceos/backend/spaceos-modules-procurement`

---

## 1. Migration SQL fájlok futtatása (sorban)

```bash
cd /opt/spaceos/backend/spaceos-modules-procurement

# Ellenőrzés: mi van a DB-ben jelenleg
sudo -u postgres psql -p 5433 -d spaceos \
  -c "SELECT table_name FROM information_schema.tables WHERE table_schema='spaceos_procurement' ORDER BY 1;"
```

A migrációkat sorban kell futtatni:

```bash
for f in ManualMigrations/PR-M1_worker_role.sql \
          ManualMigrations/PR-M2_purchase_requisition.sql \
          ManualMigrations/PR-M3_supplier_invoice.sql \
          ManualMigrations/PR-M4_invoice_match.sql \
          ManualMigrations/PR-M5_price_list.sql \
          ManualMigrations/PR-M6_delivery_line.sql \
          ManualMigrations/PR-M7_outbox_inbox.sql \
          ManualMigrations/PR-M8_audit_log.sql; do
  echo "=== $f ==="
  sudo -u postgres psql -p 5433 -d spaceos -f "$f"
done
```

> ⚠️ **delivery_line backfill:** A meglévő `delivery_line` sorokon `InventorySyncStatus = 'NotApplicable'` lesz (PR-M6). Az új sorokon `Pending`. Ez szándékos.

> ⚠️ Ha `spaceos_procurement_worker` role már létezik (PR-M1): az `IF NOT EXISTS` véd ellene.

---

## 2. Procurement service rebuild + publish

```bash
cd /opt/spaceos/backend/spaceos-modules-procurement
git pull
dotnet publish src/SpaceOS.Modules.Procurement.Api -c Release -o /tmp/procurement-publish/ 2>&1 | tail -5
sudo cp -r /tmp/procurement-publish/. /opt/spaceos/backend/spaceos-modules-procurement/publish/
sudo chown -R spaceos:spaceos /opt/spaceos/backend/spaceos-modules-procurement/publish
```

---

## 3. Worker connection string env frissítés

A `ProcurementIntegrationWorker` külön BYPASSRLS connection stringet igényel:

```bash
# Ha még nincs a procurement env-ben:
sudo grep "ProcurementWorkerConnectionString\|WorkerConnection" /etc/spaceos/procurement.env

# Ha hiányzik — hozzáadás (jelszó a spaceos_procurement_worker role jelszava):
WORKER_PASS=$(sudo -u postgres psql -p 5433 -tc \
  "SELECT rolpassword FROM pg_authid WHERE rolname='spaceos_procurement_worker';" 2>/dev/null || echo "ISMERETLEN")
echo "Worker pass: $WORKER_PASS"
# Ha ismeretlen: ALTER ROLE spaceos_procurement_worker PASSWORD '<új jelszó>' és írj az env-be
```

> Ha a `spaceos_procurement_worker` role még nem rendelkezik jelszóval (PR-M1 jelszó nélkül hozza létre):
> ```bash
> WORKER_PASS=$(openssl rand -hex 20)
> sudo -u postgres psql -p 5433 -d spaceos \
>   -c "ALTER ROLE spaceos_procurement_worker PASSWORD '$WORKER_PASS';"
> echo "ProcurementWorkerConnectionString=Host=localhost;Port=5433;Database=spaceos;Username=spaceos_procurement_worker;Password=$WORKER_PASS" \
>   | sudo tee -a /etc/spaceos/procurement.env
> ```

---

## 4. Restart + ellenőrzés

```bash
sudo systemctl restart spaceos-procurement
sleep 5
sudo systemctl status spaceos-procurement --no-pager | head -5
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5006/healthz
sudo journalctl -u spaceos-procurement -n 20 --no-pager | grep -E "fail|Error|started|Listening"
```

---

## 5. Smoke test — új endpointok

```bash
INTERNAL_SECRET=$(cat /tmp/spaceos_internal_secret.txt)

# from-reorder-alert receiver (loopbackon IGEN)
curl -s -o /dev/null -w "%{http_code}" \
  -X POST http://127.0.0.1:5006/procurement/internal/from-reorder-alert \
  -H "Authorization: Bearer $INTERNAL_SECRET" \
  -H "X-SpaceOS-TenantId: 63ef28b6-a43b-4d3f-a076-759a47911559" \
  -H "Content-Type: application/json" \
  -d '{"tenantId":"63ef28b6-a43b-4d3f-a076-759a47911559","materialCode":"NONEXISTENT","currentStock":0,"reorderPoint":5,"suggestedQuantity":10,"unitOfMeasure":"pcs","alertedAt":"2026-05-29T00:00:00Z"}'
# → 422 (orphan materialCode — helyes viselkedés)

# Procurement internal NEM elérhető kívülről
curl -s -o /dev/null -w "%{http_code}" \
  https://joinerytech.hu/procurement/internal/from-reorder-alert
# → 404
```

---

## Referencia

- `docs/knowledge/deployment/KNOWN_GOTCHAS.md` — manual migration, role jelszó, diagnózis
- `/spaceos-deploy` skill
