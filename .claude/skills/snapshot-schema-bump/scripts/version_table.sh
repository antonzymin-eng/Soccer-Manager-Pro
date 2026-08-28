#!/usr/bin/env bash
# version_table.sh — print every live schema/format version constant and its
# current value, straight from src/, so the SKILL.md table can't go stale.
#
# The prose table in SKILL.md still owns what each constant MEANS (which
# layer it versions, when to bump it) — that's judgment and stays there.
# This owns what each one IS right now, which is a fixed lookup.
#
# The `public const` anchor and the tests/ exclusion are load-bearing: a
# looser grep also matches version-history table rows sitting in comments
# (e.g. MatchEngineConstants.cs's own changelog block), which would report
# stale historical numbers as if they were the live constant.
set -euo pipefail
repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$repo_root"

grep -rnE '^[[:space:]]*public const [a-z]+ [A-Z_]*(SNAPSHOT_SCHEMA_VERSION|FORMAT_VERSION)[[:space:]]*=[[:space:]]*[0-9]+' \
  src/ --include=*.cs \
  | grep -v '/tests/' \
  | sed -E 's|^([^:]+):([0-9]+):[[:space:]]*public const [a-z]+ ([A-Z_]+)[[:space:]]*=[[:space:]]*([0-9]+).*|\4\t\3\t\1:\2|' \
  | sort -rn -k1
