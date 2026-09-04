from __future__ import annotations

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
RUNNER = ROOT / "tools" / "run-tests-local.sh"


class TestingPipelineScopeTests(unittest.TestCase):
    def run_runner(self, repo: Path, *, base_ref: str) -> subprocess.CompletedProcess[str]:
        env = os.environ.copy()
        env.update(
            {
                "GITHUB_BASE_REF": base_ref,
                "TD_PIPELINE_DRY_RUN": "1",
            }
        )
        return subprocess.run(
            ["bash", str(repo / "tools" / "run-tests-local.sh"), "--pr"],
            cwd=repo,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=10,
        )

    def init_repo(self, repo: Path) -> None:
        repo.mkdir()
        subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
        subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
        subprocess.run(["git", "config", "user.name", "Test"], cwd=repo, check=True)
        (repo / "tools").mkdir()
        shutil.copy2(RUNNER, repo / "tools" / "run-tests-local.sh")
        (repo / "docs" / "specs" / "spec-nine").mkdir(parents=True)
        (repo / "docs" / "specs" / "spec-nine" / "section-5.md").write_text(
            "# Spec #9 — Section 5\n", encoding="utf-8"
        )
        subprocess.run(["git", "add", "."], cwd=repo, check=True)
        subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)

    def test_pr_scope_fails_closed_when_named_base_cannot_be_resolved(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td) / "repo"
            self.init_repo(repo)
            proc = self.run_runner(repo, base_ref="main")
            self.assertNotEqual(proc.returncode, 0, proc.stdout)
            self.assertIn("cannot resolve PR base origin/main", proc.stdout)
            self.assertIn("unable to determine changed-spec audit scope", proc.stdout)

    def test_pr_scope_uses_base_to_head_tree_diff(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td) / "repo"
            self.init_repo(repo)
            base_sha = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=repo, text=True).strip()
            subprocess.run(
                ["git", "update-ref", "refs/remotes/origin/main", base_sha], cwd=repo, check=True
            )
            target = repo / "docs" / "specs" / "spec-nine" / "section-5.md"
            target.write_text("# Spec #9 — Section 5\nchanged\n", encoding="utf-8")
            subprocess.run(["git", "add", str(target.relative_to(repo))], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "change spec nine"], cwd=repo, check=True)

            proc = self.run_runner(repo, base_ref="main")
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertIn("Spec audit review scope: docs/specs/spec-nine", proc.stdout)
            self.assertIn("--enforce-dir docs/specs/spec-nine", proc.stdout)


if __name__ == "__main__":
    unittest.main()
