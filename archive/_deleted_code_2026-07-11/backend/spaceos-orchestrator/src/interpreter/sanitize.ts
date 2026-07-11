// src/interpreter/sanitize.ts
// Prompt injection guard for tool results — applied before passing content to the LLM.
// OWASP LLM01: never trust external data sources without sanitization.

import type { ToolResultBlock } from '../types/llm.types';
import { KernelErrorCode, KernelClientError } from '../kernel/kernelClient';

// ─── Injection patterns ───────────────────────────────────────────────────────

const INJECTION_PATTERNS: RegExp[] = [
  /ignore\s+(all\s+)?(previous|prior|above)\s+instructions?/i,
  /disregard\s+(all\s+)?(previous|prior|above)\s+instructions?/i,
  /forget\s+(all\s+)?(previous|prior|above)\s+instructions?/i,
  /\n\s*(human|assistant|system)\s*:/i,
  /<\s*(system|human|assistant)\s*>/i,
];

/**
 * Sanitize tool result content before sending it to the LLM.
 * Any content matching a known injection pattern is replaced with [REDACTED].
 */
export function sanitizeToolResultForLlm(content: string): string {
  for (const pattern of INJECTION_PATTERNS) {
    if (pattern.test(content)) {
      return '[REDACTED]';
    }
  }
  return content;
}

/**
 * Sanitize user-supplied message content before passing it to the LLM.
 * Strips control characters that have no meaningful text role and caps length
 * to prevent prompt-stuffing attacks (OWASP LLM01).
 */
export function sanitizeUserContent(content: string): string {
  return content
    .replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, '') // control chars (keep \t \n \r)
    .slice(0, 4096);                                     // max length
}

/**
 * Wrap a tool result string in a ToolResultBlock, sanitizing for prompt injection.
 */
export function wrapToolResult(toolUseId: string, content: string): ToolResultBlock {
  return {
    type: 'tool_result',
    tool_use_id: toolUseId,
    content: sanitizeToolResultForLlm(content),
  };
}

/**
 * Build a structured error ToolResultBlock from a KernelClientError.
 * The LLM receives the error code + message and can react accordingly.
 */
export function buildToolErrorResult(
  toolUseId: string,
  code: KernelErrorCode,
  message: string,
): ToolResultBlock {
  return {
    type: 'tool_result',
    tool_use_id: toolUseId,
    content: JSON.stringify({ error: code, message }),
  };
}
