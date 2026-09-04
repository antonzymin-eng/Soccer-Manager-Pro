#!/usr/bin/env python3
"""Behavioral locks for Testing Strategy #19 FR-TS-075 / FR-TS-079."""

from __future__ import annotations

import os
from pathlib import Path
import subprocess
import sys
import tempfile
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[2]
RUNNER = ROOT / "tools" / "run-tests-local.sh"
BUDGET = ROOT / "tools" / "run-with-time-budget.py"
GATE = ROOT / "tools" / "dotnet-ci" / "run-gate.sh"


def run(
    argv: list[str],
    *,
    env: dict[str, str] | None = None,
    timeout: float = 10,
) -> subprocess.CompletedProcess[str]:
    merged = os.environ.copy()
    if env:
        merged.update(env)
    return subprocess.run(
        argv,
        cwd=ROOT,
        env=merged,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=timeout,
        check=False,
    )


class TestingPipelineConformanceTests(unittest.TestCase):
    def test_pre_commit_plan_is_bounded_and_excludes_long_tiers(self) -> None:
        result = run(
            ["bash", str(RUNNER), "--pre-commit"],
            env={"TD_PIPELINE_DRY_RUN": "1"},
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("DRY-RUN budget_seconds=60", result.stdout)
        self.assertIn("FullyQualifiedName!~sim_", result.stdout)
        self.assertIn("FullyQualifiedName!~e2e_", result.stdout)
        self.assertIn("FullyQualifiedName!~Integration", result.stdout)
        self.assertIn("FullyQualifiedName!~Scenario", result.stdout)
        self.assertIn("TacticalDirector.DeterministicSim.Tests", result.stdout)
        self.assertIn("TestCategory!=Calibration", result.stdout)
        self.assertNotIn("Owner-held-red policy", result.stdout)
        self.assertNotIn("Full-match soak driver", result.stdout)

    def test_budget_helper_enforces_hard_timeout_and_propagates_success(self) -> None:
        ok = run(
            [
                sys.executable,
                str(BUDGET),
                "--seconds",
                "2",
                "--",
                sys.executable,
                "-c",
                "raise SystemExit(0)",
            ]
        )
        self.assertEqual(0, ok.returncode, ok.stderr)

        timed = run(
            [
                sys.executable,
                str(BUDGET),
                "--seconds",
                "0.05",
                "--",
                sys.executable,
                "-c",
                "import time; time.sleep(5)",
            ],
            timeout=3,
        )
        self.assertEqual(124, timed.returncode)
        self.assertIn("exceeded 0.05s wall-clock budget", timed.stderr)

    def test_nightly_plan_enables_soak_and_owner_held_report_only(self) -> None:
        result = run(
            ["bash", str(RUNNER), "--nightly"],
            env={"TD_PIPELINE_DRY_RUN": "1"},
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("Full-match soak driver: ShotOutcomeDiagnosticTests", result.stdout)
        self.assertIn("Owner-held-red policy: execute separately, report-only", result.stdout)

        gate = run(
            ["bash", str(GATE)],
            env={
                "TD_GATE_DRY_RUN": "1",
                "TD_OWNER_HELD_RED_MODE": "report-only",
            },
        )
        self.assertEqual(0, gate.returncode, gate.stderr)
        self.assertIn(
            "FullyQualifiedName!~sim_match_engine_close_chance",
            gate.stdout,
        )
        self.assertIn(
            "FullyQualifiedName~sim_match_engine_close_chance",
            gate.stdout,
        )

    def test_pr_plan_does_not_claim_certification_or_soak(self) -> None:
        result = run(
            ["bash", str(RUNNER), "--pr"],
            env={"TD_PIPELINE_DRY_RUN": "1"},
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertNotIn("Full-match soak driver", result.stdout)
        self.assertNotIn("Owner-held-red policy", result.stdout)

    def test_hook_install_and_verify_are_executable_behaviors(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            log = tmp_path / "git.log"
            fake_git = tmp_path / "git"
            fake_git.write_text(
                textwrap.dedent(
                    f"""\
                    #!/usr/bin/env bash
                    echo "$*" >> {str(log)!r}
                    if [[ "$*" == *"config --get core.hooksPath"* ]]; then
                      echo .githooks
                    fi
                    """
                ),
                encoding="utf-8",
            )
            fake_git.chmod(0o755)

            env = {"PATH": f"{tmp}{os.pathsep}{os.environ.get('PATH', '')}"}
            installed = run(["bash", str(RUNNER), "--install-hook"], env=env)
            self.assertEqual(0, installed.returncode, installed.stderr)
            self.assertIn("Pre-commit hook configuration verified.", installed.stdout)

            verified = run(["bash", str(RUNNER), "--verify-hook"], env=env)
            self.assertEqual(0, verified.returncode, verified.stderr)
            self.assertIn("Pre-commit hook configuration verified.", verified.stdout)

            calls = log.read_text(encoding="utf-8")
            self.assertIn("config core.hooksPath .githooks", calls)
            self.assertIn("config --get core.hooksPath", calls)

    def test_nightly_workflow_splits_functional_and_certified_jobs(self) -> None:
        workflow = (ROOT / ".github" / "workflows" / "nightly.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("schedule:", workflow)
        self.assertIn("tools/run-tests-local.sh --nightly", workflow)
        self.assertIn(
            "runs-on: [self-hosted, windows, x64, determinism-certified]",
            workflow,
        )
        self.assertIn(
            "win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags",
            workflow,
        )
        self.assertIn('TacticalDirector.DeterministicSim.Tests', workflow)
        self.assertIn("Unity -batchmode -runTests", workflow)


if __name__ == "__main__":
    unittest.main()
