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

    def test_policy_modes_use_survey_auditors_and_explicit_gate_arguments(self) -> None:
        expected = {
            "--pre-commit": ("budget_seconds=60", "--settings"),
            "--pr": (
                "Coverage: XPlat Code Coverage",
                "Coverage scope: changed src assemblies",
                "--owner-held-red report-only --coverage",
                "--coverage-settings",
            ),
            "--nightly": ("Full-match soak driver: ShotOutcomeDiagnosticTests", "--owner-held-red report-only --coverage"),
        }
        for mode, needles in expected.items():
            proc = self.run_cmd(
                "bash",
                str(ROOT / "tools" / "run-tests-local.sh"),
                mode,
                env={"TD_PIPELINE_DRY_RUN": "1"},
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertIn("--survey-only", proc.stdout)
            self.assertNotIn("--enforce-dir", proc.stdout)
            for needle in needles:
                self.assertIn(needle, proc.stdout)

    def test_lower_gate_rejects_ambient_filter_and_fast_mode_is_defined_without_filter(self) -> None:
        gate = ROOT / "tools" / "dotnet-ci" / "run-gate.sh"
        injected = self.run_cmd(
            "bash", str(gate),
            env={"TD_GATE_DRY_RUN": "1", "TD_GATE_TEST_FILTER": "FullyQualifiedName~OnlyMe"},
        )
        self.assertEqual(injected.returncode, 2, injected.stdout)
        self.assertIn("no longer accepted", injected.stdout)

        fast = self.run_cmd("bash", str(gate), "--fast", env={"TD_GATE_DRY_RUN": "1"})
        self.assertEqual(fast.returncode, 0, fast.stdout)
        self.assertIn("fast=1", fast.stdout)
        self.assertIn("blocking_filter=<none>", fast.stdout)
        self.assertNotIn("*.Tests.gen.csproj", (ROOT / "tools" / "dotnet-ci" / "run-gate.sh").read_text(encoding="utf-8"))

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
        self.assertIn("wall-clock budget", timeout_proc.stdout)

        success = self.run_cmd(
            "python3",
            str(ROOT / "tools" / "run-with-time-budget.py"),
            "--seconds",
            "1",
            "--",
            "python3",
            "-c",
            "print('ok')",
        )
        self.assertEqual(success.returncode, 0, success.stdout)
        self.assertIn("ok", success.stdout)

    def test_owner_held_red_verifier_requires_exact_identity_and_state(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            ledger = tmp / "ledger.txt"
            ledger.write_text(
                "sim_match_engine_close_chance|meanCosine=-0.165|goalwardShare=0.407\n",
                encoding="utf-8",
            )
            results = tmp / "results"
            results.mkdir()
            trx = results / "result.trx"
            base = """<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>
<UnitTestResult testName="TacticalDirector.MatchEngine.MatchEngineCloseChanceTests.sim_match_engine_close_chance" outcome="Failed">
<Output><ErrorInfo><Message>meanCosine=-0.165 goalwardShare=0.407</Message></ErrorInfo></Output>
</UnitTestResult></Results></TestRun>
"""
            verifier = ROOT / "tools" / "dotnet-ci" / "verify-owner-held-red.py"

            def verify(xml: str, dotnet_exit: int) -> subprocess.CompletedProcess[str]:
                trx.write_text(xml, encoding="utf-8")
                return self.run_cmd(
                    "python3", str(verifier),
                    "--ledger", str(ledger),
                    "--results", str(results),
                    "--dotnet-exit", str(dotnet_exit),
                )

            good = verify(base, 1)
            self.assertEqual(good.returncode, 0, good.stdout)

            drift = verify(base.replace("-0.165", "-0.200"), 1)
            self.assertEqual(drift.returncode, 1, drift.stdout)
            self.assertIn("changed diagnostics", drift.stdout)

            decoy = verify(
                base.replace(
                    "meanCosine=-0.165 goalwardShare=0.407",
                    "meanCosine=-0.100 (baseline -0.165) goalwardShare=0.500 (baseline 0.407)",
                ),
                1,
            )
            self.assertEqual(decoy.returncode, 1, decoy.stdout)
            self.assertIn("changed diagnostics", decoy.stdout)

            green = verify(base.replace('outcome="Failed"', 'outcome="Passed"'), 0)
            self.assertEqual(green.returncode, 1, green.stdout)
            self.assertIn("unexpectedly passed", green.stdout)

            ambiguous = base.replace(
                "</Results>",
                '<UnitTestResult testName="Other.sim_match_engine_close_chance" outcome="Failed"><Output><ErrorInfo><Message>meanCosine=-0.165 goalwardShare=0.407</Message></ErrorInfo></Output></UnitTestResult></Results>',
            )
            dup = verify(ambiguous, 1)
            self.assertEqual(dup.returncode, 1, dup.stdout)
            self.assertIn("missing or ambiguous", dup.stdout)

            extra = base.replace("</Results>", '<UnitTestResult testName="unrelated" outcome="Passed" /></Results>')
            extra_proc = verify(extra, 1)
            self.assertEqual(extra_proc.returncode, 1, extra_proc.stdout)
            self.assertIn("unexpected additional test result", extra_proc.stdout)

    def test_owner_held_red_is_not_quarantine_and_is_selected_by_exact_name(self) -> None:
        gate = ROOT / "tools" / "dotnet-ci" / "run-gate.sh"
        proc = self.run_cmd(
            "bash", str(gate), "--owner-held-red", "report-only",
            env={"TD_GATE_DRY_RUN": "1"},
        )
        self.assertEqual(proc.returncode, 0, proc.stdout)
        self.assertIn("owner_held_include=Name=sim_match_engine_close_chance", proc.stdout)
        self.assertIn("Name!=sim_match_engine_close_chance", proc.stdout)
        quarantine = (ROOT / "tools" / "dotnet-ci" / "known-failures.txt").read_text(encoding="utf-8")
        self.assertNotIn("sim_match_engine_close_chance", quarantine)

    def test_hook_uses_staged_snapshot_and_preserves_untracked_cache(self) -> None:
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
            (repo / "payload.txt").write_text("base\n", encoding="utf-8")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)

            (repo / "payload.txt").write_text("staged\n", encoding="utf-8")
            subprocess.run(["git", "add", "payload.txt"], cwd=repo, check=True)
            (repo / "payload.txt").write_text("unstaged\n", encoding="utf-8")
            first = self.run_cmd(
                "bash", str(repo / ".githooks" / "pre-commit"), cwd=repo,
                env={"TD_TEST_CAPTURE": str(capture)},
            )
            self.assertEqual(first.returncode, 0, first.stdout)
            self.assertEqual(capture.read_text(encoding="utf-8"), "staged\n")

            git_dir = Path(subprocess.check_output(["git", "rev-parse", "--absolute-git-dir"], cwd=repo, text=True).strip())
            snapshot = git_dir / "testing-strategy" / "precommit-snapshot"
            marker = snapshot / "obj" / "cache.marker"
            marker.parent.mkdir(parents=True, exist_ok=True)
            marker.write_text("warm", encoding="utf-8")

            (repo / "payload.txt").write_text("staged-two\n", encoding="utf-8")
            subprocess.run(["git", "add", "payload.txt"], cwd=repo, check=True)
            second = self.run_cmd(
                "bash", str(repo / ".githooks" / "pre-commit"), cwd=repo,
                env={"TD_TEST_CAPTURE": str(capture)},
            )
            self.assertEqual(second.returncode, 0, second.stdout)
            self.assertTrue(marker.exists(), "untracked build cache must survive snapshot refresh")

    def test_hook_installer_refuses_custom_hooks_path(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
            (repo / "tools").mkdir()
            (repo / ".githooks").mkdir()
            shutil.copy2(ROOT / "tools" / "run-tests-local.sh", repo / "tools" / "run-tests-local.sh")
            shutil.copy2(ROOT / ".githooks" / "pre-commit", repo / ".githooks" / "pre-commit")
            subprocess.run(["git", "config", "core.hooksPath", ".custom-hooks"], cwd=repo, check=True)
            proc = self.run_cmd("bash", str(repo / "tools" / "run-tests-local.sh"), "--install-hook", cwd=repo)
            self.assertEqual(proc.returncode, 2, proc.stdout)
            self.assertIn("refusing to overwrite", proc.stdout)

    def test_checklist_path_resolution_is_structural_not_semantic(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (repo / "README.md").write_text("unrelated\n", encoding="utf-8")
            (spec9 / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | exact crucial claim | `README.md` |\n",
                encoding="utf-8",
            )
            proc = self.run_cmd(
                "python3", str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec9),
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertNotIn("supporting the claim", proc.stdout)

    def test_checklist_concrete_section_and_captured_check_can_resolve(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (spec9 / "section-3.md").write_text(
                "# Spec #9 — Section 3\n## 3.2 Algorithm\nAlgorithm reviewed with proof.\n",
                encoding="utf-8",
            )
            (spec9 / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | algorithm | `section-3.md` §3.2 |\n",
                encoding="utf-8",
            )
            proc = self.run_cmd(
                "python3", str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec9),
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)


if __name__ == "__main__":
    unittest.main()
