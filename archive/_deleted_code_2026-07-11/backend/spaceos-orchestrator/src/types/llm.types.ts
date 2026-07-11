// src/types/llm.types.ts

export type MessageRole = 'user' | 'assistant';

export interface Message {
  role: MessageRole;
  content: string | ContentBlock[];
}

export type ContentBlock =
  | TextBlock
  | ToolUseBlock
  | ToolResultBlock;

export interface TextBlock {
  type: 'text';
  text: string;
}

export interface ToolUseBlock {
  type: 'tool_use';
  id: string;
  name: string;
  input: Record<string, unknown>;
}

export interface ToolResultBlock {
  type: 'tool_result';
  tool_use_id: string;
  content: string;
}

// ─── Tool Schema (Anthropic-compatible format) ───────────────────────────────

export interface ToolInputProperty {
  type: 'string' | 'number' | 'boolean' | 'object' | 'array';
  description: string;
  enum?: string[];
}

export interface ToolInputSchema {
  type: 'object';
  properties: Record<string, ToolInputProperty>;
  required: string[];
}

export interface ToolSchema {
  name: string;
  description: string;
  input_schema: ToolInputSchema;
}

// ─── LLM Provider contract ───────────────────────────────────────────────────

export interface LlmResponse {
  content: string | null;
  toolCalls: ToolCall[];
  finishReason: 'stop' | 'tool_use' | 'length' | 'error';
}

export interface ToolCall {
  id: string;
  name: string;
  input: Record<string, unknown>;
}

// ─── SSE streaming chunk types ───────────────────────────────────────────────

export type ChatChunk =
  | { type: 'text'; text: string }
  | { type: 'tool_start'; tool: string }
  | { type: 'done'; toolsUsed: string[]; iterations: number }
  | { type: 'error'; error: string };

/** The single interface every LLM adapter must implement. */
export interface ILlmProvider {
  complete(
    messages: Message[],
    tools: ToolSchema[],
    systemPrompt: string,
  ): Promise<LlmResponse>;

  /** Optional streaming support. If not provided, streamChat() falls back to complete(). */
  stream?(
    messages: Message[],
    tools: ToolSchema[],
    systemPrompt: string,
    signal?: AbortSignal,
  ): AsyncIterable<ChatChunk>;
}

// ─── Chat request/response (from React frontend) ─────────────────────────────

export interface ChatRequest {
  messages: { role: MessageRole; content: string }[];
  context?: {
    tenantId?: string;
    facilityId?: string;
  };
}

export interface ChatResponse {
  reply: string;
  toolsUsed: string[];
  iterations: number;
}
