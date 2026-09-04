from __future__ import annotations

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[2]


class TestingPipelineConformanceTests(unittest.TestCase):
    def run_cmd(
        self,
        *args: str,
        cwd: Path | None = None,
        env: dict[str, str] | None = None,
        timeout: int = 15,
    ) -> subprocess.CompletedProcess[str]:
        merged = os.environ.copy()
        if env:
            merged.update(env)
        return subprocess.run(
            args,
            cwd=cwd or ROOT,
            env=merged,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=timeout,
            check=False,
        )

    def test_runner_modes_have_executable_behavior_plans(self) -> None:
        for mode, expected in (
            ("--pre-commit", "budget_seconds=60"),
            ("--pr", "Coverage: XPlat Code Coverage"),
            ("--nightly", "Full-match soak driver: ShotOutcomeDiagnosticTests"),
        ):
            proc = self.run_cmd(
                "bash",
                str(ROOT / "tools" / "run-tests-local.sh"),
                mode,
                env={"TD_PIPELINE_DRY_RUN": "1"},
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertIn("checklist_auditor=", proc.stdout)
            self.assertIn("schema_auditor=", proc.stdout)
            self.assertIn("gate=", proc.stdout)
            self.assertIn(expected, proc.stdout)

    def test_owner_held_red_verifier_rejects_changed_diagnostics(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            ledger = tmp / "ledger.txt"
            ledger.write_text("sim_match_engine_close_chance|-0.165|0.407\n", encoding="utf-8")
            results = tmp / "results"
            results.mkdir()
            trx = results / "result.trx"
            trx.write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testName="TacticalDirector.MatchEngine.MatchEngineCloseChanceTests.sim_match_engine_close_chance"
                    outcome="Failed">
      <Output><ErrorInfo><Message>meanCosine=-0.165 goalwardShare=0.407</Message></ErrorInfo></Output>
    </UnitTestResult>
  </Results>
</TestRun>
""",
                encoding="utf-8",
            )
            verifier = ROOT / "tools" / "dotnet-ci" / "verify-owner-held-red.py"
            good = self.run_cmd(
                "python3", str(verifier),
                "--ledger", str(ledger),
                "--results", str(results),
                "--dotnet-exit", "1",
            )
            self.assertEqual(good.returncode, 0, good.stdout)
            self.assertIn("MATCHES RECORDED BASELINE", good.stdout)

            trx.write_text(
                trx.read_text(encoding="utf-8").replace("-0.165", "-0.200"),
                encoding="utf-8",
            )
            bad = self.run_cmd(
                "python3", str(verifier),
                "--ledger", str(ledger),
                "--results", str(results),
                "--dotnet-exit", "1",
            )
            self.assertEqual(bad.returncode, 1, bad.stdout)
            self.assertIn("changed diagnostics", bad.stdout)

    def test_precommit_filter_is_narrow_and_trusted(self) -> None:
        proc = self.run_cmd(
            "bash",
            str(ROOT / "tools" / "run-tests-local.sh"),
            "--pre-commit",
            env={"TD_PIPELINE_DRY_RUN": "1"},
        )
        self.assertEqual(proc.returncode, 0, proc.stdout)
        self.assertIn("FullyQualifiedName!~sim_", proc.stdout)
        self.assertIn("FullyQualifiedName!~e2e_", proc.stdout)
        self.assertIn("TacticalDirector.DeterministicSim.Tests", proc.stdout)

        injected = self.run_cmd(
            "bash",
            str(ROOT / "tools" / "dotnet-ci" / "run-gate.sh"),
            env={
                "TD_GATE_DRY_RUN": "1",
                "TD_GATE_TEST_FILTER": "FullyQualifiedName~OnlyMe",
            },
        )
        self.assertEqual(injected.returncode, 2, injected.stdout)
        self.assertIn("trusted testing-strategy runner marker", injected.stdout)

    def test_time_budget_enforces_timeout_and_propagates_success(self) -> None:
        timeout_proc = self.run_cmd(
            "python3",
            str(ROOT / "tools" / "run-with-time-budget.py"),
            "--seconds",
            "0.05",
            "--",
            "python3",
            "-c",
            "import time; time.sleep(2)",
        )
        self.assertEqual(timeout_proc.returncode, 124, timeout_proc.stdout)
        self.assertIn("command exceeded", timeout_proc.stdout)
        self.assertIn("wall-clock budget", timeout_proc.stdout)

        success_proc = self.run_cmd(
            "python3",
            str(ROOT / "tools" / "run-with-time-budget.py"),
            "--seconds",
            "1",
            "--",
            "python3",
            "-c",
            "print('ok')",
        )
        self.assertEqual(success_proc.returncode, 0, success_proc.stdout)
        self.assertIn("ok", success_proc.stdout)

    def test_owner_held_red_is_separate_from_quarantine_and_value_pinned(self) -> None:
        gate = ROOT / "tools" / "dotnet-ci" / "run-gate.sh"
        proc = self.run_cmd(
            "bash",
            str(gate),
            env={"TD_GATE_DRY_RUN": "1", "TD_OWNER_HELD_RED_MODE": "report-only"},
        )
        self.assertEqual(proc.returncode, 0, proc.stdout)
        self.assertIn("owner_held_include=FullyQualifiedName~sim_match_engine_close_chance", proc.stdout)
        ledger = (ROOT / "tools" / "dotnet-ci" / "owner-held-red.txt").read_text(encoding="utf-8")
        self.assertIn("sim_match_engine_close_chance|-0.165|0.407", ledger)
        quarantine = (ROOT / "tools" / "dotnet-ci" / "known-failures.txt").read_text(encoding="utf-8")
        self.assertNotIn("sim_match_engine_close_chance", quarantine)

    def test_hook_runs_staged_snapshot_not_unstaged_worktree(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td) / "repo"
            repo.mkdir()
            subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.name", "Test"], cwd=repo, check=True)
            (repo / ".githooks").mkdir()
            (repo / "tools").mkdir()
            shutil.copy2(ROOT / ".githooks" / "pre-commit", repo / ".githooks" / "pre-commit")
            capture = Path(td) / "captured.txt"
            (repo / "tools" / "run-tests-local.sh").write_text(
                "#!/usr/bin/env bash\nset -euo pipefail\ncat payload.txt > \"$TD_TEST_CAPTURE\"\n",
                encoding="utf-8",
            )
            (repo / "payload.txt").write_text("baseline\n", encoding="utf-8")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)

            (repo / "payload.txt").write_text("staged\n", encoding="utf-8")
            subprocess.run(["git", "add", "payload.txt"], cwd=repo, check=True)
            (repo / "payload.txt").write_text("unstaged\n", encoding="utf-8")

            proc = self.run_cmd(
                "bash",
                str(repo / ".githooks" / "pre-commit"),
                cwd=repo,
                env={"TD_TEST_CAPTURE": str(capture)},
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertEqual(capture.read_text(encoding="utf-8"), "staged\n")

    def test_hook_installer_refuses_to_overwrite_custom_hook_path(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
            (repo / "tools").mkdir()
            (repo / ".githooks").mkdir()
            shutil.copy2(ROOT / "tools" / "run-tests-local.sh", repo / "tools" / "run-tests-local.sh")
            shutil.copy2(ROOT / ".githooks" / "pre-commit", repo / ".githooks" / "pre-commit")

            subprocess.run(["git", "config", "core.hooksPath", ".custom-hooks"], cwd=repo, check=True)
            proc = self.run_cmd(
                "bash", str(repo / "tools" / "run-tests-local.sh"), "--install-hook", cwd=repo
            )
            self.assertEqual(proc.returncode, 2, proc.stdout)
            self.assertIn("refusing to overwrite", proc.stdout)
            configured = subprocess.check_output(
                ["git", "config", "--get", "core.hooksPath"], cwd=repo, text=True
            ).strip()
            self.assertEqual(configured, ".custom-hooks")

            subprocess.run(["git", "config", "--unset", "core.hooksPath"], cwd=repo, check=True)
            proc = self.run_cmd(
                "bash", str(repo / "tools" / "run-tests-local.sh"), "--install-hook", cwd=repo
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            configured = subprocess.check_output(
                ["git", "config", "--get", "core.hooksPath"], cwd=repo, text=True
            ).strip()
            self.assertEqual(configured, ".githooks")

    def test_auditors_block_broken_evidence_and_legacy_schema_is_survey_only(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (spec9 / "section-5.md").write_text(
                textwrap.dedent(
                    """\
                    # Spec #9 — Section 5: Test Plan
                    Unit Integration Simulation
                    Property tests: none.
                    Scenario list: none.
                    Coverage targets by Tier.
                    Determinism Tier classification.
                    Approval checklist linkage: section-9-approval-checklist.md.
                    """
                ),
                encoding="utf-8",
            )
            (spec9 / "section-9-approval-checklist.md").write_text(
                textwrap.dedent(
                    """\
                    # Spec #9 — Approval Checklist
                    | Row | Claim | Evidence |
                    | --- | --- | --- |
                    | 9.1 | schema | `section-5.md` |
                    """
                ),
                encoding="utf-8",
            )
            good = self.run_cmd(
                "python3",
                str(ROOT / "tools" / "checklist-auditor.py"),
                "--root",
                str(specs),
                "--repo-root",
                str(repo),
            )
            self.assertEqual(good.returncode, 0, good.stdout)

            (spec9 / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | schema | `missing.md` |\n",
                encoding="utf-8",
            )
            bad = self.run_cmd(
                "python3",
                str(ROOT / "tools" / "checklist-auditor.py"),
                "--root",
                str(specs),
                "--repo-root",
                str(repo),
            )
            self.assertEqual(bad.returncode, 1, bad.stdout)
            self.assertIn("BLOCK", bad.stdout)

            legacy = specs / "legacy"
            legacy.mkdir()
            (legacy / "section-5.md").write_text("# Spec #1 — Section 5\nUnit only.\n", encoding="utf-8")
            schema = self.run_cmd(
                "python3",
                str(ROOT / "tools" / "spec5-schema-auditor.py"),
                "--root",
                str(specs),
                "--repo-root",
                str(repo),
            )
            self.assertEqual(schema.returncode, 0, schema.stdout)
            self.assertIn("SURVEY", schema.stdout)

    def test_property_and_coverage_tool_pins_are_versioned(self) -> None:
        props = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8")
        self.assertIn('FsCheck.NUnit" Version="2.16.6"', props)
        self.assertIn('coverlet.collector" Version="6.0.4"', props)
        settings = (ROOT / "tools" / "dotnet-ci" / "coverage.runsettings").read_text(encoding="utf-8")
        self.assertIn("XPlat Code Coverage", settings)
        self.assertIn("cobertura", settings)

    def test_nightly_separates_linux_from_certified_windows_determinism(self) -> None:
        workflow = (ROOT / ".github" / "workflows" / "nightly.yml").read_text(encoding="utf-8")
        self.assertIn("Nightly full simulation + soak (non-certifying)", workflow)
        self.assertIn("ubuntu-latest", workflow)
        self.assertIn("[self-hosted, windows, x64, determinism-certified]", workflow)
        self.assertIn("win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags", workflow)
        self.assertIn("TD_UNITY_EXE", workflow)
        self.assertIn('TacticalDirector.DeterministicSim.Tests', workflow)
        self.assertNotIn("Unity -batchmode", workflow)

    def test_bootstrap_is_versioned_and_verifies_hook(self) -> None:
        bootstrap = (ROOT / "tools" / "bootstrap-dev.sh").read_text(encoding="utf-8")
        self.assertIn("--install-hook", bootstrap)
        self.assertIn("--verify-hook", bootstrap)


if __name__ == "__main__":
    unittest.main()
