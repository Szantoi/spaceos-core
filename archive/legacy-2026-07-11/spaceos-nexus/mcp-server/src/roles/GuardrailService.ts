import * as fs from 'fs';
import * as path from 'path';
import * as crypto from 'crypto';
import NodeCache from 'node-cache';
import { checkGuardrailCompliance } from './guardrailEvaluator';

export class GuardrailService {
    private databaseRoot: string;
    private schemaCache: NodeCache;
    private judgmentCache: NodeCache;

    constructor(databaseRoot: string) {
        this.databaseRoot = databaseRoot;
        // Schema cache: 5 minutes TTL
        this.schemaCache = new NodeCache({ stdTTL: 300 });
        // Judgment cache: 1 hour TTL
        this.judgmentCache = new NodeCache({ stdTTL: 3600 });
    }

    private generateHash(domain: string, role: string, prompt: string, response: string): string {
        const hash = crypto.createHash('sha256');
        hash.update(`${domain}|${role}|${prompt}|${response}`);
        return hash.digest('hex');
    }

    /**
     * Checks if the agent's response complies with its role limitations.
     * @param domain The domain of the role (e.g., 'engineering')
     * @param role The role name (e.g., 'backend_developer')
     * @param prompt The original task/prompt requested by the user
     * @param actualResponse The generated string response from the agent
     * @returns A Promise resolving to an object containing the verdict ('PASS' or 'FAIL') and the reasoning
     */
    public async checkCompliance(
        domain: string,
        role: string,
        prompt: string,
        actualResponse: string
    ): Promise<{ verdict: 'PASS' | 'FAIL', reasoning: string }> {
        const startTime = performance.now();
        const judgmentKey = this.generateHash(domain, role, prompt, actualResponse);

        // 1. Check Judgment Cache
        const cachedJudgment = this.judgmentCache.get<{ verdict: 'PASS' | 'FAIL', reasoning: string }>(judgmentKey);
        if (cachedJudgment) {
            const latency = (performance.now() - startTime).toFixed(2);
            console.log(`[Guardrail] Evaluation completed in ${latency}ms (Cache Hit)`);
            return cachedJudgment;
        }

        // 2. Load the raw YAML schema (with caching)
        const schemaKey = `${domain}|${role}`;
        let roleSchemaRaw = this.schemaCache.get<string>(schemaKey);

        if (!roleSchemaRaw) {
            const schemaRawPath = path.join(this.databaseRoot, 'roles', domain, role, `${role}.schema.yaml`);
            try {
                roleSchemaRaw = fs.readFileSync(schemaRawPath, 'utf8');
                this.schemaCache.set(schemaKey, roleSchemaRaw);
            } catch (error: any) {
                console.warn(`[Guardrail] Could not read raw schema for ${domain}/${role}. Validation might be degraded.`, error.message);
                roleSchemaRaw = `Role: ${role}\nDomain: ${domain}\nLimitations: Unknown (Failed to load schema)`;
            }
        }

        console.log(`[Guardrail] Validating response against ${domain}/${role} limitations (LLM Evaluation)...`);

        try {
            // 3. LLM Evaluation
            const evaluation = await checkGuardrailCompliance(roleSchemaRaw, prompt, actualResponse);

            // 4. Save to Cache
            this.judgmentCache.set(judgmentKey, evaluation);

            const latency = (performance.now() - startTime).toFixed(2);
            console.log(`[Guardrail] Evaluation completed in ${latency}ms (LLM)`);

            return evaluation;
        } catch (error: any) {
            console.error(`[Guardrail] Evaluator execution failed. Proceeding with caution.`, error.message);
            throw new Error(`Guardrail Engine Failure: ${error.message}`);
        }
    }
}
