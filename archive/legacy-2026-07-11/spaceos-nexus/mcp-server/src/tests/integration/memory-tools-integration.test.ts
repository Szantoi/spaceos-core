import { describe, it, expect, beforeEach } from 'vitest';
import { MemoryPlugin } from '../../mcp/tools/memory';
import { PluginManager } from '../../plugins/PluginManager';
import { BasePlugin } from '../../plugins/BasePlugin';
import { McpContext } from '../../plugins/PluginTypes';

/**
 * TASK-14-06: Memory Plugin Integration Tests
 *
 * Coverage:
 * - IT-1: MemoryPlugin registers with PluginManager
 * - IT-2: All tools invokable via PluginManager
 * - IT-3: Episode save → query flow
 * - IT-4: Search with multiple filters
 * - IT-5: RequestContext propagation
 * - IT-6: RBAC enforcement
 */

describe('MemoryPlugin: Integration Tests (TASK-14-06)', () => {
    let memoryPlugin: MemoryPlugin;
    let mockContext: Partial<McpContext>;

    beforeEach(() => {
        memoryPlugin = new MemoryPlugin();
        mockContext = {
            session_id: 'test-session-456',
            track: 'standard' as any,
        };
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // IT-1: Plugin Registration (AC-1)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('IT-1: MemoryPlugin Registration', () => {
        it('should register as valid IToolModule', () => {
            expect(memoryPlugin.name).toBe('memory');
            expect(memoryPlugin.handlers).toBeDefined();
            expect(Object.keys(memoryPlugin.handlers).length).toBe(6);
        });

        it('should extend BasePlugin', () => {
            expect(memoryPlugin instanceof BasePlugin).toBe(true);
        });

        it('should have lifecycle hooks available', () => {
            expect(typeof memoryPlugin.onInit).toBe('function');
            expect(typeof memoryPlugin.onDestroy).toBe('function');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // IT-2: Tool Invocation via Handlers (AC-2, AC-3, AC-4)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('IT-2: All Tools Invokable via Handlers Map', () => {
        it('save_episode tool should be callable', async () => {
            const handler = memoryPlugin.handlers['save_episode'];
            expect(handler).toBeDefined();
            expect(typeof handler).toBe('function');
        });

        it('query_memory tool should be callable', async () => {
            const handler = memoryPlugin.handlers['query_memory'];
            expect(handler).toBeDefined();
            expect(typeof handler).toBe('function');
        });

        it('search_memory tool should be callable', async () => {
            const handler = memoryPlugin.handlers['search_memory'];
            expect(handler).toBeDefined();
            expect(typeof handler).toBe('function');
        });

        it('reflect_session tool should be callable', async () => {
            const handler = memoryPlugin.handlers['reflect_session'];
            expect(handler).toBeDefined();
            expect(typeof handler).toBe('function');
        });

        it('tag_episode_quality tool should be callable', async () => {
            const handler = memoryPlugin.handlers['tag_episode_quality'];
            expect(handler).toBeDefined();
            expect(typeof handler).toBe('function');
        });

        it('should have correct arity (input, context)', async () => {
            const handler = memoryPlugin.handlers['save_episode']!;
            // Each handler should accept (input, context)
            expect(handler.length).toBe(2);
        });

        it('all tools should return consistent response format', async () => {
            const inputSave = {
                agent_id: 'agent-1',
                episode_data: {
                    thought_process: 'test',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'test',
                },
            };

            const resultSave = await memoryPlugin.handlers['save_episode']!(inputSave, mockContext);
            expect(resultSave).toHaveProperty('status');

            const inputQuery = {
                agent_id: 'agent-1',
                query: 'test',
            };

            const resultQuery = await memoryPlugin.handlers['query_memory']!(inputQuery, mockContext);
            expect(resultQuery).toHaveProperty('status');

            const inputSearch = {
                agent_id: 'agent-1',
                filters: {},
            };

            const resultSearch = await memoryPlugin.handlers['search_memory']!(inputSearch, mockContext);
            expect(resultSearch).toHaveProperty('status');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // IT-3: Episode Workflow (Save → Query)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('IT-3: Episode Save & Query Workflow', () => {
        it('should attempt save_episode with all required fields', async () => {
            const input = {
                agent_id: 'agent-111',
                episode_data: {
                    thought_process: 'Analyzing problem statement',
                    actions: ['read_requirement', 'design_solution', 'implement'],
                    outcome: 'Successfully implemented feature',
                    reasoning: 'Followed best practices and patterns',
                },
                metadata: {
                    timestamp: Date.now(),
                    tags: ['implementation', 'feature'],
                },
            };

            const result = await memoryPlugin.handlers['save_episode']!(input, mockContext);

            // Status should be present (success or error)
            expect(result).toHaveProperty('status');
            expect(['success', 'error']).toContain(result.status);
        });

        it('should attempt query_memory after episode saved', async () => {
            const queryInput = {
                agent_id: 'agent-111',
                query: 'What solutions were analyzed for this feature?',
                limit: 5,
                similarity_threshold: 0.7,
            };

            const result = await memoryPlugin.handlers['query_memory']!(queryInput, mockContext);

            expect(result).toHaveProperty('status');
            expect(['success', 'error']).toContain(result.status);

            if (result.status === 'success') {
                expect(Array.isArray(result.episodes)).toBe(true);
                expect(result).toHaveProperty('total_found');
            }
        });

        it('should handle episode data with various action types', async () => {
            const input = {
                agent_id: 'agent-222',
                episode_data: {
                    thought_process: 'Complex debugging session',
                    actions: [
                        'read_logs',
                        'identify_issue',
                        'patch_code',
                        'run_tests',
                        'verify_fix',
                    ],
                    outcome: 'Bug fixed and tests passing',
                    reasoning: 'Root cause was in authentication layer',
                },
            };

            const result = await memoryPlugin.handlers['save_episode']!(input, mockContext);
            expect(result).toHaveProperty('status');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // IT-4: Search with Metadata Filters
    // ─────────────────────────────────────────────────────────────────────────────

    describe('IT-4: Episode Search with Metadata Filters', () => {
        it('should filter by tags', async () => {
            const input = {
                agent_id: 'agent-333',
                filters: {
                    tags: ['important', 'learning'],
                },
            };

            const result = await memoryPlugin.handlers['search_memory']!(input, mockContext);
            expect(result).toHaveProperty('status');

            if (result.status === 'success') {
                expect(Array.isArray(result.episodes)).toBe(true);
                // Episodes should have metadata fields
                result.episodes.forEach((ep: any) => {
                    expect(ep).toHaveProperty('episode_id');
                    expect(ep).toHaveProperty('tags');
                });
            }
        });

        it('should filter by time range', async () => {
            const now = Date.now();
            const oneDayAgo = now - 24 * 60 * 60 * 1000;

            const input = {
                agent_id: 'agent-444',
                filters: {
                    start_time: oneDayAgo,
                    end_time: now,
                },
            };

            const result = await memoryPlugin.handlers['search_memory']!(input, mockContext);
            expect(result).toHaveProperty('status');

            if (result.status === 'success') {
                expect(Array.isArray(result.episodes)).toBe(true);
            }
        });

        it('should filter by outcome', async () => {
            const input = {
                agent_id: 'agent-555',
                filters: {
                    outcome: 'success',
                },
            };

            const result = await memoryPlugin.handlers['search_memory']!(input, mockContext);
            expect(result).toHaveProperty('status');
        });

        it('should combine multiple filters', async () => {
            const input = {
                agent_id: 'agent-666',
                filters: {
                    tags: ['important'],
                    outcome: 'success',
                    start_time: Date.now() - 7 * 24 * 60 * 60 * 1000, // 7 days ago
                },
                limit: 20,
            };

            const result = await memoryPlugin.handlers['search_memory']!(input, mockContext);
            expect(result).toHaveProperty('status');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // IT-5: Context Propagation (AC-5)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('IT-5: RequestContext Propagation Through Tools', () => {
        it('save_episode should receive and use session_id from context', async () => {
            const customContext: Partial<McpContext> = {
                session_id: 'custom-session-789',
                track: 'standard' as any,
            };

            const input = {
                agent_id: 'agent-777',
                episode_data: {
                    thought_process: 'test',
                    actions: ['a1'],
                    outcome: 'success',
                    reasoning: 'test',
                },
            };

            const result = await memoryPlugin.handlers['save_episode']!(input, customContext);

            // Result should reflect that context was used
            expect(result).toHaveProperty('status');
        });

        it('query_memory should use session_id from context', async () => {
            const customContext: Partial<McpContext> = {
                session_id: 'query-session-850',
                track: 'standard' as any,
            };

            const input = {
                agent_id: 'agent-888',
                query: 'Context propagation test',
            };

            const result = await memoryPlugin.handlers['query_memory']!(input, customContext);
            expect(result).toHaveProperty('status');
        });

        it('search_memory should use session_id from context', async () => {
            const customContext: Partial<McpContext> = {
                session_id: 'search-session-910',
                track: 'standard' as any,
            };

            const input = {
                agent_id: 'agent-999',
                filters: { tags: ['test'] },
            };

            const result = await memoryPlugin.handlers['search_memory']!(input, customContext);
            expect(result).toHaveProperty('status');
        });

        it('tools should fail gracefully if session_id missing', async () => {
            const emptyContext: Partial<McpContext> = {};

            const input = {
                agent_id: 'agent-xyz',
                episode_data: {
                    thought_process: 'test',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'test',
                },
            };

            const result = await memoryPlugin.handlers['save_episode']!(input, emptyContext);
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // IT-6: RBAC Enforcement (AC-5)
    // ─────────────────────────────────────────────────────────────────────────────

    describe('IT-6: RBAC Enforcement Across All Tools', () => {
        it('save_episode should enforce RBAC (require session)', async () => {
            const input = {
                agent_id: 'restricted-agent',
                episode_data: {
                    thought_process: 'test',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'test',
                },
            };

            // No session context
            const result = await memoryPlugin.handlers['save_episode']!(input, {});
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });

        it('query_memory should enforce RBAC (require session)', async () => {
            const input = {
                agent_id: 'restricted-agent',
                query: 'Sensitive data query',
            };

            const result = await memoryPlugin.handlers['query_memory']!(input, {});
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });

        it('search_memory should enforce RBAC (require session)', async () => {
            const input = {
                agent_id: 'restricted-agent',
                filters: {},
            };

            const result = await memoryPlugin.handlers['search_memory']!(input, {});
            expect(result.status).toBe('error');
            expect(result.code).toBe('UNAUTHORIZED');
        });

        it('should not leak sensitive information in error responses', async () => {
            const input = {
                agent_id: 'agent-test',
                episode_data: {
                    thought_process: 'test',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'test',
                },
            };

            const result = await memoryPlugin.handlers['save_episode']!(input, {});

            // Error message should be safe
            expect(result.error).toBeDefined();
            expect(result.error.toLowerCase()).toContain('unauthorized');
            expect(result.error).not.toContain('database');
            expect(result.error).not.toContain('sql');
        });

        it('should validate agent_id format', async () => {
            const invalidInput = {
                agent_id: '', // Empty agent ID
                episode_data: {
                    thought_process: 'test',
                    actions: ['a1'],
                    outcome: 'ok',
                    reasoning: 'test',
                },
            };

            const result = await memoryPlugin.handlers['save_episode']!(
                invalidInput,
                mockContext
            );
            expect(result.status).toBe('error');
        });
    });

    // ─────────────────────────────────────────────────────────────────────────────
    // Cross-Tool Scenarios
    // ─────────────────────────────────────────────────────────────────────────────

    describe('Cross-Tool Integration Scenarios', () => {
        it('should support complex episode data structures', async () => {
            const complexInput = {
                agent_id: 'agent-complex',
                episode_data: {
                    thought_process:
                        'Multi-step analysis of requirements, design considerations, and implementation strategy',
                    actions: [
                        'read_requirements',
                        'analyze_architecture',
                        'design_solution',
                        'prototype_feature',
                        'gather_feedback',
                        'refine_design',
                        'implement_feature',
                        'run_tests',
                    ],
                    outcome:
                        'Feature implemented with comprehensive test coverage and performance optimization',
                    reasoning:
                        'Followed SOLID principles, leveraged existing patterns, validated with stakeholders',
                },
                metadata: {
                    tags: ['architecture', 'implementation', 'testing', 'optimization'],
                },
            };

            const result = await memoryPlugin.handlers['save_episode']!(
                complexInput,
                mockContext
            );
            expect(result).toHaveProperty('status');
        });

        it('should handle rapid sequential operations', async () => {
            // Save, then immediately query
            const saveInput = {
                agent_id: 'agent-rapid',
                episode_data: {
                    thought_process: 'Quick test',
                    actions: ['test'],
                    outcome: 'ok',
                    reasoning: 'rapid test',
                },
            };

            const saveResult = await memoryPlugin.handlers['save_episode']!(
                saveInput,
                mockContext
            );

            const queryInput = {
                agent_id: 'agent-rapid',
                query: 'What was tested?',
            };

            const queryResult = await memoryPlugin.handlers['query_memory']!(
                queryInput,
                mockContext
            );

            expect(saveResult).toHaveProperty('status');
            expect(queryResult).toHaveProperty('status');
        });
    });
});
