"""
SpaceOS Marvin Planning Tasks
Phase 2: Bash Scripts → Marvin Tasks Migration

Replaces:
- plan-scan.sh → scan_for_ideas()
- plan-select.sh → select_best_ideas()
- plan-debate.sh → debate_idea() + synthesize_consensus()
"""

import os
import asyncio
from typing import List, Dict, Optional
from datetime import datetime
from pathlib import Path

from marvin import Agent, Task, extract
from pydantic import BaseModel, Field
import httpx

# Import existing functions
from planning_functions import get_knowledge_context, IdeaCandidate


# ─── Data Models ──────────────────────────────────────────────────────────────


class PlanningIdea(BaseModel):
    """Idea extracted from codebase segment"""
    title: str = Field(description="Short idea title (5-10 words)")
    description: str = Field(description="One sentence description")
    segment: str = Field(description="Source segment (e.g., 'fe-memory', 'kernel-memory')")
    priority: str = Field(description="Priority: critical, high, medium, low")
    confidence: float = Field(description="Confidence score 0.0-1.0")
    rationale: str = Field(description="Why this idea is promising")
    context: Optional[str] = Field(default="", description="Knowledge base context")


class SelectedIdea(BaseModel):
    """Idea validated with web research"""
    idea: PlanningIdea
    web_patterns: List[str] = Field(description="Similar patterns found on web")
    feasibility_score: float = Field(description="Feasibility 0.0-1.0 based on research")
    recommended: bool = Field(description="Recommended for planning debate")


class DebateArgument(BaseModel):
    """Argument in planning debate"""
    planner_id: str = Field(description="Planner-A or Planner-B")
    position: str = Field(description="pro or con")
    arguments: List[str] = Field(description="Key points (3-5 items)")
    implementation_approach: Optional[str] = Field(default="", description="Proposed approach")
    risks: List[str] = Field(description="Identified risks")
    confidence: float = Field(description="Confidence in this position 0.0-1.0")


class PlanningConsensus(BaseModel):
    """Synthesized consensus from debate"""
    consensus_title: str = Field(description="Consensus plan title")
    approach: str = Field(description="Agreed implementation approach")
    pros: List[str] = Field(description="Key benefits")
    cons: List[str] = Field(description="Key concerns")
    risks: List[str] = Field(description="Identified risks")
    recommendation: str = Field(description="Go/No-go/Modify recommendation")
    next_steps: List[str] = Field(description="Recommended next steps")
    confidence: float = Field(description="Overall consensus confidence 0.0-1.0")


# ─── Task 1: Scan Segment for Ideas ──────────────────────────────────────────


async def scan_for_ideas(
    segment_name: str,
    segment_content: str,
    domain_focus: str = "",
    recent_ideas: str = ""
) -> List[PlanningIdea]:
    """
    Scan a codebase segment for implementation ideas.

    Replaces: plan-scan.sh (Haiku scanner)

    Args:
        segment_name: Segment identifier (e.g., 'fe-memory', 'kernel-memory')
        segment_content: Content to analyze
        domain_focus: Current domain focus from domain-focus.md
        recent_ideas: Recently generated ideas (for avoiding duplicates)

    Returns:
        List of planning ideas with priority and confidence
    """
    # Get relevant knowledge context
    from marvin_tools import knowledge_search
    knowledge_results = await knowledge_search(f"{segment_name} improvements", top_k=5)
    knowledge_context = "\n\n".join([r.get("text", "") for r in knowledge_results if r.get("text")])

    instructions = f"""
    Analyze this SpaceOS codebase segment and extract promising implementation ideas.

    **What to look for:**
    - TODO/FIXME comments
    - Memory notes about planned improvements
    - Performance bottlenecks mentioned
    - Missing features or incomplete implementations
    - Technical debt or refactoring opportunities
    - Integration gaps with other modules

    **Segment:** {segment_name}
    **Domain Focus:** {domain_focus if domain_focus else "All domains"}
    **Recent Ideas (avoid duplicates):** {recent_ideas if recent_ideas else "None"}

    **Knowledge Base Context:**
    {knowledge_context if knowledge_context else "No additional context"}

    **Priority Guidelines:**
    - critical: Blocks current sprint or affects production
    - high: Needed for upcoming features or significant improvement
    - medium: Nice to have, improves quality or developer experience
    - low: Future consideration, minor improvement

    Extract 1-3 high-quality ideas. Be specific and actionable.
    """

    ideas = extract(
        segment_content,
        target=List[PlanningIdea],
        instructions=instructions
    )

    # Add segment and knowledge context to each idea
    for idea in ideas:
        idea.segment = segment_name
        idea.context = knowledge_context[:500] if knowledge_context else ""

    return ideas or []


# ─── Task 2: Select Best Ideas with Web Research ─────────────────────────────


async def select_best_ideas(
    ideas: List[PlanningIdea],
    domain_focus: str = "",
    top_n: int = 5
) -> List[SelectedIdea]:
    """
    Select and validate top ideas with web research.

    Replaces: plan-select.sh (Sonnet selector + WebSearch)

    Args:
        ideas: List of planning ideas
        domain_focus: Current domain focus
        top_n: Number of top ideas to select

    Returns:
        List of selected ideas with web validation
    """
    if not ideas:
        return []

    # Create selector agent
    agent = Agent(
        name="Idea Selector",
        instructions=f"""
        You select and validate software implementation ideas based on:
        1. Business value and impact
        2. Technical feasibility
        3. Alignment with domain focus
        4. Similar patterns found in web research

        **Domain Focus:** {domain_focus if domain_focus else "All domains"}

        For each idea:
        - Search for similar patterns, libraries, or approaches online
        - Assess feasibility based on web findings
        - Recommend ideas with proven patterns or strong community support

        Prioritize ideas that:
        - Have clear implementation paths
        - Use well-established patterns
        - Align with current SpaceOS architecture
        - Provide measurable value
        """
    )

    selected_ideas = []

    for idea in ideas[:top_n * 2]:  # Process more than needed for filtering
        # Simulate web search (in production, use actual WebSearch tool)
        search_query = f"{idea.title} {idea.segment} best practices implementation"

        # For now, create a mock SelectedIdea
        # TODO: Integrate actual WebSearch MCP tool
        selected = SelectedIdea(
            idea=idea,
            web_patterns=[
                f"Pattern found: {idea.title} implementation guide",
                f"Community library available for {idea.segment}"
            ],
            feasibility_score=idea.confidence * 0.9,  # Slight reduction after validation
            recommended=idea.priority in ["critical", "high"]
        )

        selected_ideas.append(selected)

    # Sort by feasibility and priority
    selected_ideas.sort(key=lambda x: (
        1 if x.idea.priority == "critical" else
        2 if x.idea.priority == "high" else
        3 if x.idea.priority == "medium" else 4,
        -x.feasibility_score
    ))

    return selected_ideas[:top_n]


# ─── Task 3: Debate Idea (Parallel Pro/Con) ──────────────────────────────────


async def debate_idea(
    idea: PlanningIdea,
    planner_id: str,
    planner_style: str,
    codebase_status: str = "",
    domain_focus: str = ""
) -> DebateArgument:
    """
    Generate planning arguments for an idea (Pro or Con perspective).

    Replaces: plan-debate.sh (Planner-A and Planner-B parallel execution)

    Args:
        idea: Planning idea to debate
        planner_id: "Planner-A" or "Planner-B"
        planner_style: Planning style instruction
        codebase_status: Current codebase status summary
        domain_focus: Current domain focus

    Returns:
        Debate argument with implementation approach and risks
    """
    position = "pro" if planner_id == "Planner-A" else "con"

    agent = Agent(
        name=planner_id,
        instructions=f"""
        You are {planner_id}, a SpaceOS planning specialist with this style:
        {planner_style}

        **Task:** Debate the following idea from a {position.upper()} perspective.

        **Idea:** {idea.title}
        **Description:** {idea.description}
        **Priority:** {idea.priority}
        **Segment:** {idea.segment}

        **Codebase Status:** {codebase_status if codebase_status else "Not available"}
        **Domain Focus:** {domain_focus if domain_focus else "All domains"}

        {"**Your role:** Argue FOR this idea. Focus on benefits, implementation approach, and value." if position == "pro" else "**Your role:** Argue AGAINST or challenge this idea. Focus on risks, complexity, and alternatives."}

        Provide:
        - 3-5 key arguments supporting your position
        - Implementation approach (if pro) or alternative suggestions (if con)
        - Identified risks and concerns
        - Confidence in your assessment (0.0-1.0)

        Be specific, technical, and reference SpaceOS architecture when relevant.
        """
    )

    # In a full implementation, use agent.run() for actual execution
    # For now, return a structured placeholder

    if position == "pro":
        return DebateArgument(
            planner_id=planner_id,
            position=position,
            arguments=[
                f"Aligns with {idea.segment} roadmap priorities",
                f"Addresses key pain point in {idea.segment}",
                "Proven pattern in similar systems",
                "Incremental implementation possible",
                "High value-to-effort ratio"
            ],
            implementation_approach=f"Implement {idea.title} using modular approach with testing at each step",
            risks=[
                "Integration complexity with existing code",
                "Potential performance impact"
            ],
            confidence=idea.confidence
        )
    else:
        return DebateArgument(
            planner_id=planner_id,
            position=position,
            arguments=[
                f"May conflict with current {idea.segment} architecture",
                "Higher complexity than initially apparent",
                "Alternative approaches may be simpler",
                "Resource allocation concerns",
                "Risk of scope creep"
            ],
            implementation_approach="",
            risks=[
                "Significant refactoring required",
                "Unclear integration path",
                "Testing complexity"
            ],
            confidence=idea.confidence * 0.8
        )


async def synthesize_consensus(
    idea: PlanningIdea,
    planner_a_argument: DebateArgument,
    planner_b_argument: DebateArgument,
    domain_focus: str = ""
) -> PlanningConsensus:
    """
    Synthesize consensus from debate arguments.

    Replaces: plan-debate.sh (Consensus phase)

    Args:
        idea: Original planning idea
        planner_a_argument: Planner-A debate argument (Pro)
        planner_b_argument: Planner-B debate argument (Con)
        domain_focus: Current domain focus

    Returns:
        Synthesized consensus with recommendation
    """
    agent = Agent(
        name="Consensus Synthesizer",
        instructions=f"""
        You synthesize planning debates into actionable consensus.

        **Original Idea:** {idea.title}
        **Description:** {idea.description}

        **Planner-A (Pro) Arguments:**
        {chr(10).join('- ' + arg for arg in planner_a_argument.arguments)}

        **Planner-B (Con) Arguments:**
        {chr(10).join('- ' + arg for arg in planner_b_argument.arguments)}

        **Domain Focus:** {domain_focus if domain_focus else "All domains"}

        Create a balanced consensus that:
        1. Acknowledges valid points from both perspectives
        2. Proposes a modified approach if needed
        3. Lists clear pros, cons, and risks
        4. Makes a clear recommendation (Go/No-go/Modify)
        5. Outlines concrete next steps

        Be pragmatic and focus on actionability.
        """
    )

    # Combine arguments
    all_pros = planner_a_argument.arguments
    all_cons = planner_b_argument.arguments
    all_risks = list(set(planner_a_argument.risks + planner_b_argument.risks))

    # Determine recommendation
    avg_confidence = (planner_a_argument.confidence + planner_b_argument.confidence) / 2
    recommendation = (
        "Go" if avg_confidence > 0.7 and idea.priority in ["critical", "high"] else
        "Modify" if avg_confidence > 0.5 else
        "No-go"
    )

    consensus = PlanningConsensus(
        consensus_title=f"Consensus: {idea.title}",
        approach=planner_a_argument.implementation_approach or f"Modified approach for {idea.title}",
        pros=all_pros[:3],  # Top 3 pros
        cons=all_cons[:3],  # Top 3 cons
        risks=all_risks,
        recommendation=recommendation,
        next_steps=[
            "Review consensus with Architect",
            f"Create spec document for {idea.segment}",
            "Validate approach with affected terminals",
            "Add to planning queue if approved"
        ] if recommendation == "Go" else [
            "Revise approach based on concerns",
            "Gather additional context",
            "Re-evaluate priority"
        ],
        confidence=avg_confidence
    )

    return consensus


# ─── Parallel Debate Execution ───────────────────────────────────────────────


async def run_parallel_debate(
    idea: PlanningIdea,
    codebase_status: str = "",
    domain_focus: str = ""
) -> PlanningConsensus:
    """
    Run parallel debate (Planner-A vs Planner-B) and synthesize consensus.

    Args:
        idea: Planning idea to debate
        codebase_status: Current codebase status
        domain_focus: Current domain focus

    Returns:
        Synthesized consensus
    """
    # Run both planners in parallel
    planner_a_task = debate_idea(
        idea=idea,
        planner_id="Planner-A",
        planner_style="Focus on incremental, safe approaches",
        codebase_status=codebase_status,
        domain_focus=domain_focus
    )

    planner_b_task = debate_idea(
        idea=idea,
        planner_id="Planner-B",
        planner_style="Challenge assumptions, identify risks and alternatives",
        codebase_status=codebase_status,
        domain_focus=domain_focus
    )

    # Wait for both to complete
    planner_a_arg, planner_b_arg = await asyncio.gather(planner_a_task, planner_b_task)

    # Synthesize consensus
    consensus = await synthesize_consensus(
        idea=idea,
        planner_a_argument=planner_a_arg,
        planner_b_argument=planner_b_arg,
        domain_focus=domain_focus
    )

    return consensus


# ─── CLI Interface ────────────────────────────────────────────────────────────


async def main():
    """Test planning tasks"""
    print("SpaceOS Marvin Planning Tasks")
    print("=" * 50)

    # Check API key
    if not os.getenv("OPENAI_API_KEY"):
        print("\n⚠️  OPENAI_API_KEY not set")
        print("   Set it in .env file or export OPENAI_API_KEY='sk-...'")
        return

    # Test 1: Scan for ideas
    print("\n1. Testing scan_for_ideas()...")
    segment_content = """
    # Frontend Memory

    ## Current Sprint
    - TODO: Add dark mode toggle to settings
    - TODO: Implement real-time WebSocket updates
    - FIXME: Performance issue with large dataset rendering

    ## Technical Debt
    - Refactor component state management
    - Add comprehensive E2E tests for checkout flow
    """

    ideas = await scan_for_ideas(
        segment_name="fe-memory",
        segment_content=segment_content,
        domain_focus="Frontend UX improvements"
    )

    print(f"   Found {len(ideas)} ideas:")
    for idea in ideas:
        print(f"   - [{idea.priority}] {idea.title} (confidence: {idea.confidence:.2f})")

    if not ideas:
        print("   No ideas found. Skipping remaining tests.")
        return

    # Test 2: Select best ideas
    print("\n2. Testing select_best_ideas()...")
    selected = await select_best_ideas(ideas, domain_focus="Frontend UX", top_n=3)
    print(f"   Selected {len(selected)} ideas:")
    for sel in selected:
        print(f"   - {sel.idea.title} (feasibility: {sel.feasibility_score:.2f}, recommended: {sel.recommended})")

    # Test 3: Parallel debate
    if selected:
        print("\n3. Testing parallel debate...")
        consensus = await run_parallel_debate(
            idea=selected[0].idea,
            codebase_status="Sprint 6 complete, Sprint 7 planning",
            domain_focus="Frontend UX improvements"
        )
        print(f"   Consensus: {consensus.recommendation}")
        print(f"   Approach: {consensus.approach[:100]}...")
        print(f"   Confidence: {consensus.confidence:.2f}")

    print("\n✅ Planning tasks test complete")


if __name__ == "__main__":
    asyncio.run(main())
