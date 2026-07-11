// src/llm/mock.provider.test.ts
import { describe, it, expect } from 'vitest';
import { MockProvider } from './mock.provider';

describe('MockProvider', () => {
  it('echoes the last user message', async () => {
    const provider = new MockProvider();
    const result = await provider.complete(
      [{ role: 'user', content: 'Create a tenant' }],
      [],
      '',
    );

    expect(result.finishReason).toBe('stop');
    expect(result.toolCalls).toHaveLength(0);
    expect(result.content).toContain('Create a tenant');
  });

  it('returns [mock content] for non-string content', async () => {
    const provider = new MockProvider();
    const result = await provider.complete(
      [{ role: 'user', content: [{ type: 'text', text: 'hello' }] }],
      [],
      '',
    );

    expect(result.content).toContain('[mock content]');
  });
});
