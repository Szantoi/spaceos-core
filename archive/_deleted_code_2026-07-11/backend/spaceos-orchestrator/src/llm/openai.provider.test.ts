// src/llm/openai.provider.test.ts
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { OpenAIProvider } from './openai.provider';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function makeResponse(
  content: string | null,
  finish_reason: 'stop' | 'tool_calls' | 'length' = 'stop',
  tool_calls?: Array<{ id: string; type: 'function'; function: { name: string; arguments: string } }>,
) {
  return {
    ok: true,
    text: async () => '',
    json: async () => ({
      choices: [{ message: { content, tool_calls }, finish_reason }],
    }),
  } as unknown as Response;
}

// ─── Tests ────────────────────────────────────────────────────────────────────

describe('OpenAIProvider', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  // ─── Constructor ────────────────────────────────────────────────────────────

  // env is a singleton parsed at module load — must reset modules to change values
  it('throws if OPENAI_API_KEY is missing', async () => {
    vi.resetModules();
    vi.stubEnv('OPENAI_API_KEY', '');
    const { OpenAIProvider: Fresh } = await import('./openai.provider');
    expect(() => new Fresh()).toThrow('OPENAI_API_KEY');
    vi.unstubAllEnvs();
  });

  // ─── Happy path: text response ───────────────────────────────────────────────

  it('returns text content on stop finish_reason', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(makeResponse('Hello!', 'stop')));

    const provider = new OpenAIProvider();
    const result = await provider.complete(
      [{ role: 'user', content: 'Hi' }],
      [],
      'You are helpful.',
    );

    expect(result).toEqual({ content: 'Hello!', toolCalls: [], finishReason: 'stop' });
  });

  // ─── Tool call response ──────────────────────────────────────────────────────

  it('maps tool_calls to toolCalls and sets finishReason tool_use', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        makeResponse(null, 'tool_calls', [
          {
            id: 'call_1',
            type: 'function',
            function: { name: 'get_workstations', arguments: '{"facilityId":"f1"}' },
          },
        ]),
      ),
    );

    const provider = new OpenAIProvider();
    const result = await provider.complete([{ role: 'user', content: 'list' }], [], 'system');

    expect(result.finishReason).toBe('tool_use');
    expect(result.toolCalls).toHaveLength(1);
    expect(result.toolCalls[0]).toEqual({
      id: 'call_1',
      name: 'get_workstations',
      input: { facilityId: 'f1' },
    });
  });

  // ─── finish_reason: length ───────────────────────────────────────────────────

  it('maps finish_reason length correctly', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(makeResponse('truncated', 'length')));

    const provider = new OpenAIProvider();
    const result = await provider.complete([{ role: 'user', content: 'x' }], [], '');
    expect(result.finishReason).toBe('length');
  });

  // ─── Error path ──────────────────────────────────────────────────────────────

  it('returns finishReason error on network failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network down')));

    const provider = new OpenAIProvider();
    const result = await provider.complete([{ role: 'user', content: 'x' }], [], '');
    expect(result).toEqual({ content: null, toolCalls: [], finishReason: 'error' });
  });

  it('returns finishReason error on non-ok HTTP response', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 429,
      text: async () => 'rate limited',
    } as unknown as Response));

    const provider = new OpenAIProvider();
    const result = await provider.complete([{ role: 'user', content: 'x' }], [], '');
    expect(result).toEqual({ content: null, toolCalls: [], finishReason: 'error' });
  });

  // ─── Fetch call shape ────────────────────────────────────────────────────────

  it('sends Authorization header and correct URL', async () => {
    const mockFetch = vi.fn().mockResolvedValue(makeResponse('ok'));
    vi.stubGlobal('fetch', mockFetch);

    const provider = new OpenAIProvider();
    await provider.complete([{ role: 'user', content: 'hi' }], [], 'sys');

    expect(mockFetch).toHaveBeenCalledWith(
      'https://api.example.com/v1/chat/completions',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({ 'Authorization': 'Bearer test-openai-key' }),
      }),
    );
  });

  it('omits tools field when tools array is empty', async () => {
    const mockFetch = vi.fn().mockResolvedValue(makeResponse('ok'));
    vi.stubGlobal('fetch', mockFetch);

    const provider = new OpenAIProvider();
    await provider.complete([{ role: 'user', content: 'hi' }], [], 'sys');

    const body = JSON.parse((mockFetch.mock.calls[0][1] as RequestInit).body as string);
    expect(body.tools).toBeUndefined();
  });

  it('includes mapped tools when tools are provided', async () => {
    const mockFetch = vi.fn().mockResolvedValue(makeResponse('ok'));
    vi.stubGlobal('fetch', mockFetch);

    const provider = new OpenAIProvider();
    await provider.complete(
      [{ role: 'user', content: 'hi' }],
      [{
        name: 'my_tool',
        description: 'does stuff',
        input_schema: { type: 'object', properties: {}, required: [] },
      }],
      'sys',
    );

    const body = JSON.parse((mockFetch.mock.calls[0][1] as RequestInit).body as string);
    expect(body.tools).toHaveLength(1);
    expect(body.tools[0]).toEqual({
      type: 'function',
      function: {
        name: 'my_tool',
        description: 'does stuff',
        parameters: { type: 'object', properties: {}, required: [] },
      },
    });
  });

  // ─── Message conversion ──────────────────────────────────────────────────────

  it('converts tool_result ContentBlocks to role:tool messages', async () => {
    const mockFetch = vi.fn().mockResolvedValue(makeResponse('ok'));
    vi.stubGlobal('fetch', mockFetch);

    const provider = new OpenAIProvider();
    await provider.complete(
      [{
        role: 'user',
        content: [{ type: 'tool_result', tool_use_id: 'tc_1', content: 'result data' }],
      }],
      [],
      'sys',
    );

    const body = JSON.parse((mockFetch.mock.calls[0][1] as RequestInit).body as string);
    const toolMsg = body.messages.find((m: { role: string }) => m.role === 'tool');
    expect(toolMsg).toEqual({ role: 'tool', tool_call_id: 'tc_1', content: 'result data' });
  });
});
