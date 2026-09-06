from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
AUDITOR = ROOT / "tools" / "checklist-auditor.py"


class ChecklistExecutionAndNestedScopeTests(unittest.TestCase):
    def run_auditor(
        self,
        repo: Path,
        spec: Path,
        *extra: str,
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3", str(AUDITOR),
                "--root", str(repo / "docs" / "specs"),
                "--repo-root", str(repo),
                "--changed-scope", "--enforce-dir", str(spec),
                *extra,
            ],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=15,
        )

    def write_index(self, repo: Path, folder: str = "spec-nine") -> None:
        specs = repo / "docs" / "specs"
        specs.mkdir(parents=True, exist_ok=True)
        (specs / "SPEC_INDEX.md").write_text(
            "# SPEC_INDEX.md — Canonical Specification Registry\n\n"
            "| # | Specification | Folder | Priority | Status | Approved |\n"
            "|---|---|---|---|---|---|\n"
            f"| 9 | Spec Nine | `{folder}/` | 1 | APPROVED | September 5, 2026 |\n",
            encoding="utf-8",
        )

    def test_source_file_citation_is_not_treated_as_executable_check(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo)
            source = repo / "src" / "Constants.cs"
            source.parent.mkdir()
            source.write_text("internal const int TimeoutSeconds = 60;\n", encoding="utf-8")
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | timeout constant is declared | `src/Constants.cs` |\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(repo, spec)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertNotIn("captured output", proc.stdout)

    def test_explicit_programmatic_check_must_be_captured_and_pass(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo)
            tools = repo / "tools"
            tools.mkdir()
            check = tools / "check.py"
            check.write_text("print('bad')\nraise SystemExit(1)\n", encoding="utf-8")
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | stale references absent | `python3 tools/check.py` |\n",
                encoding="utf-8",
            )

            missing_capture = self.run_auditor(repo, spec)
            self.assertEqual(missing_capture.returncode, 1, missing_capture.stdout)
            self.assertIn("no matching captured output", missing_capture.stdout)

            failed = self.run_auditor(repo, spec, "--execute-checks")
            self.assertEqual(failed.returncode, 1, failed.stdout)
            self.assertIn("programmatic check failed", failed.stdout)
            self.assertIn("bad", failed.stdout)

            check.write_text("print('ok')\n", encoding="utf-8")
            passed = self.run_auditor(repo, spec, "--execute-checks")
            self.assertEqual(passed.returncode, 0, passed.stdout)
            self.assertIn("CAPTURED CHECK exit=0", passed.stdout)
            self.assertIn("ok", passed.stdout)

    def test_nested_checklist_inherits_owning_spec_canonical_status(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            nested = spec / "checklists"
            nested.mkdir(parents=True)
            self.write_index(repo)
            (nested / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | required evidence exists | prose only |\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("BLOCK", proc.stdout)
            self.assertNotIn("missing required approval-checklist file", proc.stdout)


if __name__ == "__main__":
    unittest.main()
