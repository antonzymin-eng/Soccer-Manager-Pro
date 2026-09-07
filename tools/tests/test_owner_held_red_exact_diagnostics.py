from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
VERIFIER = ROOT / "tools" / "dotnet-ci" / "verify-owner-held-red.py"


class OwnerHeldRedExactDiagnosticsTests(unittest.TestCase):
    def verify(self, message: str) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            ledger = tmp / "ledger.txt"
            ledger.write_text(
                "sim_match_engine_close_chance|meanCosine=-0.165|goalwardShare=0.407\n",
                encoding="utf-8",
            )
            results = tmp / "results"
            results.mkdir()
            (results / "result.trx").write_text(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>\n"
                "<UnitTestResult testName=\"TacticalDirector.MatchEngine.MatchEngineCloseChanceTests.sim_match_engine_close_chance\" outcome=\"Failed\">\n"
                f"<Output><ErrorInfo><Message>{message}</Message></ErrorInfo></Output>\n"
                "</UnitTestResult></Results></TestRun>\n",
                encoding="utf-8",
            )
            return subprocess.run(
                [
                    "python3", str(VERIFIER),
                    "--ledger", str(ledger),
                    "--results", str(results),
                    "--dotnet-exit", "1",
                ],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=15,
            )

    def test_exact_recorded_values_pass(self) -> None:
        proc = self.verify("meanCosine=-0.165 goalwardShare=0.407")
        self.assertEqual(proc.returncode, 0, proc.stdout)

    def test_recorded_values_as_prefixes_of_drifted_values_fail(self) -> None:
        proc = self.verify("meanCosine=-0.1659 goalwardShare=0.4078")
        self.assertEqual(proc.returncode, 1, proc.stdout)
        self.assertIn("changed diagnostics", proc.stdout)

    def test_baseline_values_elsewhere_do_not_mask_drifted_fields(self) -> None:
        proc = self.verify(
            "meanCosine=-0.100 (baseline -0.165) "
            "goalwardShare=0.500 (baseline 0.407)"
        )
        self.assertEqual(proc.returncode, 1, proc.stdout)
        self.assertIn("changed diagnostics", proc.stdout)

    def test_duplicate_field_assignment_is_ambiguous_and_fails(self) -> None:
        proc = self.verify(
            "meanCosine=-0.165 meanCosine=-0.100 goalwardShare=0.407"
        )
        self.assertEqual(proc.returncode, 1, proc.stdout)
        self.assertIn("ambiguous field", proc.stdout)

    def test_unicode_minus_normalizes_but_value_must_remain_exact(self) -> None:
        proc = self.verify("meanCosine=−0.165 goalwardShare=0.407")
        self.assertEqual(proc.returncode, 0, proc.stdout)


if __name__ == "__main__":
    unittest.main()
