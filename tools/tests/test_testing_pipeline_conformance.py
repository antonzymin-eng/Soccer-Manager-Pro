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
            "--pr": ("Coverage: XPlat Code Coverage", "--owner-held-red report-only --coverage"),
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
            "bash",
            str(gate),
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
            ledger.write_text("sim_match_engine_close_chance|-0.165|0.407\n", encoding="utf-8")
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

            green = verify(base.replace('outcome="Failed"', 'outcome="Passed"'), 0)
            self.assertEqual(green.returncode, 1, green.stdout)
            self.assertIn("unexpectedly passed", green.stdout)

            ambiguous = base.replace(
                "</Results>",
                '<UnitTestResult testName="Other.sim_match_engine_close_chance" outcome="Failed"><Output><ErrorInfo><Message>-0.165 0.407</Message></ErrorInfo></Output></UnitTestResult></Results>',
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

    def test_checklist_path_existence_alone_does_not_resolve_claim(self) -> None:
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
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("does not contain concrete text or values supporting the claim", proc.stdout)

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
            (repo / "tools").mkdir()
            (repo / "tools" / "verify.py").write_text("print('ok')\n", encoding="utf-8")
            checklist = spec9 / "section-9-approval-checklist.md"
            checklist.write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n| --- | --- | --- |\n"
                "| 9.1 | algorithm reviewed | `section-3.md` §3.2 |\n"
                "| 9.2 | executable check | `python3 tools/verify.py` |\n",
                encoding="utf-8",
            )
            missing_capture = self.run_cmd(
                "python3", str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec9),
            )
            self.assertEqual(missing_capture.returncode, 1, missing_capture.stdout)
            self.assertIn("no matching --captured-check", missing_capture.stdout)

            captured = self.run_cmd(
                "python3", str(ROOT / "tools" / "checklist-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec9),
                "--captured-check", "python3 tools/verify.py",
            )
            self.assertEqual(captured.returncode, 0, captured.stdout)

    def test_spec5_keyword_bullets_do_not_satisfy_appendix_c(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (spec9 / "section-5.md").write_text(
                "# Spec #9 — Section 5\n**Status:** APPROVED\n"
                "- unit integration simulation\n- property\n- scenario\n- coverage tier\n- determinism\n- approval\n",
                encoding="utf-8",
            )
            proc = self.run_cmd(
                "python3", str(ROOT / "tools" / "spec5-schema-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec9),
            )
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("§5.1", proc.stdout)
            self.assertIn("§5.6", proc.stdout)

    def test_spec5_valid_appendix_c_payload_passes(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            scenario = repo / "tests" / "scenarios" / "spec-nine" / "smoke.json"
            scenario.parent.mkdir(parents=True)
            scenario.write_text("{}\n", encoding="utf-8")
            spec9.mkdir(parents=True)
            (spec9 / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n## 9.1.1 Balance verified\n",
                encoding="utf-8",
            )
            (spec9 / "section-5.md").write_text(textwrap.dedent("""\
                # Spec #9 — Section 5: Test Plan
                **Status:** APPROVED

                ## 5.1 Test Count by Taxonomy Layer
                | Layer | Count | Notes |
                |---|---:|---|
                | Unit | 3 | |
                | Integration | 1 | |
                | Simulation | 1 | |
                | Determinism (consumed from #16 §5) | — | Owned by #16 |
                | End-to-end / soak | 1 | |

                ## 5.2 Property Test List
                | Property | Tier (A/B/C) | Owning Module |
                |---|---|---|
                | `prop_balance` | A | Economy |

                ## 5.3 Scenario List
                | Scenario | Manifest Path | Tier |
                |---|---|---|
                | smoke | `tests/scenarios/spec-nine/smoke.json` | B |

                ## 5.4 Coverage Targets (Per Tier per KD-9)
                | Tier | Line | Branch |
                |---|---|---|
                | A | ≥ 98% | ≥ 95% |
                | B | ≥ 90% | ≥ 80% |
                | C | lint-only | — |

                ## 5.5 Determinism-Tier Classification of Authoritative Fields
                | Field | Tier | Source (#16 §1.1.1) |
                |---|---|---|
                | `Economy.Balance` | A | #16 §1.1.1 row Economy |

                ## 5.6 Approval-Checklist Linkage
                | Test ID | Verifies §9 Row |
                |---|---|
                | `unit_balance` | §9.1.1 |

                ## 5.7 Version History
                - v1
                """), encoding="utf-8")
            proc = self.run_cmd(
                "python3", str(ROOT / "tools" / "spec5-schema-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec9),
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)

    def test_legacy_schema_findings_remain_survey_only(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            legacy = specs / "legacy"
            legacy.mkdir(parents=True)
            (legacy / "section-5.md").write_text("# Spec #1 — Section 5\n**Status:** APPROVED\nUnit only.\n", encoding="utf-8")
            proc = self.run_cmd(
                "python3", str(ROOT / "tools" / "spec5-schema-auditor.py"),
                "--root", str(specs), "--repo-root", str(repo),
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertIn("SURVEY", proc.stdout)

    def test_workflows_route_policy_and_gate_unregistered_certified_runner(self) -> None:
        ci = (ROOT / ".github" / "workflows" / "ci.yml").read_text(encoding="utf-8")
        nightly = (ROOT / ".github" / "workflows" / "nightly.yml").read_text(encoding="utf-8")
        self.assertIn("bash tools/run-tests-local.sh --pr", ci)
        self.assertNotIn("run: bash tools/dotnet-ci/run-gate.sh\n", ci)
        self.assertIn("vars.DETERMINISM_CERTIFIED_RUNNER_ENABLED == 'true'", nightly)
        self.assertIn("vars.DETERMINISM_CERTIFIED_RUNNER_ENABLED != 'true'", nightly)
        self.assertIn("[self-hosted, windows, x64, determinism-certified]", nightly)

    def test_policy_shells_avoid_bash_4_mapfile(self) -> None:
        self.assertNotIn("mapfile", (ROOT / "tools" / "run-tests-local.sh").read_text(encoding="utf-8"))
        self.assertNotIn("mapfile", (ROOT / "tools" / "dotnet-ci" / "run-gate.sh").read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
