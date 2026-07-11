---
id: qa-handoff-template
title: "QA Handoff & Test Plan Template"
description: "Structured handoff document from development to QA. Covers implementation summary, test scenarios (E2E), build status, API documentation, deliverables checklist, and sign-off."
type: template
category: qa
purpose: general-project-qa-handoff
last_updated: 2026-03-01
---

# 🧪 QA Handoff & Test Plan Template

> **Template Célja:** Strukturált keretrendszer a fejlesztésből a QA-ba történő átadáshoz.
>
> **Használat:** Másolj máshova és töltsd ki az irányelvek szerint. A szögletes zárójelek `[ ]` helyett saját tartalmad.

---

## Executive Summary

### Status

**Overall Status:** ✅ **READY FOR QA TESTING**

```
Completed: [x]/[y] Critical Tasks
Build Status: [✅/❌]
Test Status: [x]/[y] Passing
Blockers Released: [y] components
Target Go-Live: [DATE]
```

### Quick Facts

| Metric | Value |
|--------|-------|
| **Tasks Completed** | [X critical path items] |
| **Build Errors** | [0 / List any if present] |
| **Unit Tests** | [ ]/[ ] Passing |
| **Components Added** | [Number] |
| **Breaking Changes** | [Yes/No - List if yes] |
| **Dependencies Updated** | [Yes/No] |

---

## 📋 Completed Implementation Summary

> **Instrukció:** Foglalj össze rétegről-rétegre (layers) mit fejlesztettél.
> Adj meg fájl-útvonalakat és funkciókat.

### Layer 1: [LAYER_NAME] (e.g., Data Access, Domain, API)

**Purpose:** [Mi a réteg célja?]

**Files Modified/Created:**

- ✅ `[path/to/file1.cs]` - [Brief description]
- ✅ `[path/to/file2.cs]` - [Brief description]

**Key Changes:**

- Feature/Method 1: [What was implemented]
- Feature/Method 2: [What was implemented]

---

### Layer 2: [LAYER_NAME]

**Purpose:** [Description]

**Files Modified/Created:**

- ✅ `[path/to/file.cs]` - [Description]

**Key Changes:**

- [Implementation detail]

---

### Layer 3: [LAYER_NAME]

**Purpose:** [Description]

**Files Modified/Created:**

- ✅ `[path/to/file.cs]` - [Description]

**Key Changes:**

- [Implementation detail]

---

## 🧪 Test Scenarios (E2E)

> **Instrukció:** írj le functional test case-eket. Használj ezt a formátumot minden teszthez:
>
> **Test Case [X.Y]:** [Clear Name]
>
> ```
> Setup: [What needs to be prepared]
> Request: [What endpoint/action is called]
> Expected Response: [Status code, data structure]
> Verify: [What to check in the database or response]
> ```

### Feature Group 1: [FEATURE_NAME]

#### Test Case 1.1: [Happy Path]

```
Setup: [Preparation steps]
Request: [HTTP method, endpoint, or action]
Expected Response: [Status code + likely response body]
Verify: [Database state, cache, side effects]
```

#### Test Case 1.2: [Error Path - Missing Data]

```
Setup: [Conditions]
Request: [Call with incomplete data]
Expected Response: [400/422 Bad Request]
Verify: [Error message is helpful]
```

#### Test Case 1.3: [Error Path - Not Found]

```
Setup: [Non-existent resource]
Request: [Try to access/delete]
Expected Response: [404 Not Found]
Verify: [No side effects, safe failure]
```

#### Test Case 1.4: [Edge Case - Idempotency]

```
Setup: [First action succeeds]
Request: [Same action repeated]
Expected Response: [Idempotent or explicit error]
Verify: [No duplicates, no data corruption]
```

---

### Feature Group 2: [FEATURE_NAME]

#### Test Case 2.1: [Happy Path]

```
Setup: [Preparation]
Request: [Action]
Expected Response: [Status]
Verify: [State change]
```

#### Test Case 2.2: [Error Case]

```
Setup: [Precondition not met]
Request: [Action]
Expected Response: [Error status]
Verify: [Error message]
```

---

## 🔗 Build & Test Status

### Current Status

```
✅ Build: PASSING (with [X] warnings - list if notable)
✅ Unit Tests: [X]/[Y] PASSING
✅ [Other Test Suite]: [X]/[Y] PASSING
⚠️  Warnings: [List any non-blocking warnings]
```

### How to Rebuild

```powershell
# Full build
cd "[PROJECT_ROOT]"
[BUILD_COMMAND] build

# Run all tests
[TEST_COMMAND] test

# Expected output:
# Test summary: total: [X]; failed: 0; succeeded: [X]; skipped: 0
```

### Test Duration

- Build time: ~[X] seconds
- Test time: ~[X] seconds
- Total: ~[X] seconds

---

## 📚 API Documentation

> **Instrukció:** Ha van Swagger/OpenAPI, tedd ide az elérési útvonalat.

### Swagger/OpenAPI Access

```
Local URL: [http://localhost:PORT/swagger/index.html]
OpenAPI JSON: [http://localhost:PORT/swagger/v1/swagger.json]
```

### New Endpoints

| Method | Endpoint | Purpose | Response |
|--------|----------|---------|----------|
| [GET/POST/DELETE] | `/api/[resource]/[action]` | [Description] | [200/201/204] |
| [GET/POST/DELETE] | `/api/[resource]/[action]` | [Description] | [200/201/204] |

### Endpoint Details

**[Endpoint Name]**

- **Description:** [What it does]
- **Auth Required:** [Yes/No]
- **Success Response:** [Status code + example JSON]
- **Error Responses:** [400, 404, 409, etc.]

```json
// Example response
{
  "id": "uuid",
  "status": "value",
  "message": "Description"
}
```

---

## 📊 Deliverables Checklist

### Code Changes

- [ ] [Feature 1 implementation]
- [ ] [Feature 2 implementation]
- [ ] [Database schema changes if any]
- [ ] [API endpoint additions]
- [ ] [DTO/Model changes]
- [ ] [Validation logic]

### Tests

- [ ] [Unit tests written]
- [ ] [Integration tests written]
- [ ] [Edge case coverage]
- [ ] [Error handling verified]
- [ ] [All tests passing]

### Documentation

- [ ] [Code-level comments]
- [ ] [OpenAPI/Swagger docs]
- [ ] [README updates]
- [ ] [Architecture decisions documented]
- [ ] [Migration guide if data-affecting]

### Quality

- [ ] No build errors
- [ ] No compiler warnings (or approved exceptions)
- [ ] Code review completed
- [ ] Performance impact assessed

### Project Management

- [ ] Tasks marked complete in backlog
- [ ] Blockers documented or resolved
- [ ] Dependent teams notified
- [ ] Acceptance criteria met

---

## 🎯 QA Focus Areas

### Critical Path (Must Test First)

1. **[Critical Feature 1]**
   - [Test case A]
   - [Test case B]
   - [Error scenario]

2. **[Critical Feature 2]**
   - [Test case A]
   - [Test case B]

3. **[Data Integrity]**
   - [Verify state changes]
   - [Verify no data loss]
   - [Verify cascading deletes if applicable]

### Standard Coverage (Should Test)

- [ ] Happy paths with valid data
- [ ] Error handling (400, 404, 409, etc.)
- [ ] Boundary conditions
- [ ] Concurrent operations if applicable
- [ ] JSON serialization consistency

### Optional Follow-ups (Can Test Later)

- [ ] [Performance testing]
- [ ] [Load testing]
- [ ] [Security penetration testing]
- [ ] [Accessibility testing]

---

## 📝 Known Issues & Notes

### Breaking Changes

- [ ] [Change 1 - Mitigation]
- [ ] [Change 2 - Migration plan]

### Known Limitations

- [ ] [Limitation 1]
- [ ] [Limitation 2]
- [ ] [Planned for future sprint]

### Design Decisions

**[Decision 1]:** [Explanation + Rationale + Alternative considered]

**[Decision 2]:** [Explanation + Rationale]

### Compatibility Notes

- **Backward Compatible:** [Yes/No]
- **Database Migration:** [Required/Not required]
- **APIs Breaking:** [List any breaking API changes]

---

## 🔄 Dependencies & Blockers

### Released Blockers

- ✅ [Feature X in Sprint Y] - NOW UNBLOCKED
- ✅ [Related component] - Dependency resolved

### New Blockers Created

- [ ] [Issue 1] - [Mitigation]
- [ ] [Issue 2] - [Planned resolution]

### External Dependencies

- [ ] [External service/library] - Version: [X.Y.Z]
- [ ] [Third-party integration] - Status: [Active/Pending]

---

## 📞 Contact & Questions

### Primary Contact

- **Developer:** [Name/Role]
- **Response Time:** [Hours]
- **Available:** [Days/Hours]

### If You Find Issues

1. Check the implementation summary above
2. Review test scenarios for expected behavior
3. Verify against OpenAPI documentation
4. Contact developer with:
   - Exact steps to reproduce
   - Expected vs. actual result
   - Error messages/logs

### Escalation Path

- **Critical (Build Breaking):** [Contact immediately]
- **High (Functional Blocker):** [Contact within X hours]
- **Medium (Minor Issue):** [Schedule review]
- **Low (Documentation):** [Can wait for next sprint]

---

## ✅ Sign-Off

| Role | Name | Date | Status |
|------|------|------|--------|
| Backend Developer | [Name] | [Date] | Ready for QA |
| QA Lead | [Name] | [Date] | [✅ / ⏳] Accepted |
| Tech Lead | [Name] | [Date] | [✅ / ⏳] Approved |

---

## Appendices

### A. Environment Setup

**Required Services:**

- [Service 1]: [How to start]
- [Service 2]: [How to start]

**Database:**

- [Connection string template]
- [Seed data location]

### B. Common Test Data

```
User ID: [UUID or standard ID]
Project ID: [Standard test project]
Resource URLs: [Common test resources]
```

### C. Related Documentation

- [Link to Architecture doc]
- [Link to API specification]
- [Link to Database schema]
- [Link to Sprint planning]

---

**Template Last Updated:** 2026-02-13
**Version:** 1.0
**Author Role:** Backend Developer

> 💡 **Tip:** This template is general. Customize it for your specific project as needed.
