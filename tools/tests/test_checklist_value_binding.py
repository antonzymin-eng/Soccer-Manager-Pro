from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
AUDITOR = ROOT / "tools" / "checklist-auditor.py"


class ChecklistValueBindingTests(unittest.TestCase):
    def run_case(self, evidence_sentence: str) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            specs = repo / "docs" / "specs"
            spec = specs / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-1.md").write_text(
                "# Spec #9 — Section 1\n## 1.1 Timeout\n" + evidence_sentence + "\n",
                encoding="utf-8",
            )
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | Timeout is 60 seconds | `section-1.md` §1.1 |\n",
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

    def test_60_does_not_match_600_even_without_trailing_punctuation(self) -> None:
        proc = self.run_case("The timeout is 600 seconds")
        self.assertEqual(proc.returncode, 1, proc.stdout)
        self.assertIn("does not contain concrete text or values supporting the claim", proc.stdout)

    def test_exact_numeric_value_still_resolves(self) -> None:
        proc = self.run_case("The timeout is 60 seconds.")
        self.assertEqual(proc.returncode, 0, proc.stdout)


if __name__ == "__main__":
    unittest.main()
