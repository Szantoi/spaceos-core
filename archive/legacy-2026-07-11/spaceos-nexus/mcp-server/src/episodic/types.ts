/**
 * Episode Types — TASK-12-01 (Episodic Memory Foundation)
 *
 * Defines the domain types for the episode storage layer.
 * Used by EpisodeStore, MCP tools, and future FTS5/ChromaDB search layers.
 *
 * @see TASK-12-01 Assignment Sheet
 */

// ─────────────────────────────────────────────────────────────────────────────
// CORE DOMAIN TYPES
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Valid domain categories for an episode.
 * Represents the high-level problem space an agent is working in.
 */
export type EpisodeDomain = 'discovery' | 'engineering' | 'testing' | 'deployment';

/**
 * Valid phase values within a track.
 * Represents the conceptual stage of work within an execution track.
 */
export type EpisodePhase = 'ideation' | 'implementation' | 'review' | 'refinement';

// ─────────────────────────────────────────────────────────────────────────────
// EPISODE ENTITY
// ─────────────────────────────────────────────────────────────────────────────

/**
 * A record of a tool call made during an agent session phase.
 */
export interface ToolCallRecord {
    tool: string;
    args: Record<string, unknown>;
    result?: unknown;
}

/**
 * A reference to an artifact produced during an agent session phase.
 */
export interface ArtifactRecord {
    type: string;
    path: string;
    hash?: string;
}

/**
 * Episode entity — a snapshot of agent execution within a phase.
 *
 * Persisted to SQLite `episodes` table. Used by TASK-12-02 (FTS5) and
 * TASK-12-03 (ChromaDB) for search and retrieval.
 */
export interface Episode {
    id: string;
    sessionId: string;
    domain: EpisodeDomain;
    track: string;
    phase: EpisodePhase;
    toolCalls: ToolCallRecord[];
    artifacts: ArtifactRecord[];
    outcomeSummary: string;
    createdAt: Date;
}

// ─────────────────────────────────────────────────────────────────────────────
// SERVICE INPUT / OUTPUT TYPES
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Input parameters for storing an episode.
 *
 * @see EpisodeStore.storeExperience()
 */
export interface StoreExperienceParams {
    sessionId: string;
    domain: EpisodeDomain;
    track: string;
    phase: EpisodePhase;
    toolCalls?: ToolCallRecord[];
    artifacts?: ArtifactRecord[];
    outcomeSummary: string;
}

/**
 * Result returned after successfully storing an episode.
 */
export interface StoreExperienceResult {
    episodeId: string;
    createdAt: Date;
}

// ─────────────────────────────────────────────────────────────────────────────
// EPISODE SIZE LIMIT CONSTANT
// ─────────────────────────────────────────────────────────────────────────────

/** Maximum serialized JSON size for an episode payload: 5MB */
export const EPISODE_MAX_SIZE_BYTES = 5 * 1024 * 1024;
