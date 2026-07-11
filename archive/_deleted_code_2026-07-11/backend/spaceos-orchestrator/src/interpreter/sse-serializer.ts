// src/interpreter/sse-serializer.ts
// Server-Sent Events serialization — safe, sanitized SSE output.
// Prevents protocol injection: raw \n or \r in data fields would break SSE framing.

import type { Response } from 'express';
import type { ChatChunk } from '../types/llm.types';

export class SseSerializer {
  /**
   * Escape characters that would break SSE framing.
   * JSON.stringify already escapes \n → \\n, but this provides defense-in-depth.
   */
  static sanitize(text: string): string {
    return text.replace(/\r/g, '\\r').replace(/\n/g, '\\n');
  }

  /**
   * Write a single ChatChunk as an SSE data event.
   */
  static write(res: Response, chunk: ChatChunk): void {
    const json = JSON.stringify(chunk);
    const safe = SseSerializer.sanitize(json);
    res.write(`data: ${safe}\n\n`);
  }
}
