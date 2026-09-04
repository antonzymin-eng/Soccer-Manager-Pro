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

    def test_pre_commit_excludes_long_simulation_classes(self) -> None:
        text = (ROOT / "tools" / "run-tests-local.sh").read_text(encoding="utf-8")
        self.assertIn("TD_GATE_TEST_FILTER", text)
        self.assertIn("FullyQualifiedName!~sim_", text)
        self.assertIn("FullyQualifiedName!~e2e_", text)
        self.assertIn("TestCategory!=Calibration", text)

        gate = (ROOT / "tools" / "dotnet-ci" / "run-gate.sh").read_text(encoding="utf-8")
        self.assertIn('REQUESTED_FILTER="${TD_GATE_TEST_FILTER:-}"', gate)
        self.assertIn('FILTER="$REQUESTED_FILTER&$QUARANTINE_FILTER"', gate)

    def test_versioned_pre_commit_hook_uses_local_runner(self) -> None:
        path = ROOT / ".githooks" / "pre-commit"
        self.assertTrue(path.is_file(), "FR-TS-075 pre-commit hook is missing")
        text = path.read_text(encoding="utf-8")
        self.assertIn("tools/run-tests-local.sh", text)
        self.assertIn("--pre-commit", text)

    def test_nightly_workflow_is_scheduled_and_runner_owns_soak(self) -> None:
        path = ROOT / ".github" / "workflows" / "nightly.yml"
        self.assertTrue(path.is_file(), "FR-TS-075 nightly workflow is missing")
        workflow = path.read_text(encoding="utf-8")
        self.assertIn("schedule:", workflow)
        self.assertIn("cron:", workflow)
        self.assertIn("tools/run-tests-local.sh --nightly", workflow)

        runner = (ROOT / "tools" / "run-tests-local.sh").read_text(encoding="utf-8")
        self.assertIn("export TD_SHOT_DIAGNOSTIC=1", runner)
        self.assertIn("ShotOutcomeDiagnosticTests", runner)


if __name__ == "__main__":
    unittest.main()
