from __future__ import annotations

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import time
import unittest


ROOT = Path(__file__).resolve().parents[2]
HOOK = ROOT / ".githooks" / "pre-commit"


class PrecommitIncrementalSnapshotTests(unittest.TestCase):
    def run_hook(self, repo: Path, capture: Path) -> subprocess.CompletedProcess[str]:
        env = os.environ.copy()
        env["TD_TEST_CAPTURE"] = str(capture)
        return subprocess.run(
            ["bash", str(repo / ".githooks" / "pre-commit")],
            cwd=repo,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=20,
        )

    def init_minimal_repo(self, repo: Path) -> None:
        subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
        subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
        subprocess.run(["git", "config", "user.name", "Test"], cwd=repo, check=True)
        (repo / ".githooks").mkdir()
        (repo / "tools").mkdir()
        shutil.copy2(HOOK, repo / ".githooks" / "pre-commit")
        (repo / "tools" / "run-tests-local.sh").write_text(
            "#!/usr/bin/env bash\nset -euo pipefail\ncat payload.txt > \"$TD_TEST_CAPTURE\"\n",
            encoding="utf-8",
        )
        (repo / "payload.txt").write_text("base\n", encoding="utf-8")

    def test_unchanged_tracked_mtimes_and_untracked_cache_survive_refresh(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td) / "repo"
            repo.mkdir()
            self.init_minimal_repo(repo)
            (repo / "stable.txt").write_text("unchanged\n", encoding="utf-8")
            (repo / "remove-me.txt").write_text("delete later\n", encoding="utf-8")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)

            capture = Path(td) / "captured.txt"
            (repo / "payload.txt").write_text("staged-one\n", encoding="utf-8")
            subprocess.run(["git", "add", "payload.txt"], cwd=repo, check=True)
            first = self.run_hook(repo, capture)
            self.assertEqual(first.returncode, 0, first.stdout)
            self.assertEqual(capture.read_text(encoding="utf-8"), "staged-one\n")

            git_dir = Path(
                subprocess.check_output(
                    ["git", "rev-parse", "--absolute-git-dir"], cwd=repo, text=True
                ).strip()
            )
            snapshot = git_dir / "testing-strategy" / "precommit-snapshot"
            stable_snapshot = snapshot / "stable.txt"
            stable_mtime = stable_snapshot.stat().st_mtime_ns
            marker = snapshot / "obj" / "cache.marker"
            marker.parent.mkdir(parents=True, exist_ok=True)
            marker.write_text("warm\n", encoding="utf-8")

            # Ensure a wholesale checkout-index rewrite would be observable even
            # on a filesystem with coarse timestamp resolution.
            time.sleep(1.1)
            (repo / "payload.txt").write_text("staged-two\n", encoding="utf-8")
            subprocess.run(["git", "add", "payload.txt"], cwd=repo, check=True)
            subprocess.run(["git", "rm", "-q", "remove-me.txt"], cwd=repo, check=True)

            second = self.run_hook(repo, capture)
            self.assertEqual(second.returncode, 0, second.stdout)
            self.assertEqual(capture.read_text(encoding="utf-8"), "staged-two\n")
            self.assertEqual(
                stable_snapshot.stat().st_mtime_ns,
                stable_mtime,
                "unchanged tracked files must keep mtimes so MSBuild can reuse incremental outputs",
            )
            self.assertFalse((snapshot / "remove-me.txt").exists())
            self.assertTrue(marker.exists(), "untracked bin/obj-style cache must survive refresh")

    def test_snapshot_checkout_disables_lfs_smudge_even_when_required(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td) / "repo"
            repo.mkdir()
            self.init_minimal_repo(repo)
            (repo / ".gitattributes").write_text("*.bin filter=lfs\n", encoding="utf-8")
            # Simulate a clone whose configured LFS smudge cannot provide the
            # object. Without the hook's local override checkout-index fails.
            subprocess.run(["git", "config", "filter.lfs.clean", "cat"], cwd=repo, check=True)
            subprocess.run(["git", "config", "filter.lfs.smudge", "false"], cwd=repo, check=True)
            subprocess.run(["git", "config", "filter.lfs.required", "true"], cwd=repo, check=True)
            (repo / "asset.bin").write_text("lfs-pointer-bytes\n", encoding="utf-8")
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=repo, check=True)

            capture = Path(td) / "captured.txt"
            proc = self.run_hook(repo, capture)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            git_dir = Path(
                subprocess.check_output(
                    ["git", "rev-parse", "--absolute-git-dir"], cwd=repo, text=True
                ).strip()
            )
            snapshot_asset = git_dir / "testing-strategy" / "precommit-snapshot" / "asset.bin"
            self.assertEqual(snapshot_asset.read_text(encoding="utf-8"), "lfs-pointer-bytes\n")

    def test_prune_is_linear_manifest_diff_not_nested_grep(self) -> None:
        text = HOOK.read_text(encoding="utf-8")
        self.assertIn('comm -23 "$TRACKED_MANIFEST" "$CURRENT_MANIFEST"', text)
        self.assertNotIn("grep -Fqx", text)
        self.assertIn("precommit-index-tree.txt", text)
        self.assertIn('diff --name-only -z --no-renames', text)
        self.assertIn("filter.lfs.smudge=", text)
        self.assertIn("filter.lfs.required=false", text)


if __name__ == "__main__":
    unittest.main()
