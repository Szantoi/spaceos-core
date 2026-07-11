---
id: core-error-recovery
title: "Error Recovery Strategy"
description: "Defines the agent's behavior in error situations — including build failures, test failures, scope drift, and token limit — specifying retry limits, STOP criteria, and required report templates."
type: error_recovery
scope: global
last_updated: 2026-02-13
version: 1.2
---

# Error Recovery Strategy

**Version**: 1.2
**Last updated**: 2026-02-13
**Goal**: Define agent behavior in error situations (build error, test failure, scope drift, token limit).

---

## 1. Build Error (Compilation Error)

### Identification

**When does this occur?**

- `dotnet build` or `npm run build` command fails
- Compiler error messages (syntax error, missing reference, type mismatch)

### Strategy

#### Step 1: Analyze the Error

- Read the build error message (full stack trace)
- Identify the problem:
  - **Syntax error** → Fix it (e.g. missing `;`, closing `}`)
  - **Missing reference** → Install the dependency (`dotnet add package` or `npm install`)
  - **Type mismatch** → Check the types (DTO vs. Entity mapping)

#### Step 2: Apply the Fix

- Make the necessary modification
- Re-run `dotnet build` or `npm run build`

#### Step 3: Handle Failed Attempts

```text
Attempt 1: Fix + build
Attempt 2: Alternative fix + build
Attempt 3: STOP → Write error report
```

**STOP criterion**: After **3 consecutive failed builds**, STOP and write a detailed error report:

```markdown
## Build Error - 3 Failed Attempts

**Error type**: [Syntax / Missing reference / Type mismatch]
**File**: [ProjectsController.cs]
**Line**: [42]
**Error message**:

[Full compiler error stack trace]

**Attempts**:
1. [Description: What was the fix attempt?]
2. [Description: What was the second attempt?]
3. [Description: What was the third attempt?]

**Next steps**: Manual intervention required or detailed analysis needed.
```

---

## 2. Test Failure (Unit/Integration Test Error)

### Identification

**When does this occur?**

- `dotnet test` command fails
- One or more tests fail (red state)
- Assertion failure (expected vs. actual)

### Strategy

#### Step 1: Analyze the Test Error

- Read the test error message
- Identify the problem:
  - **Assertion mismatch** → Check the expectations (expected vs. actual)
  - **Exception thrown** → Check the implementation (NullReferenceException, ArgumentException)
  - **Test setup error** → Check mock configuration

#### Step 2: Apply the Fix

- **Fix the implementation** (if the test is correct but the implementation is wrong)
- **Modify the test** (if the test contains an incorrect expectation)

#### Step 3: Handle Failed Fix Attempts

```text
Fix attempt 1: Implementation change + dotnet test
Fix attempt 2: Alternative fix + dotnet test
STOP → After 2 failed fix attempts
```

**STOP criterion**: After **2 failed fix attempts**, STOP and write a detailed error report:

```markdown
## Test Failure - 2 Failed Fix Attempts

**Test name**: [ProjectWorkflowTests.TransitionToActive_ShouldSucceed]
**Test file**: [ProjectWorkflowTests.cs]
**Error message**:

Expected: ProjectStatus.Active
Actual: ProjectStatus.Draft

**Attempts**:
1. [Description: What was the fix attempt? E.g. Status transition logic change]
2. [Description: What was the second attempt? E.g. Validation rule change]

**Analysis**:
- Is the test expectation correct?
- Is the implementation logic correct?
- Is there a missing edge case?

**Next steps**: Manual review or detailed debugging required.
```

---

## 3. Scope Drift (Feature Expansion, Over-engineering)

### Identification

**When does this occur?**

- You implement a feature that is **NOT in the backlog item**
- You make a change that **does NOT belong to the current task**
- You add "nice-to-have" features on your own initiative

**Examples:**

- ❌ Backlog item: "ProjectsController CRUD" → You also implement `WorkTasksController` (not requested)
- ❌ Backlog item: "Project list view" → You add filtering and sorting (not requested)
- ❌ Backlog item: "Unit tests" → You also write E2E tests (not requested)

### Strategy

#### Step 1: Scope Check BEFORE EVERY STEP

Ask yourself:

- **Is this in the backlog item description?**
- **Is this in the DoD (Definition of Done)?**
- **Is this in the prompt template (if referenced)?**

**If NO**, then:

```text
⛔ STOP → Do not implement it!
```

#### Step 2: Ask in Ambiguous Cases

If it is unclear whether a functionality falls within scope:

```markdown
## Scope Clarification Request

**Backlog item**: [CORE-05: Create Material entity]
**Question**: Does the Material entity need a `Supplier` navigation property?

**Rationale**:
- The backlog item does not mention it
- The reference example (Project.cs) has a navigation property (WorkTasks)
- But for Material context, it is not clear

**Options**:
A) Implement the Supplier relationship (scope expansion)
B) Implement only the basic Material entity (minimal scope)

**My recommendation**: B) Minimal scope, Supplier added as a backlog item later.
```

#### Step 3: NEVER Invent New Features

❌ **PROHIBITED**:

- "I think we need an export to Excel feature" → NOT in the backlog, do NOT implement it
- "A notification system would be nice" → NOT in the backlog, do NOT implement it

✅ **ALLOWED**:

- Only implement what is **explicitly stated** in the backlog item, DoD, or prompt template

---

## 4. Token Limit Approaching

### Identification

**When does this occur?**

- The context window is approaching the limit (e.g. 100K tokens close to 128K limit)
- Many files have been read, much code has been generated

### Strategy

**Prioritization (descending order of importance):**

1. 🔴 **Working code** (most important)
   - The implementation must work (build success)
   - Basic functionality must be ready

2. 🟡 **Tests** (important)
   - Unit tests with basic coverage
   - If token limit is approaching → Only the critical path test (happy path + 1 error case)

3. 🟢 **Remaining DoD items** (nice-to-have)
   - XML documentation (if capacity allows)
   - Refactoring (if capacity allows)

4. ⚪ **Documentation update** (optional)
   - State.md update (if capacity allows)
   - If no capacity → State what was left out

**Token limit handling:**

```markdown
## Token Limit Warning

**Current token usage**: [95K / 128K]
**Remaining capacity**: [33K tokens]

**Prioritization**:
✅ Implementation complete (build success)
✅ Unit tests with basic coverage (happy path + 1 error case)
⚠️ XML documentation skipped (token limit)
⚠️ State.md update skipped (token limit)

**To be completed in the next iteration**:
- [ ] Add XML documentation
- [ ] Update State.md
```

---

## 5. When NOT to Stop (Allowed Continuation)

There are cases when you should **NOT** stop:

### ✅ Allowed Continuation

1. **Minor warning** (build succeeds but has warnings)
   - E.g. "unused variable" warning
   - You may continue, but fix it if simple (1–2 lines)

2. **Test skipped** (test did not run, but not a failure)
   - E.g. Integration test skipped (no DB)
   - You may continue, but note it in State.md

3. **Linter warning** (non-blocking)
   - E.g. "prefer const over let"
   - You may continue, but fix it if simple

### ❌ STOP Cases (you MUST stop here)

1. **Build failure** (after 3 failed attempts)
2. **Test failure** (after 2 failed fix attempts)
3. **Scope drift** (immediately, when detected)
4. **Critical token limit** (< 5K tokens remaining)

---

## 6. Error Handling Decision Tree

```text
┌─────────────────────────────────────┐
│   Error occurred?                   │
└────────┬────────────────────────────┘
         │
         ▼
    ┌────────┴────────┐
    │  Build error?   │
    └────────┬────────┘
         Yes │  No
         ▼   │
    ┌─────────────┐ ◄──────────┐
    │  Fix (1)    │            │
    └─────┬───────┘            │
          │                    │
      Success? ◄──────────┐    │
          │ No             │    │
          ▼                │    │
    ┌─────────────┐        │    │
    │  Fix (2)    │ ───────┘    │
    └─────┬───────┘             │
          │                     │
      Success?                  │
          │ No                  │
          ▼                     │
    ┌─────────────┐             │
    │  Fix (3)    │ ────────────┘
    └─────┬───────┘
          │
      Success?
          │ No
          ▼
    ┌──────────────────┐
    │ 🛑 STOP           │
    │ Write error report│
    └──────────────────┘

    ┌────────┴────────┐
    │  Test failure?  │
    └────────┬────────┘
         Yes │  No
         ▼   │
    ┌─────────────┐ ◄────┐
    │  Fix (1)    │      │
    └─────┬───────┘      │
          │              │
      Success?           │
          │ No           │
          ▼              │
    ┌─────────────┐      │
    │  Fix (2)    │ ─────┘
    └─────┬───────┘
          │
      Success?
          │ No
          ▼
    ┌──────────────────┐
    │ 🛑 STOP           │
    │ Write error report│
    └──────────────────┘

    ┌────────┴────────┐
    │  Scope drift?   │
    └────────┬────────┘
         Yes │  No
         ▼   │
    ┌──────────────────┐
    │ 🛑 IMMEDIATE STOP│
    │ Request scope    │
    │ clarification    │
    └──────────────────┘
         │
         ▼
    ┌────────────────────┐
    │  Token limit?      │
    └────────┬───────────┘
         Yes │  No
         ▼   │
    ┌────────────────────┐
    │ Prioritize         │
    │ 1. Working code    │
    │ 2. Tests           │
    │ 3. DoD             │
    │ 4. Documentation   │
    └────────────────────┘
         │
         ▼
    ┌────────────────────┐
    │ ✅ Continue        │
    │ (normal workflow)  │
    └────────────────────┘
```

---

## 7. Error Report Template

When you STOP, use this template:

```markdown
# 🛑 Error Recovery - Manual Intervention Required

**Error Type**: [Build Failure / Test Failure / Scope Drift / Token Limit]
**Backlog Item**: [CORE-05: Create Material entity]
**Epic**: [Epic 8]
**Date**: [2026-01-27]

---

## Error Details

**File**: [Material.cs]
**Line**: [42]
**Error Message**:

[Full error stack trace]

## Recovery Attempts

### Attempt 1

**Action**: [Description: What did you try?]
**Result**: [Failed - error message]

### Attempt 2

**Action**: [Description: Alternative solution]
**Result**: [Failed - error message]

### Attempt 3 (if applicable)

**Action**: [Description: Third attempt]
**Result**: [Failed - error message]

---

## Analysis

**Root Cause (if known)**: [Description: What is the root cause, if known]
**Hypothesis**: [Description: What might be the problem?]
**Blockers**: [Description: What is blocking you?]

---

## Recommended Next Steps

1. [Suggested solution 1]
2. [Suggested solution 2]
3. [Suggested solution 3]

---

## State at Error

**Build Status**: [Success / Failure]
**Test Status**: [Pass / Fail - X/Y tests passed]
**Uncommitted Changes**: [Yes / No - file list]
```

---

## 8. Known Issues / Gotchas

**Goal**: Document recurring errors across iterations so the agent does not run into the same problem repeatedly.

### 8.1 Mandatory Rule

> **Read this section at the START of EVERY iteration!**
> If you encounter an error that is already listed here → use the documented solution.

### 8.2 Known Build Errors

| ID | Error Pattern | Root Cause | Solution |
| ---- | --------------- | ------------ | ---------- |
| B001 | `CS0246: The type or namespace 'X' could not be found` | Missing using statement or NuGet package | 1) Check usings 2) `dotnet add package [PackageName]` |
| B002 | `CS0103: The name 'X' does not exist in the current context` | Typo in variable name or scope problem | Check name match and scope |
| B003 | `NU1101: Unable to find package` | NuGet restore did not run or wrong package name | `dotnet restore` then verify exact package name on nuget.org |

### 8.3 Known Test Errors

| ID | Error Pattern | Root Cause | Solution |
| ---- | --------------- | ------------ | ---------- |
| T001 | `System.NullReferenceException` at test runtime | Mock not configured or Setup() missing | Check that all dependencies are mocked |
| T002 | `Assert.Equal() Failure` but values appear identical | String whitespace difference or DateTime precision | Use `Assert.Equal(expected.Trim(), actual.Trim())` or for dates `Assert.Equal(..., precision: TimeSpan.FromSeconds(1))` |

### 8.4 Known Terminal / Console Errors

| ID | Error Pattern | Root Cause | Solution |
| ---- | --------------- | ------------ | ---------- |
| C001 | `'npm' is not recognized` | Node.js not in PATH or not installed | In PowerShell: check `$env:PATH`, or run `nvm use` |
| C002 | `ENOENT: no such file or directory` in npm | Wrong working directory | `cd` to the correct folder (JoineryTech.Flow.Web) |
| C003 | `dotnet: command not found` | .NET SDK not in PATH | Open a new terminal window or restart VS Code |

### 8.5 Project-Specific Gotchas (JoineryTech.Flow)

| ID | Context | Gotcha | Solution |
| ---- | ----------- | -------- | ---------- |
| P001 | Backend build | The API project references both Core and Infra | If you change Core, build Core first: `dotnet build JoineryTech.Flow.Core` |
| P002 | Frontend dev | Vite dev server on port 5173 | If occupied, check for running processes |
| P003 | Tests | The test project is a separate .csproj | Use `dotnet test JoineryTech.Flow.Core.Tests` for tests |

### 8.6 Adding a New Entry (Protocol)

When you encounter the same error **2+ times** across the same or different iterations:

1. Add it to the appropriate table
2. Format: `ID | Error pattern | Root Cause | Solution`
3. ID must be unique (B00X, T00X, C00X, P00X)

---

## 9. Related Documents

- [definition_of_done_standard.md](definition_of_done_standard.md) — Quality criteria
- [constraints.md](constraints.md) — Prohibited operations

---

## 10. Architectural and Strategic Error

### Identification

**When does this occur?**

- The Architect rejects the Sign-off (Status: REJECTED).
- A solution was produced that violates a fundamental architectural constraint (e.g. business logic placed in the UI layer).

### Strategy

#### Step 1: Root Cause Analysis (Architectural)

- Read the Architect's rationale in `architect_signoff.md`.
- Identify whether the plan (Tech Lead) or the implementation (Developer) was at fault.

#### Step 2: Strategic Rollback

- If the plan was at fault: The Tech Lead must open a new TASK to correct the error.
- If the implementation was at fault: The Developer must revert the non-compliant code and rewrite it according to the standards.

**STOP criterion**: If the strategic error is discovered at Epic closure, the entire Epic must be set to "Blocked" until the Architect and Tech Lead have re-calibrated the plan.

---

## Version History

| Version | Date | Changes |
| -------- | ------- | ------------ |
| v1.0 | 2026-01-27 | Initial version — Build, Test, Scope drift, Token limit strategies |
| v1.1 | 2026-01-27 | Added Known Issues / Gotchas section — recurring error documentation |
| v1.2 | 2026-01-27 | Added Architectural errors section |
