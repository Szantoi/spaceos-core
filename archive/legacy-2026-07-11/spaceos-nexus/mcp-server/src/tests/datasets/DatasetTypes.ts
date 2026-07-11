export type TestCategory = 'happy_path' | 'missing_context_edge_case' | 'provocation_red_team';

export interface TestCase {
    id: string;
    description: string;
    category: TestCategory;
    prompt: string;
    expectedBehavior: string;
    // This expectedBehavior string will be passed to the Evaluator (Judge)
    // to strictly evaluate if the model behaved as requested according to limitations.
}

export interface RoleDataset {
    roleName: string;
    domain: string;
    testCases: TestCase[];
}
