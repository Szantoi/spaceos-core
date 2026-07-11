// src/llm/llm.provider.ts
// Re-exports the ILlmProvider interface and the factory that resolves the active provider.

export type { ILlmProvider, LlmResponse, ToolCall, ToolSchema, Message } from '../types/llm.types';

import { env } from '../config/env';
import type { ILlmProvider } from '../types/llm.types';

let _instance: ILlmProvider | null = null;

/**
 * Returns the singleton LLM provider based on the LLM_PROVIDER env variable.
 * Swap the provider by changing .env — zero code changes required elsewhere.
 */
export async function getLlmProvider(): Promise<ILlmProvider> {
  if (_instance) return _instance;

  switch (env.LLM_PROVIDER) {
    case 'anthropic': {
      const { AnthropicProvider } = await import('./anthropic.provider');
      _instance = new AnthropicProvider();
      break;
    }
    case 'openai': {
      const { OpenAIProvider } = await import('./openai.provider');
      _instance = new OpenAIProvider();
      break;
    }
    case 'mock': {
      const { MockProvider } = await import('./mock.provider');
      _instance = new MockProvider();
      break;
    }
  }

  return _instance!;
}
