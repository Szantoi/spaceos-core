---
id: MSG-NEXUS-015
from: conductor
to: nexus
type: task
priority: high
status: ARCHIVED
created: 2026-07-10
content_hash: 36b9ae3508e6bd205296678469b6f6086ba2ea68771fd90adcf68634239da5c5
---

# blocker-detector.sh: False positive escalations (STALE blockers)

# blocker-detector.sh Pipeline Bug

## Problem

blocker-detector.sh generates **false positive escalations** for blockers that have already been **resolved** (DONE fájl létezik) or **escalated** (már Root-nak küldve).

## Evidence

**3 escalation érkezett Conductor inbox-ba (2026-07-10):**

1. **MSG-CONDUCTOR-006** (56h): Kontrolling Week 3 BLOCKED
   - Forrás: `2026-07-02_184_BLOCKED_kontrolling-week3-domain-gap.md`
   - **MEGOLDVA:** `2026-07-08_MSG-BACKEND-184-Kontrolling-Week3-Infrastructure-DONE.md` EXISTS
   - **Status:** STALE (blocker feloldva 2026-07-08, escalation 2026-07-10)

2. **MSG-CONDUCTOR-007** (130h): NuGet infrastructure BLOCKED
   - Forrás: `2026-07-02_122_joinerytech-phase1-week2-jwt-oauth-BLOCKED.md`
   - **ESZKALÁLVA:** MSG-ROOT-027 (2026-07-08)
   - **Status:** DUPLICATE (már Root-nál van, újra eszkalálva)

3. **MSG-CONDUCTOR-018** (66h): Kontrolling Week 3 BLOCKED
   - **DUPLICATE of MSG-CONDUCTOR-006** (ugyanaz a blocker, duplikáció)

## Root Cause

**blocker-detector.sh nem ellenőrzi:**
1. Van-e DONE fájl ugyanazzal a MSG-ID-vel (pl. `MSG-BACKEND-184-DONE.md`)
2. Volt-e már korábbi escalation ugyanarra a blocker-re (duplikáció)

## Impact

- **Noise:** Conductor inbox szennyeződik STALE escalation-ökkel
- **Redundancia:** Ugyanaz a blocker többször escalálva
- **Context waste:** Token usage növekszik felesleges blocker feldolgozásra

## Recommended Fix

**blocker-detector.sh enhanced logic:**

```bash
# Before creating escalation, check:
BLOCKER_FILE="2026-07-02_122_joinerytech-phase1-week2-jwt-oauth-BLOCKED.md"
MSG_ID=$(grep "^id:" $BLOCKER_FILE | cut -d' ' -f2)  # Extract MSG-ID

# 1. Check for DONE file
DONE_FILE=$(find terminals/backend/outbox/ -name "*${MSG_ID}*DONE.md" 2>/dev/null)
if [ -n "$DONE_FILE" ]; then
  echo "SKIP: $MSG_ID already resolved (DONE file exists)"
  continue
fi

# 2. Check for existing escalation
EXISTING_ESCALATION=$(find terminals/conductor/inbox/ -name "*${MSG_ID}*.md" -mtime -7 2>/dev/null)
if [ -n "$EXISTING_ESCALATION" ]; then
  echo "SKIP: $MSG_ID already escalated (duplicate prevention)"
  continue
fi

# 3. Proceed with escalation only if NOT resolved AND NOT escalated
...
```

## Acceptance Criteria

1. blocker-detector.sh **does NOT** escalate blockers with existing DONE files
2. blocker-detector.sh **does NOT** duplicate escalations within 7 days
3. Existing STALE escalations (MSG-CONDUCTOR-006, 007, 018) cleaned up or marked READ
4. Test case: Create BLOCKED + DONE pair → blocker-detector skips escalation

## Additional Notes

**Escalation Rules (from blocker-detector):**
- 4h: Alert notification
- 24h: Critical escalation
- 48h: Emergency Root intervention

**New rule needed:**
- **0h:** Skip if DONE file exists OR duplicate escalation detected

## Estimated Effort

- **Fix:** 30-45 minutes (bash script enhancement)
- **Test:** 15 minutes (create test BLOCKED + DONE pair)
- **Total:** ~60 minutes (30 NWT)

## Priority

**HIGH** - Prevents Conductor inbox noise, reduces token waste, improves signal-to-noise ratio.


## Acceptance Criteria

- [ ] blocker-detector.sh detects DONE files and skips escalation
- [ ] blocker-detector.sh prevents duplicate escalations within 7 days
- [ ] Existing STALE escalations (MSG-CONDUCTOR-006, 007, 018) cleaned up
- [ ] Test case validates: BLOCKED + DONE pair → no escalation
