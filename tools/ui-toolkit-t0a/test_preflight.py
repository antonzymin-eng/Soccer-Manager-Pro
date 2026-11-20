"""Exercise the T0a runner with simulated Unity Editor output."""

import io
import json
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path
from unittest.mock import patch

import preflight


PINNED = "6000.4.9f1 (f7258d6eebbe)"
COMMIT = "a" * 40
DETACHED, TAB, REFLOW = preflight.EXPECTED_TESTS


class PreflightTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory(prefix="T0a mock checkout ")
        self.addCleanup(temp.cleanup)
        self.project = Path(temp.name).resolve()
        self.editor = self.project / "Unity Editor.exe"
        self.editor.touch()
        settings = self.project / "ProjectSettings"
        settings.mkdir()
        (settings / "ProjectVersion.txt").write_text(f"m_EditorVersionWithRevision: {PINNED}\n", encoding="utf-8")
        self.artifacts = self.project / "artifacts" / "ui-toolkit-t0a"
        project_patch = patch.object(preflight, "PROJECT", self.project)
        project_patch.start()
        self.addCleanup(project_patch.stop)

    def run_editor(self, *, overrides=None, run_result="Passed", unity_exit=0, version=PINNED,
                   compiler_error=False, xml="normal", statuses=("", ""), commits=(COMMIT, COMMIT)):
        results = dict.fromkeys(preflight.EXPECTED_TESTS, ("Passed", None))
        results.update(overrides or {})
        status_values, commit_values = iter(statuses), iter(commits)

        def git(command, *_):
            return next(status_values if command == "status" else commit_values)

        def unity(command, *, cwd, check):
            self.assertEqual(cwd, self.project)
            self.assertFalse(check)
            self.assertEqual(command[0], str(self.editor))
            self.assertIn("-batchmode", command)
            self.assertIn("-runTests", command)
            self.assertNotIn("-quit", command)
            self.assertEqual(command[command.index("-assemblyNames") + 1], preflight.ASSEMBLY)
            log_path = Path(command[command.index("-logFile") + 1])
            log = f"Built from '6000.4/staging' branch; Version is '{version} revision 16197005'\n"
            if compiler_error:
                log += "Assets/Spike.cs(1,1): error CS0246: mock compile error\n"
            log_path.write_text(log, encoding="utf-8")
            result_path = Path(command[command.index("-testResults") + 1])
            if xml == "normal":
                run = ET.Element("test-run", result=run_result)
                for name, outcome in results.items():
                    if outcome is None:
                        continue
                    result, reason = outcome
                    case = ET.SubElement(run, "test-case", fullname=preflight.FIXTURE + name, result=result)
                    if reason:
                        ET.SubElement(ET.SubElement(case, "reason"), "message").text = reason
                ET.ElementTree(run).write(result_path, encoding="utf-8", xml_declaration=True)
            elif xml == "malformed":
                result_path.write_text("<test-run", encoding="utf-8")
            return subprocess.CompletedProcess(command, unity_exit)

        stdout, stderr = io.StringIO(), io.StringIO()
        with patch.object(preflight, "has_script_mount", return_value=True), patch.object(preflight, "git", git), \
                patch.object(preflight.platform, "platform", return_value="Simulated Unity host"), \
                patch.object(preflight.subprocess, "run", unity), redirect_stdout(stdout), redirect_stderr(stderr):
            exit_code = preflight.main(["--editor", str(self.editor)])
        receipt_path, = self.artifacts.glob("*/receipt.json")
        receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
        self.assertTrue((receipt_path.parent / "editor.log").is_file())
        return exit_code, receipt, stdout.getvalue(), stderr.getvalue()

    def test_all_three_pass(self):
        exit_code, receipt, _, _ = self.run_editor()
        self.assertEqual(exit_code, 0)
        self.assertEqual(receipt["tests"], dict.fromkeys(preflight.EXPECTED_TESTS, "Passed"))
        self.assertEqual(receipt["interactive_required"], [])
        self.assertFalse(receipt["dirty"])

    def test_marked_batch_inconclusives_require_interactive_run(self):
        pending = ("Inconclusive", preflight.BATCH_LAYOUT_UNAVAILABLE)
        exit_code, receipt, stdout, _ = self.run_editor(overrides={TAB: pending, REFLOW: pending}, unity_exit=2)
        self.assertEqual(exit_code, 3)
        self.assertEqual(receipt["unity_exit_code"], 2)
        self.assertEqual(receipt["tests"][TAB], "Inconclusive")
        self.assertEqual(receipt["interactive_required"], [TAB, REFLOW])
        self.assertEqual(receipt["failures"], [])
        self.assertIn("Run interactively", stdout)

    def test_failed_assertion_is_not_deferred(self):
        overrides = {TAB: ("Inconclusive", preflight.BATCH_LAYOUT_UNAVAILABLE), REFLOW: ("Failed", None)}
        exit_code, receipt, _, _ = self.run_editor(overrides=overrides, run_result="Failed:Child", unity_exit=2)
        self.assertEqual(exit_code, 1)
        self.assertEqual(receipt["interactive_required"], [TAB])
        self.assertIn(f"{REFLOW}: Failed.", receipt["failures"])

    def test_editor_error_with_a_layout_deferral_still_fails(self):
        pending = ("Inconclusive", preflight.BATCH_LAYOUT_UNAVAILABLE)
        exit_code, receipt, _, _ = self.run_editor(overrides={TAB: pending}, unity_exit=3)
        self.assertEqual(exit_code, 1)
        self.assertIn("Unity exited with code 3.", receipt["failures"])

    def test_inconclusive_run_with_a_verified_layout_deferral(self):
        pending = ("Inconclusive", preflight.BATCH_LAYOUT_UNAVAILABLE)
        exit_code, receipt, _, _ = self.run_editor(overrides={REFLOW: pending}, run_result="Inconclusive:Child")
        self.assertEqual(exit_code, 3)
        self.assertEqual(receipt["interactive_required"], [REFLOW])

    def test_unrecognized_inconclusive_is_a_failure(self):
        exit_code, receipt, _, _ = self.run_editor(overrides={TAB: ("Inconclusive", "Another reason")}, unity_exit=2)
        self.assertEqual(exit_code, 1)
        self.assertEqual(receipt["interactive_required"], [])
        self.assertIn(f"{TAB}: Inconclusive.", receipt["failures"])

    def test_missing_detached_test_fails_even_if_xml_says_passed(self):
        exit_code, receipt, _, _ = self.run_editor(overrides={DETACHED: None})
        self.assertEqual(exit_code, 1)
        self.assertIn(f"{DETACHED}: Missing.", receipt["failures"])

    def test_editor_version_must_match(self):
        exit_code, receipt, _, _ = self.run_editor(version="6000.4.8f1 (b1111111)")
        self.assertEqual(exit_code, 1)
        self.assertIn("6000.4.8f1", receipt["failures"][0])

    def test_compiler_error_is_a_failure_even_with_passing_xml(self):
        exit_code, receipt, _, _ = self.run_editor(compiler_error=True)
        self.assertEqual(exit_code, 1)
        self.assertIn("error CS0246", receipt["compiler_errors"][0])

    def test_missing_xml_preserves_process_failure(self):
        exit_code, receipt, _, _ = self.run_editor(xml="absent", unity_exit=3)
        self.assertEqual(exit_code, 1)
        self.assertIn("Unity exited with code 3.", receipt["failures"])
        self.assertTrue(any("results could not be read" in message for message in receipt["failures"]))

    def test_malformed_xml_is_a_failure(self):
        exit_code, receipt, _, _ = self.run_editor(xml="malformed")
        self.assertEqual(exit_code, 1)
        self.assertIn("results could not be read", receipt["failures"][0])

    def test_dirty_status_warns_and_identifies_head_as_base(self):
        status = " M Assets/Spike.cs"
        exit_code, receipt, _, stderr = self.run_editor(statuses=(status, status))
        self.assertEqual(exit_code, 0)
        self.assertTrue(receipt["dirty"])
        self.assertEqual(receipt["git_status_before"], status)
        self.assertIn(f"Commit {COMMIT[:8]} is the base", stderr)

    def test_head_change_makes_receipt_fail(self):
        exit_code, receipt, _, _ = self.run_editor(commits=(COMMIT, "b" * 40))
        self.assertEqual(exit_code, 1)
        self.assertIn("The checked-out commit changed during the Unity run.", receipt["failures"])

    def test_missing_script_mount_stops_before_launch(self):
        stderr = io.StringIO()
        with patch.object(preflight, "has_script_mount", return_value=False), redirect_stderr(stderr):
            with self.assertRaises(SystemExit) as error:
                preflight.main(["--editor", str(self.editor)])
        self.assertEqual(error.exception.code, 2)
        self.assertIn("Assets/Scripts junction", stderr.getvalue())
        self.assertFalse(self.artifacts.exists())


if __name__ == "__main__":
    unittest.main()
