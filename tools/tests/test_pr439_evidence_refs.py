from __future__ import annotations

import csv
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHECKER = ROOT / "tools" / "dotnet-ci" / "check_pr439_evidence_refs.py"
ARCHIVE = ROOT / "docs" / "tracking" / "evidence" / "pr439-ref-archive"
sys.path.insert(0, str(CHECKER.parent))
import check_pr439_evidence_refs as verifier  # noqa: E402


class Pr439EvidenceRefArchiveTests(unittest.TestCase):
    def test_current_archive_integrity_passes(self) -> None:
        completed = subprocess.run(
            [sys.executable, str(CHECKER), "--repo", str(ROOT), "--mode", "archive"],
            check=False,
            capture_output=True,
            text=True,
        )
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
            with (root / verifier.DISCUSSION_SNAPSHOT).open("w", encoding="utf-8", newline="") as handle:
                writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
                writer.writeheader()
                writer.writerows(rows)
            run_ids = {row["run_id"] for row in verifier.read_tsv(ARCHIVE / "run-heads.tsv")}
            errors = verifier.validate_frozen_pr_citations(root, run_ids)
            self.assertTrue(errors)

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

    def test_digest_helper_rejects_flipped_byte(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "payload.bin"
            path.write_bytes(b"archive payload")
            expected = verifier.sha256(path.read_bytes())
            self.assertTrue(verifier.file_sha256_matches(path, expected))
            path.write_bytes(b"archive payloaD")
            self.assertFalse(verifier.file_sha256_matches(path, expected))


if __name__ == "__main__":
    unittest.main()
