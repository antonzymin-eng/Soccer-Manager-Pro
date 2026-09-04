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
            self.assertIn("auditor_args=", proc.stdout)
            self.assertIn("--changed-scope", proc.stdout)
            self.assertIn("gate=", proc.stdout)
            self.assertIn("gate_args=", proc.stdout)
            self.assertIn(expected, proc.stdout)

    def test_runner_executes_auditors_then_explicit_gate_arguments(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            root = Path(td) / "repo"
            (root / "tools" / "dotnet-ci").mkdir(parents=True)
            (root / "docs" / "specs").mkdir(parents=True)
            shutil.copy2(ROOT / "tools" / "run-tests-local.sh", root / "tools" / "run-tests-local.sh")
            shutil.copy2(ROOT / "tools" / "run-with-time-budget.py", root / "tools" / "run-with-time-budget.py")
            capture = Path(td) / "capture.txt"
            for name in ("checklist-auditor.py", "spec5-schema-auditor.py"):
                (root / "tools" / name).write_text(
                    "from pathlib import Path\n"
                    "import os\n"
                    f"Path(os.environ['TD_CAPTURE']).open('a').write('{name}\\n')\n",
                    encoding="utf-8",
                )
            (root / "tools" / "dotnet-ci" / "run-gate.sh").write_text(
                "#!/usr/bin/env bash\n"
                "set -euo pipefail\n"
                "printf 'gate:%s\\n' \"$*\" >> \"$TD_CAPTURE\"\n",
                encoding="utf-8",
            )

            proc = self.run_cmd(
                "bash",
                str(root / "tools" / "run-tests-local.sh"),
                "--pr",
                cwd=root,
                env={"TD_CAPTURE": str(capture)},
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertEqual(
                capture.read_text(encoding="utf-8").splitlines(),
                [
                    "checklist-auditor.py",
                    "spec5-schema-auditor.py",
                    "gate:--owner-held-red report-only --coverage",
                ],
            )

    def test_owner_held_red_verifier_rejects_changed_diagnostics_and_unexpected_green(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            ledger = tmp / "ledger.txt"
            ledger.write_text("sim_match_engine_close_chance|-0.165|0.407\n", encoding="utf-8")
            results = tmp / "results"
            results.mkdir()
            trx = results / "result.trx"
            failed_xml = """<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testName="TacticalDirector.MatchEngine.MatchEngineCloseChanceTests.sim_match_engine_close_chance"
                    outcome="Failed">
      <Output><ErrorInfo><Message>meanCosine=-0.165 goalwardShare=0.407</Message></ErrorInfo></Output>
    </UnitTestResult>
  </Results>
</TestRun>
"""
            trx.write_text(failed_xml, encoding="utf-8")
            verifier = ROOT / "tools" / "dotnet-ci" / "verify-owner-held-red.py"
            good = self.run_cmd(
                "python3", str(verifier),
                "--ledger", str(ledger),
                "--results", str(results),
                "--dotnet-exit", "1",
            )
            self.assertEqual(good.returncode, 0, good.stdout)
            self.assertIn("MATCHES RECORDED BASELINE", good.stdout)

            trx.write_text(failed_xml.replace("-0.165", "-0.200"), encoding="utf-8")
            bad = self.run_cmd(
                "python3", str(verifier),
                "--ledger", str(ledger),
                "--results", str(results),
                "--dotnet-exit", "1",
            )
            self.assertEqual(bad.returncode, 1, bad.stdout)
            self.assertIn("changed diagnostics", bad.stdout)

            trx.write_text(failed_xml.replace('outcome="Failed"', 'outcome="Passed"'), encoding="utf-8")
            green = self.run_cmd(
                "python3", str(verifier),
                "--ledger", str(ledger),
                "--results", str(results),
                "--dotnet-exit", "0",
            )
            self.assertEqual(green.returncode, 1, green.stdout)
            self.assertIn("unexpectedly passed", green.stdout)

    def test_precommit_filter_is_narrow_and_gate_rejects_ambient_filter(self) -> None:
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
        self.assertIn("--fast", proc.stdout)

        injected = self.run_cmd(
            "bash",
            str(ROOT / "tools" / "dotnet-ci" / "run-gate.sh"),
            env={
                "TD_GATE_DRY_RUN": "1",
                "TD_GATE_TEST_FILTER": "FullyQualifiedName~OnlyMe",
            },
        )
        self.assertEqual(injected.returncode, 2, injected.stdout)
        self.assertIn("no longer accepted", injected.stdout)

        explicit = self.run_cmd(
            "bash",
            str(ROOT / "tools" / "dotnet-ci" / "run-gate.sh"),
            "--fast",
            "--test-filter",
            "FullyQualifiedName~OnlyMe",
            env={"TD_GATE_DRY_RUN": "1"},
        )
        self.assertEqual(explicit.returncode, 0, explicit.stdout)
        self.assertIn("blocking_filter=FullyQualifiedName~OnlyMe", explicit.stdout)
        self.assertIn("fast=1", explicit.stdout)

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
            "--owner-held-red",
            "report-only",
            env={"TD_GATE_DRY_RUN": "1"},
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

    def test_auditors_block_broken_placeholder_and_prose_only_evidence_for_approved_spec(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            section5 = textwrap.dedent(
                """\
                # Spec #9 — Section 5: Test Plan
                **Status:** APPROVED
                | Layer | Unit | Integration | Simulation |
                | --- | --- | --- | --- |
                | Count | 1 | 1 | 1 |
                - Property tests: none.
                - Scenario list: none.
                - Coverage targets by Tier: none.
                - Determinism Tier classification: none.
                - Approval checklist linkage: `section-9-approval-checklist.md`.
                """
            )
            (spec9 / "section-5.md").write_text(section5, encoding="utf-8")
            checklist = spec9 / "section-9-approval-checklist.md"
            checklist.write_text(
                textwrap.dedent(
                    """\
                    # Spec #9 — Approval Checklist
                    **Status:** APPROVED
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

            checklist.write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | schema | this test check is fine |\n",
                encoding="utf-8",
            )
            prose = self.run_cmd(
                "python3",
                str(ROOT / "tools" / "checklist-auditor.py"),
                "--root",
                str(specs),
                "--repo-root",
                str(repo),
            )
            self.assertEqual(prose.returncode, 1, prose.stdout)
            self.assertIn("prose only", prose.stdout)

            checklist.write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | schema | `<file-path>` |\n",
                encoding="utf-8",
            )
            placeholder = self.run_cmd(
                "python3",
                str(ROOT / "tools" / "checklist-auditor.py"),
                "--root",
                str(specs),
                "--repo-root",
                str(repo),
            )
            self.assertEqual(placeholder.returncode, 1, placeholder.stdout)
            self.assertIn("placeholder evidence", placeholder.stdout)

    def test_amendment_draft_findings_are_reported_but_nonblocking(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (spec9 / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "**Status:** AMENDMENT DRAFT (approved baseline remains in force)\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | pending | prose only |\n",
                encoding="utf-8",
            )
            proc = self.run_cmd(
                "python3",
                str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs),
                "--repo-root", str(repo),
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertIn("SURVEY", proc.stdout)
            self.assertIn("prose only", proc.stdout)

    def test_local_section_reference_is_resolved_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (spec9 / "section-3.md").write_text("# Spec #9 — Section 3\n", encoding="utf-8")
            (spec9 / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | algorithm | §3.2 §3.4 |\n",
                encoding="utf-8",
            )
            proc = self.run_cmd(
                "python3",
                str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs),
                "--repo-root", str(repo),
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)

    def test_spec5_auditor_rejects_keywords_hidden_in_unstructured_prose(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (spec9 / "section-5.md").write_text(
                "# Spec #9 — Section 5\n"
                "**Status:** APPROVED\n"
                "This paragraph says unit integration simulation property scenario coverage tier "
                "determinism approval but defines no schema surfaces.\n",
                encoding="utf-8",
            )
            proc = self.run_cmd(
                "python3",
                str(ROOT / "tools" / "spec5-schema-auditor.py"),
                "--root",
                str(specs),
                "--repo-root",
                str(repo),
            )
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("missing structured taxonomy/test-count", proc.stdout)

    def test_legacy_schema_is_survey_only_even_when_approved(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            legacy = specs / "legacy"
            legacy.mkdir(parents=True)
            (legacy / "section-5.md").write_text(
                "# Spec #1 — Section 5\n**Status:** APPROVED\nUnit only.\n",
                encoding="utf-8",
            )
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

    def test_pr_ci_reuses_versioned_runner(self) -> None:
        workflow = (ROOT / ".github" / "workflows" / "ci.yml").read_text(encoding="utf-8")
        self.assertIn("PR functional gate (Linux shim, non-certifying)", workflow)
        self.assertIn("bash tools/run-tests-local.sh --pr", workflow)
        self.assertNotIn("run: bash tools/dotnet-ci/run-gate.sh\n", workflow)

    def test_nightly_separates_linux_from_certified_windows_determinism(self) -> None:
        workflow = (ROOT / ".github" / "workflows" / "nightly.yml").read_text(encoding="utf-8")
        self.assertIn("Nightly full simulation + soak (non-certifying)", workflow)
        self.assertIn("ubuntu-latest", workflow)
        self.assertIn("[self-hosted, windows, x64, determinism-certified]", workflow)
        self.assertIn("win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags", workflow)
        self.assertIn("TD_UNITY_EXE", workflow)
        self.assertIn("TacticalDirector.DeterministicSim.Tests", workflow)
        self.assertNotIn("Unity -batchmode", workflow)

    def test_bootstrap_is_versioned_and_verifies_hook(self) -> None:
        bootstrap = (ROOT / "tools" / "bootstrap-dev.sh").read_text(encoding="utf-8")
        self.assertIn("--install-hook", bootstrap)
        self.assertIn("--verify-hook", bootstrap)


if __name__ == "__main__":
    unittest.main()
