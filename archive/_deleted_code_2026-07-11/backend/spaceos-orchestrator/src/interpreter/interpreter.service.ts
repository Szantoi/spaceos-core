// src/interpreter/interpreter.service.ts
// The agentic loop: NLP → tool_call → Kernel → tool_result → LLM → repeat → final reply.

import { getLlmProvider } from '../llm/llm.provider';
import { DESIGN_PORTAL_TOOLS } from './tool-registry';
import { buildSystemPrompt, type SystemPromptContext } from './system-prompt';
import { executeToolCall, setKernelAuthToken, setKernelBrand } from './kernel.action';
import { wrapToolResult, buildToolErrorResult, sanitizeUserContent } from './sanitize';
import { KernelClientError } from '../kernel/kernelClient';
import { env } from '../config/env';
import type {
  Message,
  ChatRequest,
  ChatResponse,
  ChatChunk,
} from '../types/llm.types';

export async function interpret(
  request: ChatRequest,
  kernelJwt: string,
  brand?: string,
): Promise<ChatResponse> {
  // Forward the user's JWT and brand header to all Kernel calls made in this request
  setKernelAuthToken(kernelJwt);
  setKernelBrand(brand);

  const provider   = await getLlmProvider();
  const systemPrompt = buildSystemPrompt(request.context ?? {});

  // Build the initial message history — sanitize user content (OWASP LLM01)
  // Filter out empty assistant turns: the portal sometimes sends content:'' because it
  // reads the JSON /bff/chat response as SSE and never receives a text chunk.
  const messages: Message[] = request.messages
    .filter((m) => !(m.role === 'assistant' && m.content === ''))
    .map((m) => ({
      role:    m.role,
      content: m.role === 'user' ? sanitizeUserContent(m.content) : m.content,
    }));

  let iterations   = 0;
  const toolsUsed: string[] = [];

  // ─── Agentic loop ─────────────────────────────────────────────────────────
  while (iterations < env.MAX_TOOL_ITERATIONS) {
    iterations++;

    const response = await provider.complete(messages, DESIGN_PORTAL_TOOLS, systemPrompt);

    if (response.finishReason === 'stop' || response.toolCalls.length === 0) {
      // LLM has finished — return the final text response
      return {
        reply: response.content ?? '(No response generated)',
        toolsUsed,
        iterations,
      };
    }

    // ── Execute all tool calls — handle KernelClientError gracefully ─────────
    const toolResults = await Promise.all(
      response.toolCalls.map(async (call) => {
        toolsUsed.push(call.name);
        try {
          const result = await executeToolCall(call);
          // Sanitize for prompt injection before returning to LLM
          return wrapToolResult(call.id, result);
        } catch (err) {
          if (err instanceof KernelClientError) {
            // Structured error → LLM tool_result, loop continues
            return buildToolErrorResult(call.id, err.code, err.message);
          }
          throw err;  // Unknown error → loop breaks
        }
      }),
    );

    // ── Append assistant turn + tool results to history ─────────────────────
    // Assistant message must include the tool_use blocks
    messages.push({
      role: 'assistant',
      content: [
        ...(response.content ? [{ type: 'text' as const, text: response.content }] : []),
        ...response.toolCalls.map((c) => ({
          type: 'tool_use' as const,
          id:    c.id,
          name:  c.name,
          input: c.input,
        })),
      ],
    });

    // User turn with all tool results
    messages.push({
      role: 'user',
      content: toolResults,
    });
  }

  // Guard: max iterations reached
  return {
    reply: 'I reached the maximum number of steps for this request. Please try a more specific question.',
    toolsUsed,
    iterations,
  };
}

/**
 * Streaming version of interpret() — yields ChatChunks for SSE.
 * Uses provider.stream() if available; falls back to complete() and yields once.
 * AbortSignal from req.on('close') propagates to cancel the generation.
 */
export async function* streamChat(
  request: ChatRequest,
  kernelJwt: string,
  signal: AbortSignal,
  brand?: string,
): AsyncGenerator<ChatChunk> {
  setKernelAuthToken(kernelJwt);
  setKernelBrand(brand);

  const provider     = await getLlmProvider();
  const systemPrompt = buildSystemPrompt(request.context ?? {});
  const messages: Message[] = request.messages
    .filter((m) => !(m.role === 'assistant' && m.content === ''))
    .map((m) => ({
      role:    m.role,
      content: m.role === 'user' ? sanitizeUserContent(m.content) : m.content,
    }));

  if (provider.stream) {
    // Full streaming — LLM chunks arrive as they are generated
    const stream = provider.stream(messages, DESIGN_PORTAL_TOOLS, systemPrompt, signal);
    for await (const chunk of stream) {
      signal.throwIfAborted();
      yield chunk;
    }
    return;
  }

  // Fallback: use complete() and yield the entire reply as one text chunk
  signal.throwIfAborted();
  const result = await interpret(request, kernelJwt, brand);
  signal.throwIfAborted();
  yield { type: 'text', text: result.reply };
  yield { type: 'done', toolsUsed: result.toolsUsed, iterations: result.iterations };
}
