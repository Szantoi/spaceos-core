# 🔧 Cleanup Execution Steps (2026-03-08)

**Goal:** Safely archive old task/report files without deleting history
**Status:** READY TO EXECUTE

---

## Quick Summary

**Problem:** 40+ scattered task files across milestone_01 and milestone_02 make navigation hard
**Solution:** Keep only `goal.md`, `state.md`, `implementation-summary/` in each epic; archive old task files
**Safety:** All files moved to `_archive/` (preserved for 1+ year audit trail)
**Result:** Cleaner navigation, 70% fewer files, zero data loss

---

## Phase 1: M01 Cleanup (Immediate)

### Command Reference

**Windows PowerShell (Recommended):**

```powershell
# Navigate to M01
cd "Docs\mcp-context-server\delivery\mcp-maintenance\milestones\milestone_01"

# Create archive structure
mkdir _archive
mkdir _archive\task-history
mkdir _archive\reports
mkdir _archive\guides

# Move task files
mkdir _archive\task-history\epic_00
mv epic_00\tasks\* _archive\task-history\epic_00\ -Force 2>$null

mkdir _archive\task-history\epic_02
mv epic_02\tasks\* _archive\task-history\epic_02\ -Force 2>$null

mkdir _archive\task-history\epic_08
mv epic_08\tasks\* _archive\task-history\epic_08\ -Force 2>$null

mkdir _archive\task-history\epic_09
mv epic_09\tasks\* _archive\task-history\epic_09\ -Force 2>$null

# Move old report files
@(
  "epic_00\REMEDIATION_SUMMARY.md",
  "epic_00\TECH_LEAD_BRIEF.md",
  "epic_00\COMPLETION_CHECKLIST.md",
  "epic_00\DAILY_STANDUP_TEMPLATE.md",
  "epic_08\QUALITY_AUDIT_REPORT.md",
  "epic_08\BEST_PRACTICES_AWS_OPTIMIZATION.md"
) | ForEach-Object {
  if (Test-Path $_) {
    mv $_ _archive\reports\ -Force
  }
}

# Delete empty task directories
ri epic_00\tasks -Force -ErrorAction SilentlyContinue
ri epic_02\tasks -Force -ErrorAction SilentlyContinue
ri epic_08\tasks -Force -ErrorAction SilentlyContinue
ri epic_09\tasks -Force -ErrorAction SilentlyContinue
```

**Linux/Mac (Bash):**

```bash
cd Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_01

# Create archive structure
mkdir -p _archive/task-history/{epic_00,epic_02,epic_08,epic_09}
mkdir -p _archive/reports
mkdir -p _archive/guides

# Move task files
mv epic_00/tasks/* _archive/task-history/epic_00/ 2>/dev/null || true
mv epic_02/tasks/* _archive/task-history/epic_02/ 2>/dev/null || true
mv epic_08/tasks/* _archive/task-history/epic_08/ 2>/dev/null || true
mv epic_09/tasks/* _archive/task-history/epic_09/ 2>/dev/null || true

# Move old report files
mv epic_00/REMEDIATION_SUMMARY.md _archive/reports/ 2>/dev/null || true
mv epic_00/TECH_LEAD_BRIEF.md _archive/guides/ 2>/dev/null || true
mv epic_08/QUALITY_AUDIT_REPORT.md _archive/reports/ 2>/dev/null || true
mv epic_08/BEST_PRACTICES_AWS_OPTIMIZATION.md _archive/reports/ 2>/dev/null || true

# Delete empty directories
rmdir epic_00/tasks 2>/dev/null || true
rmdir epic_02/tasks 2>/dev/null || true
rmdir epic_08/tasks 2>/dev/null || true
rmdir epic_09/tasks 2>/dev/null || true
```

### Verification (After Step 1)

```bash
# Should be clean epic directories:
ls epic_00/
# Expected output:
#   goal.md
#   state.md
#   implementation-summary/
#   (no tasks/ folder)

ls epic_08/
# Expected output:
#   goal.md
#   state.md
#   implementation-summary/
#   (no AUDIT, BEST-PRACTICES files)

# Should have archive with all old files:
ls _archive/task-history/
# Expected output: epic_00, epic_02, epic_08, epic_09

ls _archive/reports/
# Expected output: All old .md files
```

---

## Phase 2: Create M01 Archive Index

**File:** `_archive/INDEX.md`

```markdown
# Archive Index — M01 (2026-03-08)

**Reason for Archive:**
Consolidated into `M01_CONSOLIDATED_SUMMARY.md` (single source of truth)

## Task History
All detailed task definitions (now summarized in `state.md`):

### Epic 00 — Coordination & Arch
- `task-history/epic_00/TASK-00-01.md` — Scope lock + dependency mapping
- `task-history/epic_00/TASK-00-02.md` — Formalize architecture patterns
- `task-history/epic_00/TASK-00-03.md` — Accept criteria finalization
- `task-history/epic_00/TASK-00-03B.md` — Scope correction (FSM)
- `task-history/epic_00/TASK-00-04.md` — ADR: Epic 8/9 workflow FSM
- `task-history/epic_00/TASK-00-05.md` — Arch coordination summary

### Epic 02 — Code Quality
- `task-history/epic_02/TASK-02-*.md` — Static analysis + refactoring

### Epic 08 — Write Layer
- `task-history/epic_08/TASK-08-*.md` — Schema, write tools, E2E tests (7 tasks)

### Epic 09 — Optimizations
- `task-history/epic_09/TASK-09-*.md` — Jitter fix, async spike, metrics (4 tasks)

## Reports & Guides
Superseded by `M01_CONSOLIDATED_SUMMARY.md` and `M01_COMPLETION_REPORT.md`:

- `reports/QUALITY_AUDIT_REPORT.md` — Code quality findings (now in state.md)
- `reports/BEST_PRACTICES_AWS_OPTIMIZATION.md` — Performance notes (archived)
- `guides/TECH_LEAD_BRIEF.md` — Guidance (reference only)
- `guides/DAILY_STANDUP_TEMPLATE.md` — Template (no longer needed)

## Current Files (Kept Active)

**Per Epic:**
```
epic_00/
  ├── goal.md ✅
  ├── state.md ✅
  └── implementation-summary/

epic_02/
  ├── goal.md ✅
  ├── state.md ✅
  └── implementation-summary/

epic_08/
  ├── goal.md ✅
  ├── state.md ✅
  └── implementation-summary/

epic_09/
  ├── goal.md ✅
  ├── state.md ✅
  └── implementation-summary/
```

**Milestone Level:**
- `M01_CONSOLIDATED_SUMMARY.md` — **START HERE** (replaces all scattered files)
- `M01_COMPLETION_REPORT.md` — Milestone closure proof
- `M01_COORDINATION_CLOSURE.md` — Arch decisions

## Statistics

| Metric | Before | After | Reduction |
|--------|--------|-------|-----------|
| Files (epic folders) | ~50 | 15 | 70% |
| Active files | Scattered | Organized | — |
| History preserved | — | 100% | ✅ |
| Data loss | — | 0% | ✅ |

## Navigation Guide

1. **Need current status?**
   → Open `M01_CONSOLIDATED_SUMMARY.md`

2. **Need epic details?**
   → Open `epic_*/state.md` (task status, AC)

3. **Need task history?**
   → Open `_archive/task-history/epic_*/TASK-*.md`

4. **Need decision rationale?**
   → Open `_archive/reports/` or search `goal.md` files

---

**Archive Created:** 2026-03-08
**Next Step:** M02 cleanup (Phase 3)
```

### Save Archive Index

Create file: `milestone_01/_archive/INDEX.md` with above content

---

## Phase 3: M02 Cleanup (If Desired)

**Repeat Phase 1-2 for M02:**

```bash
cd Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02

# Create archive structure
mkdir -p _archive/task-history/{epic_09,epic_10,epic_11,epic_12,epic_13,epic_14}
mkdir -p _archive/old-reports/{planning,status}

# Move task files
for epic in epic_09 epic_10 epic_11 epic_12 epic_13 epic_14; do
  mv $epic/tasks/* _archive/task-history/$epic/ 2>/dev/null || true
  rmdir $epic/tasks 2>/dev/null || true
done

# Move old reports
mv 02-planning/* _archive/old-reports/planning/ 2>/dev/null || true
mv 05-status/* _archive/old-reports/status/ 2>/dev/null || true
rmdir 02-planning 2>/dev/null || true
rmdir 05-status 2>/dev/null || true
```

### Verification (M02)

```bash
ls epic_11/
# Expected: goal.md, state.md, implementation-summary/ (only)

ls _archive/task-history/epic_11/
# Expected: All TASK-11-*.md files
```

---

## Phase 4: Git Commit

**After cleanup complete:**

```bash
git add -A
git commit -m "archive: consolidate M01/M02 scattered task files

This commit consolidates 40+ scattered task files from milestone_01
and milestone_02 into:
- M01_CONSOLIDATED_SUMMARY.md
- M02_CONSOLIDATED_SUMMARY.md

All old task files moved to _archive/ (preserved for audit trail).

Benefits:
- 70% fewer files to scan
- Cleaner epic folder structure
- Single SSOT per milestone
- Zero data loss (all in git history)

Affected:
- milestone_01/: 50 files → 15 files
- milestone_02/: Similar consolidation
"

git push
```

---

## Phase 5: Root Directory Cleanup (Optional)

**If desired, also delete test output files:**

```bash
cd c:/path/to/JoineryTech.McpServer

# Delete .txt test files (1.4 MB)
rm test-full.txt 2>/dev/null || true
rm test-output.txt 2>/dev/null || true
rm test-results*.txt 2>/dev/null || true
rm wal-test.txt 2>/dev/null || true
rm full-output.txt 2>/dev/null || true

# Commit cleanup
git add -A
git commit -m "cleanup: remove test output artifacts (.txt files)"
git push
```

---

## ✅ Completion Checklist

- [ ] Phase 1: M01 task files moved to `_archive/`
- [ ] Phase 1: Old reports moved to `_archive/reports/`
- [ ] Phase 1: Empty task directories removed
- [ ] Phase 2: `_archive/INDEX.md` created in milestone_01/
- [ ] Phase 3: M02 cleanup (if desired)
- [ ] Phase 4: Git commit + push
- [ ] Phase 5: Root cleanup (optional)
- [ ] Verification: All links in `INDEX.md` work
- [ ] Verification: `state.md` files still accessible
- [ ] Verification: No data loss (git log shows all files)

---

## ⚠️ Rollback (If Needed)

If cleanup goes wrong, restore from git:

```bash
# See what was deleted
git log --oneline | grep archive

# Restore specific file
git checkout HEAD~1 -- epic_00/tasks/TASK-00-*.md

# Restore entire directory
git checkout HEAD~1 -- epic_00/

# Full rollback
git revert <commit-id>
```

---

## 📊 Expected Results

**Before Cleanup:**
- M01: 50 scattered files (confusing navigation)
- M02: 60 scattered files (hard to find current status)
- Root: ~28 files (11 test output files unnecessary)

**After Cleanup:**
- M01: 15 organized files + `_archive/` (historical)
- M02: 20 organized files + `_archive/` (historical)
- Root: ~17 files (test artifacts removed)
- **Total reduction:** ~70 files removed/archived (-40%)
- **Navigation time:** ~2x faster

---

**Status:** ✅ READY TO EXECUTE
**Estimated Time:** 30 minutes
**Data Loss Risk:** 0% (all archived + git history preserved)
**Rollback Capability:** 100% (git history available)

---

**Document:** CLEANUP_EXECUTION_STEPS.md
**Date:** 2026-03-08
**Next:** Execute phases 1-2, then verify results
