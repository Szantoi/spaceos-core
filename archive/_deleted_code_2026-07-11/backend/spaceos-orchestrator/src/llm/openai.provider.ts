// src/llm/openai.provider.ts
// OpenAI Chat Completions-compatible provider (works with Google Gemini and any OpenAI-API endpoint).
// Uses native fetch — no extra packages required.
import { env } from '../config/env';
import type {
  ILlmProvider,
  LlmResponse,
  Message,
  ToolCall,
  ToolSchema,
  TextBlock,
  ToolUseBlock,
  ToolResultBlock,
} from '../types/llm.types';

// ─── Internal OpenAI wire types ───────────────────────────────────────────────

interface OpenAIToolCall {
  id: string;
  type: 'function';
  function: { name: string; arguments: string };
}

type OpenAIMessage =
  | { role: 'system' | 'user' | 'assistant'; content: string | null; tool_calls?: OpenAIToolCall[] }
  | { role: 'tool'; tool_call_id: string; content: string };

interface OpenAIResponse {
  choices: Array<{
    message: { content: string | null; tool_calls?: OpenAIToolCall[] };
    finish_reason: 'stop' | 'tool_calls' | 'length';
  }>;
}

// ─── Provider ─────────────────────────────────────────────────────────────────

export class OpenAIProvider implements ILlmProvider {
  private readonly apiKey: string;
  private readonly baseUrl: string;
  private readonly model: string;

  constructor() {
    if (!env.OPENAI_API_KEY) {
      throw new Error('OPENAI_API_KEY is required when LLM_PROVIDER=openai');
    }
    this.apiKey = env.OPENAI_API_KEY;
    this.baseUrl = env.OPENAI_BASE_URL;
    this.model = env.OPENAI_MODEL;
  }

  async complete(
    messages: Message[],
    tools: ToolSchema[],
    systemPrompt: string,
  ): Promise<LlmResponse> {
    try {
      const body: Record<string, unknown> = {
        model: this.model,
        max_tokens: 4096,
        messages: [
          { role: 'system', content: systemPrompt },
          ...this.toOpenAIMessages(messages),
        ],
      };

      if (tools.length > 0) {
        body.tools = this.toOpenAITools(tools);
      }

      const response = await fetch(`${this.baseUrl}/chat/completions`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.apiKey}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        const text = await response.text();
        throw new Error(`OpenAI API error ${response.status}: ${text}`);
      }

      const data = (await response.json()) as OpenAIResponse;
      const choice = data.choices[0];

      const toolCalls: ToolCall[] = (choice.message.tool_calls ?? []).map((tc) => ({
        id: tc.id,
        name: tc.function.name,
        input: JSON.parse(tc.function.arguments) as Record<string, unknown>,
      }));

      const finishReason: LlmResponse['finishReason'] =
        choice.finish_reason === 'tool_calls'
          ? 'tool_use'
          : choice.finish_reason === 'length'
            ? 'length'
            : 'stop';

      return { content: choice.message.content ?? null, toolCalls, finishReason };
    } catch (err) {
      console.error('[OpenAIProvider] complete() error:', err);
      return { content: null, toolCalls: [], finishReason: 'error' };
    }
  }

  // ─── Message format conversion ──────────────────────────────────────────────

  private toOpenAIMessages(messages: Message[]): OpenAIMessage[] {
    const result: OpenAIMessage[] = [];

    for (const msg of messages) {
      if (typeof msg.content === 'string') {
        result.push({ role: msg.role, content: msg.content });
        continue;
      }

      const textBlocks = msg.content.filter((b): b is TextBlock => b.type === 'text');
      const toolUseBlocks = msg.content.filter((b): b is ToolUseBlock => b.type === 'tool_use');
      const toolResultBlocks = msg.content.filter((b): b is ToolResultBlock => b.type === 'tool_result');

      if (toolResultBlocks.length > 0) {
        for (const block of toolResultBlocks) {
          result.push({ role: 'tool', tool_call_id: block.tool_use_id, content: block.content });
        }
      } else if (toolUseBlocks.length > 0) {
        result.push({
          role: 'assistant',
          content: textBlocks.map((b) => b.text).join('') || null,
          tool_calls: toolUseBlocks.map((b) => ({
            id: b.id,
            type: 'function' as const,
            function: { name: b.name, arguments: JSON.stringify(b.input) },
          })),
        });
      } else {
        result.push({ role: msg.role, content: textBlocks.map((b) => b.text).join('') });
      }
    }

    return result;
  }

  // ─── Tool schema mapping: Anthropic → OpenAI ────────────────────────────────

  private toOpenAITools(tools: ToolSchema[]) {
    return tools.map((t) => ({
      type: 'function' as const,
      function: {
        name: t.name,
        description: t.description,
        parameters: t.input_schema,
      },
    }));
  }
}
