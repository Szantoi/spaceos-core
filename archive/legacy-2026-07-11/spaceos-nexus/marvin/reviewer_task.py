"""
SpaceOS Marvin Reviewer Task
Phase 3: reviewer.sh → Marvin Task (Proof-of-Concept)

Replaces dual Haiku reviewer with Marvin async parallel execution.

NOTE: This is a skeleton implementation. Full implementation requires:
- OPENAI_API_KEY configuration
- reviewer-config.yaml integration
- Verdict keyword parsing
- Consensus logic
"""

import asyncio
from typing import List, Dict, Tuple
from pathlib import Path
from dataclasses import dataclass
from enum import Enum

from marvin import Agent, extract


# ─── Data Models ──────────────────────────────────────────────────────────────


class Verdict(str, Enum):
    APPROVE = "APPROVE"
    REJECT = "REJECT"
    UNCLEAR = "UNCLEAR"


@dataclass
class ReviewResult:
    """Single reviewer result"""
    reviewer_id: str  # "Reviewer-A" or "Reviewer-B"
    verdict: Verdict
    reasons: List[str]
    confidence: float  # 0.0-1.0
    raw_output: str


@dataclass
class ConsensusResult:
    """Dual reviewer consensus"""
    final_verdict: Verdict
    reviewer_a: ReviewResult
    reviewer_b: ReviewResult
    both_approve: bool
    reasons: List[str]


# ─── Review Task ──────────────────────────────────────────────────────────────


async def review_done_message(
    done_file_path: str,
    reviewer_id: str,
    reviewer_style: str = "strict"
) -> ReviewResult:
    """
    Review a DONE outbox message with Marvin Agent.

    Replaces: reviewer.sh (single reviewer execution)

    Args:
        done_file_path: Path to DONE message file
        reviewer_id: "Reviewer-A" or "Reviewer-B"
        reviewer_style: Review style (strict, balanced, lenient)

    Returns:
        ReviewResult with verdict and reasons

    Example:
        result = await review_done_message(
            "docs/mailbox/fe/outbox/2026-06-17_001_task-done.md",
            "Reviewer-A",
            "strict"
        )
        if result.verdict == Verdict.APPROVE:
            print(f"APPROVED: {result.reasons}")
    """
    # Read DONE message
    done_path = Path(done_file_path)
    if not done_path.exists():
        return ReviewResult(
            reviewer_id=reviewer_id,
            verdict=Verdict.UNCLEAR,
            reasons=[f"File not found: {done_file_path}"],
            confidence=0.0,
            raw_output=""
        )

    done_content = done_path.read_text()

    # Create reviewer agent
    agent = Agent(
        name=reviewer_id,
        instructions=f"""
        You are {reviewer_id}, a SpaceOS DONE message reviewer with {reviewer_style} standards.

        **Task:** Review this DONE message for completion quality.

        **Criteria:**
        1. All Definition of Done items completed?
        2. Code quality meets standards (if applicable)?
        3. Tests pass (if applicable)?
        4. Documentation updated?
        5. No TODO/FIXME left behind?
        6. Inbox task requirements fully addressed?

        **Style:** {reviewer_style}
        - strict: Reject if any criteria not met
        - balanced: Allow minor issues if core complete
        - lenient: Approve if main objectives met

        **Output Format:**
        - Start with APPROVE or REJECT
        - List 3-5 specific reasons
        - Be concise and actionable

        **DONE Message:**
        {done_content}

        Your verdict:
        """
    )

    # For proof-of-concept, return structured placeholder
    # In production, use: response = await agent.run()
    # Then parse verdict from response

    # Mock verdict extraction (requires OPENAI_API_KEY for actual execution)
    raw_output = f"[{reviewer_id} would analyze and return verdict here]"

    # Parse verdict (simplified)
    if "TODO" in done_content or "FIXME" in done_content:
        verdict = Verdict.REJECT
        reasons = ["Found unresolved TODO/FIXME items"]
    elif "Definition of Done" not in done_content:
        verdict = Verdict.REJECT
        reasons = ["Missing Definition of Done section"]
    else:
        verdict = Verdict.APPROVE
        reasons = ["All Definition of Done items completed", "Documentation updated"]

    return ReviewResult(
        reviewer_id=reviewer_id,
        verdict=verdict,
        reasons=reasons,
        confidence=0.85,
        raw_output=raw_output
    )


async def dual_review(
    done_file_path: str,
    require_both: bool = True
) -> ConsensusResult:
    """
    Run dual parallel review (Reviewer-A + Reviewer-B).

    Replaces: reviewer.sh (full dual review logic)

    Args:
        done_file_path: Path to DONE message file
        require_both: Require both APPROVE for final APPROVE (default: True)

    Returns:
        ConsensusResult with final verdict and both reviewer results

    Example:
        consensus = await dual_review("docs/mailbox/fe/outbox/DONE.md")
        if consensus.final_verdict == Verdict.APPROVE:
            print("APPROVED by consensus")
        else:
            print(f"REJECTED: {consensus.reasons}")
    """
    # Run both reviewers in parallel
    reviewer_a_task = review_done_message(
        done_file_path,
        reviewer_id="Reviewer-A",
        reviewer_style="strict"
    )

    reviewer_b_task = review_done_message(
        done_file_path,
        reviewer_id="Reviewer-B",
        reviewer_style="balanced"
    )

    reviewer_a, reviewer_b = await asyncio.gather(reviewer_a_task, reviewer_b_task)

    # Consensus logic
    both_approve = (
        reviewer_a.verdict == Verdict.APPROVE and
        reviewer_b.verdict == Verdict.APPROVE
    )

    if require_both:
        final_verdict = Verdict.APPROVE if both_approve else Verdict.REJECT
    else:
        # At least one approve
        final_verdict = (
            Verdict.APPROVE
            if reviewer_a.verdict == Verdict.APPROVE or reviewer_b.verdict == Verdict.APPROVE
            else Verdict.REJECT
        )

    # Combine reasons
    reasons = []
    if final_verdict == Verdict.APPROVE:
        reasons.append("Both reviewers approved (dual consensus)")
        reasons.extend(reviewer_a.reasons[:2])
    else:
        if reviewer_a.verdict == Verdict.REJECT:
            reasons.append(f"Reviewer-A rejected: {reviewer_a.reasons[0]}")
        if reviewer_b.verdict == Verdict.REJECT:
            reasons.append(f"Reviewer-B rejected: {reviewer_b.reasons[0]}")

    return ConsensusResult(
        final_verdict=final_verdict,
        reviewer_a=reviewer_a,
        reviewer_b=reviewer_b,
        both_approve=both_approve,
        reasons=reasons
    )


# ─── CLI Interface ────────────────────────────────────────────────────────────


async def main():
    """Test reviewer task"""
    import sys

    if len(sys.argv) < 2:
        print("Usage: python reviewer_task.py <done_file_path>")
        sys.exit(1)

    done_file = sys.argv[1]

    print(f"Reviewing: {done_file}")
    print("=" * 60)

    consensus = await dual_review(done_file, require_both=True)

    print(f"\nFinal Verdict: {consensus.final_verdict.value}")
    print(f"Both Approve: {consensus.both_approve}")
    print(f"\nReviewer-A: {consensus.reviewer_a.verdict.value} (confidence: {consensus.reviewer_a.confidence:.2f})")
    for reason in consensus.reviewer_a.reasons:
        print(f"  - {reason}")

    print(f"\nReviewer-B: {consensus.reviewer_b.verdict.value} (confidence: {consensus.reviewer_b.confidence:.2f})")
    for reason in consensus.reviewer_b.reasons:
        print(f"  - {reason}")

    print(f"\nConsensus Reasons:")
    for reason in consensus.reasons:
        print(f"  - {reason}")


if __name__ == "__main__":
    asyncio.run(main())
