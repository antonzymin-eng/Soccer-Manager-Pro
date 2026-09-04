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
            self.assertIn("--survey-only", proc.stdout)
            self.assertNotIn("--enforce-dir", proc.stdout)
            self.assertIn("gate=", proc.stdout)
            self.assertIn("gate_args=", proc.stdout)
            self.assertIn(expected, proc.stdout)

    def test_runner_executes_survey_auditors_then_explicit_pr_gate_arguments(self) -> None:
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
                    "import os, sys\n"
                    f"Path(os.environ['TD_CAPTURE']).open('a').write('{name}:' + ' '.join(sys.argv[1:]) + '\\n')\n",
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
            lines = capture.read_text(encoding="utf-8").splitlines()
            self.assertEqual(len(lines), 3, lines)
            self.assertIn("checklist-auditor.py:", lines[0])
            self.assertIn("--survey-only", lines[0])
            self.assertIn("spec5-schema-auditor.py:", lines[1])
            self.assertIn("--survey-only", lines[1])
            self.assertEqual(lines[2], "gate:--owner-held-red report-only --coverage")

    def test_owner_held_red_verifier_rejects_drift_green_ambiguity_and_extra_results(self) -> None:
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
                "python3", str(verifier), "--ledger", str(ledger), "--results", str(results), "--dotnet-exit", "1"
            )
            self.assertEqual(good.returncode, 0, good.stdout)
            self.assertIn("MATCHES RECORDED BASELINE", good.stdout)

            trx.write_text(failed_xml.replace("-0.165", "-0.200"), encoding="utf-8")
            drift = self.run_cmd(
                "python3", str(verifier), "--ledger", str(ledger), "--results", str(results), "--dotnet-exit", "1"
            )
            self.assertEqual(drift.returncode, 1, drift.stdout)
            self.assertIn("changed diagnostics", drift.stdout)

            trx.write_text(failed_xml.replace('outcome="Failed"', 'outcome="Passed"'), encoding="utf-8")
            green = self.run_cmd(
                "python3", str(verifier), "--ledger", str(ledger), "--results", str(results), "--dotnet-exit", "0"
            )
            self.assertEqual(green.returncode, 1, green.stdout)
            self.assertIn("unexpectedly passed", green.stdout)

            ambiguous = failed_xml.replace(
                "</Results>",
                '<UnitTestResult testName="Other.sim_match_engine_close_chance" outcome="Failed"><Output><ErrorInfo><Message>-0.165 0.407</Message></ErrorInfo></Output></UnitTestResult></Results>',
            )
            trx.write_text(ambiguous, encoding="utf-8")
            dup = self.run_cmd(
                "python3", str(verifier), "--ledger", str(ledger), "--results", str(results), "--dotnet-exit", "1"
            )
            self.assertEqual(dup.returncode, 1, dup.stdout)
            self.assertIn("missing or ambiguous", dup.stdout)

            extra = failed_xml.replace(
                "</Results>",
                '<UnitTestResult testName="unrelated_test" outcome="Passed" /></Results>',
            )
            trx.write_text(extra, encoding="utf-8")
            extra_proc = self.run_cmd(
                "python3", str(verifier), "--ledger", str(ledger), "--results", str(results), "--dotnet-exit", "1"
            )
            self.assertEqual(extra_proc.returncode, 1, extra_proc.stdout)
            self.assertIn("unexpected additional test result", extra_proc.stdout)

    def test_gate_rejects_ambient_filter_and_accepts_explicit_settings(self) -> None:
        gate = ROOT / "tools" / "dotnet-ci" / "run-gate.sh"
        injected = self.run_cmd(
            "bash",
            str(gate),
            env={"TD_GATE_DRY_RUN": "1", "TD_GATE_TEST_FILTER": "FullyQualifiedName~OnlyMe"},
        )
        self.assertEqual(injected.returncode, 2, injected.stdout)
        self.assertIn("no longer accepted", injected.stdout)

        settings = ROOT / "tools" / "dotnet-ci" / "precommit.runsettings"
        explicit = self.run_cmd(
            "bash", str(gate), "--fast", "--settings", str(settings), env={"TD_GATE_DRY_RUN": "1"}
        )
        self.assertEqual(explicit.returncode, 0, explicit.stdout)
        self.assertIn(f"settings={settings}", explicit.stdout)
        self.assertIn("fast=1", explicit.stdout)
        self.assertIn("blocking_filter=<none>", explicit.stdout)

    def test_time_budget_enforces_timeout_and_propagates_success(self) -> None:
        timeout_proc = self.run_cmd(
            "python3", str(ROOT / "tools" / "run-with-time-budget.py"), "--seconds", "0.05", "--",
            "python3", "-c", "import time; time.sleep(2)",
        )
        self.assertEqual(timeout_proc.returncode, 124, timeout_proc.stdout)
        self.assertIn("command exceeded", timeout_proc.stdout)
        self.assertIn("wall-clock budget", timeout_proc.stdout)

        success_proc = self.run_cmd(
            "python3", str(ROOT / "tools" / "run-with-time-budget.py"), "--seconds", "1", "--",
            "python3", "-c", "print('ok')",
        )
        self.assertEqual(success_proc.returncode, 0, success_proc.stdout)
        self.assertIn("ok", success_proc.stdout)

    def test_owner_held_red_is_separate_from_quarantine_and_exactly_selected(self) -> None:
        gate = ROOT / "tools" / "dotnet-ci" / "run-gate.sh"
        proc = self.run_cmd(
            "bash", str(gate), "--owner-held-red", "report-only", env={"TD_GATE_DRY_RUN": "1"}
        )
        self.assertEqual(proc.returncode, 0, proc.stdout)
        self.assertIn("owner_held_include=Name=sim_match_engine_close_chance", proc.stdout)
        self.assertIn("Name!=sim_match_engine_close_chance", proc.stdout)
        ledger = (ROOT / "tools" / "dotnet-ci" / "owner-held-red.txt").read_text(encoding="utf-8")
        self.assertIn("sim_match_engine_close_chance|-0.165|0.407", ledger)
        quarantine = (ROOT / "tools" / "dotnet-ci" / "known-failures.txt").read_text(encoding="utf-8")
        self.assertNotIn("sim_match_engine_close_chance", quarantine)

    def test_hook_runs_staged_snapshot_and_preserves_untracked_build_cache(self) -> None:
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

            first = self.run_cmd(
                "bash", str(repo / ".githooks" / "pre-commit"), cwd=repo,
                env={"TD_TEST_CAPTURE": str(capture)},
            )
            self.assertEqual(first.returncode, 0, first.stdout)
            self.assertEqual(capture.read_text(encoding="utf-8"), "staged\n")

            git_dir = Path(subprocess.check_output(["git", "rev-parse", "--absolute-git-dir"], cwd=repo, text=True).strip())
            snapshot = git_dir / "testing-strategy" / "precommit-snapshot"
            marker = snapshot / "src" / "cache.marker"
            marker.parent.mkdir(parents=True, exist_ok=True)
            marker.write_text("warm\n", encoding="utf-8")

            (repo / "payload.txt").write_text("staged-two\n", encoding="utf-8")
            subprocess.run(["git", "add", "payload.txt"], cwd=repo, check=True)
            (repo / "payload.txt").write_text("unstaged-two\n", encoding="utf-8")
            second = self.run_cmd(
                "bash", str(repo / ".githooks" / "pre-commit"), cwd=repo,
                env={"TD_TEST_CAPTURE": str(capture)},
            )
            self.assertEqual(second.returncode, 0, second.stdout)
            self.assertEqual(capture.read_text(encoding="utf-8"), "staged-two\n")
            self.assertTrue(marker.exists(), "persistent snapshot must preserve untracked build outputs")

    def test_hook_installer_refuses_to_overwrite_custom_hook_path(self) -> None:
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
            configured = subprocess.check_output(["git", "config", "--get", "core.hooksPath"], cwd=repo, text=True).strip()
            self.assertEqual(configured, ".custom-hooks")

            subprocess.run(["git", "config", "--unset", "core.hooksPath"], cwd=repo, check=True)
            proc = self.run_cmd("bash", str(repo / "tools" / "run-tests-local.sh"), "--install-hook", cwd=repo)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            configured = subprocess.check_output(["git", "config", "--get", "core.hooksPath"], cwd=repo, text=True).strip()
            self.assertEqual(configured, ".githooks")

    def test_checklist_auditor_rejects_prose_and_placeholders_at_explicit_approval_gate(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            checklist = spec9 / "section-9-approval-checklist.md"
            checklist.write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | schema | this test check is fine |\n",
                encoding="utf-8",
            )
            prose = self.run_cmd(
                "python3", str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec9),
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
                "python3", str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec9),
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
                "python3", str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertIn("SURVEY", proc.stdout)

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
                "python3", str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)

    def test_spec5_auditor_rejects_keywords_hidden_in_unstructured_prose(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (spec9 / "section-5.md").write_text(
                "# Spec #9 — Section 5\n**Status:** APPROVED\n"
                "This paragraph says unit integration simulation property scenario coverage tier determinism approval but defines no schema surfaces.\n",
                encoding="utf-8",
            )
            proc = self.run_cmd(
                "python3", str(ROOT / "tools" / "spec5-schema-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
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
                "# Spec #1 — Section 5\n**Status:** APPROVED\nUnit only.\n", encoding="utf-8"
            )
            schema = self.run_cmd(
                "python3", str(ROOT / "tools" / "spec5-schema-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
            )
            self.assertEqual(schema.returncode, 0, schema.stdout)
            self.assertIn("SURVEY", schema.stdout)

    def test_property_coverage_and_precommit_settings_are_versioned(self) -> None:
        props = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8")
        self.assertIn('FsCheck.NUnit" Version="2.16.6"', props)
        self.assertIn('coverlet.collector" Version="6.0.4"', props)
        coverage = (ROOT / "tools" / "dotnet-ci" / "coverage.runsettings").read_text(encoding="utf-8")
        self.assertIn("XPlat Code Coverage", coverage)
        self.assertIn("cobertura", coverage)
        precommit = (ROOT / "tools" / "dotnet-ci" / "precommit.runsettings").read_text(encoding="utf-8")
        self.assertIn("method !~ '^int_'", precommit)
        self.assertIn("method !~ '^sim_'", precommit)
        self.assertIn("method !~ '^e2e_'", precommit)

    def test_pr_ci_reuses_versioned_runner(self) -> None:
        workflow = (ROOT / ".github" / "workflows" / "ci.yml").read_text(encoding="utf-8")
        self.assertIn("bash tools/run-tests-local.sh --pr", workflow)
        self.assertNotIn("run: bash tools/dotnet-ci/run-gate.sh\n", workflow)

    def test_nightly_certified_job_is_gated_until_runner_registration(self) -> None:
        workflow = (ROOT / ".github" / "workflows" / "nightly.yml").read_text(encoding="utf-8")
        self.assertIn("Nightly full simulation + soak (non-certifying)", workflow)
        self.assertIn("ubuntu-latest", workflow)
        self.assertIn("D E T E R M I N I S M".replace(" ", ""), "DETERMINISM")  # sanity: keep this test behavioral below
        self.assertIn("D E T E R M I N I S M_CERTIFIED_RUNNER_ENABLED".replace(" ", ""), workflow)
        self.assertIn("vars.DETERMINISM_CERTIFIED_RUNNER_ENABLED == 'true'", workflow)
        self.assertIn("[self-hosted, windows, x64, determinism-certified]", workflow)
        self.assertIn("win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags", workflow)
        self.assertIn("TD_UNITY_EXE", workflow)
        self.assertIn("TacticalDirector.DeterministicSim.Tests", workflow)

    def test_bootstrap_prepares_persistent_hook_cache(self) -> None:
        bootstrap = (ROOT / "tools" / "bootstrap-dev.sh").read_text(encoding="utf-8")
        self.assertIn("--install-hook", bootstrap)
        self.assertIn("--verify-hook", bootstrap)
        self.assertIn("TD_PRECOMMIT_PREPARE=1", bootstrap)
        hook = (ROOT / ".githooks" / "pre-commit").read_text(encoding="utf-8")
        self.assertIn("precommit-snapshot", hook)
        self.assertNotIn("mktemp -d", hook)

    def test_policy_shells_do_not_require_bash_mapfile(self) -> None:
        # macOS still commonly exposes bash 3.2; mapfile/readarray would make the
        # developer hook fail before tests. Arrays used by these scripts are
        # bash-3-compatible, but mapfile is not.
        self.assertNotIn("mapfile", (ROOT / "tools" / "run-tests-local.sh").read_text(encoding="utf-8"))
        self.assertNotIn("mapfile", (ROOT / "tools" / "dotnet-ci" / "run-gate.sh").read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
