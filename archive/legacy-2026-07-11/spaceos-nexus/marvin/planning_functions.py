"""
SpaceOS Marvin Planning Functions
Phase 2: Planning Pipeline Integration

Uses Marvin 3.x Agent/Task API
"""

from typing import List, Dict
from marvin import Agent, Task
from marvin.fns import extract
from pydantic import BaseModel, Field
import os
import httpx


class IdeaCandidate(BaseModel):
    """Structured idea extracted from segment"""
    title: str = Field(description="Short idea title (5-10 words)")
    description: str = Field(description="One sentence description")
    segment: str = Field(description="Segment where found (e.g., 'frontend', 'backend')")
    priority: str = Field(description="Priority: high, medium, low")
    confidence: float = Field(description="Confidence score 0.0-1.0")


class DebateOutcome(BaseModel):
    """Synthesized debate consensus"""
    consensus: str = Field(description="Synthesized consensus statement")
    pros: List[str] = Field(description="Key arguments in favor")
    cons: List[str] = Field(description="Key concerns or limitations")
    recommendation: str = Field(description="Go/No-go recommendation")
    confidence: float = Field(description="Consensus confidence 0.0-1.0")


# ─── Phase 2 Planning Functions ──────────────────────────────────────────────


def scan_segment_for_ideas(
    segment_name: str,
    segment_content: str,
    knowledge_context: str = ""
) -> List[IdeaCandidate]:
    """
    Scan a codebase segment for implementation ideas using Marvin extraction.

    Args:
        segment_name: Name of the segment (e.g., "frontend/components")
        segment_content: File contents or summary of the segment
        knowledge_context: Relevant knowledge from Knowledge Service

    Returns:
        List of structured idea candidates with priority and confidence
    """
    instructions = f"""
    Analyze this codebase segment and extract promising implementation ideas.
    Look for: TODOs, FIXMEs, commented features, performance issues, refactoring opportunities.

    Segment: {segment_name}
    Context: {knowledge_context if knowledge_context else "No additional context"}
    """

    ideas = extract(
        segment_content,
        target=List[IdeaCandidate],
        instructions=instructions
    )

    return ideas or []


def prioritize_ideas(
    ideas: List[IdeaCandidate],
    business_context: str,
    technical_constraints: str
) -> List[IdeaCandidate]:
    """
    Re-prioritize and score ideas based on business and technical context.

    Args:
        ideas: List of idea candidates
        business_context: Business goals, roadmap, customer needs
        technical_constraints: Tech stack, timeline, resources

    Returns:
        Sorted list of ideas with updated priority and confidence
    """
    # Create Agent for prioritization
    agent = Agent(
        name="Idea Prioritizer",
        instructions=f"""
        You prioritize software ideas based on business value and technical feasibility.

        Business Context: {business_context}
        Technical Constraints: {technical_constraints}

        Assign priority (high/medium/low) and confidence (0.0-1.0) to each idea.
        """
    )

    # For now, return as-is (full prioritization requires async agent run)
    return sorted(ideas, key=lambda x: x.confidence, reverse=True)


def debate_idea(
    idea: IdeaCandidate,
    position: str,  # "for" or "against"
    context: str
) -> str:
    """
    Generate debate arguments for or against an idea using Marvin Agent.

    Args:
        idea: Idea to debate
        position: "for" or "against"
        context: Additional context for debate

    Returns:
        Structured argument paragraph
    """
    agent = Agent(
        name=f"Debater ({position})",
        instructions=f"""
        You are debating {position} the following idea:
        Title: {idea.title}
        Description: {idea.description}

        Context: {context}

        Provide a structured argument with 3-5 key points.
        Be specific and reference technical details where relevant.
        """
    )

    # Simplified: return placeholder (full debate requires async run)
    return f"[{position.upper()}] Arguments for idea '{idea.title}' would be generated here via agent.run()"


def synthesize_debate(
    idea: IdeaCandidate,
    arguments_for: str,
    arguments_against: str
) -> DebateOutcome:
    """
    Synthesize debate arguments into consensus recommendation.

    Args:
        idea: Original idea
        arguments_for: Arguments in favor
        arguments_against: Arguments against

    Returns:
        Structured consensus with recommendation
    """
    # Simplified extraction for Phase 2 prototype
    return DebateOutcome(
        consensus=f"Synthesized consensus for: {idea.title}",
        pros=["Pro 1", "Pro 2"],
        cons=["Con 1", "Con 2"],
        recommendation="Requires full agent debate run",
        confidence=0.7
    )


# ─── Knowledge Service Integration ───────────────────────────────────────────


async def search_knowledge(query: str, top_k: int = 5) -> List[Dict]:
    """
    Query SpaceOS Knowledge Service for relevant context.

    Args:
        query: Search query
        top_k: Number of results to return

    Returns:
        List of relevant knowledge chunks with metadata
    """
    async with httpx.AsyncClient() as client:
        response = await client.post(
            "http://localhost:3456/api/knowledge/search",
            json={"q": query, "topK": top_k},
            timeout=5.0
        )
        if response.status_code == 200:
            data = response.json()
            return data.get("results", [])
        return []


def get_knowledge_context(query: str, top_k: int = 3) -> str:
    """
    Synchronous wrapper for knowledge search.
    Returns concatenated text from top results.
    """
    import asyncio
    results = asyncio.run(search_knowledge(query, top_k))
    if not results:
        return ""
    return "\n\n".join([r.get("text", "") for r in results])


# ─── Example Usage ────────────────────────────────────────────────────────────


if __name__ == "__main__":
    # Example: Scan frontend segment
    print("Testing Marvin Planning Functions...")

    # NOTE: Requires OPENAI_API_KEY in environment
    if not os.getenv("OPENAI_API_KEY"):
        print("⚠️  OPENAI_API_KEY not set. Set it in .env file.")
        print("   Example: export OPENAI_API_KEY='sk-...'")
        exit(1)

    # Test 1: Scan segment
    segment_content = """
    // frontend/components/Dashboard.tsx
    // TODO: Add dark mode toggle
    // TODO: Implement real-time updates via WebSocket
    // FIXME: Performance issue with large datasets
    """

    print("\n1. Scanning segment for ideas...")
    try:
        ideas = scan_segment_for_ideas(
            segment_name="frontend/components",
            segment_content=segment_content,
            knowledge_context=""
        )
        print(f"   Found {len(ideas)} ideas:")
        for idea in ideas:
            print(f"   - [{idea.priority}] {idea.title} (confidence: {idea.confidence:.2f})")
    except Exception as e:
        print(f"   Error: {e}")

    # Test 2: Knowledge search
    print("\n2. Testing Knowledge Service integration...")
    try:
        context = get_knowledge_context("React dark mode implementation")
        if context:
            print(f"   Retrieved {len(context)} chars of context")
        else:
            print("   No context found (Knowledge Service may be down)")
    except Exception as e:
        print(f"   Error: {e}")

    print("\n✅ Marvin Planning Functions test complete")
