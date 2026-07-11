// src/llm/anthropic.provider.ts
import Anthropic from '@anthropic-ai/sdk';
import { env } from '../config/env';
import type {
  ILlmProvider,
  LlmResponse,
  Message,
  ToolCall,
  ToolSchema,
} from '../types/llm.types';

export class AnthropicProvider implements ILlmProvider {
  private readonly client: Anthropic;
  private readonly model = 'claude-sonnet-4-20250514';

  constructor() {
    if (!env.ANTHROPIC_API_KEY) {
      throw new Error('ANTHROPIC_API_KEY is required when LLM_PROVIDER=anthropic');
    }
    this.client = new Anthropic({ apiKey: env.ANTHROPIC_API_KEY });
  }

  async complete(
    messages: Message[],
    tools: ToolSchema[],
    systemPrompt: string,
  ): Promise<LlmResponse> {
    const response = await this.client.messages.create({
      model: this.model,
      max_tokens: 4096,
      system: systemPrompt,
      tools: tools as unknown as Anthropic.Tool[],
      messages: messages as Anthropic.MessageParam[],
    });

    const toolCalls: ToolCall[] = response.content
      .filter((b): b is Anthropic.ToolUseBlock => b.type === 'tool_use')
      .map((b) => ({ id: b.id, name: b.name, input: b.input as Record<string, unknown> }));

    const textContent = response.content
      .filter((b): b is Anthropic.TextBlock => b.type === 'text')
      .map((b) => b.text)
      .join('');

    return {
      content: textContent || null,
      toolCalls,
      finishReason: response.stop_reason === 'tool_use' ? 'tool_use' : 'stop',
    };
  }
}
