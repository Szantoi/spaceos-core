-- TASK-11-07: Audit Log Table
-- Stores all MCP tool call metadata for security and performance tracking.

CREATE TABLE IF NOT EXISTS audit_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    domain TEXT NOT NULL,
    role TEXT NOT NULL,
    tool_name TEXT NOT NULL,
    input_hash TEXT, -- SHA-256 hash of input for PII protection
    output_hash TEXT, -- SHA-256 hash of output
    latency_ms INTEGER NOT NULL,
    status_code TEXT NOT NULL, -- e.g., 'SUCCESS', 'INVALID_INPUT', 'INTERNAL_ERROR'
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Index for performance querying by session and time
CREATE INDEX IF NOT EXISTS idx_audit_log_session_time ON audit_log (session_id, timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_log_tool_name ON audit_log (tool_name);
