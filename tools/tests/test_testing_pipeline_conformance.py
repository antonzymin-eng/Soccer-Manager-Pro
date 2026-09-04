#!/usr/bin/env python3
"""Structural locks for Testing Strategy #19 FR-TS-075 / FR-TS-079."""

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]


class TestingPipelineConformanceTests(unittest.TestCase):
    def test_local_runner_is_stable_entry_point(self) -> None:
        path = ROOT / "tools" / "run-tests-local.sh"
        self.assertTrue(path.is_file(), "FR-TS-079 runner is missing")
        text = path.read_text(encoding="utf-8")
        self.assertIn("tools/dotnet-ci/run-gate.sh", text)
        self.assertIn("--pre-commit", text)
        self.assertIn("--nightly", text)

    def test_versioned_pre_commit_hook_uses_local_runner(self) -> None:
        path = ROOT / ".githooks" / "pre-commit"
        self.assertTrue(path.is_file(), "FR-TS-075 pre-commit hook is missing")
        text = path.read_text(encoding="utf-8")
        self.assertIn("tools/run-tests-local.sh", text)
        self.assertIn("--pre-commit", text)

    def test_nightly_workflow_is_scheduled_and_uses_local_runner(self) -> None:
        path = ROOT / ".github" / "workflows" / "nightly.yml"
        self.assertTrue(path.is_file(), "FR-TS-075 nightly workflow is missing")
        text = path.read_text(encoding="utf-8")
        self.assertIn("schedule:", text)
        self.assertIn("cron:", text)
        self.assertIn("tools/run-tests-local.sh --nightly", text)


if __name__ == "__main__":
    unittest.main()
