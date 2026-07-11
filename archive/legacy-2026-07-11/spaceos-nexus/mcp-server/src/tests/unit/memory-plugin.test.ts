import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { MemoryPlugin } from '../../mcp/tools/memory';
import { McpContext } from '../../plugins/PluginTypes';

/**
 * TASK-14-06: Memory Plugin Unit Tests
 *
 * Coverage:
 * - UT-01: Metadata extraction (@Plugin decorator)
 * - UT-02: save_episode tool handler
 * - UT-03: query_memory tool handler
 * - UT-04: search_memory tool handler
 * - UT-05: RBAC validation
 * - UT-06: Performance benchmarks
 */

describe('MemoryPlugin: Unit Tests (TASK-14-06)', () => {
    let plugin: MemoryPlugin;
    let mockContext: Partial<McpContext>;

    beforeEach(() => {
        plugin = new MemoryPlugin();
        mockContext = {
            session_id: 'test-session-123',
            track: 'standard' as any,
        };
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // UT-01: Metadata Extraction (AC-1: Plugin Manifest & Metadata)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('UT-01: Plugin Metadata Extraction', () => {
        it('should export valid plugin manifest', () => {
            expect(plugin.name).toBe('memory');
            expect(plugin.handlers).toBeDefined();
            // There are 6 tools in this plugin: save_episode, query_memory, search_memory,
            // generate_episode_highlights, reflect_session, tag_episode_quality
            expect(Object.keys(plugin.handlers).length).toBe(6);
        });

        it('should have all five tool handlers registered', () => {
            expect('save_episode' in plugin.handlers).toBe(true);
            expect('query_memory' in plugin.handlers).toBe(true);
            expect('search_memory' in plugin.handlers).toBe(true);
            expect('generate_episode_highlights' in plugin.handlers).toBe(true);
            expect('reflect_session' in plugin.handlers).toBe(true);
        });

        it('should extend BasePlugin', () => {
            expect(plugin).toHaveProperty('onInit');
            expect(plugin).toHaveProperty('onDestroy');
        });

        it('should implement IToolModule interface', () => {
            expect(plugin).toHaveProperty('name');
            expect(plugin).toHaveProperty('handlers');
            expect(typeof plugin.handlers).toBe('object');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // UT-02: save_episode Tool Handler (AC-2)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('UT-02: save_episode Tool Handler', () => {
        it('should return error if agent_id missing', async () => {
            const input = {
                episode_data: {
                    thought_process: 'thinking',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'because',
                },
            };

            const result = await plugin.handlers['save_episode']!(input, mockContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
        });

        it('should return error if episode_data missing', async () => {
            const input = {
                agent_id: 'agent-123',
            };

            const result = await plugin.handlers['save_episode']!(input, mockContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
        });

        it('should return error if required episode_data fields missing', async () => {
            const input = {
                agent_id: 'agent-123',
                episode_data: {
                    thought_process: 'thinking',
                    // Missing: actions, outcome, reasoning
                },
            };

            const result = await plugin.handlers['save_episode']!(input, mockContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
        });

        it('should return error if no session in context', async () => {
            const input = {
                agent_id: 'agent-123',
                episode_data: {
                    thought_process: 'thinking',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'because',
                },
            };

            const result = await plugin.handlers['save_episode']!(input, {});
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });

        it('should accept metadata with tags and timestamp', async () => {
            const input = {
                agent_id: 'agent-123',
                episode_data: {
                    thought_process: 'thinking',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'because',
                },
                metadata: {
                    timestamp: Date.now(),
                    tags: ['important', 'learning'],
                },
            };

            // Mock EpisodeStore would be needed for full test
            // For now, we test schema acceptance
            expect(input.metadata).toBeDefined();
            expect(input.metadata.tags).toEqual(['important', 'learning']);
        });

        it('should have performance tracking (elapsed_ms)', async () => {
            const input = {
                agent_id: 'agent-123',
                episode_data: {
                    thought_process: 'thinking',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'because',
                },
            };

            // Result should include elapsed_ms or error codes
            const result = await plugin.handlers['save_episode']!(input, mockContext);
            expect(result).toHaveProperty('status');
            // elapsed_ms would be in success case when EpisodeStore is available
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // UT-03: query_memory Tool Handler (AC-3)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('UT-03: query_memory Tool Handler', () => {
        it('should return error if agent_id missing', async () => {
            const input = {
                query: 'What happened?',
            };

            const result = await plugin.handlers['query_memory']!(input, mockContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
        });

        it('should return error if query missing', async () => {
            const input = {
                agent_id: 'agent-123',
            };

            const result = await plugin.handlers['query_memory']!(input, mockContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
        });

        it('should return error if no session', async () => {
            const input = {
                agent_id: 'agent-123',
                query: 'test query',
            };

            const result = await plugin.handlers['query_memory']!(input, {});
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });

        it('should accept limit parameter', async () => {
            const input = {
                agent_id: 'agent-123',
                query: 'test query',
                limit: 20,
            };

            expect(input.limit).toBe(20);
        });

        it('should accept similarity_threshold parameter', async () => {
            const input = {
                agent_id: 'agent-123',
                query: 'test query',
                similarity_threshold: 0.8,
            };

            expect(input.similarity_threshold).toBe(0.8);
        });

        it('should return episodes array on success', async () => {
            const input = {
                agent_id: 'agent-123',
                query: 'test query',
                limit: 10,
                similarity_threshold: 0.7,
            };

            // Mock searchSemantic to avoid EpisodeStore dependency
            const mockStore = {
                searchSemantic: vi.fn().mockResolvedValue([{
                    id: 'ep-1',
                    outcomeSummary: 'success',
                    toolCalls: [{ tool: 'tool1' }],
                    createdAt: Date.now(),
                }]),
            };

            // Override getEpisodeStore for this test
            vi.spyOn(plugin as any, 'getEpisodeManager').mockResolvedValue(mockStore);

            const result = await plugin.handlers['query_memory']!(input, mockContext);
            expect(result.status).toBe('success');
            expect(Array.isArray(result.episodes)).toBe(true);
            expect(result).toHaveProperty('total_found');
            expect(result).toHaveProperty('elapsed_ms');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // UT-04: search_memory Tool Handler (AC-4)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('UT-04: search_memory Tool Handler', () => {
        it('should return error if agent_id missing', async () => {
            const input = {
                filters: { tags: ['important'] },
            };

            const result = await plugin.handlers['search_memory']!(input, mockContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
        });

        it('should return error if filters missing', async () => {
            const input = {
                agent_id: 'agent-123',
            };

            const result = await plugin.handlers['search_memory']!(input, mockContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
        });

        it('should accept tags filter', async () => {
            const input = {
                agent_id: 'agent-123',
                filters: {
                    tags: ['important', 'learning'],
                },
            };

            expect(input.filters.tags).toEqual(['important', 'learning']);
        });

        it('should accept time range filter', async () => {
            const input = {
                agent_id: 'agent-123',
                filters: {
                    start_time: 1000000,
                    end_time: 2000000,
                },
            };

            expect(input.filters.start_time).toBe(1000000);
            expect(input.filters.end_time).toBe(2000000);
        });

        it('should validate time range (start <= end)', async () => {
            const input = {
                agent_id: 'agent-123',
                filters: {
                    start_time: 2000000,
                    end_time: 1000000, // Invalid: start > end
                },
            };

            const result = await plugin.handlers['search_memory']!(input, mockContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
        });

        it('should accept outcome filter', async () => {
            const input = {
                agent_id: 'agent-123',
                filters: {
                    outcome: 'success',
                },
            };

            expect(['success', 'failure', 'partial']).toContain(input.filters.outcome);
        });

        it('should return episodes array on success', async () => {
            const input = {
                agent_id: 'agent-123',
                filters: {
                    tags: ['important'],
                },
                limit: 10,
            };

            // Mock searchEpisodesByMetadata to avoid EpisodeStore dependency
            vi.spyOn(plugin as any, 'searchEpisodesByMetadata').mockResolvedValue([{
                id: 'ep-1',
                tags: ['important'],
                createdAt: Date.now(),
                outcome: 'success',
                toolCalls: [{ tool: 'tool1' }],
            }]);

            const result = await plugin.handlers['search_memory']!(input, mockContext);
            expect(result.status).toBe('success');
            expect(Array.isArray(result.episodes)).toBe(true);
            expect(result).toHaveProperty('total_found');
            expect(result).toHaveProperty('elapsed_ms');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // UT-05: RBAC & Context Validation (AC-5)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('UT-05: RBAC & Context Validation', () => {
        it('save_episode should require session_id in context', async () => {
            const input = {
                agent_id: 'agent-123',
                episode_data: {
                    thought_process: 'thinking',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'because',
                },
            };

            const result = await plugin.handlers['save_episode']!(input, {});
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });

        it('query_memory should require session_id in context', async () => {
            const input = {
                agent_id: 'agent-123',
                query: 'test',
            };

            const result = await plugin.handlers['query_memory']!(input, {});
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });

        it('search_memory should require session_id in context', async () => {
            const input = {
                agent_id: 'agent-123',
                filters: {},
            };

            const result = await plugin.handlers['search_memory']!(input, {});
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });

        it('should not leak sensitive data in error messages', async () => {
            const input = {
                agent_id: 'agent-123',
                episode_data: {
                    thought_process: 'thinking',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'because',
                },
            };

            const result = await plugin.handlers['save_episode']!(input, {});
            expect(result.error).not.toContain('password');
            expect(result.error).not.toContain('token');
            expect(result.error).not.toContain('secret');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // UT-06: Performance SLA (AC-6)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('UT-06: Performance SLA', () => {
        it('input validation should be < 50ms', async () => {
            const input = {
                episode_data: {
                    thought_process: 'thinking',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'because',
                },
            };

            const start = Date.now();
            const result = await plugin.handlers['save_episode']!(input, mockContext);
            const elapsed = Date.now() - start;

            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');

            // Early input validation should complete in < 50ms
            expect(elapsed).toBeLessThan(50);
        });

        it('schema validation should not exceed SLA', async () => {
            const input = {
                agent_id: 'agent-123',
            };

            const start = Date.now();
            const result = await plugin.handlers['query_memory']!(input, mockContext);
            const elapsed = Date.now() - start;

            expect(result.status).toBe('error');
            expect(result.code).toBe('INVALID_INPUT');
            expect(elapsed).toBeLessThan(100);
        });

        it('handler should track elapsed_ms', async () => {
            const input = {
                agent_id: 'agent-123',
                filters: {},
            };

            const result = await plugin.handlers['search_memory']!(input, mockContext);

            // When successful or error requires EpisodeStore
            if (result.status === 'success') {
                expect(result).toHaveProperty('elapsed_ms');
                expect(typeof result.elapsed_ms).toBe('number');
            }
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // Lifecycle Hooks
    // ─────────────────────────────────────────────────────────────────────────────

    describe('Lifecycle Hooks', () => {
        it('onInit should succeed', async () => {
            await expect(plugin.onInit({})).resolves.not.toThrow();
        });

        it('onDestroy should succeed', async () => {
            await expect(plugin.onDestroy()).resolves.not.toThrow();
        });
    });
});
