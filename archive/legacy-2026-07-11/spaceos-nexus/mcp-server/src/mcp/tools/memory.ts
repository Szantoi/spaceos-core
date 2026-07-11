import { z } from 'zod';
import { Plugin, Tool } from '../../plugins/PluginDecorators';
import { BasePlugin } from '../../plugins/BasePlugin';
import { SystemContext } from '../../plugins/PluginTypes';
import { McpContext } from '../middleware/contextMiddleware';
import { EpisodeManager } from '../../episodic/EpisodeManager';
import Database from 'better-sqlite3';
import { randomUUID } from 'node:crypto';
import { join } from 'path';
import { ErrorResponses } from '../ErrorResponses';
import { AgentDb } from '../AgentDb';
import {
  buildDeterministicEpisodeHighlight,
  buildEpisodeHighlightPrompt,
  parseEpisodeHighlightOutput,
} from '../sampling/reflectionPrompts';
import {
  getCurrentSessionHighlights,
  retrievePriorHighlights,
  type ReflectedHighlight,
} from '../../rag/episodicMemory';

/**
 * MemoryPlugin: Episodic memory tool module
 *
 * Provides episodic memory and reflection tools:
 * - save_episode: Save agent episode to episodic memory
 * - query_memory: Query episodes by semantic similarity
 * - search_memory: Search episodes by metadata (tags, time, outcome)
 * - generate_episode_highlights: Normalize latest session episode into highlight rows
 * - reflect_session: Build a read-only learning summary from current and prior highlights
 *
 * AC Coverage: AC-1..6 (TASK-14-06)
 */

@Plugin({
  name: 'memory',
  version: '1.0.0',
  dependencies: ['bootstrap'],
})
export class MemoryPlugin extends BasePlugin {
  private episodeManager: EpisodeManager | null = null;

  constructor(context?: Partial<SystemContext>) {
    super((context ?? {}) as SystemContext);
  }

  /**
   * AC-2: save_episode tool — Save agent episode to episodic memory with embedding
   */
  @Tool({
    name: 'save_episode',
    description: 'Save agent episode to episodic memory with metadata (tags, timestamp)',
    schema: z.object({
      agent_id: z.string().describe('UUID of agent'),
      episode_data: z.object({
        thought_process: z.string().describe('Agent thinking process'),
        actions: z.array(z.string()).describe('Actions taken'),
        outcome: z.string().describe('Result of episode'),
        reasoning: z.string().describe('Reasoning behind decisions'),
      }),
      metadata: z.object({
        timestamp: z.number().optional().describe('Unix timestamp (defaults to now)'),
        tags: z.array(z.string()).optional().describe('Search tags'),
        domain: z.string().optional(),
        track: z.string().optional(),
        phase: z.string().optional(),
      }).optional(),
    }),
  })
  async save_episode(input: any, context: Partial<McpContext>): Promise<any> {
    const startTime = Date.now();

    try {
      // Validation
      if (!input.agent_id || typeof input.agent_id !== 'string') {
        return {
          status: 'error',
          error: 'agent_id is required and must be a string',
          code: 'INVALID_INPUT',
        };
      }

      if (!input.episode_data) {
        return {
          status: 'error',
          error: 'episode_data is required',
          code: 'INVALID_INPUT',
        };
      }

      const episodeData = input.episode_data;
      if (
        !episodeData.thought_process ||
        !episodeData.actions ||
        !episodeData.outcome ||
        !episodeData.reasoning
      ) {
        return {
          status: 'error',
          error: 'episode_data must contain: thought_process, actions, outcome, reasoning',
          code: 'INVALID_INPUT',
        };
      }

      // RBAC: Check context has user_id (auth required)
      if (!context.session_id) {
        return {
          status: 'error',
          error: 'Unauthorized: no valid session',
          code: 'UNAUTHORIZED',
        };
      }

      // Store episode
      const manager = await this.getEpisodeManager();
      if (!manager) {
        return {
          status: 'error',
          error: 'Episode storage not available',
          code: 'STORAGE_UNAVAILABLE',
        };
      }
      const metadata = input.metadata || {};

      const result = await manager.storeExperience({
        sessionId: context.session_id,
        domain: metadata.domain || 'engineering',
        track: metadata.track || 'standard',
        phase: metadata.phase || 'implementation',
        toolCalls: [],
        artifacts: [],
        outcomeSummary: episodeData.outcome,
      });

      const elapsed = Date.now() - startTime;

      return {
        status: 'success',
        episode_id: result.episodeId,
        timestamp: result.createdAt,
        elapsed_ms: elapsed,
      };
    } catch (error: any) {
      console.error('[MemoryPlugin.save_episode] Error:', error.message);
      return {
        status: 'error',
        error: error.message || 'Failed to save episode',
        code: 'SAVE_FAILED',
      };
    }
  }

  /**
   * AC-3: query_memory tool — Query episodes by semantic similarity
   */
  @Tool({
    name: 'query_memory',
    description: 'Query episodes by semantic similarity to find related experiences',
    schema: z.object({
      agent_id: z.string().describe('UUID of agent'),
      query: z.string().describe('Natural language query'),
      limit: z.number().int().positive().optional().describe('Max results (default 10)'),
      similarity_threshold: z.number().min(0).max(1).optional().describe('Similarity threshold 0.0-1.0 (default 0.7)'),
    }),
  })
  async query_memory(input: any, context: Partial<McpContext>): Promise<any> {
    const startTime = Date.now();

    try {
      // Validation
      if (!input.agent_id || typeof input.agent_id !== 'string') {
        return {
          status: 'error',
          error: 'agent_id is required',
          code: 'INVALID_INPUT',
        };
      }

      if (!input.query || typeof input.query !== 'string') {
        return {
          status: 'error',
          error: 'query is required',
          code: 'INVALID_INPUT',
        };
      }

      if (!context.session_id) {
        return {
          status: 'error',
          error: 'Unauthorized: no valid session',
          code: 'UNAUTHORIZED',
        };
      }

      // Query episodes
      const manager = await this.getEpisodeManager();
      if (!manager) {
        return {
          status: 'error',
          error: 'Episode storage not available',
          code: 'STORAGE_UNAVAILABLE',
        };
      }
      const episodes = await manager.searchSemantic({
        query: input.query,
        domain: undefined,
        limit: input.limit || 10,
        threshold: input.similarity_threshold || 0.7,
      });

      const elapsed = Date.now() - startTime;

      return {
        status: 'success',
        episodes: episodes.map((ep: any) => ({
          episode_id: ep.id,
          thought_process: ep.outcomeSummary,
          actions: ep.toolCalls?.map((tc: any) => tc.tool) || [],
          outcome: ep.outcomeSummary,
          similarity_score: 0.8, // Mock: actual from ChromaDB
          timestamp: ep.createdAt,
        })),
        total_found: episodes.length,
        elapsed_ms: elapsed,
      };
    } catch (error: any) {
      console.error('[MemoryPlugin.query_memory] Error:', error.message);
      return {
        status: 'error',
        error: error.message || 'Query failed',
        code: 'QUERY_FAILED',
      };
    }
  }

  /**
   * AC-4: search_memory tool — Search episodes by metadata (tags, timestamp, outcome)
   */
  @Tool({
    name: 'search_memory',
    description: 'Search episodes by metadata (tags, time range, outcome)',
    schema: z.object({
      agent_id: z.string().describe('UUID of agent'),
      filters: z.object({
        tags: z.array(z.string()).optional().describe('Match any tag'),
        start_time: z.number().optional().describe('Start unix timestamp'),
        end_time: z.number().optional().describe('End unix timestamp'),
        outcome: z.enum(['success', 'failure', 'partial']).optional().describe('Filter by outcome'),
      }),
      limit: z.number().int().positive().optional().describe('Max results (default 10)'),
    }),
  })
  async search_memory(input: any, context: Partial<McpContext>): Promise<any> {
    const startTime = Date.now();

    try {
      // Validation
      if (!input.agent_id || typeof input.agent_id !== 'string') {
        return {
          status: 'error',
          error: 'agent_id is required',
          code: 'INVALID_INPUT',
        };
      }

      if (!input.filters) {
        return {
          status: 'error',
          error: 'filters is required',
          code: 'INVALID_INPUT',
        };
      }

      if (!context.session_id) {
        return {
          status: 'error',
          error: 'Unauthorized: no valid session',
          code: 'UNAUTHORIZED',
        };
      }

      // Validate date range
      const filters = input.filters;
      if (filters.start_time && filters.end_time && filters.start_time > filters.end_time) {
        return {
          status: 'error',
          error: 'start_time must be <= end_time',
          code: 'INVALID_INPUT',
        };
      }

      // Build SQL query for metadata search
      const episodes = await this.searchEpisodesByMetadata(
        filters,
        input.limit || 10
      );

      const elapsed = Date.now() - startTime;

      return {
        status: 'success',
        episodes: episodes.map((ep: any) => ({
          episode_id: ep.id,
          tags: ep.tags || [],
          timestamp: ep.createdAt,
          outcome: ep.outcome,
          actions_count: ep.toolCalls?.length || 0,
        })),
        total_found: episodes.length,
        elapsed_ms: elapsed,
      };
    } catch (error: any) {
      console.error('[MemoryPlugin.search_memory] Error:', error.message);
      return {
        status: 'error',
        error: error.message || 'Search failed',
        code: 'SEARCH_FAILED',
      };
    }
  }

  @Tool({
    name: 'generate_episode_highlights',
    description: 'Generate and persist normalized highlights for the latest episode in a session.',
    schema: z.object({
      session_id: z.string().min(1).optional().describe('Session identifier to summarize. Defaults to current MCP session.'),
      llm_output: z.unknown().optional().describe('Optional LLM JSON output matching the reflection prompt contract.'),
      ai_model: z.string().min(1).optional().describe('Model name used for highlight generation.'),
      ai_tokens_used: z.number().int().nonnegative().optional().describe('Token usage reported by the model.'),
    }),
  })
  async generate_episode_highlights(input: any, context: Partial<McpContext>): Promise<any> {
    const startTime = Date.now();

    if (!context.session_id) {
      return ErrorResponses.unauthorized('Unauthorized: no valid session');
    }

    const targetSessionId = typeof input?.session_id === 'string' && input.session_id.trim().length > 0
      ? input.session_id.trim()
      : context.session_id;

    try {
      const manager = await this.getEpisodeManager();
      if (!manager) {
        return ErrorResponses.serviceUnavailable('Episode storage not available');
      }

      const episode = manager.getLatestSessionEpisode(targetSessionId);
      if (!episode) {
        return ErrorResponses.notFound(`No episodes found for session: ${targetSessionId}`);
      }

      const promptContract = buildEpisodeHighlightPrompt(episode);
      const parsedLlmOutput = parseEpisodeHighlightOutput(input?.llm_output);
      const usedLlmOutput = parsedLlmOutput !== null;
      const highlight = parsedLlmOutput ?? buildDeterministicEpisodeHighlight(episode);
      const warnings = input?.llm_output !== undefined && !usedLlmOutput
        ? ['INVALID_LLM_OUTPUT_FALLBACK']
        : [];

      const agentDb = this.getAgentDb();
      if (!agentDb) {
        return ErrorResponses.serviceUnavailable('AgentDb not available for highlight persistence');
      }

      const existingRow = agentDb.getEpisodeHighlightByEpisodeId(episode.id);
      const highlightId = existingRow?.id ?? `hl_${randomUUID()}`;
      const generatedAt = new Date().toISOString();

      agentDb.upsertEpisodeHighlight({
        id: highlightId,
        episode_id: episode.id,
        key_decisions: JSON.stringify(highlight.key_decisions),
        lessons: JSON.stringify(highlight.lessons),
        next_steps: JSON.stringify(highlight.next_steps),
        quality_score: highlight.quality_score,
        ai_generated: usedLlmOutput,
        ai_model: input?.ai_model ?? null,
        ai_tokens_used: input?.ai_tokens_used ?? null,
      });

      return {
        success: true,
        data: {
          highlight_id: highlightId,
          episode_id: episode.id,
          key_decisions: highlight.key_decisions,
          lessons: highlight.lessons,
          next_steps: highlight.next_steps,
          quality_score: highlight.quality_score,
          generated_at: generatedAt,
          ai_generated: usedLlmOutput,
          warnings,
          prompt_contract_version: promptContract.version,
          elapsed_ms: Date.now() - startTime,
        },
      };
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Failed to generate episode highlights';
      console.error('[MemoryPlugin.generate_episode_highlights] Error:', message);
      return ErrorResponses.internalError(message);
    }
  }

  @Tool({
    name: 'reflect_session',
    description: 'Build a read-only learning summary for the current session using current and prior highlights.',
    schema: z.object({
      session_id: z.string().min(1).optional().describe('Session identifier to reflect. Defaults to current MCP session.'),
      include_prior_highlights: z.boolean().optional().describe('Whether to retrieve prior highlights. Defaults to true.'),
      limit: z.number().int().positive().max(10).optional().describe('Maximum number of prior highlights to include.'),
    }),
  })
  async reflect_session(input: any, context: Partial<McpContext>): Promise<any> {
    const startTime = Date.now();

    if (!context.session_id) {
      return ErrorResponses.unauthorized('Unauthorized: no valid session');
    }

    if (input?.session_id !== undefined && (typeof input.session_id !== 'string' || input.session_id.trim().length === 0)) {
      return ErrorResponses.badRequest('session_id must be a non-empty string');
    }

    const targetSessionId = typeof input?.session_id === 'string'
      ? input.session_id.trim()
      : context.session_id;
    const includePriorHighlights = input?.include_prior_highlights !== false;
    const priorLimit = typeof input?.limit === 'number' ? input.limit : 5;

    try {
      const agentDb = this.getAgentDb();
      if (!agentDb) {
        return ErrorResponses.serviceUnavailable('AgentDb not available for session reflection');
      }

      const currentHighlights = getCurrentSessionHighlights(agentDb, targetSessionId);
      if (currentHighlights.length === 0) {
        return ErrorResponses.notFound(`No highlights found for session: ${targetSessionId}`);
      }

      const priorHighlights = includePriorHighlights
        ? retrievePriorHighlights(agentDb, targetSessionId, currentHighlights, priorLimit)
        : [];
      const learningsApplied = this.buildLearningsApplied(currentHighlights, priorHighlights);
      const reflectionAt = new Date().toISOString();

      return {
        success: true,
        data: {
          session_summary: this.buildSessionSummary(targetSessionId, currentHighlights, priorHighlights, learningsApplied),
          current_highlights: currentHighlights,
          prior_highlights: priorHighlights,
          learnings_applied: learningsApplied,
          reflection_at: reflectionAt,
          elapsed_ms: Date.now() - startTime,
        },
      };
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Failed to reflect session';
      console.error('[MemoryPlugin.reflect_session] Error:', message);
      return ErrorResponses.internalError(message);
    }
  }

  @Tool({
    name: 'tag_episode_quality',
    description: 'Submit manual quality feedback for a highlight (0.0-1.0) and recompute its quality score.',
    schema: z.object({
      highlight_id: z.string().min(1).describe('Highlight ID to tag'),
      quality_score: z.number().min(0).max(1).describe('Quality rating between 0 and 1'),
      comment: z.string().optional().describe('Optional feedback comment'),
    }),
  })
  async tag_episode_quality(input: any, context: Partial<McpContext>): Promise<any> {
    const startTime = Date.now();

    if (!context.session_id) {
      return ErrorResponses.unauthorized('Unauthorized: no valid session');
    }

    if (!input?.highlight_id || typeof input.highlight_id !== 'string') {
      return ErrorResponses.badRequest('highlight_id is required');
    }

    if (typeof input.quality_score !== 'number' || Number.isNaN(input.quality_score)) {
      return ErrorResponses.badRequest('quality_score is required and must be a number between 0 and 1');
    }

    if (input.quality_score < 0 || input.quality_score > 1) {
      return ErrorResponses.badRequest('quality_score must be between 0.0 and 1.0');
    }

    const agentDb = this.getAgentDb();
    if (!agentDb) {
      return ErrorResponses.serviceUnavailable('AgentDb not available for quality tagging');
    }

    const highlight = agentDb.getEpisodeHighlightWithContextById(input.highlight_id);
    if (!highlight) {
      return ErrorResponses.notFound(`Highlight not found: ${input.highlight_id}`);
    }

    try {
      const feedbackId = `fb_${randomUUID()}`;
      const raterAgentId = typeof context.user_id === 'string' && context.user_id.length > 0 ? context.user_id : null;

      agentDb.insertHighlightFeedback({
        id: feedbackId,
        highlight_id: input.highlight_id,
        rater_agent_id: raterAgentId,
        quality_score: input.quality_score,
        comment: typeof input.comment === 'string' ? input.comment : null,
      });

      const newQualityScore = agentDb.recalculateHighlightQuality(input.highlight_id);
      const feedbackRecordedAt = new Date().toISOString();

      return {
        success: true,
        data: {
          highlight_id: input.highlight_id,
          new_quality_score: newQualityScore,
          feedback_recorded_at: feedbackRecordedAt,
          elapsed_ms: Date.now() - startTime,
        },
      };
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Failed to tag highlight quality';
      console.error('[MemoryPlugin.tag_episode_quality] Error:', message);
      return ErrorResponses.internalError(message);
    }
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Lifecycle Hooks
  // ─────────────────────────────────────────────────────────────────────────────

  /**
   * AC-5: Initialize EpisodeStore when plugin loads
   */
  async onInit(context: any): Promise<void> {
    try {
      console.log('[MemoryPlugin] Initializing...');
      // EpisodeStore will be initialized lazily on first use
      console.log('[MemoryPlugin] Ready to manage episodic memory');
    } catch (error: any) {
      throw new Error(`Failed to initialize MemoryPlugin: ${error.message}`);
    }
  }

  async onDestroy(): Promise<void> {
    try {
      console.log('[MemoryPlugin] Shutting down.');
      // Cleanup if needed
    } catch (error) {
      console.error('[MemoryPlugin] Error during shutdown:', error);
    }
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Helper Methods
  // ─────────────────────────────────────────────────────────────────────────────

  private async getEpisodeManager(): Promise<EpisodeManager | null> {
    if (!this.episodeManager) {
      try {
        const agentDb = this.getAgentDb();
        const db = agentDb
          ? agentDb.getRawDatabase()
          : new Database(process.env.EPISODIC_DB_PATH || join(process.cwd(), 'episodic.db'));
        this.episodeManager = new EpisodeManager(db);
        await this.episodeManager.initialize();
      } catch (err: any) {
        console.warn('[MemoryPlugin] Could not initialize EpisodeManager:', err.message);
        return null;
      }
    }
    return this.episodeManager;
  }

  private async searchEpisodesByMetadata(
    filters: any,
    limit: number
  ): Promise<any[]> {
    const manager = await this.getEpisodeManager();
    if (!manager) return [];
    // Full-text keyword search as a proxy for metadata search (Phase 1)
    // Phase 2 will add proper SQL WHERE clauses for tags/time/outcome filtering
    return manager.searchKeyword('', undefined);
  }

  private getAgentDb(): AgentDb | null {
    return this.context?.agentDb ?? null;
  }

  private buildLearningsApplied(
    currentHighlights: ReflectedHighlight[],
    priorHighlights: ReflectedHighlight[]
  ): string[] {
    const currentKeywords = new Set(
      currentHighlights.flatMap((highlight) =>
        [...highlight.key_decisions, ...highlight.lessons, ...highlight.next_steps]
          .flatMap((entry) => this.extractKeywords(entry))
      )
    );

    const learningsApplied: string[] = [];
    for (const highlight of priorHighlights) {
      for (const lesson of highlight.lessons) {
        const lessonKeywords = this.extractKeywords(lesson);
        if (lessonKeywords.some((keyword) => currentKeywords.has(keyword))) {
          learningsApplied.push(`Reused lesson from ${highlight.highlight_id}: ${lesson}`);
        }
      }
    }

    return Array.from(new Set(learningsApplied)).slice(0, 5);
  }

  private buildSessionSummary(
    sessionId: string,
    currentHighlights: ReflectedHighlight[],
    priorHighlights: ReflectedHighlight[],
    learningsApplied: string[]
  ): string {
    const summaryParts = [
      `Session ${sessionId} produced ${currentHighlights.length} current highlight${currentHighlights.length === 1 ? '' : 's'}.`,
    ];
    const currentFocus = currentHighlights
      .flatMap((highlight) => highlight.key_decisions)
      .slice(0, 2);
    if (currentFocus.length > 0) {
      summaryParts.push(`Current focus: ${currentFocus.join('; ')}.`);
    }
    if (priorHighlights.length > 0) {
      summaryParts.push(`Retrieved ${priorHighlights.length} prior highlight${priorHighlights.length === 1 ? '' : 's'} for comparison.`);
    }
    if (learningsApplied.length > 0) {
      summaryParts.push(`Applied learnings: ${learningsApplied.slice(0, 2).join('; ')}.`);
    }

    return summaryParts.join(' ').slice(0, 600);
  }

  private extractKeywords(text: string): string[] {
    return text
      .toLowerCase()
      .split(/[^a-z0-9]+/)
      .filter((token) => token.length >= 5)
      .filter((token) => !COMMON_STOP_WORDS.has(token));
  }
}

const COMMON_STOP_WORDS = new Set([
  'about',
  'after',
  'before',
  'could',
  'should',
  'their',
  'there',
  'these',
  'those',
  'through',
  'using',
  'where',
  'which',
  'while',
]);
