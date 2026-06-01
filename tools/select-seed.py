#!/usr/bin/env python3
"""
File:    tools/select-seed.py
Spec:    Performance Optimization Strategy #18 §3.3.2, Appendix E, Code Standards #20
Purpose: Emits a deterministic profiling seed for use in baseline-capture sessions.

Stage 0:    returns a fixed Stage-0 development seed so reviewer output is reproducible
            across machines. The seed is recorded verbatim in the session manifest.
Stage 0+1:  replaced by a seed derived from the current git SHA + scenario manifest ID,
            ensuring each CI run uses a unique but deterministic seed (KD-6).

Usage:
    SEED=$(python3 tools/select-seed.py)
    # or for a specific scenario:
    SEED=$(python3 tools/select-seed.py --scenario-id DS-SCEN-001)
"""

from __future__ import annotations

import argparse
import hashlib
import subprocess
import sys


# Stage 0 fallback seed (SplitMix64 constant; matches the value used in
# DeterministicSimConstants — not copied here to avoid cross-tool coupling).
_STAGE0_DEV_SEED = 0x9E3779B97F4A7C15


def git_sha() -> str | None:
    """Return the current HEAD SHA or None if not in a git repo."""
    try:
        result = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            capture_output=True,
            text=True,
            check=True,
        )
        return result.stdout.strip()
    except Exception:
        return None


def derive_seed(sha: str, scenario_id: str) -> int:
    """
    Derive a deterministic uint64 seed from git SHA + scenario ID.
    Uses SHA-256 truncated to 8 bytes, matching the KD-6 derivation intent.
    Python tooling: mask intermediates with 0xFFFFFFFFFFFFFFFF per CLAUDE.md.
    """
    digest = hashlib.sha256(f"{sha}:{scenario_id}".encode()).digest()
    seed = int.from_bytes(digest[:8], byteorder="little")
    return seed & 0xFFFFFFFFFFFFFFFF


def main() -> int:
    parser = argparse.ArgumentParser(description="Select deterministic profiling seed")
    parser.add_argument(
        "--scenario-id",
        default="",
        help="Scenario manifest ID to mix into the seed (Stage 0+1 mode)",
    )
    args = parser.parse_args()

    sha = git_sha()
    if sha is None or not args.scenario_id:
        # Stage 0: no git SHA available or no scenario specified — use fixed dev seed.
        seed = _STAGE0_DEV_SEED
    else:
        seed = derive_seed(sha, args.scenario_id)

    print(seed)
    return 0


if __name__ == "__main__":
    sys.exit(main())
