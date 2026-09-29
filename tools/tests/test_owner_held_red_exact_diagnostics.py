from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
VERIFIER = ROOT / "tools" / "dotnet-ci" / "verify-owner-held-red.py"


class OwnerHeldRedExactDiagnosticsTests(unittest.TestCase):
    def verify(
        self,
        message: str,
        ordinary_name: str = "TacticalDirector.MatchEngine.OtherTests.ordinary_test",
        dedicated_count: int = 1,
    ) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            ledger = tmp / "ledger.txt"
            ledger.write_text(
                "sim_match_engine_close_chance|meanCosine=-0.165|goalwardShare=0.407\n",
                encoding="utf-8",
            )
            ordinary = tmp / "ordinary"
            ordinary.mkdir()
            (ordinary / "ordinary.trx").write_text(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>\n"
                f"<UnitTestResult testName=\"{ordinary_name}\" outcome=\"Passed\" />\n"
                "</Results></TestRun>\n",
                encoding="utf-8",
            )
            results = tmp / "results"
            results.mkdir()
            dedicated_rows = "".join(
                (
                    "<UnitTestResult testName=\"TacticalDirector.MatchEngine.MatchEngineCloseChanceTests.sim_match_engine_close_chance\" outcome=\"Failed\">\n"
                    f"<Output><ErrorInfo><Message>{message}</Message></ErrorInfo></Output>\n"
                    "</UnitTestResult>\n"
                )
                for _ in range(dedicated_count)
            )
            (results / "result.trx").write_text(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>\n"
                f"{dedicated_rows}"
                "</Results></TestRun>\n",
                encoding="utf-8",
            )
            return subprocess.run(
                [
                    "python3", str(VERIFIER),
                    "--ledger", str(ledger),
                    "--ordinary-results", str(ordinary),
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
        self.assertIn(
            "TRX RESULT RECORDS (all outcomes; not pass cardinality):",
            proc.stdout,
        )
        self.assertIn(
            "OWNER-HELD ISOLATION: sim_match_engine_close_chance ordinary=0 dedicated=1",
            proc.stdout,
        )

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

    def test_owner_held_name_in_ordinary_capture_fails_isolation(self) -> None:
        proc = self.verify(
            "meanCosine=-0.165 goalwardShare=0.407",
            ordinary_name=(
                "TacticalDirector.MatchEngine.MatchEngineCloseChanceTests."
                "sim_match_engine_close_chance"
            ),
        )
        self.assertEqual(proc.returncode, 1, proc.stdout)
        self.assertIn("leaked into ordinary sweep", proc.stdout)

    def test_duplicate_dedicated_result_fails_isolation(self) -> None:
        proc = self.verify(
            "meanCosine=-0.165 goalwardShare=0.407",
            dedicated_count=2,
        )
        self.assertEqual(proc.returncode, 1, proc.stdout)
        self.assertIn("dedicated occurrences=2", proc.stdout)

    def test_empty_ledger_requires_no_dedicated_verification(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            ledger = tmp / "ledger.txt"
            ledger.write_text("# no owner-held rows\n", encoding="utf-8")
            proc = subprocess.run(
                [
                    "python3", str(VERIFIER),
                    "--ledger", str(ledger),
                    "--ordinary-results", str(tmp / "ordinary"),
                    "--results", str(tmp / "results"),
                    "--dotnet-exit", "0",
                ],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=15,
            )
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertIn("no dedicated verification is required", proc.stdout)

    def test_missing_ordinary_capture_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            ledger = tmp / "ledger.txt"
            ledger.write_text(
                "sim_match_engine_close_chance|meanCosine=-0.165|goalwardShare=0.407\n",
                encoding="utf-8",
            )
            ordinary = tmp / "ordinary"
            ordinary.mkdir()
            results = tmp / "results"
            results.mkdir()
            (results / "result.trx").write_text(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>\n"
                "<UnitTestResult testName=\"TacticalDirector.MatchEngine.MatchEngineCloseChanceTests.sim_match_engine_close_chance\" outcome=\"Failed\">\n"
                "<Output><ErrorInfo><Message>meanCosine=-0.165 goalwardShare=0.407</Message></ErrorInfo></Output>\n"
                "</UnitTestResult></Results></TestRun>\n",
                encoding="utf-8",
            )
            proc = subprocess.run(
                [
                    "python3", str(VERIFIER),
                    "--ledger", str(ledger),
                    "--ordinary-results", str(ordinary),
                    "--results", str(results),
                    "--dotnet-exit", "1",
                ],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=15,
            )
            self.assertEqual(proc.returncode, 2, proc.stdout)
            self.assertIn("ordinary sweep capture has no TRX files", proc.stdout)

    def test_malformed_ordinary_trx_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            ledger = tmp / "ledger.txt"
            ledger.write_text(
                "sim_match_engine_close_chance|meanCosine=-0.165|goalwardShare=0.407\n",
                encoding="utf-8",
            )
            ordinary = tmp / "ordinary"
            ordinary.mkdir()
            (ordinary / "ordinary.trx").write_text("<not-closed>", encoding="utf-8")
            results = tmp / "results"
            results.mkdir()
            (results / "result.trx").write_text(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>\n"
                "<UnitTestResult testName=\"TacticalDirector.MatchEngine.MatchEngineCloseChanceTests.sim_match_engine_close_chance\" outcome=\"Failed\">\n"
                "<Output><ErrorInfo><Message>meanCosine=-0.165 goalwardShare=0.407</Message></ErrorInfo></Output>\n"
                "</UnitTestResult></Results></TestRun>\n",
                encoding="utf-8",
            )
            proc = subprocess.run(
                [
                    "python3", str(VERIFIER),
                    "--ledger", str(ledger),
                    "--ordinary-results", str(ordinary),
                    "--results", str(results),
                    "--dotnet-exit", "1",
                ],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                timeout=15,
            )
            self.assertEqual(proc.returncode, 2, proc.stdout)
            self.assertIn("malformed TRX", proc.stdout)


if __name__ == "__main__":
    unittest.main()
