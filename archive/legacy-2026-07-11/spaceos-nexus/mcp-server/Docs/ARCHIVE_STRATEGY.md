# 🗂️ Archive Strategy — Cleanup Guide (2026-03-08)

**Purpose:** Safely organize scattered task/report files without deleting history
**Status:** READY TO EXECUTE

---

## 📋 Archive Plan

### M01 Cleanup

**Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_01/`

#### Step 1: Create Archive Directory
```
mkdir _archive
mkdir _archive/task-history
mkdir _archive/reports
mkdir _archive/guides
```

#### Step 2: Move Files (NOT DELETE)

**Move Task Files:**
```
epic_00/tasks/TASK-00-*.md → _archive/task-history/epic_00/
epic_02/tasks/TASK-02-*.md → _archive/task-history/epic_02/
epic_08/tasks/TASK-08-*.md → _archive/task-history/epic_08/
epic_09/tasks/TASK-09-*.md → _archive/task-history/epic_09/
```

**Move Old Reports:**
```
epic_00/REMEDIATION_SUMMARY.md → _archive/reports/
epic_00/TECH_LEAD_BRIEF.md → _archive/guides/
epic_00/COMPLETION_CHECKLIST.md → _archive/guides/
epic_00/DAILY_STANDUP_TEMPLATE.md → _archive/guides/
epic_08/AUDIT-*.md → _archive/reports/
epic_08/BEST-PRACTICES-*.md → _archive/reports/
epic_08/CRITICAL-*.md → _archive/reports/
epic_08/FIX-PROPOSAL-*.md → _archive/reports/
```

#### Step 3: Delete Empty Folders
```
rm -f epic_00/tasks (if empty)
rm -f epic_02/tasks (if empty)
rm -f epic_08/tasks (if empty)
rm -f epic_09/tasks (if empty)
```

#### Step 4: Create Archive Index
```
Create: _archive/INDEX.md
Content:
  - Reference links to all archived files
  - Reason each was archived
  - Link to new consolidated summary (M01_CONSOLIDATED_SUMMARY.md)
```

---

### M02 Cleanup

**Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/`

#### Step 1: Create Archive Directory
```
mkdir _archive
mkdir _archive/task-history
mkdir _archive/old-reports
```

#### Step 2: Move Task Files
```
epic_09/tasks/ → _archive/task-history/epic_09/
epic_10/tasks/ → _archive/task-history/epic_10/
epic_11/tasks/ → _archive/task-history/epic_11/
epic_12/tasks/ → _archive/task-history/epic_12/
epic_13/tasks/ → _archive/task-history/epic_13/
epic_14/tasks/ → _archive/task-history/epic_14/
```

#### Step 3: Move Old Status Reports
```
02-planning/ → _archive/old-reports/planning/
05-status/   → _archive/old-reports/status/
```

**Reason:** Superseded by new consolidated reports:
- `M02_MILESTONE_STATUS_REPORT_2026-03-08.md`
- `M02_CONSOLIDATED_SUMMARY.md`

#### Step 4: Create Archive Index
```
Create: _archive/INDEX.md
Content:
  - All moved files listed with timestamps
  - Link to new consolidated summary (M02_CONSOLIDATED_SUMMARY.md)
  - Reference to M02_MILESTONE_STATUS_REPORT_2026-03-08.md
```

#### Step 5: Delete Empty Folders
```
rm -rf epic_*/tasks/ (if empty after move)
rm -rf 02-planning/ (if empty after move)
rm -rf 05-status/ (if empty after move)
```

---

## 📊 Impact

### Before Cleanup
```
milestone_01/
├── epic_00/
│   ├── tasks/
│   │   ├── TASK-00-01.md
│   │   ├── TASK-00-02.md
│   │   ├── TASK-00-03.md
│   │   ├── TASK-00-03B.md
│   │   ├── TASK-00-04.md
│   │   └── TASK-00-05.md
│   ├── goal.md
│   ├── state.md
│   ├── REMEDIATION_SUMMARY.md
│   ├── TECH_LEAD_BRIEF.md
│   └── ... (10+ other files)
├── epic_08/
│   ├── tasks/
│   │   ├── TASK-08-*.md (7 files)
│   ├── AUDIT-*.md
│   ├── BEST-PRACTICES-*.md
│   └── ... (10+ other files)
└── ... (many more files)

TOTAL: ~50 files in milestone_01
```

### After Cleanup
```
milestone_01/
├── epic_00/
│   ├── goal.md ✅ KEEP
│   ├── state.md ✅ KEEP
│   └── implementation-summary/ ✅ KEEP
├── epic_02/
│   ├── goal.md ✅ KEEP
│   ├── state.md ✅ KEEP
│   └── implementation-summary/ ✅ KEEP
├── epic_08/
│   ├── goal.md ✅ KEEP
│   ├── state.md ✅ KEEP
│   └── implementation-summary/ ✅ KEEP
├── epic_09/
│   ├── goal.md ✅ KEEP
│   ├── state.md ✅ KEEP
│   └── implementation-summary/ ✅ KEEP
├── M01_CONSOLIDATED_SUMMARY.md ✅ NEW
├── M01_DOCUMENTATION_INDEX.md ✅ NEW
├── _archive/ (contains all moved files)
└── ... (key milestone reports)

TOTAL: ~15 files in milestone_01 (organized)
```

**Reduction:** ~50 files → 15 files (70% fewer files to scan)

---

## ✅ Safe Files (MUST KEEP)

### Per Epic
- ✅ `epic_*/goal.md` — Goals + AC (authoritative)
- ✅ `epic_*/state.md` — Task status (tracking)
- ✅ `epic_*/README.md` — If exists (overview)
- ✅ `epic_*/implementation-summary/` — Proof of completion

### Per Milestone
- ✅ `M01_COMPLETION_REPORT.md` — Milestone closure
- ✅ `M01_COORDINATION_CLOSURE.md` — Arch decisions
- ✅ `M01_CONSOLIDATED_SUMMARY.md` — NEW (consolidation)
- ✅ `M02_MILESTONE_STATUS_REPORT_*.md` — Current status
- ✅ `M02_CONSOLIDATED_SUMMARY.md` — NEW (consolidation)

---

## 🗑️ Deletable Files

### TASK Files (→ Archive)
- ❌ `epic_*/tasks/TASK-*.md` (all individual task definitions)
- **Why:** Consolidated in `state.md` task list
- **Action:** Move to `_archive/task-history/`

### Old Report Files (→ Archive)
- ❌ `milestone_01/epic_00/REMEDIATION_SUMMARY.md`
- ❌ `milestone_01/epic_08/AUDIT-*.md`
- ❌ `milestone_01/epic_08/BEST-PRACTICES-*.md`
- ❌ `milestone_01/epic_08/CRITICAL-*.md`
- ❌ `milestone_01/epic_08/FIX-PROPOSAL-*.md`
- ❌ `milestone_02/02-planning/M02-*.md` (old status reports)
- ❌ `milestone_02/05-status/M02-*.md` (superseded)

**Why:** Replaced by consolidated summary files
**Action:** Move to `_archive/old-reports/`

### Templates & Checklists (→ Archive)
- ❌ `epic_*/DAILY_STANDUP_TEMPLATE.md`
- ❌ `epic_*/COMPLETION_CHECKLIST.md`
- ❌ `epic_*/TECH_LEAD_*.md` (guidance documents)

**Why:** No longer needed (archived as reference)
**Action:** Move to `_archive/guides/`

---

## 🚀 Benefits After Cleanup

**For Developers:**
- ✅ Fewer files to scan (organized structure)
- ✅ Clear navigation: epic/ contains only active files
- ✅ History preserved (in _archive/)
- ✅ New consolidated summaries at milestone level

**For Architects:**
- ✅ Single SSOT per milestone (M01/M02_CONSOLIDATED_SUMMARY.md)
- ✅ Quick access to current state (state.md, goal.md)
- ✅ Proof of completion (implementation-summary/)

**For PMs:**
- ✅ Status clear (see M02_MILESTONE_STATUS_REPORT_*.md)
- ✅ Decision history (archived for audit)
- ✅ No clutter in active folders

---

## ⚠️ Safety Checklist

Before executing cleanup:

- [ ] Verify `_archive/` will be created
- [ ] Confirm all files moved (not deleted) from epic/ to _archive/
- [ ] Ensure `M01_CONSOLIDATED_SUMMARY.md` exists and is correct
- [ ] Ensure `M02_CONSOLIDATED_SUMMARY.md` exists and is correct
- [ ] Create `_archive/INDEX.md` with reference links
- [ ] Git commit with message: "archive: consolidate M01/M02 task files"
- [ ] No files are permanently deleted (all in _archive/)
- [ ] History preserved (git log will show all files for audit)

---

## 📝 Archive Index Template

**File:** `_archive/INDEX.md`

```markdown
# Archive Index — M01 Task Files

**Created:** 2026-03-08
**Reason:** Consolidation into M01_CONSOLIDATED_SUMMARY.md

## Task History
- `task-history/epic_00/TASK-00-*.md` (5 coordination tasks)
- `task-history/epic_02/TASK-02-*.md` (1 static analysis task)
- `task-history/epic_08/TASK-08-*.md` (3 write layer tasks)
- `task-history/epic_09/TASK-09-*.md` (4 optimizations)

## Reports
- `reports/AUDIT-*.md` (audit findings)
- `reports/BEST-PRACTICES-*.md` (AWS research)
- `reports/FIX-PROPOSAL-*.md` (implementation guides)

## New Consolidated Files
- [M01_CONSOLIDATED_SUMMARY.md](../M01_CONSOLIDATED_SUMMARY.md) ← START HERE
- [M01_COMPLETION_REPORT.md](../M01_COMPLETION_REPORT.md)

---

For full context, see: ../M01_CONSOLIDATED_SUMMARY.md
```

---

## ✔️ Execution Readiness

**Status:** ✅ READY TO EXECUTE

**Next Steps:**
1. Create `_archive/` directories (M01 + M02)
2. Move files (as per strategy above)
3. Create `_archive/INDEX.md` (reference)
4. Test: Verify all links work
5. Git commit + push

**Estimated Time:** 30 minutes (manual or script)

---

**Document:** ARCHIVE_STRATEGY.md
**Date:** 2026-03-08
**Status:** Ready for execution
