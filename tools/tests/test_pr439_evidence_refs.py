from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHECKER = ROOT / "tools" / "dotnet-ci" / "check_pr439_evidence_refs.py"


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


if __name__ == "__main__":
    unittest.main()
