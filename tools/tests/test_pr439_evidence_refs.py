from __future__ import annotations

import csv
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Callable

ROOT = Path(__file__).resolve().parents[2]
CHECKER = ROOT / "tools" / "dotnet-ci" / "check_pr439_evidence_refs.py"
ARCHIVE = ROOT / "docs" / "tracking" / "evidence" / "pr439-ref-archive"
sys.path.insert(0, str(CHECKER.parent))
import check_pr439_evidence_refs as verifier  # noqa: E402


class Pr439EvidenceRefArchiveTests(unittest.TestCase):
    def _checker(self, root: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                sys.executable,
                str(root / "tools" / "dotnet-ci" / "check_pr439_evidence_refs.py"),
                "--repo",
                str(root),
                "--mode",
                "archive",
            ],
            check=False,
            capture_output=True,
            text=True,
        )

    def _assert_tampered_worktree_fails(self, mutate: Callable[[Path], None]) -> str:
        with tempfile.TemporaryDirectory() as tmp:
            worktree = Path(tmp) / "repo"
            added = subprocess.run(
                ["git", "-C", str(ROOT), "worktree", "add", "--detach", str(worktree), "HEAD"],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, added.returncode, added.stdout + added.stderr)
            try:
                mutate(worktree)
                completed = self._checker(worktree)
                self.assertNotEqual(0, completed.returncode, completed.stdout + completed.stderr)
                return completed.stdout + completed.stderr
            finally:
                subprocess.run(
                    ["git", "-C", str(ROOT), "worktree", "remove", "--force", str(worktree)],
                    check=False,
                    capture_output=True,
                    text=True,
                )

    def test_current_archive_integrity_passes(self) -> None:
        completed = self._checker(ROOT)
        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertIn("PR439 evidence-ref reconciliation: PASS", completed.stdout)

    def test_frozen_discussion_snapshot_covers_exact_run_ledger(self) -> None:
        run_ids = {row["run_id"] for row in verifier.read_tsv(ARCHIVE / "run-heads.tsv")}
        self.assertEqual([], verifier.validate_frozen_pr_citations(ROOT, run_ids))

    def test_frozen_discussion_snapshot_rejects_citation_tamper(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            target = root / verifier.ARCHIVE_ROOT
            target.mkdir(parents=True)
            for rel in (verifier.DISCUSSION_SNAPSHOT, verifier.DISCUSSION_META):
                shutil.copy2(ROOT / rel, root / rel)
            rows = verifier.read_tsv(root / verifier.DISCUSSION_SNAPSHOT)
            rows[0]["run_ids"] = rows[0]["run_ids"] + ",35814941362"
            fields = ["source_kind", "source_id", "source_url", "body_sha256", "run_ids"]
            with (root / verifier.DISCUSSION_SNAPSHOT).open(
                "w", encoding="utf-8", newline=""
            ) as handle:
                writer = csv.DictWriter(
                    handle, fieldnames=fields, delimiter="\t", lineterminator="\n"
                )
                writer.writeheader()
                writer.writerows(rows)
            run_ids = {row["run_id"] for row in verifier.read_tsv(ARCHIVE / "run-heads.tsv")}
            errors = verifier.validate_frozen_pr_citations(root, run_ids)
            self.assertTrue(errors)

    def test_live_snapshot_comparison_reports_added_edited_and_removed_sources(self) -> None:
        frozen = verifier.read_tsv(ROOT / verifier.DISCUSSION_SNAPSHOT)
        self.assertEqual([], verifier.compare_live_discussion_snapshot(ROOT, frozen))

        edited = [dict(row) for row in frozen]
        edited[0]["body_sha256"] = "0" * 64
        self.assertTrue(verifier.compare_live_discussion_snapshot(ROOT, edited))

        added = [dict(row) for row in frozen]
        added.append(
            {
                "source_kind": "issue_comment",
                "source_id": "9999999999",
                "source_url": "https://github.com/example/example/pull/439#issuecomment-9999999999",
                "body_sha256": "1" * 64,
                "run_ids": "",
            }
        )
        self.assertTrue(verifier.compare_live_discussion_snapshot(ROOT, added))

        removed = [dict(row) for row in frozen[1:]]
        self.assertTrue(verifier.compare_live_discussion_snapshot(ROOT, removed))

    def test_post_delete_topology_rejects_recreated_ref(self) -> None:
        dispositions = [{"delete_now": "true"} for _ in range(verifier.EXPECTED_REF_COUNT)]
        errors = verifier.validate_post_delete_topology(
            {"evidence/pr439-recreated"}, dispositions
        )
        self.assertTrue(errors)

    def test_post_delete_topology_rejects_mixed_authorization(self) -> None:
        dispositions = [{"delete_now": "true"} for _ in range(verifier.EXPECTED_REF_COUNT)]
        dispositions[-1]["delete_now"] = "false"
        self.assertTrue(verifier.validate_post_delete_topology(set(), dispositions))

    def test_end_to_end_rejects_tampered_archived_job_log(self) -> None:
        def mutate(root: Path) -> None:
            rows = verifier.read_tsv(root / verifier.JOB_LOGS)
            path = root / rows[0]["archive_path"]
            data = bytearray(path.read_bytes())
            self.assertTrue(data)
            data[len(data) // 2] ^= 0x01
            path.write_bytes(data)

        output = self._assert_tampered_worktree_fails(mutate)
        self.assertIn("job-log", output)

    def test_end_to_end_rejects_superseded_id_in_cited_run_ledger(self) -> None:
        def mutate(root: Path) -> None:
            path = root / verifier.RUN_HEADS
            rows = verifier.read_tsv(path)
            fields = list(rows[0])
            rows[0]["run_id"] = "35814941362"
            with path.open("w", encoding="utf-8", newline="") as handle:
                writer = csv.DictWriter(
                    handle, fieldnames=fields, delimiter="\t", lineterminator="\n"
                )
                writer.writeheader()
                writer.writerows(rows)

        output = self._assert_tampered_worktree_fails(mutate)
        self.assertTrue(
            "historical run IDs leaked" in output
            or "frozen PR #439 citation coverage mismatch" in output
        )


if __name__ == "__main__":
    unittest.main()
