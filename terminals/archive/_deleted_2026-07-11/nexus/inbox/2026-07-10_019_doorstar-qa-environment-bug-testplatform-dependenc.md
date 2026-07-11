---
id: MSG-NEXUS-019
from: root
to: nexus
type: task
priority: critical
status: READ
created: 2026-07-10
content_hash: 426ee7f28761522b6fc754cf38236b930fb1eb962eeca045b02a76a9657e801e
---

# Doorstar QA Environment Bug - TestPlatform dependency failure

## CRITICAL PATH BLOCKER - Doorstar Soft Launch

A Backend QA integration tesztek nem futnak dependency resolution hiba miatt.

**Hiba:**
```
Microsoft.TestPlatform.CommunicationUtilities (18.3.0) + Docker.DotNet (3.125.15)
missing from testhost despite CopyLocalLockFileAssemblies=true
```

**Környezet:**
- .NET SDK 10.0.203
- Docker running
- NuGet cache OK

**Feladat:**
1. Diagnózis: miért nem másolódnak a DLL-ek a testhost-ba?
2. Fix: `CopyLocalLockFileAssemblies`, `PrivateAssets` beállítások ellenőrzése
3. Ha SDK probléma: jelezd VPS Operator felé

**Evidence file:** `/opt/spaceos/terminals/backend/outbox/2026-07-10_195-BLOCKED_qa-integration-tests-env-issue.md`

## Acceptance Criteria

- [ ] Doorstar E2E tesztek futnak zöldben
- [ ] CP-DOORSTAR-QA checkpoint DONE
- [ ] Nincs dependency resolution error
