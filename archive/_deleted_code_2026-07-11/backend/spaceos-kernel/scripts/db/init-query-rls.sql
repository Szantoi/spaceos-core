-- scripts/db/init-query-rls.sql
-- RLS policies for Tool Registry query tables.
-- Run after Migration 0015 on production.

-- FlowEpics
ALTER TABLE "FlowEpics" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "FlowEpics" FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "tenant_isolation_flow_epics" ON "FlowEpics";
CREATE POLICY "tenant_isolation_flow_epics"
    ON "FlowEpics"
    USING (
        "TenantId" = COALESCE(
            NULLIF(current_setting('app.current_tenant_id', true), ''),
            '00000000-0000-0000-0000-000000000000'
        )::uuid
    );

-- WorkStations
ALTER TABLE "WorkStations" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "WorkStations" FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "tenant_isolation_workstations" ON "WorkStations";
CREATE POLICY "tenant_isolation_workstations"
    ON "WorkStations"
    USING (
        "TenantId" = COALESCE(
            NULLIF(current_setting('app.current_tenant_id', true), ''),
            '00000000-0000-0000-0000-000000000000'
        )::uuid
    );

-- Facilities
ALTER TABLE "Facilities" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Facilities" FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "tenant_isolation_facilities" ON "Facilities";
CREATE POLICY "tenant_isolation_facilities"
    ON "Facilities"
    USING (
        "TenantId" = COALESCE(
            NULLIF(current_setting('app.current_tenant_id', true), ''),
            '00000000-0000-0000-0000-000000000000'
        )::uuid
    );
