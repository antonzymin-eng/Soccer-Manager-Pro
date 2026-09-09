#!/usr/bin/env python3
"""Run a command under a hard wall-clock budget.

Exit 124 on timeout (matching the conventional timeout(1) code), otherwise
propagate the child exit code. Stdout/stderr stream directly to the caller.
"""

from __future__ import annotations

import argparse
import math
import os
import signal
import subprocess
import sys


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--seconds", type=float, required=True)
    parser.add_argument("command", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    if not math.isfinite(args.seconds) or args.seconds <= 0:
        parser.error("--seconds must be a finite value > 0")
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

    try:
        proc = subprocess.Popen(args.command, **kwargs)
    except FileNotFoundError:
        print(f"ERROR: command not found: {args.command[0]}", file=sys.stderr)
        return 127
    except PermissionError:
        print(f"ERROR: command is not executable: {args.command[0]}", file=sys.stderr)
        return 126
    except OSError as error:
        print(f"ERROR: could not start command: {error}", file=sys.stderr)
        return 126

    def stop_process_tree() -> None:
        """Stop the complete child tree while tolerating exit races."""
        if proc.poll() is not None:
            return
        try:
            if os.name == "nt":
                proc.send_signal(signal.CTRL_BREAK_EVENT)
            else:
                os.killpg(proc.pid, signal.SIGTERM)
        except ProcessLookupError:
            return

        try:
            proc.wait(timeout=2)
            return
        except subprocess.TimeoutExpired:
            pass

        try:
            if os.name == "nt":
                proc.kill()
            else:
                os.killpg(proc.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass

        proc.wait()

    try:
        return proc.wait(timeout=args.seconds)
    except subprocess.TimeoutExpired:
        print(
            f"ERROR: command exceeded {args.seconds:g}s wall-clock budget",
            file=sys.stderr,
        )
        stop_process_tree()
        return 124
    except KeyboardInterrupt:
        stop_process_tree()
        return 130


if __name__ == "__main__":
    raise SystemExit(main())
