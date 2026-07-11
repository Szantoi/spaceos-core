-- TASK-18-09: Extend Audit Log with Cost Tracking Fields
--
-- Adds LLM cost metadata to audit_log for reflection tool usage tracking.
-- Additive migration: ALTER TABLE ADD COLUMN (idempotent via try-catch in AgentDb.initSchema).

-- Add ai_model field to track which model was used (e.g., 'gpt-4', 'claude-3-opus')
ALTER TABLE audit_log ADD COLUMN ai_model TEXT;

-- Add ai_tokens_used field to track token consumption for cost reporting
ALTER TABLE audit_log ADD COLUMN ai_tokens_used INTEGER;

-- Add cost_amount_usd field for auditable cost tracking (nullable, safe defaults)
ALTER TABLE audit_log ADD COLUMN cost_amount_usd REAL;

-- Extend cost tracking index for queries by tool + time + cost
CREATE INDEX IF NOT EXISTS idx_audit_log_tool_cost ON audit_log (tool_name, timestamp, ai_model);
