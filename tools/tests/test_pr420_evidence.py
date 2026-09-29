from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
TOOL = ROOT / "tools" / "dotnet-ci" / "pr420-evidence.py"
EVIDENCE = ROOT / "docs" / "tracking" / "evidence" / "pr420-owner-held-isolation"


class Pr420EvidenceTests(unittest.TestCase):
    def run_tool(self, *args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            ["python3", str(TOOL), *args],
            cwd=ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=30,
        )

    def test_committed_payload_verifies(self) -> None:
        proc = self.run_tool("verify", "--output-dir", str(EVIDENCE))
        self.assertEqual(proc.returncode, 0, proc.stdout)
        self.assertIn("source_artifact_id=10578621101", proc.stdout)
        self.assertIn("members=70", proc.stdout)

    def test_bootstrap_rejects_unpinned_source(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            fake = tmp / "wrong.zip"
            fake.write_bytes(b"not the pinned Actions artifact")
            proc = self.run_tool(
                "bootstrap",
                "--source",
                str(fake),
                "--output-dir",
                str(tmp / "out"),
            )
        self.assertEqual(proc.returncode, 2, proc.stdout)
        self.assertIn("does not match pinned PR #420 evidence", proc.stdout)


if __name__ == "__main__":
    unittest.main()
