from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
AUDITOR = ROOT / "tools" / "checklist-auditor.py"


class ChecklistStructuralEvidenceTests(unittest.TestCase):
    def run_case(
        self,
        claim: str,
        evidence_sentence: str,
        evidence: str = "`section-1.md` §1.1",
    ) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec = specs / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-1.md").write_text(
                "# Spec #9 — Section 1\n## 1.1 Setting\n" + evidence_sentence + "\n",
                encoding="utf-8",
            )
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                f"| 9.1 | {claim} | {evidence} |\n",
                encoding="utf-8",
            )
            return subprocess.run(
                [
                    "python3", str(AUDITOR),
                    "--root", str(specs),
                    "--repo-root", str(repo),
                    "--changed-scope",
                    "--enforce-dir", str(spec),
                ],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=15,
            )

    def test_automation_does_not_guess_natural_language_entailment(self) -> None:
        proc = self.run_case(
            "Feature is enabled for production",
            "The feature is not enabled for production.",
        )
        self.assertEqual(proc.returncode, 0, proc.stdout)
        self.assertNotIn("supporting the claim", proc.stdout)

    def test_numeric_disagreement_is_manual_stage0_judgment(self) -> None:
        proc = self.run_case("Timeout is 60 seconds", "The timeout is 600 seconds")
        self.assertEqual(proc.returncode, 0, proc.stdout)

    def test_missing_cited_section_fails_structurally(self) -> None:
        proc = self.run_case(
            "Timeout is 60 seconds",
            "The timeout is 60 seconds.",
            "`section-1.md` §1.999",
        )
        self.assertEqual(proc.returncode, 1, proc.stdout)
        self.assertIn("unresolved evidence section", proc.stdout)

    def test_missing_cited_file_fails_structurally(self) -> None:
        proc = self.run_case(
            "Timeout is 60 seconds",
            "The timeout is 60 seconds.",
            "`missing.md`",
        )
        self.assertEqual(proc.returncode, 1, proc.stdout)
        self.assertIn("unresolved evidence path", proc.stdout)


if __name__ == "__main__":
    unittest.main()
