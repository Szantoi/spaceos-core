import { ChromaClient, Collection } from 'chromadb';
import { GoogleGenerativeAIEmbeddings } from '@langchain/google-genai';
import { MemoryVectorStore } from "@langchain/classic/vectorstores/memory";
import { Document } from "@langchain/core/documents";
import * as dotenv from 'dotenv';
import { Episode } from './types';
import { EmbeddingCache } from './EmbeddingCache';

dotenv.config();

/**
 * ChromaDB semantic search for episodic memory.
 * Follows the pattern of `src/rag/VectorStore.ts`.
 */
export class EpisodicChromaClient {
    private collection: Collection | null = null;
    private memoryStore: MemoryVectorStore | null = null;
    private client: ChromaClient;
    private embeddings: GoogleGenerativeAIEmbeddings;
    private cache: EmbeddingCache;
    private isChromaConnected = false;

    constructor() {
        this.client = new ChromaClient();
        this.embeddings = new GoogleGenerativeAIEmbeddings({
            apiKey: process.env.GOOGLE_API_KEY,
            modelName: 'gemini-embedding-001',
        });
        this.cache = new EmbeddingCache(this.embeddings);
    }

    async init(): Promise<void> {
        try {
            await this.client.heartbeat();
            this.collection = await this.client.getOrCreateCollection({
                name: 'mcp_episodes',
                metadata: { description: 'Episodic memory for MCP agent sessions' },
            });
            this.isChromaConnected = true;
            console.log('🟢 [Episodic] Connected to local ChromaDB server.');
        } catch (err) {
            console.warn('⚠️ [Episodic] ChromaDB not available, falling back to MemoryVectorStore');
            this.isChromaConnected = false;
            this.memoryStore = new MemoryVectorStore(this.embeddings);
        }
    }

    async addEpisode(episode: Episode): Promise<void> {
        // Skip indexing when semantic layer is not initialized.
        if (!this.isChromaConnected && !this.memoryStore) {
            return;
        }

        const embedding = await this.cache.getEmbedding(episode.outcomeSummary, 'doc');

        if (this.isChromaConnected && this.collection) {
            await this.collection.add({
                ids: [episode.id],
                embeddings: [embedding],
                documents: [episode.outcomeSummary],
                metadatas: [{
                    domain: episode.domain,
                    phase: episode.phase,
                    session_id: episode.sessionId,
                    track: episode.track
                }],
            });
        } else if (this.memoryStore) {
            await this.memoryStore.addVectors(
                [embedding],
                [new Document({
                    pageContent: episode.outcomeSummary,
                    metadata: {
                        id: episode.id,
                        domain: episode.domain,
                        phase: episode.phase,
                        session_id: episode.sessionId,
                        track: episode.track
                    }
                })]
            );
        }
    }

    /**
     * Searches for episodes based on semantic similarity.
     * @returns Array of Episode IDs matching the query and threshold.
     */
    async searchIds(params: {
        query: string;
        domain?: string;
        limit?: number;
        threshold?: number;
    }): Promise<string[]> {
        const { query, domain, limit = 10, threshold = 0.5 } = params;
        const embedding = await this.cache.getEmbedding(query, 'query');

        if (this.isChromaConnected && this.collection) {
            const results = await this.collection.query({
                queryEmbeddings: [embedding],
                nResults: limit,
                where: domain ? { domain: { "$eq": domain } } : undefined
            });

            if (!results || !results.ids || results.ids.length === 0) return [];

            const ids: string[] = [];
            const distances = results.distances ? results.distances[0] : [];
            const sourceIds = results.ids[0];

            for (let i = 0; i < sourceIds.length; i++) {
                const distance = distances[i] ?? 1;
                const similarity = 1 - distance;

                if (similarity >= threshold) {
                    ids.push(sourceIds[i]);
                }
            }
            return ids;
        } else if (this.memoryStore) {
            const results = await this.memoryStore.similaritySearchVectorWithScore(embedding, limit * 2);

            return results
                .filter(([doc, score]) => {
                    // For cosine similarity in LangChain, score is similar to similarity (higher is better)
                    // but implementation specific. We'll assume threshold mapping is needed or just use results.
                    if (domain && doc.metadata.domain !== domain) return false;
                    return score >= threshold;
                })
                .slice(0, limit)
                .map(([doc]) => doc.metadata.id);
        }

        return [];
    }

    /**
     * Get cache metrics.
     */
    getCacheMetrics() {
        return this.cache.getMetrics();
    }
}
