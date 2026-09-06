#!/usr/bin/env python3
"""Run a command under a hard wall-clock budget.

Exit 124 on timeout (matching the conventional timeout(1) code), otherwise
propagate the child exit code. Stdout/stderr stream directly to the caller.
"""

from __future__ import annotations

import argparse
import os
import signal
import subprocess
import sys


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--seconds", type=float, required=True)
    parser.add_argument("command", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    if args.seconds <= 0:
        parser.error("--seconds must be > 0")
    if not args.command or args.command[0] != "--" or len(args.command) == 1:
        parser.error("command must follow '--'")
    args.command = args.command[1:]
    return args


def main() -> int:
    args = parse_args()

    kwargs: dict[str, object] = {}
    if os.name == "nt":
        kwargs["creationflags"] = subprocess.CREATE_NEW_PROCESS_GROUP
    else:
        kwargs["start_new_session"] = True

    proc = subprocess.Popen(args.command, **kwargs)
    try:
        return proc.wait(timeout=args.seconds)
    except subprocess.TimeoutExpired:
        print(
            f"ERROR: command exceeded {args.seconds:g}s wall-clock budget",
            file=sys.stderr,
        )
        if os.name == "nt":
            proc.send_signal(signal.CTRL_BREAK_EVENT)
            try:
                proc.wait(timeout=2)
            except subprocess.TimeoutExpired:
                proc.kill()
        else:
            os.killpg(proc.pid, signal.SIGTERM)
            try:
                proc.wait(timeout=2)
            except subprocess.TimeoutExpired:
                os.killpg(proc.pid, signal.SIGKILL)
        proc.wait()
        return 124


if __name__ == "__main__":
    raise SystemExit(main())
