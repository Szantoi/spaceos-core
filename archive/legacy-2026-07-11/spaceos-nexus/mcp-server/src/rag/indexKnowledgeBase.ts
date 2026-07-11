import * as fs from 'fs';
import * as path from 'path';
import * as yaml from 'js-yaml';
import { MarkdownTextSplitter } from "@langchain/textsplitters";
import { addChunks } from "./VectorStore";

const SERVER_DIR = path.resolve(__dirname, '../../');
const DB_PATH = path.resolve(SERVER_DIR, 'database');

/** Recursively finds all `.knowledge.md` files under the given directory. */
async function getKnowledgeFiles(dir: string, fileList: string[] = []): Promise<string[]> {
    const files = await fs.promises.readdir(dir);

    for (const file of files) {
        const filePath = path.join(dir, file);
        const stat = await fs.promises.stat(filePath);

        if (stat.isDirectory()) {
            await getKnowledgeFiles(filePath, fileList);
        } else if (filePath.endsWith('.knowledge.md')) {
            fileList.push(filePath);
        }
    }
    return fileList;
}

/**
 * Parses YAML frontmatter from a markdown file.
 * Returns the extracted metadata object, or an empty object if none is found.
 */
function parseFrontmatter(content: string): Record<string, string> {
    const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---/);
    if (!match) return {};

    try {
        const parsed = yaml.load(match[1]) as Record<string, string>;
        return parsed ?? {};
    } catch {
        return {};
    }
}

export async function buildKnowledgeBaseIndex() {
    console.log("📚 Starting Knowledge Base Indexing...");

    // Validate DB path
    if (!fs.existsSync(DB_PATH)) {
        console.error(`❌ Database path not found: ${DB_PATH}`);
        return;
    }

    const knowledgeFiles = await getKnowledgeFiles(DB_PATH);
    console.log(`🔍 Found ${knowledgeFiles.length} .knowledge.md files in database.`);

    const splitter = new MarkdownTextSplitter({
        chunkSize: 1000,
        chunkOverlap: 200,
    });

    let totalChunks = 0;

    for (const file of knowledgeFiles) {
        const content = await fs.promises.readFile(file, 'utf-8');
        const relativePath = path.relative(DB_PATH, file).replace(/\\/g, '/');

        // Extract frontmatter metadata (name, description, domain, last_updated)
        const frontmatter = parseFrontmatter(content);
        const chunkMetadata = {
            source: relativePath,
            domain: frontmatter.domain ?? 'unknown',
            name: frontmatter.name ?? path.basename(file, '.knowledge.md'),
        };

        const langChainChunks = await splitter.createDocuments([content], [chunkMetadata]);

        if (langChainChunks.length === 0) continue;

        const mappedChunks = langChainChunks.map(c => {
            // ChromaDB only accepts strings, numbers, or booleans as metadata values.
            const sanitizedMetadata: Record<string, any> = {};
            for (const key in c.metadata) {
                if (typeof c.metadata[key] === 'object' && c.metadata[key] !== null) {
                    sanitizedMetadata[key] = JSON.stringify(c.metadata[key]);
                } else {
                    sanitizedMetadata[key] = c.metadata[key];
                }
            }

            return {
                text: c.pageContent,
                metadata: sanitizedMetadata
            };
        });

        await addChunks(mappedChunks);
        totalChunks += mappedChunks.length;
        console.log(`   Indexed: ${relativePath} [domain: ${chunkMetadata.domain}] (${mappedChunks.length} chunks)`);
    }

    console.log(`✅ Indexing completed successfully! Added ${totalChunks} chunks to vector store.`);
}

// Allow to run the script standalone
if (require.main === module) {
    buildKnowledgeBaseIndex()
        .then(() => process.exit(0))
        .catch(error => {
            console.error(error);
            process.exit(1);
        });
}
