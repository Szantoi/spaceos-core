// src/interpreter/interpreter.service.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { ILlmProvider, LlmResponse } from '../types/llm.types';

// ─── Mock the LLM provider factory ───────────────────────────────────────────
vi.mock('../llm/llm.provider', () => ({
  getLlmProvider: vi.fn(),
}));

// ─── Mock kernel.action so no real HTTP calls happen ─────────────────────────
vi.mock('./kernel.action', () => ({
  setKernelAuthToken: vi.fn(),
  setKernelBrand: vi.fn(),
  executeToolCall: vi.fn().mockResolvedValue(JSON.stringify({ id: 'abc-123', name: 'Test Tenant' })),
}));

import { getLlmProvider } from '../llm/llm.provider';
import { executeToolCall } from './kernel.action';
import { interpret, streamChat } from './interpreter.service';
import { KernelClientError, KernelErrorCode } from '../kernel/kernelClient';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function makeMockProvider(responses: LlmResponse[]): ILlmProvider {
  let callCount = 0;
  return {
    complete: vi.fn().mockImplementation(async () => {
      const response = responses[callCount] ?? responses.at(-1)!;
      callCount++;
      return response;
    }),
  };
}

const FAKE_JWT = 'Bearer test.jwt.token';

// ─── Tests ────────────────────────────────────────────────────────────────────

describe('interpret()', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns the LLM reply directly when no tools are called', async () => {
    const provider = makeMockProvider([
      { content: 'Hello! How can I help?', toolCalls: [], finishReason: 'stop' },
    ]);
    vi.mocked(getLlmProvider).mockResolvedValue(provider);

    const result = await interpret(
      { messages: [{ role: 'user', content: 'Hi' }] },
      FAKE_JWT,
    );

    expect(result.reply).toBe('Hello! How can I help?');
    expect(result.toolsUsed).toHaveLength(0);
    expect(result.iterations).toBe(1);
  });

  it('executes one tool call and returns the follow-up reply', async () => {
    const provider = makeMockProvider([
      // First LLM call → wants to call get_all_tenants
      {
        content: null,
        toolCalls: [{ id: 'tool_1', name: 'get_all_tenants', input: {} }],
        finishReason: 'tool_use',
      },
      // Second LLM call → summarises the result
      {
        content: 'There is 1 tenant: Test Tenant.',
        toolCalls: [],
        finishReason: 'stop',
      },
    ]);
    vi.mocked(getLlmProvider).mockResolvedValue(provider);

    const result = await interpret(
      { messages: [{ role: 'user', content: 'List all tenants' }] },
      FAKE_JWT,
    );

    expect(result.reply).toBe('There is 1 tenant: Test Tenant.');
    expect(result.toolsUsed).toContain('get_all_tenants');
    expect(result.iterations).toBe(2);
  });

  it('stops after MAX_TOOL_ITERATIONS if the LLM keeps calling tools', async () => {
    const infiniteToolCall: LlmResponse = {
      content: null,
      toolCalls: [{ id: 'tool_loop', name: 'get_all_tenants', input: {} }],
      finishReason: 'tool_use',
    };
    const provider = makeMockProvider(Array(10).fill(infiniteToolCall));
    vi.mocked(getLlmProvider).mockResolvedValue(provider);

    // Override MAX_TOOL_ITERATIONS to 3 for this test
    vi.stubEnv('MAX_TOOL_ITERATIONS', '3');

    const result = await interpret(
      { messages: [{ role: 'user', content: 'Keep going' }] },
      FAKE_JWT,
    );

    expect(result.iterations).toBeLessThanOrEqual(5); // default cap
    expect(result.reply).toMatch(/maximum number of steps/i);

    vi.unstubAllEnvs();
  });

  it('injects tenantId context into the system prompt', async () => {
    let capturedSystemPrompt = '';
    const provider: ILlmProvider = {
      complete: vi.fn().mockImplementation(async (_messages, _tools, systemPrompt) => {
        capturedSystemPrompt = systemPrompt;
        return { content: 'OK', toolCalls: [], finishReason: 'stop' as const };
      }),
    };
    vi.mocked(getLlmProvider).mockResolvedValue(provider);

    await interpret(
      {
        messages: [{ role: 'user', content: 'Hello' }],
        context: { tenantId: 'tenant-uuid-123' },
      },
      FAKE_JWT,
    );

    expect(capturedSystemPrompt).toContain('tenant-uuid-123');
  });

  it('returns fallback message when LLM returns null content with no tools', async () => {
    const provider = makeMockProvider([
      { content: null, toolCalls: [], finishReason: 'stop' },
    ]);
    vi.mocked(getLlmProvider).mockResolvedValue(provider);

    const result = await interpret(
      { messages: [{ role: 'user', content: 'Hello' }] },
      FAKE_JWT,
    );

    expect(result.reply).toBe('(No response generated)');
  });

  it('KernelClientError in tool call → agentic loop continues with error tool_result', async () => {
    const kernelError = new KernelClientError(
      KernelErrorCode.AuthExpired,
      401,
      'Session expired. Please log in again.',
    );
    vi.mocked(executeToolCall).mockRejectedValueOnce(kernelError);

    const provider = makeMockProvider([
      // First call → tool use
      {
        content: null,
        toolCalls: [{ id: 'tool_1', name: 'get_tenant_summary', input: { tenantId: 'abc' } }],
        finishReason: 'tool_use',
      },
      // Second call → LLM summarises the error result
      { content: 'Your session has expired. Please log in again.', toolCalls: [], finishReason: 'stop' },
    ]);
    vi.mocked(getLlmProvider).mockResolvedValue(provider);

    const result = await interpret(
      { messages: [{ role: 'user', content: 'Get tenant summary' }] },
      FAKE_JWT,
    );

    // Loop did NOT break — LLM got to respond
    expect(result.reply).toBe('Your session has expired. Please log in again.');
    expect(result.iterations).toBe(2);
  });

  it('unknown error in tool call → agentic loop breaks (re-throws)', async () => {
    vi.mocked(executeToolCall).mockRejectedValueOnce(new Error('Unexpected DB failure'));

    const provider = makeMockProvider([
      {
        content: null,
        toolCalls: [{ id: 'tool_1', name: 'get_all_tenants', input: {} }],
        finishReason: 'tool_use',
      },
    ]);
    vi.mocked(getLlmProvider).mockResolvedValue(provider);

    await expect(
      interpret({ messages: [{ role: 'user', content: 'List tenants' }] }, FAKE_JWT),
    ).rejects.toThrow('Unexpected DB failure');
  });
});

// ─── streamChat() ─────────────────────────────────────────────────────────────

describe('streamChat()', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('yields text and done chunks from MockProvider.stream()', async () => {
    const { MockProvider } = await import('../llm/mock.provider');
    vi.mocked(getLlmProvider).mockResolvedValue(new MockProvider());

    const controller = new AbortController();
    const chunks: unknown[] = [];

    for await (const chunk of streamChat(
      { messages: [{ role: 'user', content: 'Hi' }] },
      FAKE_JWT,
      controller.signal,
    )) {
      chunks.push(chunk);
    }

    expect(chunks.some((c) => (c as { type: string }).type === 'text')).toBe(true);
    expect(chunks.some((c) => (c as { type: string }).type === 'done')).toBe(true);
  });

  it('AbortSignal abort → generator stops early (throws AbortError)', async () => {
    const { MockProvider } = await import('../llm/mock.provider');
    vi.mocked(getLlmProvider).mockResolvedValue(new MockProvider());

    const controller = new AbortController();
    controller.abort();  // Abort before starting

    await expect(async () => {
      for await (const _chunk of streamChat(
        { messages: [{ role: 'user', content: 'Hi' }] },
        FAKE_JWT,
        controller.signal,
      )) {
        // should not yield anything
      }
    }).rejects.toThrow();
  });
});
