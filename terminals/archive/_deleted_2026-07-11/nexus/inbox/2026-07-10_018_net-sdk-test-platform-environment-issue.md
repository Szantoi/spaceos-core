---
id: MSG-NEXUS-018
from: backend
to: nexus
type: task
priority: high
status: UNREAD
created: 2026-07-10
content_hash: 4cfaccc771aa99f358f6c2d25ff8299294c06b09fbaf272e85bb863f5e359c39
---

# .NET SDK Test Platform Environment Issue

# .NET SDK VSTest Integration Broken

## Problem
Production QA Integration Tests (MSG-BACKEND-195) are **code-complete** (0 errors, builds successfully), but **test execution blocked** by persistent .NET SDK environment issue.

**Error:**
```
An assembly specified in the application dependencies manifest (testhost.deps.json) was not found:
  package: 'Microsoft.TestPlatform.CommunicationUtilities', version: '17.11.1-release-24455-02'
  path: 'Microsoft.TestPlatform.CommunicationUtilities.dll'
```

## Environment
- .NET SDK: 10.0.203 (primary), 8.0.419 (fallback)
- Test Framework: xUnit 2.9.3 + Testcontainers 3.9.0
- Docker: ✅ Running
- DLL exists: `/opt/dotnet/sdk/8.0.419/Microsoft.TestPlatform.CommunicationUtilities.dll`

## Investigation Completed
✅ Code verified correct (builds with 0 errors)
✅ Multiple clean/restore/rebuild cycles
✅ Forced .NET 8 SDK with global.json — same error
✅ Removed unused dependencies
✅ Docker daemon verified running

**Root Cause:** VSTest test host cannot resolve internal SDK assembly despite DLL presence in SDK directory.

## Requested Diagnosis
1. Check .NET SDK 10.0.203 installation integrity
2. Verify VSTest integration functional
3. Test if SDK 8.0.419 can run tests elsewhere
4. Recommend solution: reinstall SDK / use xUnit console runner / downgrade to stable SDK

**Full investigation details:** `/opt/spaceos/terminals/backend/outbox/2026-07-10_196-BLOCKED_qa-integration-tests-dotnet-sdk-issue.md`

## Acceptance Criteria

- [ ] Identify root cause of Microsoft.TestPlatform.CommunicationUtilities resolution failure
- [ ] Provide working solution for running .NET tests
- [ ] Verify dotnet test command can execute xUnit tests successfully
