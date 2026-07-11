# Reviewer Security Architecture — Immutability & Audit Trail

## Probléma: Frontmatter mutation TILOS

**Jelenlegi:**
```yaml
---
review_attempt: 2   # ← Egyszerűen módosítható, nincs audit trail
---
```

**Veszély:**
- Bárki átírhatja `review_attempt: 0`-ra → végtelen loop
- Nincs bizonyíték ki, mikor módosította
- SHA-256 hash hiányzik → integrity nem ellenőrizhető

---

## Megoldás: Immutable Review Log + Hash Verification

### 1. Review Decision Log (JSONL - append-only)

**Lokáció:** `logs/reviews/decisions.jsonl`

**Formátum:**
```json
{
  "timestamp": "2026-06-23T04:30:12.456Z",
  "review_id": "REV-2026-06-23-001",
  "inbox_file": "terminals/backend/inbox/2026-06-23_030_quote-api.md",
  "inbox_hash": "sha256:a3f2e1...",
  "done_file": "terminals/backend/outbox/2026-06-23_034_quote-api-done.md",
  "done_hash": "sha256:7d9c4b...",
  "task_type": "CODE",
  "review_attempt": 1,
  "reviewer_a": {
    "model": "claude-haiku-4-5",
    "verdict": "APPROVE",
    "feedback_hash": "sha256:..."
  },
  "reviewer_b": {
    "model": "claude-haiku-4-5",
    "verdict": "REJECT",
    "feedback_hash": "sha256:..."
  },
  "final_verdict": "REJECTED",
  "reject_inbox_created": "docs/mailbox/backend/inbox/2026-06-23_031_review-reject-xxx.md",
  "git_commit": "a3f2e1b4c5d6e7f8"
}
```

**Előnyök:**
- ✅ Append-only → nem módosítható (csak hozzáfűzhető)
- ✅ SHA-256 hash minden fájlra → integrity check
- ✅ Git commit hash → verziókövetés
- ✅ Teljes audit trail → ki, mikor, miért

---

### 2. Task Type Definitions (külön YAML fájlok)

**Lokáció:** `config/task-types/*.yaml`

**Bővíthetőség:** Új task type YAML fájl hozzáadásával

**Verziókezelés:** Minden task type config verziókövetett

---

### 3. Hash Verification Workflow

**Review előtt:**
```typescript
// 1. Compute hashes
const inboxHash = await sha256File(inboxPath);
const doneHash = await sha256File(donePath);

// 2. Check previous review attempts
const previousReviews = await queryReviewLog({
  inbox_hash: inboxHash
});

// 3. Detect tampering
if (previousReviews.some(r => r.done_hash !== doneHash)) {
  // DONE file módosítva → új review attempt
  reviewAttempt = previousReviews.length + 1;
}

// 4. Check max attempts
const taskTypeConfig = await loadTaskType(taskType);
if (reviewAttempt > taskTypeConfig.escalation_policy.max_attempts) {
  await escalateToRoot(inboxHash, doneHash, previousReviews);
  return;
}
```

**Review után:**
```typescript
// 1. Append to decision log (immutable)
await appendReviewDecision({
  review_id: generateReviewId(),
  timestamp: new Date().toISOString(),
  inbox_hash: inboxHash,
  done_hash: doneHash,
  task_type: taskType,
  review_attempt: reviewAttempt,
  ...reviewResults
});

// 2. Git commit (auto)
await gitCommit([
  'logs/reviews/decisions.jsonl',
  rejectInboxPath  // ha REJECT
], `review: ${verdict} - ${doneBase} (attempt ${reviewAttempt})`);
```

---

### 4. Integrity Check API

**Query review log:**
```bash
GET /api/reviews/history?inbox_hash=sha256:...
```

Response:
```json
{
  "inbox_hash": "sha256:a3f2e1...",
  "total_attempts": 2,
  "reviews": [
    {
      "review_id": "REV-2026-06-23-001",
      "attempt": 1,
      "verdict": "REJECTED",
      "timestamp": "2026-06-23T04:30:12Z"
    }
  ],
  "escalated": true,
  "escalation_msg": "MSG-ROOT-045"
}
```

**Verify file integrity:**
```bash
GET /api/reviews/verify?file=terminals/backend/outbox/xxx.md
```

Response:
```json
{
  "file": "terminals/backend/outbox/2026-06-23_034_quote-api-done.md",
  "current_hash": "sha256:7d9c4b...",
  "review_log_hash": "sha256:7d9c4b...",
  "integrity": "OK",
  "last_review": "REV-2026-06-23-002"
}
```

---

### 5. Task Type Bővíthetőség

**Új task type hozzáadása:**

1. Új YAML config létrehozása: `config/task-types/SECURITY_AUDIT.yaml`
2. Config reload: `POST /api/reviews/reload-task-types`
3. Inbox frontmatter használja: `task_type: SECURITY_AUDIT`

**Task type verziókezelés:**
- Minden task type config tartalmaz `version` mezőt
- `changelog` dokumentálja a változásokat
- Git history biztosítja az audit trail-t

---

## Előnyök összefoglalva

✅ **Immutability** — append-only log, SHA-256 hash
✅ **Audit trail** — minden review döntés git commit + JSONL log
✅ **Integrity check** — file tampering detektálható
✅ **Bővíthetőség** — új task type YAML-lal adható hozzá
✅ **Verziókezelés** — task type criteria változások követhetők
✅ **Manual override** — Root escalation ha reviewer túl szigorú
✅ **Compliance-ready** — teljes audit log minden döntésről

---

## Implementációs prioritás

**Phase 1 (kritikus):**
1. Review decision log (JSONL)
2. SHA-256 hash verification
3. Max attempts + escalation

**Phase 2:**
4. Task type YAML definitions
5. Criteria scoring system
6. Git auto-commit

**Phase 3:**
7. Integrity check API
8. Dashboard UI review history-hoz
9. Task type verziókezelés
