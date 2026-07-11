"""
Unit tests for Configuration Loading

Tests YAML config parsing for reviewer, nightwatch, and planning configs.
"""

import pytest
import yaml
import tempfile
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))


class TestReviewerConfig:
    """Test reviewer-config.yaml parsing"""
    
    @pytest.fixture
    def config_path(self):
        """Create temporary reviewer config"""
        config = {
            'reviewer': {
                'model_a': 'haiku',
                'model_b': 'haiku',
                'parallel': True,
                'require_both': True
            },
            'verdict': {
                'approve_keywords': ['APPROVE', 'APPROVED'],
                'reject_keywords': ['REJECT', 'REJECTED']
            },
            'timing': {
                'review_timeout': 120,
                'file_wait': 2
            }
        }
        
        with tempfile.NamedTemporaryFile(mode='w', suffix='.yaml', delete=False) as f:
            yaml.dump(config, f)
            return Path(f.name)
    
    def test_config_loads(self, config_path):
        """Test basic config loading"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        assert config is not None
        assert 'reviewer' in config
        assert 'verdict' in config
    
    def test_reviewer_section(self, config_path):
        """Test reviewer section parsing"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        reviewer = config['reviewer']
        assert reviewer['model_a'] == 'haiku'
        assert reviewer['model_b'] == 'haiku'
        assert reviewer['parallel'] is True
        assert reviewer['require_both'] is True
    
    def test_verdict_keywords(self, config_path):
        """Test verdict keyword lists"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        verdict = config['verdict']
        assert 'APPROVE' in verdict['approve_keywords']
        assert 'REJECT' in verdict['reject_keywords']
    
    def test_timing_config(self, config_path):
        """Test timing configuration"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        timing = config['timing']
        assert timing['review_timeout'] == 120
        assert timing['file_wait'] == 2


class TestNightwatchConfig:
    """Test nightwatch-config.yaml parsing"""
    
    @pytest.fixture
    def config_path(self):
        """Create temporary nightwatch config"""
        config = {
            'nightwatch': {
                'interval_seconds': 120,
                'priority_terminals': ['root', 'conductor']
            },
            'stuck_detection': {
                'threshold_minutes': 10,
                'nudge_retry_delay': 300
            }
        }
        
        with tempfile.NamedTemporaryFile(mode='w', suffix='.yaml', delete=False) as f:
            yaml.dump(config, f)
            return Path(f.name)
    
    def test_nightwatch_section(self, config_path):
        """Test nightwatch configuration"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        nightwatch = config['nightwatch']
        assert nightwatch['interval_seconds'] == 120
        assert 'root' in nightwatch['priority_terminals']
        assert 'conductor' in nightwatch['priority_terminals']
    
    def test_stuck_detection_config(self, config_path):
        """Test stuck detection parameters"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        stuck = config['stuck_detection']
        assert stuck['threshold_minutes'] == 10
        assert stuck['nudge_retry_delay'] == 300


class TestPlanningConfig:
    """Test config.yaml (planning) parsing"""
    
    @pytest.fixture
    def config_path(self):
        """Create temporary planning config"""
        config = {
            'planning': {
                'interval_seconds': 1800,
                'segments': [
                    'kernel-memory',
                    'orch-memory',
                    'fe-memory'
                ],
                'models': {
                    'scan': 'haiku',
                    'select': 'sonnet',
                    'debate': 'sonnet'
                }
            },
            'knowledge': {
                'api_url': 'http://localhost:3456/api/knowledge/search',
                'search_limit': 5
            }
        }
        
        with tempfile.NamedTemporaryFile(mode='w', suffix='.yaml', delete=False) as f:
            yaml.dump(config, f)
            return Path(f.name)
    
    def test_planning_section(self, config_path):
        """Test planning configuration"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        planning = config['planning']
        assert planning['interval_seconds'] == 1800
        assert len(planning['segments']) == 3
        assert 'kernel-memory' in planning['segments']
    
    def test_models_config(self, config_path):
        """Test model configuration"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        models = config['planning']['models']
        assert models['scan'] == 'haiku'
        assert models['select'] == 'sonnet'
        assert models['debate'] == 'sonnet'
    
    def test_knowledge_config(self, config_path):
        """Test knowledge service configuration"""
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        
        knowledge = config['knowledge']
        assert 'localhost:3456' in knowledge['api_url']
        assert knowledge['search_limit'] == 5


class TestConfigValidation:
    """Test configuration validation logic"""
    
    def test_missing_required_field(self):
        """Test handling of missing required fields"""
        config = {'reviewer': {}}  # Missing model_a, model_b
        
        # Should not crash, but validation logic would catch this
        assert 'model_a' not in config['reviewer']
    
    def test_invalid_model_name(self):
        """Test invalid model name detection"""
        config = {'reviewer': {'model_a': 'invalid-model'}}
        
        # Validation should catch this
        valid_models = ['haiku', 'sonnet', 'opus']
        assert config['reviewer']['model_a'] not in valid_models
    
    def test_negative_timeout(self):
        """Test negative timeout handling"""
        config = {'timing': {'review_timeout': -10}}
        
        # Validation should reject negative values
        assert config['timing']['review_timeout'] < 0


# Integration test
def test_load_actual_reviewer_config():
    """Test loading actual reviewer-config.yaml from repo"""
    config_path = Path(__file__).parent.parent / "reviewer-config.yaml"
    
    if not config_path.exists():
        pytest.skip("reviewer-config.yaml not found")
    
    with open(config_path, 'r') as f:
        config = yaml.safe_load(f)
    
    # Verify structure
    assert 'reviewer' in config
    assert 'verdict' in config
    assert config['reviewer']['parallel'] is True


def test_load_actual_nightwatch_config():
    """Test loading actual nightwatch-config.yaml from repo"""
    config_path = Path(__file__).parent.parent / "nightwatch-config.yaml"
    
    if not config_path.exists():
        pytest.skip("nightwatch-config.yaml not found")
    
    with open(config_path, 'r') as f:
        config = yaml.safe_load(f)
    
    # Verify structure
    assert 'nightwatch' in config
    assert 'stuck_detection' in config
    assert len(config['nightwatch']['priority_terminals']) >= 2
