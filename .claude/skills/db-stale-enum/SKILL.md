# Skill: Fix Stale DB Enum Values

## When to invoke
Use `/db-stale-enum` when a .NET API throws a PostgreSQL error like:
- `invalid input value for enum "XxxType": "OldValue"`
- Column constraint violation on an enum column
- 500 errors on endpoints that read/write an enum column

This happens when migrations added/renamed enum values but old test data in the DB still has the old string.

## Root Cause
PostgreSQL stores enum values as strings. EF Core creates DB enums from C# enums. When a C# enum is renamed or values are removed, old rows still have the old string value — PostgreSQL rejects them on read.

## Diagnosis
```sql
-- Check what values are actually in the DB column
SELECT DISTINCT "ColumnName" FROM "TableName" LIMIT 20;

-- Check what EF expects (look at the migration)
-- Or check the C# enum definition
```

Connect to DB:
```bash
PGPASSWORD=spaceos_db_pass psql -h 127.0.0.1 -p 5433 -U spaceos -d spaceos
```

## Fix Pattern
```sql
-- Map old values to new ones
UPDATE "TableName"
SET "EnumColumn" = 'NewValue'
WHERE "EnumColumn" IN ('OldValue1', 'OldValue2');

-- Handle nulls and empty strings if needed
UPDATE "TableName"
SET "EnumColumn" = 'DefaultValue'
WHERE "EnumColumn" IS NULL OR "EnumColumn" = '';
```

## Real Examples from SpaceOS

### WorkflowPhase (2026-04)
C# enum: `Discovery | Delivery | ClosedDone`
Old DB values: `Draft`, `InProgress`
Fix:
```sql
UPDATE "FlowEpics" SET "WorkflowPhase" = 'Discovery' WHERE "WorkflowPhase" = 'Draft';
UPDATE "FlowEpics" SET "WorkflowPhase" = 'Delivery' WHERE "WorkflowPhase" = 'InProgress';
```

### FlowEpicScope (2026-04)
C# enum: `DoorOrder | CuttingPlan | MicroAssembly`
Old DB values: `Internal`, `External`, empty string, NULL
Fix:
```sql
UPDATE "FlowEpics"
SET "Scope" = 'DoorOrder'
WHERE "Scope" IN ('Internal','External') OR "Scope" IS NULL OR "Scope" = '';
```

## Permission Denied (42501)
If the fix fails with `ERROR: permission denied for table "XxxTable"`, the table is owned by `postgres` but the service user is `spaceos`. Fix:
```sql
-- Run as superuser
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "TableName" TO spaceos;
```

Run as superuser: `sudo -u postgres psql -d spaceos -c "GRANT ..."`

## After Fix
1. Restart the affected service if it's caching the schema
2. Re-run the failing E2E test / API call to confirm
3. The fix is a one-time data migration — no code change needed

## DB Connection Details
- Host: 127.0.0.1
- Port: 5433 (kernel + modules)
- User: spaceos
- Password: spaceos_db_pass
- DBs: `spaceos` (kernel), `spaceos_inventory`, `spaceos_cutting`, `spaceos_procurement`, etc.
