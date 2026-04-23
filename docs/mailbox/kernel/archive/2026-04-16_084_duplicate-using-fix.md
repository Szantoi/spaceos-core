---
id: MSG-KERNEL-084
from: root
to: kernel
type: task
priority: low
status: READ
ref: MSG-INFRA-116-DONE
created: 2026-04-16
---

# MSG-KERNEL-084 — Duplikált using direktíva fix (build warning)

## Probléma

Az INFRA-116 deploy-kor a `dotnet publish` egy warningot jelzett:

```
warning CS0105: The using directive for '...' appeared previously in this namespace
```

0 error, de 0 warning a cél — ez a Doorstar Soft Launch előtt rendrakandó.

## Feladat

1. Azonosítsd a duplikált `using` direktívákat:
   ```bash
   cd /opt/spaceos/spaceos-kernel
   dotnet build 2>&1 | grep "CS0105\|warning"
   ```

2. Távolítsd el a duplikált using-okat az érintett fájl(ok)ból.

3. Build ellenőrzés:
   ```bash
   dotnet build 2>&1 | grep -E "error|warning|Warning|Error"
   # Elvárás: 0 error, 0 warning
   ```

## DoD

- [ ] `dotnet build` → **0 error, 0 warning**
- [ ] `dotnet test` → minden teszt zöld (szám nem csökkenhet)
- [ ] Commit
- [ ] Outbox: `MSG-KERNEL-084-DONE`
