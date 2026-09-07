from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
AUDITOR = ROOT / "tools" / "checklist-auditor.py"


class Pr357RemainingCodexFindingsTests(unittest.TestCase):
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

    def run_auditor(
        self,
        repo: Path,
        spec: Path,
        *extra: str,
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3",
                str(AUDITOR),
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

    def test_command_substitution_is_rejected_before_execution(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo)
            tools = repo / "tools"
            tools.mkdir()
            (tools / "good.py").write_text("print('good')\n", encoding="utf-8")
            (tools / "failing.py").write_text(
                "print('nested-ran')\nraise SystemExit(1)\n", encoding="utf-8"
            )
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | checks pass | `python3 tools/good.py $(python3 tools/failing.py)` |\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(repo, spec, "--execute-checks")
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("invalid/unsupported programmatic invocation", proc.stdout)
            self.assertNotIn("CAPTURED CHECK", proc.stdout)
            self.assertNotIn("nested-ran", proc.stdout)

    def test_external_absolute_path_is_not_repository_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            repo = root / "repo"
            repo.mkdir()
            outside = root / "outside.txt"
            outside.write_text("not repository evidence\n", encoding="utf-8")
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo)
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                f"| 9.1 | evidence exists | `{outside}` |\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("unresolved evidence path", proc.stdout)

    def test_untracked_repository_file_is_not_approval_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            subprocess.run(["git", "init", "-q", str(repo)], check=True)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo)
            source = repo / "src" / "Untracked.cs"
            source.parent.mkdir()
            source.write_text("internal const int Value = 1;\n", encoding="utf-8")
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | source exists | `src/Untracked.cs` |\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("unresolved evidence path", proc.stdout)

    def test_checkbox_continuation_evidence_is_folded_into_row(self) -> None:
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
                "- [x] Timeout constant is declared\n"
                "  - Evidence: `src/Constants.cs`\n",
                encoding="utf-8",
            )

            proc = self.run_auditor(repo, spec)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertNotIn("prose only", proc.stdout)

    def test_noncheckbox_partial_status_blocks_but_positive_status_passes(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo)
            source = repo / "src" / "Constants.cs"
            source.parent.mkdir()
            source.write_text("internal const int TimeoutSeconds = 60;\n", encoding="utf-8")
            checklist = spec / "section-9-approval-checklist.md"
            prefix = (
                "# Spec #9 — Approval Checklist\n"
                "| Row | Claim | Status | Evidence |\n|---|---|---|---|\n"
            )
            checklist.write_text(
                prefix
                + "| 9.1 | timeout constant exists | ⚠ PARTIAL | `src/Constants.cs` |\n",
                encoding="utf-8",
            )

            partial = self.run_auditor(repo, spec)
            self.assertEqual(partial.returncode, 1, partial.stdout)
            self.assertIn("status is not recognizably checked", partial.stdout)

            checklist.write_text(
                prefix
                + "| 9.1 | timeout constant exists | ✅ PASS | `src/Constants.cs` |\n",
                encoding="utf-8",
            )
            passed = self.run_auditor(repo, spec)
            self.assertEqual(passed.returncode, 0, passed.stdout)


if __name__ == "__main__":
    unittest.main()
