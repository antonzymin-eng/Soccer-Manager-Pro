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

    def test_nonapproved_to_approved_transition_is_emitted(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.name", "Test"], cwd=repo, check=True)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            section = spec / "section-5.md"
            section.write_text(
                "# Spec #9 — Section 5\n**Status:** IN REVIEW\n",
                encoding="utf-8",
            )
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)
            base = self.git(repo, "rev-parse", "HEAD")

            section.write_text(
                "# Spec #9 — Section 5\n**Status:** APPROVED\n",
                encoding="utf-8",
            )
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "approve"], cwd=repo, check=True)

            proc = self.run_detector(repo, base)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertEqual(proc.stdout.strip(), "docs/specs/spec-nine")

    def test_edit_to_already_approved_spec_is_not_an_approval_transition(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.name", "Test"], cwd=repo, check=True)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            section = spec / "section-5.md"
            section.write_text(
                "# Spec #9 — Section 5\n**Status:** APPROVED\nold\n",
                encoding="utf-8",
            )
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "approved base"], cwd=repo, check=True)
            base = self.git(repo, "rev-parse", "HEAD")

            section.write_text(
                "# Spec #9 — Section 5\n**Status:** APPROVED\nnew\n",
                encoding="utf-8",
            )
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "ordinary edit"], cwd=repo, check=True)

            proc = self.run_detector(repo, base)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertEqual(proc.stdout.strip(), "")

    def test_missing_base_commit_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.name", "Test"], cwd=repo, check=True)
            (repo / "README.md").write_text("x\n", encoding="utf-8")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)

            proc = self.run_detector(repo, "0" * 40)
            self.assertNotEqual(proc.returncode, 0, proc.stdout)


if __name__ == "__main__":
    unittest.main()
