from __future__ import annotations

import os
from pathlib import Path
import subprocess
import tempfile
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[2]
RUNNER = ROOT / "tools" / "run-tests-local.sh"


class TestingPipelineScopeTests(unittest.TestCase):
    def run_cmd(self, *args: str, cwd: Path | None = None, env: dict[str, str] | None = None) -> subprocess.CompletedProcess[str]:
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
            check=False,
            timeout=15,
        )

    def test_routine_pr_pipeline_is_survey_only_and_does_not_depend_on_base_diff(self) -> None:
        # A deliberately unresolvable PR base must not matter: routine CI is a
        # corpus survey, not an approval-transition gate for every changed spec.
        proc = self.run_cmd(
            "bash",
            str(RUNNER),
            "--pr",
            env={
                "GITHUB_BASE_REF": "definitely-does-not-exist",
                "TD_PIPELINE_DRY_RUN": "1",
            },
        )
        self.assertEqual(proc.returncode, 0, proc.stdout)
        self.assertIn("--survey-only", proc.stdout)
        self.assertNotIn("--enforce-dir", proc.stdout)
        self.assertNotIn("changed-spec audit scope", proc.stdout)

    def test_existing_approved_spec_debt_is_survey_in_routine_mode_but_blocks_explicit_approval_walk(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec9 = specs / "spec-nine"
            spec9.mkdir(parents=True)
            (spec9 / "section-5.md").write_text(
                "# Spec #9 — Section 5\n**Status:** APPROVED\nUnit only.\n",
                encoding="utf-8",
            )
            (spec9 / "section-9-approval-checklist.md").write_text(
                textwrap.dedent(
                    """\
                    # Spec #9 — Approval Checklist
                    **Status:** APPROVED
                    | Row | Claim | Evidence |
                    | --- | --- | --- |
                    | 9.1 | unresolved | prose only |
                    """
                ),
                encoding="utf-8",
            )

            for auditor in ("checklist-auditor.py", "spec5-schema-auditor.py"):
                survey = self.run_cmd(
                    "python3",
                    str(ROOT / "tools" / auditor),
                    "--root", str(specs),
                    "--repo-root", str(repo),
                    "--survey-only",
                )
                self.assertEqual(survey.returncode, 0, survey.stdout)
                self.assertIn("SURVEY", survey.stdout)

                approval = self.run_cmd(
                    "python3",
                    str(ROOT / "tools" / auditor),
                    "--root", str(specs),
                    "--repo-root", str(repo),
                    "--changed-scope",
                    "--enforce-dir", str(spec9),
                )
                self.assertEqual(approval.returncode, 1, approval.stdout)
                self.assertIn("BLOCK", approval.stdout)


if __name__ == "__main__":
    unittest.main()
