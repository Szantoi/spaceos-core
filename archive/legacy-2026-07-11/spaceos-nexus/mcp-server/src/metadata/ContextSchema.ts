/**
 * EPIC-09: Context Schema Type Definitions
 * All context layer tables (roles, role_schemas, workflows, templates, etc.)
 * Generated from ADR-001 + TASK-09-01
 *
 * @fileoverview Context layer SSOT schema interfaces for agent role identity
 *              and capabilities (read-only from agent perspective)
 */

// ============================================================================
// 1. ROLES — Agent identity repository
// ============================================================================

/**
 * RoleRecord — Core role definition
 * Identity: (domain, role_name) composite UNIQUE
 */
export interface RoleRecord {
    id: number;
    domain: string;
    role_name: string;
    content: string;          // Full markdown from .role.md
    version: string;
    last_updated: string;     // ISO 8601
    created_at: string;       // ISO 8601
}

export type RoleCreateInput = Omit<RoleRecord, 'id' | 'created_at' | 'last_updated'>;

// ============================================================================
// 2. ROLE_SCHEMAS — Capabilities + personalization
// ============================================================================

/**
 * RoleSchemaRecord — Capabilities and personalization
 * One schema per role (1:1 with roles)
 * Contains persona, tool permissions, and role-specific constraints
 */
export interface RoleSchemaRecord {
    id: number;
    domain: string;
    role_name: string;
    persona_identity?: string;
    persona_style?: string;
    mcp_tool_permissions: string;    // JSON array: ["tool1", "tool2"]
    limitations?: string;            // JSON array of constraints
    handoff_triggers?: string;       // JSON array of escalation triggers
    version: string;
    last_updated: string;
    created_at: string;
}

export interface RoleSchemaInput extends Omit<RoleSchemaRecord, 'id' | 'created_at' | 'last_updated'> { }

export function parseToolPermissions(json: string): string[] {
    try {
        const parsed = JSON.parse(json);
        return Array.isArray(parsed) ? parsed : [];
    } catch {
        console.warn('Failed to parse tool permissions:', json);
        return [];
    }
}

// ============================================================================
// 3. RUNBOOKS — Procedural guidance
// ============================================================================

/**
 * RunbookRecord — Step-by-step operational procedures
 * Locked 1:1 with role (unique on domain+role_name)
 */
export interface RunbookRecord {
    id: number;
    domain: string;
    role_name: string;
    content: string;
    version: string;
    last_updated: string;
    created_at: string;
}

export type RunbookCreateInput = Omit<RunbookRecord, 'id' | 'created_at' | 'last_updated'>;

// ============================================================================
// 4. WORKFLOWS — Role-specific patterns
// ============================================================================

/**
 * WorkflowRecord — Execution patterns for role
 * Multiple workflows per role (discovery, delivery, ideation, etc.)
 * Identity: (domain, role_name, workflow_type) composite UNIQUE
 */
export interface WorkflowRecord {
    id: number;
    domain: string;
    role_name: string;
    workflow_type: string;    // e.g., "default", "ideation", "delivery"
    content: string;          // Full markdown FSM definition
    version: string;
    last_updated: string;
    created_at: string;
}

export type WorkflowCreateInput = Omit<WorkflowRecord, 'id' | 'created_at' | 'last_updated'>;

// ============================================================================
// 5. TEMPLATES — Reusable structures
// ============================================================================

/**
 * TemplateRecord — Reusable response/prompt templates
 * Multiple templates per role (multiple template_name values)
 * Identity: (domain, role_name, template_name) composite UNIQUE
 */
export interface TemplateRecord {
    id: number;
    domain: string;
    role_name: string;
    template_name: string;
    content: string;          // Full markdown or mustache template
    version: string;
    last_updated: string;
    created_at: string;
}

export type TemplateCreateInput = Omit<TemplateRecord, 'id' | 'created_at' | 'last_updated'>;

// ============================================================================
// 6. STANDARDS — Global best practices
// ============================================================================

/**
 * StandardRecord — Global standards, ADRs, and patterns
 * Not role-specific; available to all roles
 * Identity: std_id (unique, e.g., "03-agent-system/standards/...")
 */
export interface StandardRecord {
    id: number;
    std_id: string;           // Unique: "03-agent-system/..."
    category?: string;        // e.g., "adr", "standard", "pattern"
    title?: string;
    content: string;          // Full markdown
    version: string;
    last_updated: string;
    created_at: string;
}

export type StandardCreateInput = Omit<StandardRecord, 'id' | 'created_at' | 'last_updated'>;

// ============================================================================
// COMPOSITE TYPES — Used by bootstrap + context loading
// ============================================================================

/**
 * BootstrapContext — Complete agent context at session start
 * Fetched from context layer on agent initialization
 */
export interface BootstrapContext {
    agent_id: string;
    domain: string;
    role_name: string;
    role: RoleRecord;
    schema: RoleSchemaRecord;
    runbook: RunbookRecord;
    workflows: WorkflowRecord[];
    templates: TemplateRecord[];
    standards: StandardRecord[];
}

/**
 * ContextRequest — Query parameters for context loading
 */
export interface ContextRequest {
    domain: string;
    role_name: string;
    track: 'discovery' | 'delivery';
    phase?: string;
}

// ============================================================================
// ERROR TYPES
// ============================================================================

/**
 * ContextSchemaError — Base error for context layer
 */
export class ContextSchemaError extends Error {
    constructor(public code: string, public details?: Record<string, unknown>) {
        super(code);
        this.name = 'ContextSchemaError';
    }
}

/**
 * RoleNotFoundError — Role not found in context
 */
export class RoleNotFoundError extends ContextSchemaError {
    constructor(domain: string, role_name: string) {
        super('ROLE_NOT_FOUND', { domain, role_name });
    }
}

/**
 * SchemaValidationError — Schema validation failed
 */
export class SchemaValidationError extends ContextSchemaError {
    constructor(table: string, reason: string) {
        super('SCHEMA_VALIDATION_FAILED', { table, reason });
    }
}
