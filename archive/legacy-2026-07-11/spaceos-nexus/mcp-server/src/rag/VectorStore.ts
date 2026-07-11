import { ChromaClient, Collection } from "chromadb";
import { MemoryVectorStore } from "@langchain/classic/vectorstores/memory";
import { GoogleGenerativeAIEmbeddings } from "@langchain/google-genai";
import { Document } from "@langchain/core/documents";
import * as dotenv from "dotenv";

dotenv.config();

// Fallback to Memory Vector Store if ChromaDB is not running locally.
let collection: Collection | null = null;
let memoryStore: MemoryVectorStore | null = null;
let isChromaConnected = false;

const embeddings = new GoogleGenerativeAIEmbeddings({
    apiKey: process.env.GOOGLE_API_KEY,
    modelName: "gemini-embedding-001"
});

/**
 * Initializes the vector database connection.
 * Attempts to connect to a local ChromaDB instance (default: localhost:8000).
 * If it fails, falls back to an in-memory VectorStore.
 */
export async function initVectorStore() {
    if (isChromaConnected || memoryStore) return;

    try {
        const client = new ChromaClient();
        await client.heartbeat(); // test the connection

        collection = await client.getOrCreateCollection({
            name: "knowledge-base",
            metadata: { "description": "JoineryTech.Flow Operative Process Standards and Roles" }
        });
        isChromaConnected = true;
        console.log("🟢 [VDB] Connected to local ChromaDB server.");
    } catch (err) {
        console.warn("⚠️ [VDB] ChromaDB server is not running on localhost:8000.");
        console.warn("   Falling back to MemoryVectorStore for MVP usage.");
        isChromaConnected = false;
        memoryStore = new MemoryVectorStore(embeddings);
    }
}

/**
 * Adds an array of document chunks to the database.
 */
export async function addChunks(chunks: Array<{ text: string, metadata: any }>) {
    await initVectorStore();

    if (isChromaConnected && collection) {
        // Prepare arrays for Chroma batch upsert
        // Filter out empty or whitespace-only chunks to prevent embedding failures
        const validChunks = chunks.filter(c => c.text && c.text.trim().length > 0);

        if (validChunks.length === 0) return;

        const ids = validChunks.map((_, i) => `${validChunks[i].metadata.source}_chunk_${i}_${Date.now()}`);
        const documents = validChunks.map(c => c.text);
        const metadatas = validChunks.map(c => c.metadata);

        // Calculate embeddings using Google Generative AI
        const embeddingsArray = await embeddings.embedDocuments(documents);

        // Filter out any documents where the embedding failed to generate properly
        const validIndices = embeddingsArray.map((emb, idx) => emb.length > 0 ? idx : -1).filter(idx => idx !== -1);

        if (validIndices.length === 0) {
            console.warn(`[VDB] Skipping batch: no valid embeddings generated for ${documents.length} chunks.`);
            return;
        }

        const validIds = validIndices.map(i => ids[i]);
        const validDocuments = validIndices.map(i => documents[i]);
        const validMetadatas = validIndices.map(i => metadatas[i]);
        const validEmbeddings = validIndices.map(i => embeddingsArray[i]);

        await collection.upsert({
            ids: validIds,
            documents: validDocuments,
            embeddings: validEmbeddings,
            metadatas: validMetadatas
        });
    } else if (memoryStore) {
        const lcDocs = chunks.map(c => new Document({ pageContent: c.text, metadata: c.metadata }));
        await memoryStore.addDocuments(lcDocs);
    }
}

/**
 * Queries the knowledge base for relevant standard documents.
 * @param queryText The user question or task description.
 * @param topK Number of chunks to retrieve.
 * @param domain Optional domain filter (e.g. "engineering", "management"). When provided,
 *               only chunks whose `domain` metadata matches will be returned.
 * @returns An array of string chunks matching the query.
 */
export async function queryKnowledge(queryText: string, topK: number = 3, domain?: string): Promise<string[]> {
    await initVectorStore();

    if (isChromaConnected && collection) {
        // Calculate embedding for the query
        const queryEmbedding = await embeddings.embedQuery(queryText);

        const queryOptions: any = {
            queryEmbeddings: [queryEmbedding],
            nResults: topK
        };

        if (domain) {
            queryOptions.where = { domain: { "$eq": domain } };
        }

        const results = await collection.query(queryOptions);

        if (results.documents && results.documents.length > 0) {
            return results.documents[0].filter(doc => doc !== null) as string[];
        }
        return [];
    } else if (memoryStore) {
        // Over-fetch when domain filtering so we can trim to topK after filtering
        const fetchK = domain ? topK * 3 : topK;
        const results = await memoryStore.similaritySearch(queryText, fetchK);

        const filtered = domain
            ? results.filter(doc => doc.metadata.domain === domain)
            : results;

        return filtered.slice(0, topK).map(doc => doc.pageContent);
    }

    return [];
}
