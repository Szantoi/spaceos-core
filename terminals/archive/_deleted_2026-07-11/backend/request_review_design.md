# request_review MCP Tool Design

## Problem Statement

Terminal-reviewer infrastructure uses tmux send-keys which fails with "no current target" errors when review sessions don't exist.

**Evidence:**
```
Review error: Command failed: tmux send-keys -t spaceos-review-architect [...]
no current target
```

## Solution Architecture

Replace tmux-based review system with **MCP-driven review workflow**.

---

## MCP Tool Signature

```typescript
mcp__spaceos-knowledge__request_review(
  reviewer: 'architect' | 'librarian',
  inbox_message_id: string,
  done_message_id: string
): Promise<{
  verdict: 'APPROVE' | 'REJECT',
  feedback: string,
  reviewer: string,
  timestamp: string
}>
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `reviewer` | `'architect' \| 'librarian'` | Which terminal performs the review |
| `inbox_message_id` | `string` | Original inbox task message ID (e.g., MSG-BACKEND-042) |
| `done_message_id` | `string` | DONE outbox message ID to review |

### Response

```typescript
{
  verdict: 'APPROVE' | 'REJECT',     // Review verdict
  feedback: string,                  // Detailed feedback (markdown)
  reviewer: string,                  // Terminal that did the review
  timestamp: string,                 // ISO 8601 timestamp
  sessionLog?: string                // Optional: tmux session log path
}
```

---

## Implementation Flow

### 1. Review Request Received (via MCP)

```
Backend → MCP request_review tool → Knowledge Service
```

### 2. Spawn Review Session

**Function:** `startTerminalSession()` from sessionStarter.ts

```typescript
import { startTerminalSession } from './sessionStarter';

const sessionName = await startTerminalSession(
  reviewer,              // 'architect' or 'librarian'
  'haiku',               // Fast, cheap model for review
  buildReviewPrompt(inbox_message_id, done_message_id)
);
```

### 3. Wait for Structured Response

**Timeout:** 2 minutes (120 seconds)

**Expected response format:**
```
VERDICT: APPROVE
FEEDBACK: Implementation matches spec...
```

**Polling strategy:**
- Poll tmux pane output every 2 seconds
- Extract verdict using regex: `/VERDICT: (APPROVE|REJECT)/`
- Extract feedback using regex: `/FEEDBACK: (.*)/s`
- Timeout after 120s → fallback to manual approval

### 4. Return Response

```typescript
return {
  verdict: 'APPROVE',
  feedback: 'Implementation matches spec...',
  reviewer: 'architect',
  timestamp: new Date().toISOString()
};
```

---

## Audit Logging

**Log file:** `/opt/spaceos/logs/reviews/YYYY-MM-DD-review.log`

**Format:**
```json
{
  "timestamp": "2026-07-03T10:30:00Z",
  "inbox_message_id": "MSG-BACKEND-042",
  "done_message_id": "MSG-BACKEND-042-DONE",
  "reviewer": "architect",
  "verdict": "APPROVE",
  "feedback": "...",
  "duration_ms": 15432
}
```

---

## Fallback: Manual Approval

**If timeout or error:**

1. Log warning: `Review service unavailable for ${reviewer}`
2. Create inbox message for Conductor:
   ```yaml
   ---
   id: MSG-CONDUCTOR-NNN
   from: system
   to: conductor
   type: question
   priority: high
   status: UNREAD
   created: YYYY-MM-DD
   ---

   # Manual Review Required

   ## Reason
   Review session timeout for ${reviewer}

   ## Task
   ${inbox_message_id} → ${done_message_id}

   ## Request
   Please manually review and approve/reject.
   ```
3. Return:
   ```typescript
   {
     verdict: 'PENDING_MANUAL',
     feedback: 'Review timeout - manual approval required',
     reviewer: 'manual',
     timestamp: new Date().toISOString()
   }
   ```

---

## Code Changes Required

### File 1: `/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp.ts`

**Add tool definition:**
```typescript
{
  name: 'request_review',
  description: 'Request review from architect or librarian terminal. Returns APPROVE/REJECT verdict.',
  inputSchema: {
    type: 'object',
    properties: {
      reviewer: {
        type: 'string',
        enum: ['architect', 'librarian'],
        description: 'Which terminal performs the review'
      },
      inbox_message_id: {
        type: 'string',
        description: 'Original inbox task message ID (e.g., MSG-BACKEND-042)'
      },
      done_message_id: {
        type: 'string',
        description: 'DONE outbox message ID to review'
      }
    },
    required: ['reviewer', 'inbox_message_id', 'done_message_id']
  }
}
```

**Add handler:**
```typescript
case 'request_review': {
  const reviewer = String(args.reviewer || '');
  const inboxId = String(args.inbox_message_id || '');
  const doneId = String(args.done_message_id || '');

  const result = await requestReview(reviewer, inboxId, doneId);
  return {
    content: [{ type: 'text', text: JSON.stringify(result, null, 2) }]
  };
}
```

### File 2: `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/terminalReviewer.ts`

**New function:** `requestReview()`

```typescript
export async function requestReview(
  reviewer: 'architect' | 'librarian',
  inboxMessageId: string,
  doneMessageId: string
): Promise<{
  verdict: 'APPROVE' | 'REJECT' | 'PENDING_MANUAL',
  feedback: string,
  reviewer: string,
  timestamp: string
}> {
  const startTime = Date.now();

  try {
    // 1. Build review prompt
    const prompt = reviewer === 'architect'
      ? await buildArchitectPrompt(inboxMessageId, doneMessageId)
      : await buildLibrarianPrompt(inboxMessageId, doneMessageId);

    // 2. Start review session (NOT tmux send-keys, use startTerminalSession)
    const sessionName = await startTerminalSession(reviewer, 'haiku', prompt);

    // 3. Wait for structured response (2 min timeout)
    const response = await waitForReviewResponse(sessionName, 120000);

    // 4. Parse verdict + feedback
    const verdict = parseVerdict(response);
    const feedback = parseFeedback(response);

    // 5. Log audit trail
    await logReview({
      inboxMessageId,
      doneMessageId,
      reviewer,
      verdict,
      feedback,
      duration_ms: Date.now() - startTime
    });

    return {
      verdict,
      feedback,
      reviewer,
      timestamp: new Date().toISOString()
    };

  } catch (error) {
    // Fallback: manual approval
    console.error(`[Review] Error in ${reviewer} review:`, error);
    await createManualReviewRequest(reviewer, inboxMessageId, doneMessageId);

    return {
      verdict: 'PENDING_MANUAL',
      feedback: `Review timeout - manual approval required. Error: ${error.message}`,
      reviewer: 'manual',
      timestamp: new Date().toISOString()
    };
  }
}
```

### File 3: Scripts refactor (later phase)

Once MCP tool is working:
- Update `scripts/terminal-reviewer.sh` to use MCP API
- Update `scripts/nightwatch.sh` to call new reviewer
- Deprecate old tmux-based review functions

---

## Testing Plan

### Test 1: Happy Path (Architect APPROVE)

```bash
# Create test DONE message
echo "Test implementation" > /opt/spaceos/terminals/backend/outbox/test-done.md

# Call MCP tool
curl -X POST http://localhost:3456/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "request_review",
      "arguments": {
        "reviewer": "architect",
        "inbox_message_id": "MSG-BACKEND-TEST",
        "done_message_id": "MSG-BACKEND-TEST-DONE"
      }
    },
    "id": 1
  }'

# Expected response
{
  "jsonrpc": "2.0",
  "result": {
    "content": [{
      "type": "text",
      "text": "{\"verdict\":\"APPROVE\",\"feedback\":\"...\",\"reviewer\":\"architect\",\"timestamp\":\"...\"}"
    }]
  }
}
```

### Test 2: Timeout Fallback

```bash
# Kill MCP service
sudo systemctl stop spaceos-nexus

# Try review
curl -X POST http://localhost:3456/mcp ...

# Expected: PENDING_MANUAL verdict + inbox message for Conductor
```

### Test 3: Dual Review (Architect + Librarian)

```bash
# Sequential reviews
curl ... reviewer=architect
curl ... reviewer=librarian

# Both must APPROVE for final approval
```

---

## Acceptance Criteria

- [x] Design document created
- [ ] `request_review` MCP tool implemented
- [ ] `requestReview()` function in terminalReviewer.ts
- [ ] Audit logging to `/opt/spaceos/logs/reviews/`
- [ ] Fallback manual approval working
- [ ] Test: Architect APPROVE
- [ ] Test: Librarian REJECT
- [ ] Test: Timeout fallback
- [ ] No "no current target" errors

---

## Security Considerations

**Authorization:**
- Only Root, Conductor, and the reviewing terminal can call `request_review`
- Verify reviewer terminal has CLAUDE.md + valid identity

**Audit Trail:**
- Every review logged with timestamp, verdict, feedback
- Session logs preserved in `/opt/spaceos/logs/sessions/`

**Timeout Protection:**
- 2 minute hard timeout prevents hanging reviews
- Fallback to manual approval prevents workflow blockage

---

## Next Steps

1. Implement `requestReview()` in terminalReviewer.ts
2. Add MCP tool definition + handler in mcp.ts
3. Add audit logging
4. Test with real DONE messages
5. Refactor scripts to use MCP (Phase 2)
6. Deprecate old tmux functions (Phase 3)

---

**Design Status:** ✅ COMPLETE
**Implementation Status:** ⏳ PENDING
**Review Date:** 2026-07-03
