#!/usr/bin/env python3
"""Capture the T0a Unity Editor import, compile, and EditMode receipt."""

import argparse
import json
import platform
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[2]
ASSEMBLY = "TacticalDirector.UiToolkitSpike.Tests"
FIXTURE = f"{ASSEMBLY}.UiToolkitSpikeTests."
WINDOW_TESTS = (
    "TabMovesFocusForwardAndBackwardInAnEditorPanel",
    "UssDoublesTextSizeAndReflowsInAnEditorPanel",
)
EXPECTED_TESTS = ("DetachedCloneDeclaresFocusableControlsInOrder", *WINDOW_TESTS)
# This is the reason supplied by RequireRootLayout in the spike's Editor tests.
BATCH_LAYOUT_UNAVAILABLE = "T0a batch Editor window layout unavailable."
UNITY_TEST_FAILURE_EXIT = 2
INTERACTIVE_REQUIRED_EXIT = 3
EDITOR_BANNER = re.compile(r"(?:Version is '|Initialize engine version: )([^\s]+ \([0-9a-f]+\))")
COMPILER_ERROR = re.compile(r"\berror CS\d+\b|scripts have compiler errors", re.IGNORECASE)


def git(*args):
    return subprocess.check_output(["git", "-C", str(PROJECT), *args], text=True).strip("\n")


def has_script_mount():
    return (PROJECT / "Assets" / "Scripts").resolve() == PROJECT / "src"


def inspect_results(path, receipt):
    try:
        run = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as error:
        receipt["failures"].append(f"Unity EditMode results could not be read: {error}")
        return

    if run.tag != "test-run":
        receipt["failures"].append("Expected Unity's NUnit test-run XML.")
        return

    tests = {case.get("fullname"): case for case in run.iter("test-case")}
    receipt["test_result"] = run.get("result")
    receipt["tests"] = {}
    for name in EXPECTED_TESTS:
        case = tests.pop(FIXTURE + name, None)
        result = case.get("result", "Missing") if case is not None else "Missing"
        receipt["tests"][name] = result
        if (result == "Inconclusive" and name in WINDOW_TESTS and
                BATCH_LAYOUT_UNAVAILABLE in case.findtext("reason/message", default="")):
            receipt["interactive_required"].append(name)
        elif result != "Passed":
            receipt["failures"].append(f"{name}: {result}.")

    for name, case in tests.items():
        if case.get("result") != "Passed":
            receipt["failures"].append(f"{name}: {case.get('result', 'Missing')}.")

    run_result = run.get("result", "Missing")
    is_inconclusive = run_result.split(":", 1)[0] == "Inconclusive"
    if run_result != "Passed" and not (is_inconclusive and receipt["interactive_required"]):
        receipt["failures"].append(f"Unity EditMode run result: {run_result}.")


def inspect_log(path, pinned_version, receipt):
    try:
        log = path.read_text(encoding="utf-8-sig", errors="replace")
    except OSError as error:
        receipt["failures"].append(f"Unity Editor log could not be read: {error}")
        return

    banner = EDITOR_BANNER.search(log)
    receipt["unity_editor_version"] = banner[1] if banner else None
    if not banner or banner[1] != pinned_version:
        observed = banner[1] if banner else "unknown"
        receipt["failures"].append(f"Expected Unity {pinned_version}; Editor log reports {observed}.")
    receipt["compiler_errors"] = [line.strip() for line in log.splitlines() if COMPILER_ERROR.search(line)]
    if receipt["compiler_errors"]:
        receipt["failures"].append("Unity logged C# compilation errors. See editor.log.")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--editor", required=True, type=Path, help="Path to the pinned Unity Editor executable")
    editor = parser.parse_args(argv).editor.resolve()
    if not editor.is_file():
        parser.error(f"Unity Editor executable does not exist: {editor}")
    if not has_script_mount():
        parser.error("Create the Assets/Scripts junction or symlink into src as shown in Assets/README.md.")

    version_file = PROJECT / "ProjectSettings" / "ProjectVersion.txt"
    match = re.search(r"^m_EditorVersionWithRevision: ([^\r\n]+)$", version_file.read_text(), re.MULTILINE)
    if not match:
        parser.error(f"Could not read the pinned Unity version from {version_file}.")
    pinned_version = match[1]
    commit = git("rev-parse", "HEAD")
    git_status_before = git("status", "--short")
    started = datetime.now(timezone.utc)
    artifact_root = PROJECT / "artifacts" / "ui-toolkit-t0a"
    artifact_root.mkdir(parents=True, exist_ok=True)
    output = Path(tempfile.mkdtemp(prefix=f"{started:%Y%m%dT%H%M%SZ}-{commit[:8]}-", dir=artifact_root))
    log, results = output / "editor.log", output / "editmode.xml"
    command = [
        str(editor), "-batchmode", "-projectPath", str(PROJECT), "-runTests", "-testPlatform", "EditMode",
        "-assemblyNames", ASSEMBLY, "-testResults", str(results), "-logFile", str(log),
    ]
    receipt = {
        "commit": commit,
        "host_platform": platform.platform(),
        "started_utc": started.isoformat(timespec="seconds"),
        "project_unity_version": pinned_version,
        "git_status_before": git_status_before,
        "dirty": bool(git_status_before),
        "command": command,
        "failures": [],
        "interactive_required": [],
    }
    if receipt["dirty"]:
        print(f"Warning: checkout has uncommitted changes. Commit {commit[:8]} is the base of the tested tree.",
              file=sys.stderr, flush=True)
    print(f"T0a batch receipt directory {output}", flush=True)
    try:
        unity_exit = subprocess.run(command, cwd=PROJECT, check=False).returncode
        receipt["unity_exit_code"] = unity_exit
        inspect_log(log, pinned_version, receipt)
        inspect_results(results, receipt)
        # The Unity Test Framework's command line counts inconclusive tests as failed for its exit code.
        only_window_deferrals = receipt["interactive_required"] and not receipt["failures"]
        if unity_exit != 0 and not (unity_exit == UNITY_TEST_FAILURE_EXIT and only_window_deferrals):
            receipt["failures"].append(f"Unity exited with code {unity_exit}.")
    except OSError as error:
        receipt["failures"].append(f"Could not launch Unity: {error}")
    except KeyboardInterrupt:
        receipt["failures"].append("The Unity run was interrupted.")
    finally:
        try:
            receipt["git_status_after"] = git("status", "--short")
            receipt["commit_after"] = git("rev-parse", "HEAD")
            if receipt["commit_after"] != commit:
                receipt["failures"].append("The checked-out commit changed during the Unity run.")
        except (OSError, subprocess.CalledProcessError) as error:
            receipt["failures"].append(f"Could not record Git state after the Unity run: {error}")
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")

    for message in receipt["failures"]:
        print(message, file=sys.stderr)
    if receipt["failures"]:
        return 1
    if receipt["interactive_required"]:
        print("Run interactively in the Unity EditMode Test Runner:")
        for name in receipt["interactive_required"]:
            print(f"- {name}")
        return INTERACTIVE_REQUIRED_EXIT
    print("Unity ran all three T0a EditMode checks successfully. See editor.log for import diagnostics.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
