import { GuardrailService } from '../../roles/GuardrailService';
import * as path from 'path';

const databaseRoot = path.join(__dirname, '..', '..', '..', 'database');

async function testCache() {
    console.log("=== 🛡️ Testing Guardrail Caching Directly ===");

    // Use an invalid mock domain so it fails validation (fallback to default string) quickly,
    // but the LLM still evaluates it.
    const service = new GuardrailService(databaseRoot);

    const domain = 'engineering';
    const role = 'backend_developer';
    const prompt = 'Please sort this binary tree.';
    const response = 'Here is the python code to sort a binary tree...';

    console.log(`\\n▶️ CALL 1: LLM Evaluation`);
    const res1 = await service.checkCompliance(domain, role, prompt, response);
    console.log(`Verdict 1: ${res1.verdict}`);

    console.log(`\\n▶️ CALL 2: Expected Cache Hit`);
    const res2 = await service.checkCompliance(domain, role, prompt, response);
    console.log(`Verdict 2: ${res2.verdict}`);

    console.log('\\n=== 🔚 Cache Test Completed ===');
}

testCache().catch(console.error);
