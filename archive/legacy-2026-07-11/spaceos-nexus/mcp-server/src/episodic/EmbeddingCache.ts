import NodeCache from 'node-cache';
import { GoogleGenerativeAIEmbeddings } from '@langchain/google-genai';

/**
 * EmbeddingCache — TASK-12-04: Embedding Cache Implementation
 *
 * Provides synchronous caching for asynchronous Google Generative AI embedding calls.
 * Uses `node-cache` as per project standards.
 */
export class EmbeddingCache {
    /**
     * Document cache: TTL 7 days, max 500 entries.
     */
    private documentCache = new NodeCache({ stdTTL: 7 * 24 * 60 * 60, maxKeys: 500 });

    /**
     * Query cache: TTL 1 day, max 500 entries.
     */
    private queryCache = new NodeCache({ stdTTL: 24 * 60 * 60, maxKeys: 500 });

    private metrics = {
        cacheHit: 0,
        cacheMiss: 0
    };

    private embedModel: GoogleGenerativeAIEmbeddings;

    constructor(embedModel: GoogleGenerativeAIEmbeddings) {
        this.embedModel = embedModel;
    }

    /**
     * Get or fetch embedding for a given text.
     *
     * @param text - The text to embed.
     * @param type - 'doc' for persistent items, 'query' for transient searches.
     * @returns The embedding vector (768 dimensions for gemini-embedding-001).
     */
    async getEmbedding(text: string, type: 'doc' | 'query'): Promise<number[]> {
        const cache = type === 'doc' ? this.documentCache : this.queryCache;

        const cached = cache.get<number[]>(text);
        if (cached) {
            this.metrics.cacheHit++;
            return cached;
        }

        // Fetch new embedding from Google API
        const embedding = await this.embedModel.embedQuery(text);

        cache.set(text, embedding);
        this.metrics.cacheMiss++;

        return embedding;
    }

    /**
     * Return cache performance metrics.
     */
    getMetrics() {
        const total = this.metrics.cacheHit + this.metrics.cacheMiss;
        return {
            hits: this.metrics.cacheHit,
            misses: this.metrics.cacheMiss,
            hitRate: total > 0 ? this.metrics.cacheHit / total : 0,
        };
    }
}
