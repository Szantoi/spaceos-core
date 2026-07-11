/**
 * EpisodeManager — TASK-12-04: E2E Orchestration Layer
 *
 * High-level facade over EpisodeStore + EpisodicChromaClient.
 * Used for end-to-end workflows: session creation, experience storage,
 * and hybrid retrieval in a single, consistent API.
 *
 * @see TASK-12-04-ASSIGNMENT.md
 */

import Database from 'better-sqlite3';
import { join } from 'path';
import { EpisodeStore } from './EpisodeStore';
import { searchExperience } from './FtsSearch';
import {
    Episode,
    StoreExperienceParams,
    StoreExperienceResult,
    EpisodeDomain,
    EpisodePhase,
} from './types';

// ─────────────────────────────────────────────────────────────────────────────
// TYPES
// ─────────────────────────────────────────────────────────────────────────────

export interface SessionContext {
    sessionId: string;
    domain: EpisodeDomain;
    track: string;
}

export interface HybridSearchParams {
    query: string;
    domain?: EpisodeDomain;
    limit?: number;
    threshold?: number;
}

export interface EpisodeManagerConfig {
    /** Path to SQLite database file. Defaults to ':memory:' for tests. */
    dbPath?: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// EPISODE MANAGER
// ─────────────────────────────────────────────────────────────────────────────

/**
 * EpisodeManager provides a unified API for the full episodic memory workflow:
 *
 * 1. `createSession()` — Initialise a session context.
 * 2. `storeExperience()` — Record an agent experience.
 * 3. `searchHybrid()` — Retrieve via FTS5 + Semantic (ChromaDB) combined search.
 * 4. `searchKeyword()` — Fast keyword-only retrieval.
 * 5. `searchSemantic()` — Semantic-only retrieval via ChromaDB.
 * 6. `getCacheMetrics()` — Inspect embedding cache performance.
 */
export class EpisodeManager {
    private store: EpisodeStore;
    private initialized = false;

    constructor(private readonly db: Database.Database) {
        this.store = new EpisodeStore(db);
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /**
     * Initialize the manager. Must be called before any operations.
     * Connects ChromaDB (or falls back to MemoryVectorStore).
     */
    async initialize(): Promise<void> {
        if (this.initialized) return;
        await this.store.initialize();
        this.initialized = true;
    }

    // ── Session Management ────────────────────────────────────────────────────

    /**
     * Create a new session context (value object).
     * This is a lightweight factory — sessions are not persisted separately.
     */
    createSession(params: SessionContext): SessionContext {
        return { ...params };
    }

    // ── Write Operations ──────────────────────────────────────────────────────

    /**
     * Record an agent experience within a session.
     *
     * @param params - Full experience data including session context.
     * @returns { episodeId, createdAt }
     */
    async storeExperience(params: StoreExperienceParams): Promise<StoreExperienceResult> {
        this.assertInitialized();
        return this.store.storeExperience(params);
    }

    // ── Read / Search Operations ──────────────────────────────────────────────

    /**
     * Hybrid search: FTS5 keyword + ChromaDB semantic, deduplicated.
     * Keyword hits are prioritized in the merged result set.
     */
    async searchHybrid(params: HybridSearchParams): Promise<Episode[]> {
        this.assertInitialized();
        return this.store.searchHybrid(params);
    }

    /**
     * FTS5 keyword-only search.
     */
    searchKeyword(query: string, domain?: EpisodeDomain): Episode[] {
        this.assertInitialized();
        return searchExperience(this.db, query, domain);
    }

    /**
     * ChromaDB semantic-only search.
     */
    async searchSemantic(params: HybridSearchParams): Promise<Episode[]> {
        this.assertInitialized();
        return this.store.searchSemantic(params);
    }

    /**
     * Retrieve a single episode by ID.
     */
    getEpisode(episodeId: string): Episode | undefined {
        this.assertInitialized();
        return this.store.getEpisode(episodeId);
    }

    /**
     * Retrieve all episodes for a session (newest-first).
     */
    getSessionEpisodes(sessionId: string): Episode[] {
        this.assertInitialized();
        return this.store.getEpisodesBySession(sessionId);
    }

    /**
     * Retrieve the newest episode for a session.
     */
    getLatestSessionEpisode(sessionId: string): Episode | undefined {
        this.assertInitialized();
        return this.store.getLatestEpisodeBySession(sessionId);
    }

    // ── Performance ───────────────────────────────────────────────────────────

    /**
     * Return embedding cache metrics.
     * Use to verify 30-50% cost reduction targets (AC-5 / AC-15b).
     */
    getCacheMetrics(): { hits: number; misses: number; hitRate: number } {
        return this.store.getCacheMetrics();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private assertInitialized(): void {
        if (!this.initialized) {
            throw new Error('[EpisodeManager] Not initialized. Call initialize() first.');
        }
    }

    // ── Static Factory ────────────────────────────────────────────────────────

    /**
     * Create and initialize an EpisodeManager from a database path.
     * Convenience factory for production usage.
     */
    static async create(config: EpisodeManagerConfig = {}): Promise<EpisodeManager> {
        const dbPath = config.dbPath ?? join(process.cwd(), 'database', 'mcp.db');
        const db = new Database(dbPath);
        const manager = new EpisodeManager(db);
        await manager.initialize();
        return manager;
    }
}
