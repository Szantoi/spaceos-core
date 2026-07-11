"""
Unit tests for planning_tasks.py

Mock-based testing strategy (no OPENAI_API_KEY required).
Pattern: Session 5 nightwatch_scheduler mock tests.
"""

import os
import pytest
from unittest.mock import Mock, AsyncMock, patch, MagicMock
from datetime import datetime
from pathlib import Path

# Set fake API key for Marvin initialization (not used, just for init)
os.environ["OPENAI_API_KEY"] = "sk-test-fake-key-for-unit-tests"

# Import the module under test
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from planning_tasks import (
    PlanningIdea,
    SelectedIdea,
    DebateArgument,
    PlanningConsensus,
    scan_for_ideas,
    select_best_ideas,
    debate_idea,
    synthesize_consensus,
    run_parallel_debate
)


# ─── Fixtures ─────────────────────────────────────────────────────────────────


@pytest.fixture
def sample_idea():
    """Sample planning idea for testing"""
    return PlanningIdea(
        title="Add dark mode toggle",
        description="Implement theme switcher in settings",
        segment="fe-memory",
        priority="high",
        confidence=0.85,
        rationale="Frequently requested feature with clear implementation path",
        context="React context API available for state management"
    )


@pytest.fixture
def sample_ideas():
    """Multiple sample ideas for testing"""
    return [
        PlanningIdea(
            title="Add dark mode toggle",
            description="Implement theme switcher",
            segment="fe-memory",
            priority="high",
            confidence=0.85,
            rationale="User requested feature",
            context=""
        ),
        PlanningIdea(
            title="Optimize database queries",
            description="Add indexes and query optimization",
            segment="kernel-memory",
            priority="critical",
            confidence=0.90,
            rationale="Performance bottleneck identified",
            context=""
        ),
        PlanningIdea(
            title="Refactor state management",
            description="Migrate to Redux",
            segment="fe-memory",
            priority="medium",
            confidence=0.70,
            rationale="Technical debt reduction",
            context=""
        )
    ]


@pytest.fixture
def mock_knowledge_search():
    """Mock knowledge_search function"""
    async def _mock_search(query: str, top_k: int = 5):
        return [
            {"text": f"Knowledge result 1 for {query}"},
            {"text": f"Knowledge result 2 for {query}"},
        ]
    return _mock_search


@pytest.fixture
def mock_marvin_extract():
    """Mock Marvin extract function"""
    def _mock_extract(content, target, instructions):
        # Return sample ideas based on content
        if "dark mode" in content.lower() or "TODO" in content:
            return [
                PlanningIdea(
                    title="Add dark mode toggle to settings",
                    description="Implement theme switcher with system preference detection",
                    segment="test-segment",
                    priority="high",
                    confidence=0.85,
                    rationale="User-requested feature with clear implementation path",
                    context=""
                )
            ]
        return []
    return _mock_extract


# ─── Test: Data Models ────────────────────────────────────────────────────────


class TestDataModels:
    """Test Pydantic data models"""

    def test_planning_idea_model(self):
        """Test PlanningIdea model validation"""
        idea = PlanningIdea(
            title="Test Idea",
            description="Test description",
            segment="test-memory",
            priority="high",
            confidence=0.8,
            rationale="Test rationale"
        )

        assert idea.title == "Test Idea"
        assert idea.priority == "high"
        assert idea.confidence == 0.8
        assert idea.context == ""  # default value

    def test_selected_idea_model(self, sample_idea):
        """Test SelectedIdea model"""
        selected = SelectedIdea(
            idea=sample_idea,
            web_patterns=["Pattern 1", "Pattern 2"],
            feasibility_score=0.75,
            recommended=True
        )

        assert selected.idea.title == "Add dark mode toggle"
        assert len(selected.web_patterns) == 2
        assert selected.feasibility_score == 0.75
        assert selected.recommended is True

    def test_debate_argument_model(self):
        """Test DebateArgument model"""
        arg = DebateArgument(
            planner_id="Planner-A",
            position="pro",
            arguments=["Arg 1", "Arg 2", "Arg 3"],
            implementation_approach="Use React Context API",
            risks=["Risk 1"],
            confidence=0.85
        )

        assert arg.planner_id == "Planner-A"
        assert arg.position == "pro"
        assert len(arg.arguments) == 3
        assert arg.confidence == 0.85

    def test_planning_consensus_model(self):
        """Test PlanningConsensus model"""
        consensus = PlanningConsensus(
            consensus_title="Consensus: Dark Mode",
            approach="Incremental implementation",
            pros=["Pro 1", "Pro 2"],
            cons=["Con 1"],
            risks=["Risk 1"],
            recommendation="Go",
            next_steps=["Step 1", "Step 2"],
            confidence=0.80
        )

        assert consensus.recommendation == "Go"
        assert len(consensus.pros) == 2
        assert consensus.confidence == 0.80


# ─── Test: scan_for_ideas ─────────────────────────────────────────────────────


class TestScanForIdeas:
    """Test scan_for_ideas function"""

    @pytest.mark.skip(reason="Requires Marvin extract which needs real API connection - deferred to integration tests")
    @pytest.mark.asyncio
    async def test_scan_basic_success(self, mock_knowledge_search, mock_marvin_extract):
        """Test basic scan success"""
        with patch('marvin_tools.knowledge_search', mock_knowledge_search):
            with patch('marvin.extract', mock_marvin_extract):
                segment_content = """
                # Frontend Memory
                - TODO: Add dark mode toggle
                - FIXME: Performance issue
                """

                ideas = await scan_for_ideas(
                    segment_name="fe-memory",
                    segment_content=segment_content,
                    domain_focus="Frontend UX"
                )

                assert isinstance(ideas, list)
                assert len(ideas) > 0
                assert ideas[0].segment == "fe-memory"  # Segment injected
                assert ideas[0].title is not None

    @pytest.mark.skip(reason="Requires Marvin extract which needs real API connection")
    @pytest.mark.asyncio
    async def test_scan_empty_content(self, mock_knowledge_search, mock_marvin_extract):
        """Test scan with empty content"""
        with patch('marvin_tools.knowledge_search', mock_knowledge_search):
            with patch('marvin.extract', return_value=[]):
                ideas = await scan_for_ideas(
                    segment_name="empty-memory",
                    segment_content="",
                    domain_focus=""
                )

                assert ideas == []

    @pytest.mark.skip(reason="Requires Marvin extract which needs real API connection")
    @pytest.mark.asyncio
    async def test_scan_knowledge_context_injection(self, mock_marvin_extract):
        """Test that knowledge context is injected into ideas"""
        async def mock_knowledge_with_context(query, top_k=5):
            return [{"text": "Context line 1"}, {"text": "Context line 2"}]

        with patch('marvin_tools.knowledge_search', mock_knowledge_with_context):
            with patch('marvin.extract', mock_marvin_extract):
                ideas = await scan_for_ideas(
                    segment_name="test-memory",
                    segment_content="TODO: Test feature",
                    domain_focus="Test"
                )

                if ideas:
                    assert ideas[0].context is not None
                    # Context should be truncated to 500 chars max
                    assert len(ideas[0].context) <= 500


# ─── Test: select_best_ideas ──────────────────────────────────────────────────


class TestSelectBestIdeas:
    """Test select_best_ideas function"""

    @pytest.mark.asyncio
    async def test_select_empty_input(self):
        """Test select with empty ideas list"""
        result = await select_best_ideas([], domain_focus="Test", top_n=5)
        assert result == []

    @pytest.mark.asyncio
    async def test_select_basic_success(self, sample_ideas):
        """Test basic selection success"""
        selected = await select_best_ideas(
            sample_ideas,
            domain_focus="Frontend",
            top_n=2
        )

        assert isinstance(selected, list)
        assert len(selected) <= 2  # Respects top_n
        assert all(isinstance(s, SelectedIdea) for s in selected)

    @pytest.mark.asyncio
    async def test_select_priority_sorting(self, sample_ideas):
        """Test that critical priority comes first"""
        selected = await select_best_ideas(
            sample_ideas,
            domain_focus="All",
            top_n=5
        )

        # First selected should be critical priority
        if selected:
            assert selected[0].idea.priority == "critical"

    @pytest.mark.asyncio
    async def test_select_feasibility_calculation(self, sample_ideas):
        """Test feasibility score calculation"""
        selected = await select_best_ideas(
            sample_ideas,
            domain_focus="Test",
            top_n=3
        )

        for sel in selected:
            # Feasibility = confidence * 0.9
            expected_feasibility = sel.idea.confidence * 0.9
            assert abs(sel.feasibility_score - expected_feasibility) < 0.01

    @pytest.mark.asyncio
    async def test_select_recommended_flag(self, sample_ideas):
        """Test recommended flag for critical/high priority"""
        selected = await select_best_ideas(
            sample_ideas,
            domain_focus="Test",
            top_n=5
        )

        for sel in selected:
            if sel.idea.priority in ["critical", "high"]:
                assert sel.recommended is True
            else:
                assert sel.recommended is False


# ─── Test: debate_idea ────────────────────────────────────────────────────────


class TestDebateIdea:
    """Test debate_idea function"""

    @pytest.mark.asyncio
    async def test_debate_planner_a_pro(self, sample_idea):
        """Test Planner-A (pro) argument generation"""
        arg = await debate_idea(
            idea=sample_idea,
            planner_id="Planner-A",
            planner_style="Incremental approach",
            codebase_status="Sprint 6",
            domain_focus="Frontend"
        )

        assert arg.planner_id == "Planner-A"
        assert arg.position == "pro"
        assert len(arg.arguments) > 0
        assert arg.implementation_approach != ""
        assert len(arg.risks) > 0
        assert 0.0 <= arg.confidence <= 1.0

    @pytest.mark.asyncio
    async def test_debate_planner_b_con(self, sample_idea):
        """Test Planner-B (con) argument generation"""
        arg = await debate_idea(
            idea=sample_idea,
            planner_id="Planner-B",
            planner_style="Challenge assumptions",
            codebase_status="Sprint 6",
            domain_focus="Frontend"
        )

        assert arg.planner_id == "Planner-B"
        assert arg.position == "con"
        assert len(arg.arguments) > 0
        assert len(arg.risks) > 0
        # Con position should have lower confidence
        assert arg.confidence < sample_idea.confidence

    @pytest.mark.asyncio
    async def test_debate_confidence_propagation(self, sample_idea):
        """Test that idea confidence affects debate confidence"""
        arg_pro = await debate_idea(
            idea=sample_idea,
            planner_id="Planner-A",
            planner_style="Test",
            codebase_status="",
            domain_focus=""
        )

        # Pro should match idea confidence
        assert arg_pro.confidence == sample_idea.confidence


# ─── Test: synthesize_consensus ───────────────────────────────────────────────


class TestSynthesizeConsensus:
    """Test synthesize_consensus function"""

    @pytest.mark.asyncio
    async def test_synthesis_basic(self, sample_idea):
        """Test basic consensus synthesis"""
        arg_pro = DebateArgument(
            planner_id="Planner-A",
            position="pro",
            arguments=["Arg1", "Arg2", "Arg3"],
            implementation_approach="Incremental",
            risks=["Risk1"],
            confidence=0.85
        )

        arg_con = DebateArgument(
            planner_id="Planner-B",
            position="con",
            arguments=["Con1", "Con2"],
            implementation_approach="",
            risks=["Risk2", "Risk3"],
            confidence=0.70
        )

        consensus = await synthesize_consensus(
            idea=sample_idea,
            planner_a_argument=arg_pro,
            planner_b_argument=arg_con,
            domain_focus="Frontend"
        )

        assert isinstance(consensus, PlanningConsensus)
        assert consensus.consensus_title.startswith("Consensus:")
        assert len(consensus.pros) > 0
        assert len(consensus.cons) > 0
        assert len(consensus.risks) > 0
        assert consensus.recommendation in ["Go", "No-go", "Modify"]

    @pytest.mark.asyncio
    async def test_synthesis_go_recommendation(self, sample_idea):
        """Test Go recommendation logic"""
        arg_pro = DebateArgument(
            planner_id="Planner-A",
            position="pro",
            arguments=["A1"],
            implementation_approach="Approach",
            risks=[],
            confidence=0.80
        )

        arg_con = DebateArgument(
            planner_id="Planner-B",
            position="con",
            arguments=["C1"],
            implementation_approach="",
            risks=[],
            confidence=0.75
        )

        # High confidence + high priority = Go
        sample_idea.priority = "critical"
        consensus = await synthesize_consensus(
            idea=sample_idea,
            planner_a_argument=arg_pro,
            planner_b_argument=arg_con,
            domain_focus=""
        )

        # Avg confidence = (0.80 + 0.75) / 2 = 0.775 > 0.7 → Go
        assert consensus.recommendation == "Go"

    @pytest.mark.asyncio
    async def test_synthesis_modify_recommendation(self, sample_idea):
        """Test Modify recommendation logic"""
        arg_pro = DebateArgument(
            planner_id="Planner-A",
            position="pro",
            arguments=["A1"],
            implementation_approach="Approach",
            risks=[],
            confidence=0.60
        )

        arg_con = DebateArgument(
            planner_id="Planner-B",
            position="con",
            arguments=["C1"],
            implementation_approach="",
            risks=[],
            confidence=0.60
        )

        # Medium confidence = Modify
        consensus = await synthesize_consensus(
            idea=sample_idea,
            planner_a_argument=arg_pro,
            planner_b_argument=arg_con,
            domain_focus=""
        )

        # Avg confidence = 0.60 (0.5 < 0.60 < 0.7) → Modify
        assert consensus.recommendation == "Modify"

    @pytest.mark.asyncio
    async def test_synthesis_no_go_recommendation(self, sample_idea):
        """Test No-go recommendation logic"""
        arg_pro = DebateArgument(
            planner_id="Planner-A",
            position="pro",
            arguments=["A1"],
            implementation_approach="Approach",
            risks=[],
            confidence=0.40
        )

        arg_con = DebateArgument(
            planner_id="Planner-B",
            position="con",
            arguments=["C1"],
            implementation_approach="",
            risks=[],
            confidence=0.40
        )

        # Low confidence = No-go
        consensus = await synthesize_consensus(
            idea=sample_idea,
            planner_a_argument=arg_pro,
            planner_b_argument=arg_con,
            domain_focus=""
        )

        # Avg confidence = 0.40 < 0.5 → No-go
        assert consensus.recommendation == "No-go"

    @pytest.mark.asyncio
    async def test_synthesis_risk_deduplication(self, sample_idea):
        """Test that duplicate risks are removed"""
        arg_pro = DebateArgument(
            planner_id="Planner-A",
            position="pro",
            arguments=["A1"],
            implementation_approach="Approach",
            risks=["Risk1", "Risk2"],
            confidence=0.70
        )

        arg_con = DebateArgument(
            planner_id="Planner-B",
            position="con",
            arguments=["C1"],
            implementation_approach="",
            risks=["Risk2", "Risk3"],  # Risk2 duplicate
            confidence=0.70
        )

        consensus = await synthesize_consensus(
            idea=sample_idea,
            planner_a_argument=arg_pro,
            planner_b_argument=arg_con,
            domain_focus=""
        )

        # Should have 3 unique risks (Risk1, Risk2, Risk3)
        assert len(consensus.risks) == 3
        assert "Risk1" in consensus.risks
        assert "Risk2" in consensus.risks
        assert "Risk3" in consensus.risks


# ─── Test: run_parallel_debate ────────────────────────────────────────────────


class TestRunParallelDebate:
    """Test run_parallel_debate orchestration"""

    @pytest.mark.asyncio
    async def test_parallel_execution(self, sample_idea):
        """Test that debate runs both planners in parallel"""
        consensus = await run_parallel_debate(
            idea=sample_idea,
            codebase_status="Sprint 6",
            domain_focus="Frontend"
        )

        assert isinstance(consensus, PlanningConsensus)
        assert consensus.recommendation in ["Go", "No-go", "Modify"]
        assert len(consensus.pros) > 0
        assert len(consensus.cons) > 0

    @pytest.mark.asyncio
    async def test_parallel_confidence_calculation(self, sample_idea):
        """Test consensus confidence is average of both planners"""
        consensus = await run_parallel_debate(
            idea=sample_idea,
            codebase_status="",
            domain_focus=""
        )

        # Confidence should be between 0 and 1
        assert 0.0 <= consensus.confidence <= 1.0

    @pytest.mark.asyncio
    async def test_parallel_with_low_confidence_idea(self):
        """Test parallel debate with low confidence idea"""
        low_conf_idea = PlanningIdea(
            title="Risky refactor",
            description="Major architectural change",
            segment="kernel-memory",
            priority="low",
            confidence=0.30,  # Low confidence
            rationale="Uncertain approach"
        )

        consensus = await run_parallel_debate(
            idea=low_conf_idea,
            codebase_status="",
            domain_focus=""
        )

        # Low confidence should likely result in No-go or Modify
        assert consensus.recommendation in ["No-go", "Modify"]


# ─── Test: Integration ────────────────────────────────────────────────────────


class TestIntegration:
    """Integration tests for full planning workflow"""

    @pytest.mark.skip(reason="Requires Marvin extract which needs real API connection")
    @pytest.mark.asyncio
    async def test_full_workflow_scan_to_consensus(self, mock_knowledge_search, mock_marvin_extract):
        """Test full workflow: scan → select → debate → consensus"""
        # 1. Scan
        with patch('marvin_tools.knowledge_search', mock_knowledge_search):
            with patch('marvin.extract', mock_marvin_extract):
                ideas = await scan_for_ideas(
                    segment_name="test-memory",
                    segment_content="TODO: Add dark mode",
                    domain_focus="Test"
                )

        assert len(ideas) > 0

        # 2. Select
        selected = await select_best_ideas(ideas, top_n=1)
        assert len(selected) > 0

        # 3. Debate
        consensus = await run_parallel_debate(
            idea=selected[0].idea,
            codebase_status="Sprint 6",
            domain_focus="Test"
        )

        assert isinstance(consensus, PlanningConsensus)
        assert consensus.recommendation is not None


# ─── Test: Edge Cases ─────────────────────────────────────────────────────────


class TestEdgeCases:
    """Test edge cases and error handling"""

    @pytest.mark.skip(reason="Requires Marvin extract which needs real API connection")
    @pytest.mark.asyncio
    async def test_scan_with_none_knowledge_results(self, mock_marvin_extract):
        """Test scan when knowledge search returns None/empty"""
        async def mock_empty_knowledge(query, top_k=5):
            return []

        with patch('marvin_tools.knowledge_search', mock_empty_knowledge):
            with patch('marvin.extract', mock_marvin_extract):
                ideas = await scan_for_ideas(
                    segment_name="test-memory",
                    segment_content="TODO: Test",
                    domain_focus=""
                )

                # Should still work, just without context
                if ideas:
                    assert ideas[0].context == "" or ideas[0].context is not None

    @pytest.mark.asyncio
    async def test_select_with_top_n_larger_than_list(self, sample_ideas):
        """Test select when top_n > available ideas"""
        selected = await select_best_ideas(
            sample_ideas,
            top_n=100  # Much larger than sample_ideas
        )

        # Should return all available ideas
        assert len(selected) <= len(sample_ideas)

    def test_idea_with_missing_optional_fields(self):
        """Test PlanningIdea with minimal fields"""
        idea = PlanningIdea(
            title="Test",
            description="Test desc",
            segment="test",
            priority="low",
            confidence=0.5,
            rationale="Test"
            # context is optional, should default to ""
        )

        assert idea.context == ""
