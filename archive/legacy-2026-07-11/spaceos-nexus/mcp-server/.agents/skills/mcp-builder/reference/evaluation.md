# MCP Server Evaluation Guide

## Requirements
- 10 READ-ONLY, INDEPENDENT questions.
- Each requires multiple tool calls.
- Static, verifiable answers (human-readable preferred).

## Process
1. **Inspection**: Understand API and tools without implementation details.
2. **Exploration**: Use read-only calls (with limits/pagination) to find realistic data.
3. **Generation**: Create XML `qa_pair` elements.
4. **Verification**: Solve tasks yourself to ensure answer stability.

## Format
```xml
<evaluation>
   <qa_pair>
      <question>...</question>
      <answer>...</answer>
   </qa_pair>
</evaluation>
```
