from __future__ import annotations

import os
from pathlib import Path
import subprocess
import unittest


ROOT = Path(__file__).resolve().parents[2]
GATE = ROOT / "tools" / "dotnet-ci" / "run-gate.sh"
SETTINGS = ROOT / "tools" / "dotnet-ci" / "precommit.runsettings"


class RunGateArgumentConflictTests(unittest.TestCase):
    def test_settings_and_coverage_fail_loud_instead_of_last_flag_wins(self) -> None:
        env = os.environ.copy()
        env["TD_GATE_DRY_RUN"] = "1"
        proc = subprocess.run(
            [
                "bash",
                str(GATE),
                "--settings",
                str(SETTINGS),
                "--coverage",
            ],
            cwd=ROOT,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=15,
        )
        self.assertEqual(proc.returncode, 2, proc.stdout)
        self.assertIn("--coverage and --settings cannot be combined", proc.stdout)


if __name__ == "__main__":
    unittest.main()
