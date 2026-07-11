---
id: definition_of_done
title: "Definition of Done (DoD)"
description: "Defines the mandatory acceptance criteria that every backlog item must satisfy before it can be considered complete."
type: standard
scope: global
last_updated: 2026-01-29
---

## Definition of Done (DoD)

A backlog item is considered **done** only when ALL of the following criteria are met.

---

## 1. Functionality (mandatory)

- ✅ The feature works according to the specification
- ✅ The code builds successfully (`dotnet build` / `npm run build`)
- ✅ No lint or type errors
- ✅ The affected UI can be tested manually (on dev server)

---

## 2. Testing (mandatory)

Tests for all code touched in the iteration:

- ✅ **Happy path** — success scenario covered
- ✅ **Error / edge cases** — invalid inputs, boundary conditions
- ✅ **Input validation** (where relevant)
- ✅ **All tests GREEN** (`dotnet test` PASS)

---

## 3. Documentation (mandatory)

**STATE.md updated:**

- What was completed
- What decisions were made
- What the next step is

**Implementation plan updated** (epic-scoped plan, e.g. `docs/{project}/epics/{EPIC}/tasks/{ID}.md` or `epics/*/tasks/{ID}.md`):

- Errors documented (if any)
- Deviations from the plan (if any)

---

## 4. Git (mandatory)

- ✅ Exactly **1 commit** created
- ✅ The commit is:
  - Small in size (1 backlog item = 1 commit)
  - Tied to a single backlog item
  - Uses the correct commit message format:

    ```text
    <type>(<scope>): <description>
    ```

---

## 5. Iteration Close (mandatory)

- ✅ The agent **STOPS**
- ✅ Does not begin the next backlog item

---

## 6. UI Visual Gate (if UI was affected)

If any UI code was modified, ALL of the following must pass:

| Criterion | Command | Status |
| ----------- | --------- | -------- |
| Build successful | `npm run build` | ✅ PASS |
| No lint errors | `npm run lint` | ✅ PASS |
| Type-check OK | `npm run type-check` | ✅ PASS |
| No console errors | Verified in dev server | ✅ OK |
| Visually functional | Interactions tested | ✅ OK |

---

## 7. Persistence Epic Requirements (if database schema is affected)

When an Epic involves database schema changes (Entity Framework, migrations, etc.):

### Database Schema Management

- [ ] **EF Core Migrations Created:** All schema changes captured in versioned migrations
- [ ] **Migrations Applied Locally:** Developer validated migration application (clean DB test)
- [ ] **Migration Rollback Strategy:** Documented rollback procedure (if production deployment)
- [ ] **Database Policy Documented:** .gitignore rules, local DB handling, CI validation

### Testing Requirements

- [ ] **Integration Tests for Migrations:** Migration application tested in CI
- [ ] **Repository CRUD Tests:** Entity CRUD operations validated via integration tests
- [ ] **Clean Environment Test:** Migration tested on fresh database (no manual schema changes)

### CI/CD Integration

- [ ] **Migration Generation Workflow:** CI workflow for `dotnet ef migrations add` (if applicable)
- [ ] **Migration Validation Step:** CI validates migration application on test DB
- [ ] **Build Stability:** No regression in API/app startup after migration

### Documentation

- [ ] **Migration Strategy:** Document migration approach in Epic plan
- [ ] **Seeding Strategy:** Document test data seeding (if applicable)
- [ ] **Production Deployment Plan:** Migration execution plan for production (if applicable)

**Rationale:** Epic 08 demonstrated the importance of structured migration workflows. These criteria ensure persistence Epics maintain quality and reduce deployment risks.

**Example Epic:** [Epic 08 - Persistence & CI/CD](../../joinerytech-flow/delivery/epics/EPIC-08/state.md)

---

## Version History

| Version | Date | Changes |
| -------- | ------- | ------------ |
| v1.0 | 2026-01-20 | Initial version |
| v2.0 | 2026-01-27 | Typo fixes, structure clarification, sync with runbook |
| v3.0 | 2026-02-18 | Persistence Epic Requirements section (Epic 08 calibration) |
