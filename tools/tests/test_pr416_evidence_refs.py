from __future__ import annotations

import importlib.util
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "dotnet-ci" / "check_pr416_evidence_refs.py"

_spec = importlib.util.spec_from_file_location("check_pr416_evidence_refs", SCRIPT)
assert _spec and _spec.loader
checker = importlib.util.module_from_spec(_spec)
sys.modules[_spec.name] = checker
_spec.loader.exec_module(checker)


class Pr416EvidenceRefCheckerTests(unittest.TestCase):
    def test_parse_tsv_requires_exact_schema(self) -> None:
        rows = checker.parse_tsv(
            "a\tb\n1\t2\n",
            ["a", "b"],
            "fixture.tsv",
        )
        self.assertEqual(rows, [{"a": "1", "b": "2"}])
        with self.assertRaises(checker.CheckError):
            checker.parse_tsv("b\ta\n2\t1\n", ["a", "b"], "fixture.tsv")

    def test_zero_retain_disposition_is_counted_explicitly(self) -> None:
        rows = (
            [{"disposition": "deletable"}] * 21
            + [{"disposition": "policy-retained"}] * 3
        )
        counts, unknown = checker._normalize_disposition_counts(rows)
        self.assertEqual(checker.EXPECTED_DISPOSITION_COUNTS, counts)
        self.assertEqual([], unknown)

    def test_archive_path_shape_fails_closed(self) -> None:
        checker._validate_archive_path(
            "docs/tracking/evidence/pr416-ref-archive/current/x/file.cs.txt"
        )
        for bad in (
            "/docs/tracking/evidence/pr416-ref-archive/current/x/file.cs.txt",
            "docs/tracking/evidence/pr416-ref-archive/../escape.txt",
            "docs/tracking/evidence/pr416-ref-archive/current/x/file.cs",
        ):
            with self.subTest(path=bad), self.assertRaises(checker.CheckError):
                checker._validate_archive_path(bad)

    def test_history_enumeration_retains_intermediate_blob_states(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            subprocess.run(["git", "init", "-b", "main"], cwd=repo, check=True, capture_output=True)
            subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.name", "Test"], cwd=repo, check=True)

            tracked = repo / "tracked.txt"
            tracked.write_text("base\n", encoding="utf-8")
            subprocess.run(["git", "add", "tracked.txt"], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-m", "base"], cwd=repo, check=True, capture_output=True)

            subprocess.run(["git", "switch", "-c", "evidence"], cwd=repo, check=True, capture_output=True)
            tracked.write_text("intermediate\n", encoding="utf-8")
            subprocess.run(["git", "commit", "-am", "intermediate"], cwd=repo, check=True, capture_output=True)
            intermediate_blob = subprocess.run(
                ["git", "rev-parse", "HEAD:tracked.txt"],
                cwd=repo,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip()

            tracked.write_text("tip\n", encoding="utf-8")
            subprocess.run(["git", "commit", "-am", "tip"], cwd=repo, check=True, capture_output=True)
            tip_blob = subprocess.run(
                ["git", "rev-parse", "HEAD:tracked.txt"],
                cwd=repo,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip()

            merge_base, commits, instances = checker.enumerate_instances(
                repo, "evidence", "evidence", "main"
            )
            self.assertTrue(merge_base)
            self.assertEqual(len(commits), 2)
            self.assertEqual(
                [item.blob for item in instances if item.path == "tracked.txt"],
                [intermediate_blob, tip_blob],
            )


if __name__ == "__main__":
    unittest.main()
