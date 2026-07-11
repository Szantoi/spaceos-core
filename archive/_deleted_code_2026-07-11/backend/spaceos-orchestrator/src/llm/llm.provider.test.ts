// src/llm/llm.provider.test.ts
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('getLlmProvider factory edge cases', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('LLM_PROVIDER=openai with no OPENAI_API_KEY throws', async () => {
    vi.stubEnv('LLM_PROVIDER', 'openai');
    vi.stubEnv('OPENAI_API_KEY', '');
    const { getLlmProvider } = await import('./llm.provider');
    await expect(getLlmProvider()).rejects.toThrow('OPENAI_API_KEY');
  });

  it('LLM_PROVIDER=anthropic with no ANTHROPIC_API_KEY throws', async () => {
    vi.stubEnv('LLM_PROVIDER', 'anthropic');
    vi.stubEnv('ANTHROPIC_API_KEY', '');
    const { getLlmProvider } = await import('./llm.provider');
    await expect(getLlmProvider()).rejects.toThrow('ANTHROPIC_API_KEY');
  });

  it('LLM_PROVIDER=mock returns a MockProvider instance', async () => {
    vi.stubEnv('LLM_PROVIDER', 'mock');
    const { getLlmProvider } = await import('./llm.provider');
    const { MockProvider } = await import('./mock.provider');
    const provider = await getLlmProvider();
    expect(provider).toBeInstanceOf(MockProvider);
  });
});
