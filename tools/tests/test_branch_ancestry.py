from __future__ import annotations

import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "dotnet-ci" / "check_branch_ancestry.py"


def _load_checker():
    spec = importlib.util.spec_from_file_location("check_branch_ancestry", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _git(repo: Path, *args: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=True,
        capture_output=True,
        text=True,
    )
    return completed.stdout.strip()


def _make_repo(root: Path) -> tuple[Path, str, str]:
    repo = root / "source"
    subprocess.run(["git", "init", "-b", "main", str(repo)], check=True, capture_output=True)
    _git(repo, "config", "user.name", "Test")
    _git(repo, "config", "user.email", "test@example.com")
    (repo / "file.txt").write_text("one\n", encoding="utf-8")
    _git(repo, "add", "file.txt")
    _git(repo, "commit", "-m", "one")
    first = _git(repo, "rev-parse", "HEAD")
    (repo / "file.txt").write_text("two\n", encoding="utf-8")
    _git(repo, "commit", "-am", "two")
    second = _git(repo, "rev-parse", "HEAD")
    return repo, first, second


class BranchAncestryTests(unittest.TestCase):
    def test_full_history_reports_ancestor(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            repo, first, second = _make_repo(Path(tmp))
            result, ancestor_sha, descendant_sha = checker.check_ancestry(
                repo, first, second
            )
        self.assertTrue(result)
        self.assertEqual(first, ancestor_sha)
        self.assertEqual(second, descendant_sha)

    def test_full_history_reports_non_ancestor(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            repo, first, _ = _make_repo(Path(tmp))
            _git(repo, "checkout", "-b", "side", first)
            (repo / "side.txt").write_text("side\n", encoding="utf-8")
            _git(repo, "add", "side.txt")
            _git(repo, "commit", "-m", "side")
            side = _git(repo, "rev-parse", "HEAD")
            main = _git(repo, "rev-parse", "main")
            result, _, _ = checker.check_ancestry(repo, side, main)
        self.assertFalse(result)

    def test_cli_uses_distinct_exit_for_guard_error(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source, _, _ = _make_repo(root)
            shallow = root / "shallow"
            subprocess.run(
                [
                    "git",
                    "clone",
                    "--depth",
                    "1",
                    "--branch",
                    "main",
                    f"file://{source}",
                    str(shallow),
                ],
                check=True,
                capture_output=True,
            )
            completed = subprocess.run(
                [
                    "python3",
                    str(SCRIPT),
                    "--repo",
                    str(shallow),
                    "--ancestor",
                    "HEAD",
                    "--descendant",
                    "HEAD",
                ],
                check=False,
                capture_output=True,
                text=True,
            )
        self.assertEqual(3, completed.returncode, completed.stdout + completed.stderr)
        self.assertIn("history is shallow", completed.stdout)

    def test_shallow_clone_fails_closed_before_ancestry_claim(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source, _, _ = _make_repo(root)
            shallow = root / "shallow"
            subprocess.run(
                [
                    "git",
                    "clone",
                    "--depth",
                    "1",
                    "--branch",
                    "main",
                    f"file://{source}",
                    str(shallow),
                ],
                check=True,
                capture_output=True,
            )
            self.assertTrue(checker.is_shallow(shallow))
            with self.assertRaisesRegex(
                checker.GitCheckError,
                "history is shallow.*not authoritative",
            ):
                checker.check_ancestry(shallow, "HEAD", "HEAD")


if __name__ == "__main__":
    unittest.main()
