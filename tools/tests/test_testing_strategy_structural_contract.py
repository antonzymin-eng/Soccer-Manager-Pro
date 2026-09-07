from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
CHECKLIST = ROOT / "tools" / "checklist-auditor.py"
SCHEMA = ROOT / "tools" / "spec5-schema-auditor.py"


class TestingStrategyStructuralContractTests(unittest.TestCase):
    def run_auditor(
        self,
        script: Path,
        repo: Path,
        spec: Path,
        *extra: str,
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3",
                str(script),
                "--root",
                str(repo / "docs" / "specs"),
                "--repo-root",
                str(repo),
                "--changed-scope",
                "--enforce-dir",
                str(spec),
                *extra,
            ],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=15,
        )

    def test_table_status_checkbox_must_be_checked(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-1.md").write_text(
                "# Spec #9 — Section 1\n## 1.1 Evidence\nStructural evidence.\n",
                encoding="utf-8",
            )
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Status | Evidence |\n"
                "|---|---|---|---|\n"
                "| 9.1 | structural evidence exists | [ ] | `section-1.md` §1.1 |\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("status checkbox is not checked", proc.stdout)

    def test_section_reference_is_bound_to_explicit_markdown_path(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec = specs / "spec-nine"
            other = specs / "other"
            spec.mkdir(parents=True)
            other.mkdir(parents=True)
            (spec / "section-3.md").write_text(
                "# Spec #9 — Section 3\n## 3.2 Local section\nLocal text.\n",
                encoding="utf-8",
            )
            (other / "section-3.md").write_text(
                "# Other — Section 3\n## 3.1 Different section\nOther text.\n",
                encoding="utf-8",
            )
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n"
                "|---|---|---|\n"
                "| 9.1 | cross-file section resolves | `docs/specs/other/section-3.md` §3.2 |\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("unresolved evidence section(s): §3.2", proc.stdout)

    def test_compound_programmatic_invocation_is_rejected_before_execution(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            tools = repo / "tools"
            spec.mkdir(parents=True)
            tools.mkdir(parents=True)
            good_marker = repo / "good-ran.txt"
            bad_marker = repo / "bad-ran.txt"
            (tools / "good.py").write_text(
                "from pathlib import Path\nPath('good-ran.txt').write_text('ran')\n",
                encoding="utf-8",
            )
            (tools / "failing.py").write_text(
                "from pathlib import Path\nPath('bad-ran.txt').write_text('ran')\nraise SystemExit(1)\n",
                encoding="utf-8",
            )
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n"
                "|---|---|---|\n"
                "| 9.1 | both checks execute | `python3 tools/good.py && python3 tools/failing.py` |\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(CHECKLIST, repo, spec, "--execute-checks")
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("invalid/unsupported programmatic invocation", proc.stdout)
            self.assertFalse(good_marker.exists(), "invalid compound command must not be partially executed")
            self.assertFalse(bad_marker.exists(), "invalid compound command must not be partially executed")

    def test_legacy_specs_block_when_explicitly_reapproved_but_remain_survey_elsewhere(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "legacy-one"
            spec.mkdir(parents=True)
            (spec / "section-5.md").write_text(
                "# Spec #1 — Section 5\n**Status:** APPROVED\nUnit only.\n",
                encoding="utf-8",
            )
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #1 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n"
                "|---|---|---|\n"
                "| 9.1 | evidence exists | prose only |\n",
                encoding="utf-8",
            )

            checklist_reapproval = self.run_auditor(CHECKLIST, repo, spec)
            schema_reapproval = self.run_auditor(SCHEMA, repo, spec)
            self.assertEqual(checklist_reapproval.returncode, 1, checklist_reapproval.stdout)
            self.assertEqual(schema_reapproval.returncode, 1, schema_reapproval.stdout)
            self.assertIn("BLOCK", checklist_reapproval.stdout)
            self.assertIn("BLOCK", schema_reapproval.stdout)

            checklist_survey = subprocess.run(
                [
                    "python3",
                    str(CHECKLIST),
                    "--root",
                    str(repo / "docs" / "specs"),
                    "--repo-root",
                    str(repo),
                ],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=15,
            )
            schema_survey = subprocess.run(
                [
                    "python3",
                    str(SCHEMA),
                    "--root",
                    str(repo / "docs" / "specs"),
                    "--repo-root",
                    str(repo),
                ],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=15,
            )
            self.assertEqual(checklist_survey.returncode, 0, checklist_survey.stdout)
            self.assertEqual(schema_survey.returncode, 0, schema_survey.stdout)
            self.assertIn("SURVEY", checklist_survey.stdout)
            self.assertIn("SURVEY", schema_survey.stdout)


if __name__ == "__main__":
    unittest.main()
