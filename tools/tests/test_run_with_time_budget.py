import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import time
import unittest


ROOT = Path(__file__).resolve().parents[2]
RUNNER = ROOT / "tools" / "run-with-time-budget.py"


class RunWithTimeBudgetTests(unittest.TestCase):
    def run_runner(self, *arguments, timeout=10):
        return subprocess.run(
            [sys.executable, str(RUNNER), *arguments],
            capture_output=True,
            text=True,
            timeout=timeout,
        )

    def test_rejects_non_finite_and_non_positive_budgets(self):
        for value in ("nan", "inf", "-inf", "0", "-1"):
            with self.subTest(value=value):
                result = self.run_runner(f"--seconds={value}", "--", sys.executable, "-c", "pass")
                self.assertEqual(result.returncode, 2)
                self.assertIn("finite value > 0", result.stderr)

    def test_propagates_child_exit_code(self):
        result = self.run_runner("--seconds", "2", "--", sys.executable, "-c", "raise SystemExit(23)")
        self.assertEqual(result.returncode, 23)

    def test_missing_command_has_shell_compatible_exit_without_traceback(self):
        result = self.run_runner("--seconds", "1", "--", "/definitely/not/a/command")
        self.assertEqual(result.returncode, 127)
        self.assertIn("command not found", result.stderr)
        self.assertNotIn("Traceback", result.stderr)

    @unittest.skipIf(os.name == "nt", "POSIX executable mode test")
    def test_non_executable_command_has_shell_compatible_exit_without_traceback(self):
        with tempfile.TemporaryDirectory() as directory:
            command = Path(directory) / "not-executable"
            command.write_text("#!/bin/sh\nexit 0\n")
            command.chmod(0o600)
            result = self.run_runner("--seconds", "1", "--", str(command))

        self.assertEqual(result.returncode, 126)
        self.assertIn("not executable", result.stderr)
        self.assertNotIn("Traceback", result.stderr)

    def test_timeout_returns_124(self):
        result = self.run_runner("--seconds", "0.05", "--", sys.executable, "-c", "import time; time.sleep(5)")
        self.assertEqual(result.returncode, 124)
        self.assertIn("exceeded", result.stderr)

    @unittest.skipIf(os.name == "nt", "POSIX process-group assertion")
    def test_timeout_terminates_descendant_processes(self):
        with tempfile.TemporaryDirectory() as directory:
            pid_file = Path(directory) / "pid"
            source = (
                "import pathlib,subprocess,sys,time; "
                "p=subprocess.Popen([sys.executable,'-c','import time; time.sleep(30)']); "
                f"pathlib.Path({str(pid_file)!r}).write_text(str(p.pid)); "
                "time.sleep(30)"
            )
            result = self.run_runner("--seconds", "0.2", "--", sys.executable, "-c", source)
            self.assertEqual(result.returncode, 124)
            child_pid = int(pid_file.read_text())
            for _ in range(50):
                try:
                    os.kill(child_pid, 0)
                except ProcessLookupError:
                    break
                status = Path(f"/proc/{child_pid}/status")
                if status.exists() and "State:\tZ" in status.read_text():
                    break
                time.sleep(0.02)
            else:
                self.fail("descendant survived timeout process-group termination")

    @unittest.skipIf(os.name == "nt", "POSIX signal assertion")
    def test_interrupt_cleans_up_child_and_returns_130(self):
        proc = subprocess.Popen(
            [
                sys.executable,
                str(RUNNER),
                "--seconds",
                "30",
                "--",
                sys.executable,
                "-c",
                "import time; print('ready', flush=True); time.sleep(30)",
            ],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
        )
        self.assertEqual(proc.stdout.readline().strip(), "ready")
        proc.send_signal(signal.SIGINT)
        proc.communicate(timeout=5)
        self.assertEqual(proc.returncode, 130)


if __name__ == "__main__":
    unittest.main()
