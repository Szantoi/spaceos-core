"""
SpaceOS Marvin Tools — MCP Server Integration
Phase 2.5: McpServer tools wrapped as Marvin-compatible tools

Integrates:
- Knowledge Service search (discoverySearch MCP tool)
- Future: Additional MCP tools from spaceos-nexus/mcp-server

Note: Marvin 3.x doesn't have @tool decorator.
These are plain async functions that can be passed to Agent(tools=[...])
"""

import os
import httpx
from typing import List, Dict, Optional


# ─── Knowledge Service Tool ──────────────────────────────────────────────────


async def knowledge_search(
    query: str,
    top_k: int = 5
) -> List[Dict]:
    """
    Search the SpaceOS knowledge base for relevant context.

    Wraps the Knowledge Service API (discoverySearch MCP tool equivalent).

    Args:
        query: Search query (e.g., "RLS pattern", "React dark mode")
        top_k: Number of results to return (default: 5)

    Returns:
        List of knowledge chunks with metadata:
        [{
            "id": "doc-id",
            "text": "relevant content...",
            "metadata": {"source": "path/to/doc.md"},
            "distance": 0.123
        }]

    Example:
        results = await knowledge_search("JWT authentication patterns")
        for result in results:
            print(f"Found: {result['metadata']['source']}")
            print(f"Content: {result['text'][:200]}")
    """
    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(
                "http://localhost:3456/api/knowledge/search",
                json={"q": query, "topK": top_k},
                timeout=5.0
            )

            if response.status_code != 200:
                return [{
                    "id": "error",
                    "text": f"Knowledge Service error: {response.status_code}",
                    "metadata": {},
                    "distance": 1.0
                }]

            data = response.json()
            return data.get("results", [])

    except Exception as e:
        return [{
            "id": "error",
            "text": f"Knowledge Service connection failed: {str(e)}",
            "metadata": {},
            "distance": 1.0
        }]


def get_knowledge_context_sync(
    query: str,
    top_k: int = 3
) -> str:
    """
    Synchronous wrapper for knowledge_search().
    Returns concatenated text from top results.

    Args:
        query: Search query
        top_k: Number of results (default: 3)

    Returns:
        Concatenated text from knowledge base

    Example:
        context = get_knowledge_context_sync("RLS patterns")
        # Returns: "Row Level Security (RLS) implementation...\n\nPostgreSQL RLS..."
    """
    import asyncio

    try:
        results = asyncio.run(knowledge_search(query, top_k))

        if not results or results[0].get("id") == "error":
            return f"[Knowledge Service unavailable: {results[0].get('text', 'unknown error') if results else 'no results'}]"

        return "\n\n".join([r.get("text", "") for r in results if r.get("text")])

    except Exception as e:
        return f"[Knowledge Service error: {str(e)}]"


# ─── Discovery Search Context Builder ────────────────────────────────────────


async def build_discovery_context(
    queries: List[str],
    top_k_per_query: int = 3
) -> str:
    """
    Build rich context from multiple knowledge searches.

    Useful for Marvin Agents that need comprehensive context.

    Args:
        queries: List of search queries (e.g., ["RLS patterns", "JWT auth"])
        top_k_per_query: Results per query (default: 3)

    Returns:
        Formatted context string with sections per query

    Example:
        context = await build_discovery_context([
            "Row Level Security implementation",
            "PostgreSQL RLS patterns"
        ])

        agent = Agent(
            name="Database Expert",
            instructions=f"Context:\\n{context}\\n\\nTask: Design RLS policy"
        )
    """
    sections = []

    for query in queries:
        results = await knowledge_search(query, top_k_per_query)

        if not results or results[0].get("id") == "error":
            sections.append(f"**Query:** {query}\n[No results found]")
            continue

        texts = [r.get("text", "") for r in results if r.get("text")]
        combined = "\n\n".join(texts)

        sections.append(f"**Query:** {query}\n{combined}")

    return "\n\n---\n\n".join(sections)


# ─── Tool Registry ───────────────────────────────────────────────────────────


MARVIN_TOOLS = [
    knowledge_search,
    get_knowledge_context_sync,
]

"""
Marvin Tools available for Agent/Task integration.

Usage:
    from marvin import Agent
    from marvin_tools import knowledge_search, MARVIN_TOOLS

    agent = Agent(
        name="Research Agent",
        tools=[knowledge_search]
    )

    # Or use all tools:
    agent = Agent(
        name="Full Agent",
        tools=MARVIN_TOOLS
    )
"""


# ─── Testing ──────────────────────────────────────────────────────────────────


async def test_tools():
    """Test Marvin tools"""
    print("Testing Marvin Tools")
    print("=" * 50)

    # Test 1: Knowledge search
    print("\n1. Testing knowledge_search()...")
    results = await knowledge_search("RLS pattern", top_k=3)

    if results:
        print(f"   Found {len(results)} results")
        for i, result in enumerate(results[:2], 1):
            text = result.get("text", "")[:100]
            source = result.get("metadata", {}).get("source", "unknown")
            print(f"   {i}. {source}: {text}...")
    else:
        print("   No results")

    # Test 2: Sync wrapper
    print("\n2. Testing get_knowledge_context_sync()...")
    context = get_knowledge_context_sync("database patterns", top_k=2)
    print(f"   Context length: {len(context)} chars")
    print(f"   Preview: {context[:150]}...")

    # Test 3: Discovery context builder
    print("\n3. Testing build_discovery_context()...")
    context = await build_discovery_context([
        "security patterns",
        "deployment patterns"
    ], top_k_per_query=2)

    print(f"   Context sections: {context.count('---')}")
    print(f"   Total length: {len(context)} chars")

    print("\n✅ Marvin tools test complete")


if __name__ == "__main__":
    import asyncio
    asyncio.run(test_tools())
