// src/llm/mock.provider.ts
// Used for local development and tests — no API key required.
import type { ILlmProvider, LlmResponse, Message, ToolSchema, ChatChunk } from '../types/llm.types';

export class MockProvider implements ILlmProvider {
  async complete(
    messages: Message[],
    _tools: ToolSchema[],
    _systemPrompt: string,
  ): Promise<LlmResponse> {
    const lastMessage = messages.at(-1);
    const content =
      typeof lastMessage?.content === 'string'
        ? lastMessage.content
        : '[mock content]';

    return {
      content: `[MOCK] Echo: "${content}". LLM_PROVIDER=mock — switch to "anthropic" for real responses.`,
      toolCalls: [],
      finishReason: 'stop',
    };
  }

  async *stream(
    messages: Message[],
    tools: ToolSchema[],
    systemPrompt: string,
    signal?: AbortSignal,
  ): AsyncIterable<ChatChunk> {
    signal?.throwIfAborted();
    const result = await this.complete(messages, tools, systemPrompt);
    signal?.throwIfAborted();
    yield { type: 'text', text: result.content ?? '' };
    signal?.throwIfAborted();
    yield { type: 'done', toolsUsed: [], iterations: 1 };
  }
}
