---
id: dev-a-task-10-06-prompt
title: "Dev A — TASK-10-06: Error Handling & OWASP Validation"
epic: EPIC-10
milestone: M02
duration: 6 hours
start: "2026-03-09 09:00 UTC"
end: "2026-03-10 15:00 UTC"
target_ac: "20/20"
target_tests: "≥20 unit tests"
language: en
---

# 🎯 DEV A — TASK-10-06 Execution Prompt

## Phase 2 Context
- **Date:** 2026-03-09 to 2026-03-10 (6h, parallel with Dev B + C)
- **Team:** Dev A (error handling), Dev B (performance), Dev C (documentation)
- **Standup:** 09:00, 12:00, 18:00 UTC daily
- **Merge Gate:** 15:00 UTC on 2026-03-10 (or before)

---

## 🎯 Your Mission

Implement **strict input validation** and **standardized error handling** in `bootstrap_agent` to:
1. Prevent injection attacks (SQL, command, XSS, path traversal)
2. Standardize all error responses (format, codes, HTTP mappings)
3. Achieve ≥85% code coverage with unit + integration tests
4. Validate OWASP compliance (0 injection bypasses)

---

## 📋 Acceptance Criteria (AC) — 20 Items

### Input Validation (AC-1 through AC-8)

- [ ] **AC-1:** `domain` parameter matches `/^[a-z-]+$/` regex
  - Valid: `"engineering"`, `"joinerytech-design"`
  - Invalid: `"ENGINEERING"`, `"engineering!"`, `"123domain"`
  
- [ ] **AC-2:** `role` parameter matches `/^[a-z_]+$/` regex
  - Valid: `"backend_developer"`, `"qa_lead"`
  - Invalid: `"Backend Developer"`, `"qa-lead"`, `"123role"`

- [ ] **AC-3:** Invalid domain → `error_code: "invalid_domain"` + HTTP 400
  
- [ ] **AC-4:** Invalid role → `error_code: "invalid_role"` + HTTP 400

- [ ] **AC-5:** Missing required parameter → descriptive error message (no SQL revealed)

- [ ] **AC-6:** SQL injection attempt (e.g., `domain: "'; DROP TABLE roles; --"`) → blocked, HTTP 400

- [ ] **AC-7:** Command injection attempt (e.g., `domain: "$(rm -rf /)"`) → blocked, HTTP 400

- [ ] **AC-8:** Path traversal attempt (e.g., `role: "../../etc/passwd"`) → blocked, HTTP 400

### Error Response Standardization (AC-9 through AC-15)

- [ ] **AC-9:** All errors follow format:
  ```json
  {
    "success": false,
    "error_code": "ERROR_CODE",
    "error_message": "Human-readable message",
    "details": { /* optional */ }
  }
  ```

- [ ] **AC-10:** Success responses follow format:
  ```json
  {
    "success": true,
    "payload_version": "1.0",
    "identity": { "domain": "...", "role": "..." },
    ...
  }
  ```

- [ ] **AC-11:** Error codes are standardized: `invalid_domain`, `invalid_role`, `role_not_found`, `session_not_found`, `query_timeout`, `db_connection_error`, `data_integrity_error`

- [ ] **AC-12:** HTTP status mappings:
  - 400 Bad Request (client error: invalid params, injection)
  - 404 Not Found (role not found, session not found)
  - 504 Gateway Timeout (query timeout)
  - 500 Internal Server Error (DB connection, data integrity)

- [ ] **AC-13:** Error messages do NOT contain SQL queries, database paths, internal stack traces

- [ ] **AC-14:** Logging includes error context (timestamp, error_code, request params without secrets)

- [ ] **AC-15:** All error cases are documented in `ERROR_CODES.md` with HTTP mappings

### OWASP Compliance (AC-16 through AC-20)

- [ ] **AC-16:** OWASP injection matrix tested:
  - SQL injection (15+ payloads tested, 0 bypasses)
  - Command injection (10+ payloads tested, 0 bypasses)
  - XSS payloads (8+ payloads tested, 0 bypasses)
  - Path traversal (5+ payloads tested, 0 bypasses)

- [ ] **AC-17:** Prepared statements / parameterized queries used (no string concatenation in SQL)

- [ ] **AC-18:** Input validation happens BEFORE database queries

- [ ] **AC-19:** Rate limiting or circuit breaker for repeated invalid inputs (optional, but recommended)

- [ ] **AC-20:** All edge cases tested:
  - Empty string parameters
  - Unicode/UTF-8 characters
  - Very long strings (>1000 chars)
  - Null/undefined parameters

---

## 🛠️ Implementation Phases

### Phase 1: Input Validator (1.5h)

**Goal:** Create `src/mcp/InputValidator.ts`

```typescript
// Signature
export class InputValidator {
  validateDomain(domain: string): void  // throws on invalid
  validateRole(role: string): void      // throws on invalid
  validateInput(domain: string, role: string): void // combined
}

// Test: 5+ unit tests (happy path + 5 invalid cases)
```

**Deliverable:**
- [ ] `InputValidator.ts` created (strict regex validation)
- [ ] Unit tests: 5-8 test cases
- [ ] No injection bypasses

---

### Phase 2: Error Response Standardization (2h)

**Goal:** Modify `src/mcp/ErrorResponses.ts` and integrate into `BootstrapService`

```typescript
// Error response builder
export function buildErrorResponse(
  error_code: string,
  error_message: string,
  details?: object
): { success: false; error_code: string; ... }

// Success response builder
export function buildSuccessResponse(
  payload: BootstrapPayload
): { success: true; ... }

// HTTP status mapper
export function getHttpStatus(error_code: string): number
```

**Deliverable:**
- [ ] `ErrorResponses.ts` complete
- [ ] All 7 error codes defined + HTTP mappings
- [ ] BootstrapService.ts updated to use error builders
- [ ] Unit tests: 8-10 test cases
- [ ] Integration tests: 5-7 test cases (real error scenarios)

---

### Phase 3: OWASP Injection Validation (2h)

**Goal:** Create `src/tests/unit/owasp-injection.test.ts`

**Test Matrix:**

```typescript
const INJECTION_PAYLOADS = {
  sql: [
    "'; DROP TABLE roles; --",
    "1' OR '1'='1",
    "admin'--",
    // ... 12 more SQL payloads
  ],
  command: [
    "$(rm -rf /)",
    "`whoami`",
    "| cat /etc/passwd",
    // ... 7 more command payloads
  ],
  xss: [
    "<script>alert('xss')</script>",
    "javascript:alert(1)",
    // ... 6 more XSS payloads
  ],
  pathTraversal: [
    "../../etc/passwd",
    "..\\..\\windows\\system32",
    // ... 3 more path traversal payloads
  ]
};

// Each payload → should be rejected with 400 error
```

**Deliverable:**
- [ ] Injection test file with 40+ payloads
- [ ] All payloads tested → 100% blocked
- [ ] Zero injection bypasses ✅
- [ ] Performance test: validation < 5ms per call

---

### Phase 4: Testing & Coverage (0.5h)

**Goal:** Achieve ≥85% code coverage

**Coverage Breakdown:**
- `InputValidator.ts`: 100% (all branches)
- `ErrorResponses.ts`: 100% (all error codes + HTTP mappings)
- `BootstrapService.ts` (input validation touch points): ≥90%

**Deliverable:**
- [ ] Jest coverage report: ≥85%
- [ ] All critical paths tested
- [ ] Edge cases covered

---

## 📦 Files to Create/Modify

| File | Type | Lines | Purpose |
|:-----|:----:|:-----:|:--------|
| `src/mcp/InputValidator.ts` | NEW | ~80 | Regex-based validation |
| `src/mcp/ErrorResponses.ts` | MODIFY | +50 | Error builders + HTTP mapper |
| `src/mcp/BootstrapService.ts` | MODIFY | +20 | Integrate validators |
| `src/tests/unit/InputValidator.test.ts` | NEW | ~120 | Validation unit tests |
| `src/tests/unit/ErrorResponses.test.ts` | NEW | ~150 | Error response tests |
| `src/tests/unit/owasp-injection.test.ts` | NEW | ~180 | Injection payload tests |
| `src/tests/integration/bootstrap-error.test.ts` | NEW | ~120 | Integration tests |
| `database/standards/00-foundation/ERROR_CODES.md` | NEW | ~50 | Error catalog |

---

## 🧪 Test Strategy

### Unit Tests (12-15 tests)
- `InputValidator`: domain/role validation (happy + 5 invalid cases)
- `ErrorResponses`: error code mapping (7 codes) + HTTP status mapping

### Injection Tests (40+ scenarios)
- SQL (15 payloads)
- Command (10 payloads)
- XSS (8 payloads)
- Path traversal (5 payloads)
- All → 100% blocked expected

### Integration Tests (5-8 tests)
- Invalid domain → 400 + error_code
- SQL injection → 400 + error_code (no stack trace leaked)
- Missing parameter → 400 + descriptive message
- Valid bootstrap → 200 + success payload

### Edge Cases (3-5 tests)
- Empty strings
- Unicode characters (UTF-8)
- Very long inputs (>1000 chars)
- Null/undefined parameters

---

## 📊 Success Metrics

| Metric | Target | Check |
|:-------|:------:|:-----:|
| AC Completion | 20/20 | [ ] |
| Test Coverage | ≥85% | [ ] |
| Injection Bypasses | 0 | [ ] |
| Code Compilation | 0 errors | [ ] |
| TypeScript Strict | Yes | [ ] |

---

## 🔗 Key References

- [ERROR_CODES.md](../../database/standards/00-foundation/ERROR_CODES.md) — Centralized error definitions
- [PERFORMANCE-SLA.md](../../Docs/mcp-context-server/PERFORMANCE-SLA.md) — SLA reminder (validation must be <5ms)
- [bootstrap_agent.md](../../Docs/tools/bootstrap_agent.md) — Tool spec you're securing

---

## 📞 Daily Standups (Solo Format if Needed)

### Day 1 (2026-03-09)
**Goal:** Validator + ErrorResponses complete, 50% tests passing

**Standup at 09:00 UTC:**
- Yesterday: None (start day)
- Today: Build InputValidator + ErrorResponses (2-3h)
- Blockers: None expected

**Standup at 12:00 UTC:**
- Progress: InputValidator done, starting error response builder
- Next: ErrorResponses complete, unit tests

**Standup at 18:00 UTC:**
- Status: InputValidator + ErrorResponses done (2/4 phases)
- Tomorrow: Injection tests + integration tests

### Day 2 (2026-03-10)
**Goal:** OWASP tests complete, all 20 AC verified, ready for merge

**Standup at 09:00 UTC:**
- Yesterday: Validator + ErrorResponses done
- Today: OWASP injection matrix (40+ payloads) + integration tests
- Blockers: None expected

**Standup at 12:00 UTC:**
- Progress: 30+ injection payloads tested, 0 bypasses so far
- Next: Finish all 40 payloads, coverage report

**Standup at 18:00 UTC (PRE-MERGE CHECK):**
- Status: ALL 20 AC VERIFIED ✅
- Coverage: 87% (target ≥85%) ✅
- Injection matrix: 40+ payloads, 0 bypasses ✅
- Ready for merge? YES → Create PR, link to TASK-10-06

---

## 🎬 Quick Start Checklist (Copy-Paste Ready)

```
═══════════════════════════════════════════════════════════
  DEV A — TASK-10-06 QUICK START (6h, 20 AC)
═══════════════════════════════════════════════════════════

[ ] 09:00 Day 1: Read this prompt fully + review TASK-10-06.md
[ ] 09:30 Day 1: Create InputValidator.ts (regex validation)
[ ] 11:00 Day 1: Create unit tests for InputValidator
[ ] 12:00 Day 1: STANDUP (progress check)
[ ] 12:30 Day 1: Create ErrorResponses.ts (standardize errors)
[ ] 14:30 Day 1: Create integration tests (error scenarios)
[ ] 18:00 Day 1: STANDUP (50% checkpoint)
[ ] 18:30 Day 1: Prep OWASP injection matrix

[ ] 09:00 Day 2: OWASP injection tests (40+ payloads)
[ ] 11:00 Day 2: Verify 0 injection bypasses
[ ] 12:00 Day 2: STANDUP (coverage check)
[ ] 13:00 Day 2: Coverage report (target ≥85%)
[ ] 14:00 Day 2: Final AC verification (20/20)
[ ] 14:30 Day 2: Create PR + merge request
[ ] 18:00 Day 2: FINAL STANDUP (merge status)

═══════════════════════════════════════════════════════════
```

---

## 🚀 How to Execute

### Step 1: Branch Creation
```bash
git checkout -b dev-a/task-10-06-error-handling
```

### Step 2: Skeleton Files
```bash
touch src/mcp/InputValidator.ts
touch src/tests/unit/InputValidator.test.ts
touch src/tests/unit/owasp-injection.test.ts
```

### Step 3: Implementation Order (Phase by phase)
1. InputValidator (regex logic)
2. ErrorResponses (builders + HTTP mapper)
3. Injection tests (40+ payloads)
4. Integration tests
5. Coverage report

### Step 4: Continuous Testing
```bash
npm test -- src/mcp/InputValidator.test.ts --watch
npm test -- --coverage
```

### Step 5: Pre-Merge
```bash
npm test       # All tests passing?
npm run lint   # No TypeScript errors?
npm run build  # Compiles?
```

### Step 6: Create PR
- Title: `feat(TASK-10-06): Error handling & OWASP validation`
- Description: Link to TASK-10-06.md, AC checklist, coverage %
- Request review from: Tech Lead

---

## 📚 Implementation Guidance

### InputValidator Pattern
```typescript
export class InputValidator {
  private static readonly DOMAIN_REGEX = /^[a-z-]+$/;
  private static readonly ROLE_REGEX = /^[a-z_]+$/;

  static validateDomain(domain: unknown): void {
    if (typeof domain !== 'string') throw new Error('...');
    if (!this.DOMAIN_REGEX.test(domain)) throw new Error('...');
  }

  static validateRole(role: unknown): void {
    if (typeof role !== 'string') throw new Error('...');
    if (!this.ROLE_REGEX.test(role)) throw new Error('...');
  }
}
```

### Error Builder Pattern
```typescript
export function buildErrorResponse(
  code: string,
  message: string,
  details?: object
): ErrorResponse {
  return {
    success: false,
    error_code: code,
    error_message: message,
    ...(details && { details })
  };
}

export function getHttpStatus(errorCode: string): number {
  const MAP: Record<string, number> = {
    invalid_domain: 400,
    invalid_role: 400,
    role_not_found: 404,
    // ...
  };
  return MAP[errorCode] || 500;
}
```

### Integration Test Pattern
```typescript
test('Invalid domain → 400 + error_code', async () => {
  const response = await bootstrap_agent({
    domain: 'INVALID!',
    role: 'backend_developer'
  });
  expect(response.success).toBe(false);
  expect(response.error_code).toBe('invalid_domain');
  expect(response.statusCode).toBe(400);
});
```

---

## ⏱️ Time Budget (6 hours)

| Phase | Time | Actual |
|:------|:----:|:-----:|
| Phase 1: InputValidator | 1.5h | [ ] |
| Phase 2: ErrorResponses | 2.0h | [ ] |
| Phase 3: OWASP Tests | 2.0h | [ ] |
| Phase 4: Coverage | 0.5h | [ ] |
| **TOTAL** | **6.0h** | **[ ]** |

---

## ✅ Final Checklist Before Merge

- [ ] All 20 AC verified manually
- [ ] Jest coverage report: ≥85%
- [ ] 0 TypeScript errors
- [ ] 0 injection bypasses
- [ ] All tests passing (`npm test`)
- [ ] Code compiled (`npm run build`)
- [ ] PR created with clear description
- [ ] Tech Lead notified for review

---

**Ready to start? Copy this file, paste into your terminal, and execute!** 🚀

*Last Updated: 2026-03-06*

