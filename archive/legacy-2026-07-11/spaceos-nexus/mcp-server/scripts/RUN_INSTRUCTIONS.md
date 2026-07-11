# Generate M02 Task Files — Run Instructions

## Prerequisites
- PowerShell 7.0+ (Windows)
- Workspace root: `c:\Users\szant\Documents\Development\JoineryTech.McpServer`

## Quick Start

### Option 1: Default Run (Skip Existing)
```powershell
cd c:\Users\szant\Documents\Development\JoineryTech.McpServer
.\scripts\Generate-M02-Tasks.ps1
```

### Option 2: Force Overwrite Existing Files
```powershell
.\scripts\Generate-M02-Tasks.ps1 -Force
```

### Option 3: Specify Custom Root Path
```powershell
.\scripts\Generate-M02-Tasks.ps1 -RootPath "C:\path\to\workspace"
```

## Output

The script will:
1. **Create 51 TASK-XX-YY.md files** across 6 EPICs
2. **Auto-create directories** if they don't exist
3. **Generate manifest** at `scripts/m02-tasks-manifest.txt`
4. **Display summary** showing all created files

## Exit Codes
- `0` — Success
- `1` — Error (check output for details)

## File Distribution (Generated)

| EPIC | Tasks | Location |
|:-----|:------|:---------|
| EPIC-09 | 8 | `Docs/.../milestone_02/epic_09/tasks/` |
| EPIC-10 | 8 | `Docs/.../milestone_02/epic_10/tasks/` |
| EPIC-11 | 8 | `Docs/.../milestone_02/epic_11/tasks/` |
| EPIC-12 | 8 | `Docs/.../milestone_02/epic_12/tasks/` |
| EPIC-13 | 7 | `Docs/.../milestone_02/epic_13/tasks/` |
| EPIC-14 | 12 | `Docs/.../milestone_02/epic_14/tasks/` |
| **TOTAL** | **51** | — |

## Post-Generation Steps

1. **Verify creation:**
   ```powershell
   ls Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_*/tasks/ | Measure-Object
   ```

2. **Review manifest:**
   ```powershell
   cat scripts/m02-tasks-manifest.txt
   ```

3. **Update EPIC state.md files** with task references if needed

4. **Commit to git:**
   ```powershell
   git add Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/*/tasks/
   git commit -m "docs: generate 51 M02 task files (EPIC-09 to EPIC-14)"
   ```

## Troubleshooting

### Script won't run
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Permission denied on directory creation
- Run PowerShell as Administrator
- Or manually create `Docs/.../milestone_02/epic_*/tasks/` directories first

### Manifest not found
- Check that `scripts/` directory exists in workspace root
- Run with full path: `c:\path\to\workspace\scripts\Generate-M02-Tasks.ps1`
