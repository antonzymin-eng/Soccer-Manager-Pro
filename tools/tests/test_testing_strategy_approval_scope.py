from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
DETECTOR = ROOT / "tools" / "testing-strategy-approval-scope.py"


class TestingStrategyApprovalScopeTests(unittest.TestCase):
    def git(self, repo: Path, *args: str) -> str:
        return subprocess.check_output(["git", *args], cwd=repo, text=True).strip()

    def init_repo(self, repo: Path) -> Path:
        subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
        subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
        subprocess.run(["git", "config", "user.name", "Test"], cwd=repo, check=True)
        spec = repo / "docs" / "specs" / "spec-nine"
        spec.mkdir(parents=True)
        (spec / "section-5.md").write_text(
            "# Spec #9 — Section 5\n> **Status:** `APPROVED`\n",
            encoding="utf-8",
        )
        return spec

    def write_index(self, repo: Path, status: str, approved: str = "—") -> None:
        index = repo / "docs" / "specs" / "SPEC_INDEX.md"
        index.write_text(
            "# SPEC_INDEX.md — Canonical Specification Registry\n\n"
            "Approval status here overrides individual spec files.\n\n"
            "| # | Specification | Folder | Priority | Status | Approved |\n"
            "|---|---|---|---|---|---|\n"
            f"| 9 | Spec Nine | `spec-nine/` | 1 | {status} | {approved} |\n",
            encoding="utf-8",
        )

    def run_detector(self, repo: Path, base: str, head: str = "HEAD") -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3",
                str(DETECTOR),
                "--repo-root",
                str(repo),
                "--base",
                base,
                "--head",
                head,
            ],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=15,
        )

    def test_nonapproved_to_approved_registry_transition_is_emitted(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            self.init_repo(repo)
            self.write_index(repo, "IN REVIEW")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)
            base = self.git(repo, "rev-parse", "HEAD")

            self.write_index(repo, "APPROVED", "September 5, 2026")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "approve"], cwd=repo, check=True)

            proc = self.run_detector(repo, base)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertEqual(proc.stdout.strip(), "docs/specs/spec-nine")

    def test_edit_to_already_approved_registry_is_not_an_approval_transition(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = self.init_repo(repo)
            self.write_index(repo, "APPROVED", "May 15, 2026")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "approved base"], cwd=repo, check=True)
            base = self.git(repo, "rev-parse", "HEAD")

            (spec / "section-5.md").write_text(
                "# Spec #9 — Section 5\n> **Status:** `APPROVED`\nordinary edit\n",
                encoding="utf-8",
            )
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "ordinary edit"], cwd=repo, check=True)

            proc = self.run_detector(repo, base)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertEqual(proc.stdout.strip(), "")

    def test_approved_metadata_change_is_reapproval_transition(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            self.init_repo(repo)
            self.write_index(repo, "APPROVED", "May 15, 2026")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "approved base"], cwd=repo, check=True)
            base = self.git(repo, "rev-parse", "HEAD")

            # Amendment reapproval can keep canonical status APPROVED while its
            # sign-off metadata advances. That must still trigger both auditors.
            self.write_index(repo, "APPROVED", "September 5, 2026")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "reapprove amendment"], cwd=repo, check=True)

            proc = self.run_detector(repo, base)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertEqual(proc.stdout.strip(), "docs/specs/spec-nine")

    def test_local_approved_text_does_not_override_in_review_registry(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = self.init_repo(repo)
            self.write_index(repo, "IN REVIEW")
            (spec / "section-5.md").write_text(
                "# Spec #9 — Section 5\n> **Status:** `IN REVIEW`\n",
                encoding="utf-8",
            )
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)
            base = self.git(repo, "rev-parse", "HEAD")

            # Presentation text flips, canonical registry does not.
            (spec / "section-5.md").write_text(
                "# Spec #9 — Section 5\n> **Status:** `APPROVED`\n",
                encoding="utf-8",
            )
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "local-only flip"], cwd=repo, check=True)

            proc = self.run_detector(repo, base)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertEqual(proc.stdout.strip(), "")

    def test_missing_base_commit_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            self.init_repo(repo)
            self.write_index(repo, "APPROVED")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)

            proc = self.run_detector(repo, "0" * 40)
            self.assertNotEqual(proc.returncode, 0, proc.stdout)


if __name__ == "__main__":
    unittest.main()
