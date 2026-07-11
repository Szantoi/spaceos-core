/**
 * EpisodeStore — TASK-12-01: Episode Schema & Storage
 *
 * Provides insert and retrieval operations for the `episodes` SQLite table.
 * Schema is initialized idempotently via loadMigration() on construction.
 *
 * **Key Constraints:**
 * - Max payload: 5MB per episode (AC-3)
 * - Uses better-sqlite3 synchronous API (consistent with project patterns)
 * - UUID generation via crypto.randomUUID() (Node.js 15.7+)
 *
 * **Used by:**
 * - MCP tool: store_experience (TASK-12-01)
 * - FTS5 search layer (TASK-12-02)
 * - ChromaDB semantic search (TASK-12-03)
 *
 * @see TASK-12-01-ASSIGNMENT.md
 */

import Database from 'better-sqlite3';
import { readFileSync } from 'fs';
import { join } from 'path';
import {
    Episode,
    StoreExperienceParams,
    StoreExperienceResult,
    EPISODE_MAX_SIZE_BYTES,
    ToolCallRecord,
    ArtifactRecord,
} from './types';
import { EpisodicChromaClient } from './EpisodicChromaClient';
import { searchExperience } from './FtsSearch';

// ─────────────────────────────────────────────────────────────────────────────
// RAW ROW TYPE (SQLite result)
// ─────────────────────────────────────────────────────────────────────────────

interface EpisodeRow {
    id: string;
    session_id: string;
    domain: string;
    track: string;
    phase: string;
    tool_calls_json: string | null;
    artifacts_json: string | null;
    outcome_summary: string;
    created_at: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// EPISODE STORE
// ─────────────────────────────────────────────────────────────────────────────

export class EpisodeStore {
    private readonly db: Database.Database;
    private readonly chromaClient: EpisodicChromaClient;

    /**
     * Create an EpisodeStore bound to an existing better-sqlite3 connection.
     * Initializes the schema idempotently on construction (CREATE TABLE IF NOT EXISTS).
     *
     * @param db - A writable better-sqlite3 Database instance (admin pool).
     */
    constructor(db: Database.Database) {
        this.db = db;
        this.chromaClient = new EpisodicChromaClient();
        this.initializeSchema();
    }

    /**
     * Initializes the episodic storage system, including ChromaDB connection.
     */
    async initialize(): Promise<void> {
        await this.chromaClient.init();
    }

    // ─── Schema Initialization ────────────────────────────────────────────────

    /**
     * Initialize the episodes table and indexes (idempotent).
     * Loads from the SQL migration file to ensure a single source of truth.
     */
    private initializeSchema(): void {
        try {
            const migrations = ['003_episodes.sql', '004_episodes_fts5.sql'] as const;
            for (const migrationFile of migrations) {
                const migrationPath = join(__dirname, 'migrations', migrationFile);
                const sql = readFileSync(migrationPath, { encoding: 'utf-8' });
                this.db.exec(sql);
            }
        } catch (error) {
            throw new Error(
                `[EpisodeStore] Failed to initialize schema: ${error instanceof Error ? error.message : String(error)}`
            );
        }
    }

    // ─── Write Operations ────────────────────────────────────────────────────

    /**
     * Store a new agent experience (episode).
     *
     * **AC-3:** Throws if total serialized JSON payload exceeds 5MB.
     *
     * @param params - Episode data
     * @returns Stored episode ID and creation timestamp
     * @throws Error with code `episode_size_exceeded` if payload > 5MB
     */
    async storeExperience(params: StoreExperienceParams): Promise<StoreExperienceResult> {
        // AC-3: Enforce 5MB size limit on total JSON payload
        const serialized = JSON.stringify(params);
        if (serialized.length > EPISODE_MAX_SIZE_BYTES) {
            const error = new Error(
                `Episode payload exceeds 5MB limit (got ${serialized.length} bytes)`
            );
            (error as NodeJS.ErrnoException).code = 'episode_size_exceeded';
            throw error;
        }

        const episodeId = this.generateEpisodeId();
        const createdAt = new Date();

        const stmt = this.db.prepare(`
            INSERT INTO episodes (
                id, session_id, domain, track, phase,
                tool_calls_json, artifacts_json, outcome_summary
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        `);

        stmt.run(
            episodeId,
            params.sessionId,
            params.domain,
            params.track,
            params.phase,
            JSON.stringify(params.toolCalls ?? []),
            JSON.stringify(params.artifacts ?? []),
            params.outcomeSummary
        );

        // AC-1: Index in ChromaDB for semantic search
        const episode = this.getEpisode(episodeId);
        if (episode) {
            await this.chromaClient.addEpisode(episode);
        }

        return { episodeId, createdAt };
    }

    // ─── Read Operations ────────────────────────────────────────────────────

    /**
     * Retrieve a single episode by its ID.
     *
     * @param episodeId - Episode UUID
     * @returns Episode entity, or undefined if not found
     */
    getEpisode(episodeId: string): Episode | undefined {
        const stmt = this.db.prepare(`
            SELECT
                id, session_id, domain, track, phase,
                tool_calls_json, artifacts_json, outcome_summary, created_at
            FROM episodes
            WHERE id = ?
        `);

        const row = stmt.get(episodeId) as EpisodeRow | undefined;
        if (!row) return undefined;

        return this.mapRow(row);
    }

    /**
     * Retrieve all episodes for a given session, ordered newest-first.
     *
     * Uses `idx_episodes_session` index for sub-5ms performance on 1000 episodes (AC-2).
     *
     * @param sessionId - Agent session UUID
     * @returns Array of Episode entities (may be empty)
     */
    getEpisodesBySession(sessionId: string): Episode[] {
        const stmt = this.db.prepare(`
            SELECT
                id, session_id, domain, track, phase,
                tool_calls_json, artifacts_json, outcome_summary, created_at
            FROM episodes
            WHERE session_id = ?
            ORDER BY created_at DESC
        `);

        const rows = stmt.all(sessionId) as EpisodeRow[];
        return rows.map(row => this.mapRow(row));
    }

    /**
     * Retrieve the latest episode for a given session.
     */
    getLatestEpisodeBySession(sessionId: string): Episode | undefined {
        const stmt = this.db.prepare(`
            SELECT
                id, session_id, domain, track, phase,
                tool_calls_json, artifacts_json, outcome_summary, created_at
            FROM episodes
            WHERE session_id = ?
            ORDER BY created_at DESC
            LIMIT 1
        `);

        const row = stmt.get(sessionId) as EpisodeRow | undefined;
        return row ? this.mapRow(row) : undefined;
    }

    /**
     * Retrieve episodes filtered by domain, track, and session.
     *
     * Uses `idx_episodes_domain_track_session` composite index for sub-10ms
     * performance on 1000 episodes (AC-2).
     *
     * @param domain - Domain filter
     * @param track - Track filter
     * @param sessionId - Session filter
     * @returns Array of Episode entities
     */
    getEpisodesByDomainTrackSession(
        domain: string,
        track: string,
        sessionId: string
    ): Episode[] {
        const stmt = this.db.prepare(`
            SELECT
                id, session_id, domain, track, phase,
                tool_calls_json, artifacts_json, outcome_summary, created_at
            FROM episodes
            WHERE domain = ? AND track = ? AND session_id = ?
            ORDER BY created_at DESC
        `);

        const rows = stmt.all(domain, track, sessionId) as EpisodeRow[];
        return rows.map(row => this.mapRow(row));
    }

    /**
     * Searches for episodes using semantic similarity.
     */
    async searchSemantic(params: {
        query: string;
        domain?: string;
        limit?: number;
        threshold?: number;
    }): Promise<Episode[]> {
        const ids = await this.chromaClient.searchIds(params);
        if (ids.length === 0) return [];

        // Fetch full episodes from SQLite for the matched IDs
        const episodes: Episode[] = [];
        for (const id of ids) {
            const ep = this.getEpisode(id);
            if (ep) episodes.push(ep);
        }
        return episodes;
    }

    /**
     * Executes a hybrid search combining FTS5 keyword matching and
     * ChromaDB semantic similarity.
     *
     * Merges results and deduplicates by episode ID.
     * Hits in FTS5 are prioritized (simple merge).
     */
    async searchHybrid(params: {
        query: string;
        domain?: string;
        limit?: number;
        threshold?: number;
    }): Promise<Episode[]> {
        const { query, domain, limit = 10, threshold = 0.5 } = params;

        // 1. Keyword search (synchronous)
        const keywordMatches = searchExperience(this.db, query, domain);

        // 2. Semantic search (asynchronous)
        const semanticMatches = await this.searchSemantic({
            query,
            domain,
            limit,
            threshold
        });

        // 3. Merge and deduplicate
        const seen = new Set<string>();
        const merged: Episode[] = [];

        // Add keyword matches first (prioritize exact keyword hits)
        for (const ep of keywordMatches) {
            if (!seen.has(ep.id)) {
                seen.add(ep.id);
                merged.push(ep);
            }
        }

        // Add semantic matches
        for (const ep of semanticMatches) {
            if (!seen.has(ep.id)) {
                seen.add(ep.id);
                merged.push(ep);
            }
        }

        return merged.slice(0, limit);
    }

    /**
     * Exposes embedding cache metrics for runtime observability and tests.
     */
    getCacheMetrics(): { hits: number; misses: number; hitRate: number } {
        return this.chromaClient.getCacheMetrics();
    }

    // ─── Private Helpers ────────────────────────────────────────────────────

    /**
     * Map a raw SQLite row to a typed Episode entity.
     */
    private mapRow(row: EpisodeRow): Episode {
        return {
            id: row.id,
            sessionId: row.session_id,
            domain: row.domain as Episode['domain'],
            track: row.track,
            phase: row.phase as Episode['phase'],
            toolCalls: this.parseJson<ToolCallRecord[]>(row.tool_calls_json, []),
            artifacts: this.parseJson<ArtifactRecord[]>(row.artifacts_json, []),
            outcomeSummary: row.outcome_summary,
            createdAt: new Date(row.created_at),
        };
    }

    /**
     * Safely parse a nullable JSON string with a typed fallback.
     */
    private parseJson<T>(json: string | null, fallback: T): T {
        if (!json) return fallback;
        try {
            return JSON.parse(json) as T;
        } catch {
            return fallback;
        }
    }

    /**
     * Generate a unique episode identifier.
     * Uses crypto.randomUUID() (Node.js 15.7+) prefixed with 'ep_'.
     */
    private generateEpisodeId(): string {
        return `ep_${crypto.randomUUID()}`;
    }
}
