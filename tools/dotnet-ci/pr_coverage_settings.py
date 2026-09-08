#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
import subprocess
import sys
from xml.sax.saxutils import escape

NO_DELTA_ASSEMBLY = "__NoProductionCoverageDelta__"


def _read_asmdef(path: Path) -> dict[str, object]:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValueError(f"cannot read asmdef {path}: {exc}") from exc
    name = data.get("name")
    if not isinstance(name, str) or not name:
        raise ValueError(f"asmdef has no valid name: {path}")
    refs = data.get("references", [])
    if not isinstance(refs, list) or any(not isinstance(x, str) for x in refs):
        raise ValueError(f"asmdef has invalid references: {path}")
    return data


def _is_test_or_shim(name: str) -> bool:
    return name.endswith(".Tests") or name.startswith("UnityShim")


def _nearest_asmdef(repo_root: Path, rel_path: str) -> Path:
    path = repo_root / rel_path
    current = path.parent
    src_root = repo_root / "src"
    while current == src_root or src_root in current.parents:
        asmdefs = sorted(current.glob("*.asmdef"))
        if asmdefs:
            if len(asmdefs) != 1:
                names = ", ".join(str(p.relative_to(repo_root)) for p in asmdefs)
                raise ValueError(f"ambiguous asmdef ownership for {rel_path}: {names}")
            return asmdefs[0]
        if current == src_root:
            break
        current = current.parent
    raise ValueError(f"changed source file has no asmdef owner: {rel_path}")


def coverage_assemblies(repo_root: Path, changed_paths: list[str]) -> list[str]:
    assemblies: set[str] = set()
    for rel in changed_paths:
        if not rel.startswith("src/"):
            continue
        suffix = Path(rel).suffix.lower()
        if suffix not in {".cs", ".asmdef"}:
            continue

        asmdef_path = repo_root / rel if suffix == ".asmdef" else _nearest_asmdef(repo_root, rel)
        if not asmdef_path.exists():
            # Deleted source/asmdef contributes no current executable lines to measure.
            continue
        data = _read_asmdef(asmdef_path)
        name = str(data["name"])
        if _is_test_or_shim(name):
            for ref in data.get("references", []):
                ref_name = str(ref)
                if ref_name.startswith("GUID:") or _is_test_or_shim(ref_name):
                    continue
                assemblies.add(ref_name)
        else:
            assemblies.add(name)
    return sorted(assemblies)


def changed_paths(repo_root: Path, base: str, head: str) -> list[str]:
    proc = subprocess.run(
        [
            "git",
            "-C",
            str(repo_root),
            "diff",
            "--name-only",
            "--diff-filter=ACMRTUXB",
            f"{base}...{head}",
            "--",
            "src",
        ],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if proc.returncode != 0:
        raise ValueError(f"git diff failed for {base}...{head}: {proc.stderr.strip()}")
    return [line.strip() for line in proc.stdout.splitlines() if line.strip()]


def render_settings(assemblies: list[str]) -> str:
    selected = assemblies or [NO_DELTA_ASSEMBLY]
    include = ",".join(f"[{name}]*" for name in selected)
    return f'''<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Include>{escape(include)}</Include>
          <Exclude>[*.Tests]*,[UnityShim*]*</Exclude>
          <ExcludeByFile>**/tests/**,**/Tests/**,**/*.gen.cs</ExcludeByFile>
          <SingleHit>true</SingleHit>
          <UseSourceLink>false</UseSourceLink>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
'''


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate bounded PR Coverlet settings from changed src/ assembly ownership.")
    parser.add_argument("--repo-root", type=Path, required=True)
    parser.add_argument("--base", required=True)
    parser.add_argument("--head", default="HEAD")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    root = args.repo_root.resolve()
    try:
        paths = changed_paths(root, args.base, args.head)
        assemblies = coverage_assemblies(root, paths)
    except ValueError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(render_settings(assemblies), encoding="utf-8")
    if assemblies:
        print("PR coverage assemblies: " + ", ".join(assemblies))
    else:
        print("PR coverage assemblies: none (no changed production/test-owned src assembly)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
