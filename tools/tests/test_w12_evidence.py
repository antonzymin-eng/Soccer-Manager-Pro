from __future__ import annotations

import importlib.util
import subprocess
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "dotnet-ci" / "check_w12_evidence.py"


def _load_checker():
    spec = importlib.util.spec_from_file_location("check_w12_evidence", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class W12EvidenceTests(unittest.TestCase):
    def test_committed_pre_post_record_reconciles_to_raw_artifacts(self) -> None:
        completed = subprocess.run(
            [sys.executable, str(SCRIPT), "--repo", str(ROOT)],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertIn("W12 evidence reconciliation: PASS", completed.stdout)

    def test_latest_pass_bound_rejects_impossible_transcription(self) -> None:
        checker = _load_checker()
        records = [
            {
                "seed": "0xBAD",
                "team": 0,
                "samples": 100,
                "latestPass": 81,
                "exits": {"InPossession": 20, "Cooldown": 0, "StaleTick": 0, "InvariantRejected": 80},
            }
        ]
        errors = checker.validate_accounting(records)
        self.assertTrue(any("latestPass=81 exceeds eligible upper bound 80" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
