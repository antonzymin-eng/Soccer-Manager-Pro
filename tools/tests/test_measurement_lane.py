"""Regression tests for the manually dispatched match measurement lane."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[2]
MODULE_PATH = ROOT / "tools" / "dotnet-ci" / "measurement_catalog.py"
SPEC = importlib.util.spec_from_file_location("measurement_catalog", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
measurement_catalog = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = measurement_catalog
SPEC.loader.exec_module(measurement_catalog)


def _write_trx(path: Path, outcomes: list[str]) -> None:
    namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
    root = ET.Element(f"{{{namespace}}}TestRun")
    results = ET.SubElement(root, f"{{{namespace}}}Results")
    for index, outcome in enumerate(outcomes):
        ET.SubElement(
            results,
            f"{{{namespace}}}UnitTestResult",
            {
                "testId": str(index),
                "testName": f"Diagnostic{index}",
                "outcome": outcome,
            },
        )
    ET.ElementTree(root).write(path, encoding="utf-8", xml_declaration=True)


class MeasurementLaneTests(unittest.TestCase):
    def test_catalog_is_grounded_in_tracked_sources(self) -> None:
        measurement_catalog.validate_repo(ROOT)

    def test_resolve_writes_one_canonical_tuple_without_setting_arbitrary_filter(self) -> None:
        inst = measurement_catalog.get_instrument("goal-conversion")
        with tempfile.TemporaryDirectory() as tmp:
            github_env = Path(tmp) / "github-env"
            measurement_catalog.write_github_env(inst, github_env)
            values = dict(
                line.split("=", 1)
                for line in github_env.read_text(encoding="utf-8").splitlines()
            )

        self.assertEqual(values["MEASUREMENT_INSTRUMENT"], "goal-conversion")
        self.assertEqual(values["MEASUREMENT_ENV"], "TD_CONVERSION_DIAGNOSTIC")
        self.assertEqual(
            values["MEASUREMENT_FILTER"], "FullyQualifiedName~GoalConversionDiagnostic"
        )
        self.assertTrue(values["MEASUREMENT_PROJECT"].endswith("match-engine-tests.gen.csproj"))
        self.assertEqual(
            values["MEASUREMENT_SENTINEL"], "REPORT A — what a keeper contact DOES."
        )
        self.assertNotIn("TD_CONVERSION_DIAGNOSTIC", values)

    def test_unknown_instrument_fails_loud(self) -> None:
        with self.assertRaisesRegex(measurement_catalog.MeasurementError, "unknown instrument"):
            measurement_catalog.get_instrument("typo-does-not-exist")

    def test_verify_run_rejects_missing_trx(self) -> None:
        inst = measurement_catalog.get_instrument("goal-conversion")
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            output = tmp_path / "measurement.txt"
            output.write_text(inst.sentinel, encoding="utf-8")
            with self.assertRaisesRegex(measurement_catalog.MeasurementError, "no TRX"):
                measurement_catalog.verify_run(inst, tmp_path, output)

    def test_verify_run_rejects_ignored_only_result(self) -> None:
        inst = measurement_catalog.get_instrument("goal-conversion")
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            _write_trx(tmp_path / "measurement.trx", ["NotExecuted"])
            output = tmp_path / "measurement.txt"
            output.write_text(inst.sentinel, encoding="utf-8")
            with self.assertRaisesRegex(
                measurement_catalog.MeasurementError,
                "did not execute a passing test",
            ):
                measurement_catalog.verify_run(inst, tmp_path, output)

    def test_verify_run_rejects_passing_test_without_report_sentinel(self) -> None:
        inst = measurement_catalog.get_instrument("goal-conversion")
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            _write_trx(tmp_path / "measurement.trx", ["Passed"])
            output = tmp_path / "measurement.txt"
            output.write_text("green test, but no diagnostic report\n", encoding="utf-8")
            with self.assertRaisesRegex(
                measurement_catalog.MeasurementError, "sentinel was absent"
            ):
                measurement_catalog.verify_run(inst, tmp_path, output)

    def test_verify_run_rejects_failed_result_even_if_report_exists(self) -> None:
        inst = measurement_catalog.get_instrument("goal-conversion")
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            _write_trx(tmp_path / "measurement.trx", ["Passed", "Failed"])
            output = tmp_path / "measurement.txt"
            output.write_text(inst.sentinel, encoding="utf-8")
            with self.assertRaisesRegex(
                measurement_catalog.MeasurementError,
                "non-passing outcomes",
            ):
                measurement_catalog.verify_run(inst, tmp_path, output)

    def test_verify_run_accepts_passed_test_plus_expected_report(self) -> None:
        inst = measurement_catalog.get_instrument("goal-conversion")
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            _write_trx(tmp_path / "measurement.trx", ["Passed"])
            output = tmp_path / "measurement.txt"
            output.write_text(
                "prefix\n" + inst.sentinel + "\nmeasurement body\n",
                encoding="utf-8",
            )
            measurement_catalog.verify_run(inst, tmp_path, output)

    def test_workflow_has_no_independent_diagnostic_filter_input(self) -> None:
        text = (ROOT / ".github" / "workflows" / "measure.yml").read_text(encoding="utf-8")

        self.assertNotIn("test_filter:", text)
        self.assertNotIn("diagnostic:", text)
        self.assertIn("measurement_catalog.py resolve", text)
        self.assertIn("measurement_catalog.py verify-run", text)
        self.assertIn("<TreatNoTestsAsError>true</TreatNoTestsAsError>", text)
        self.assertIn('--logger "console;verbosity=detailed"', text)
        self.assertIn('--logger "trx;LogFileName=measurement.trx"', text)
        self.assertIn('env "$MEASUREMENT_ENV=1" dotnet test "$MEASUREMENT_PROJECT"', text)
        self.assertIn("permissions:\n  contents: read", text)

    def test_workflow_verifies_only_instrument_output_not_operator_reason(self) -> None:
        text = (ROOT / ".github" / "workflows" / "measure.yml").read_text(encoding="utf-8")

        self.assertIn('MEASURE_OUT="artifacts/measurements/instrument-output.txt"', text)
        self.assertIn('2>&1 | tee "$MEASURE_OUT" | tee -a "$OUT"', text)
        self.assertIn('--trx-dir "$RESULTS" --output "$MEASURE_OUT"', text)
        self.assertIn('echo "# reason:     $REASON"', text)
        self.assertNotIn('--trx-dir "$RESULTS" --output "$OUT"', text)


if __name__ == "__main__":
    unittest.main()
