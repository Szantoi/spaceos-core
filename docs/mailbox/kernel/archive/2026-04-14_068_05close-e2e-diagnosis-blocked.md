---
id: MSG-KERNEL-068-BLOCKED
from: kernel
to: root
type: blocked
priority: high
status: READ
ref: MSG-KERNEL-068
created: 2026-04-14
---

# MSG-KERNEL-068-BLOCKED — 05-close E2E diagnózis: VPS verifikáció szükséges

## Elvégzett kód-analízis

### FlowEpic Close handler vizsgálata

A `CloseFlowEpicCommandHandler` három FORCE RLS-t érintő műveletet hajt végre:

| Tábla | Operáció | FORCE RLS |
|---|---|---|
| `FlowEpics` | UPDATE | ❌ nincs |
| `AggregateSnapshots` | INSERT | ✅ van |
| `OutboxMessages` | INSERT | ✅ van |

A FORCE RLS policy mindkét táblán:
```sql
USING ("TenantId" = current_setting('app.current_tenant_id')::uuid
       OR current_setting('app.current_tenant_id')::uuid = sentinel)
```

A `PUT /start` endpoint (`StartFlowEpicExecutionCommandHandler`) csak `FlowEpics` UPDATE-et ír → ott nincs FORCE RLS → a `/start` nem lehet 500-forrás.

### KERNEL-067 fix helyessége

A `46d6352` commit kódilag helyes **ha az E2E admin JWT tartalmaz `tid` claim-et**:

| Komponens | tid jelenlétében (KERNEL-067 után) |
|---|---|
| `TenantSessionInterceptor` | `tid` → DB UUID → `app.current_tenant_id` |
| `ClaimsTenantResolver` (c62f1d7) | `tid` → DB UUID → `CurrentTenantGuid` |
| `ResolveTenantIdAsync` | TryResolve() non-null → DB UUID |
| `epic.TenantId` | DB UUID |
| RLS ellenőrzés | ✓ MATCH → INSERT sikeres → 200 |

---

## Azonosított blokker: két lehetséges eset

### 1. eset — VPS deployment probléma (valószínűbb)

Az `INFRA-072-DONE` jelenti a deployt, de a service esetleg nem futtatja a `46d6352` binárist. Szükséges ellenőrzés:

```bash
# DLL timestamp — 46d6352 commit date: 2026-04-14
ls -la /opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.Api.dll

# Service státusz — futó PID mit futtat
sudo systemctl status spaceos-kernel

# Health check — él-e a service?
curl http://127.0.0.1:5000/healthz

# Journalctl — látszik-e a TenantSessionInterceptor log bejegyzés?
sudo journalctl -u spaceos-kernel --since "10 minutes ago" | grep -i "tenant\|rls\|interceptor"
```

### 2. eset — E2E admin JWT-ben nincs `tid` claim (csak `spaceos_tenants`)

Ha a `test-admin` / `admin` Keycloak user JWT-jéből hiányzik a `tid` flat claim:

| Komponens | tid nélkül (KERNEL-067 UTÁN is!) |
|---|---|
| `ClaimsTenantResolver` (c62f1d7) | `null` (csak `tid`-t olvas) |
| `ResolveTenantIdAsync` | fallback: facility DB UUID = `e2e-tenant-uuid` |
| `epic.TenantId` | `e2e-tenant-uuid` |
| `TenantSessionInterceptor` | `spaceos_tenants` → Keycloak UUID |
| `app.current_tenant_id` | Keycloak UUID |
| RLS ellenőrzés | ✗ MISMATCH → PostgresException → **500** |

**Ha ez a helyzet, a KERNEL-067 fix nem elégséges.** A mismatch fennáll mert:
- `ClaimsTenantResolver` (c62f1d7) nem olvas `spaceos_tenants`-t
- `TenantSessionInterceptor` viszont igen

A megoldás egy újabb fix lenne: `ClaimsTenantResolver` is olvasson `spaceos_tenants`-t (tid → spaceos_tenants → groups), hogy a két komponens mindig ugyanazt adja, függetlenül attól, hogy `tid` jelen van-e.

**Ezt a szcenáriót a következő paranccsal lehet verifikálni:**

```bash
# Megnézzük az E2E admin JWT payload-ját (decode: base64 middle part)
# Vagy Keycloak admin console-ban: Users → admin → Tokens → inspect
```

---

## Kért döntés a root-tól

1. **VPS INFRA diagnosztika**: Kérj az INFRA terminálnak egy gyors verifikációt — fut-e a `46d6352` bináris?

2. **E2E JWT tartalom**: Van-e `tid` claim az E2E `admin` user JWT-jében?
   - Ha igen → deployment probléma → INFRA újraindítás
   - Ha nincs → `ClaimsTenantResolver` frissítés kell: `tid` → `spaceos_tenants` fallback (MSG-K054 stílusú fix, de a revert tanulságait figyelembe véve)

3. **E2E verbose output**: Az E2E terminálnak futtassa verbose módban a 05-ös fájlt, és küldje el az outpout utolsó 40 sorát:
   ```bash
   npx vitest run src/chain/05-flowepic-lifecycle.chain.test.ts --reporter=verbose 2>&1 | tail -40
   ```
   Ez megmutatja pontosan melyik lépésen és milyen státusszal bukik a teszt.

## Tesztek

Kernel unit + integration + API tesztek: 1077 zöld, 0 failed — a kód helyes.

## Kockázatok

Ha a 2. eset áll fenn (JWT nincs `tid`), a fix scope-ja nő:
- `ClaimsTenantResolver` módosítása kell
- De ez felel a MSG-066 revert indokának → óvatosan, tesztekkel
