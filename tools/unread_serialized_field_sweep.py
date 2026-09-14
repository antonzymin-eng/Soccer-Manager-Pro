#!/usr/bin/env python3
"""Find serialized/snapshot carrier fields that are written but never behaviorally read.

This is a conservative lexical audit, not a C# compiler. It is deliberately a
candidate generator for manual triage. Transport-only reads in serialization,
capture, restore, and codec methods do not count as behavioral consumption.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass, field
import json
from pathlib import Path
import re
from typing import Iterator, Sequence


FIELD_RE = re.compile(
    r"""^\s*
    (?P<access>public|internal|private|protected)\s+
    (?:(?:static|readonly|volatile|const|new|unsafe)\s+)*
    (?P<type>[A-Za-z_][\w<>\[\],?.:]*)\s+
    (?P<name>[A-Za-z_]\w*)\s*
    (?:=[^;]*)?;
    \s*$""",
    re.VERBOSE,
)
TYPE_RE = re.compile(
    r"\b(?:class|struct|record)\s+(?:struct\s+|class\s+)?(?P<name>[A-Za-z_]\w*)"
)
METHOD_RE = re.compile(
    r"""^\s*
    (?:(?:public|internal|private|protected|static|virtual|override|sealed|async|unsafe|new|partial)\s+)*
    [A-Za-z_][\w<>\[\],?.:]*\s+
    (?P<name>[A-Za-z_]\w*)\s*\([^;]*\)\s*
    (?:where\b[^{}]*)?
    (?P<brace>\{)?\s*$""",
    re.VERBOSE,
)
SERIALIZE_ATTR_RE = re.compile(r"\[\s*(?:UnityEngine\.)?SerializeField\s*\]")
ATTRIBUTE_ONLY_RE = re.compile(r"^\s*\[[^\]]+\]\s*$")
IDENT_RE_TEMPLATE = r"\b{}\b"

TEST_PARTS = {
    "test", "tests", "testing", "testdata", "test-data", "editor-tests",
}
TRANSPORT_PATH_MARKERS = (
    "serializer", "serialization", "codec", "save",
)
TRANSPORT_METHOD_MARKERS = (
    "serialize", "deserialize", "capture", "restore", "write", "read",
    "encode", "decode", "pack", "unpack", "save", "load",
)
CARRIER_TYPE_SUFFIXES = (
    "Snapshot", "State", "SaveData", "SaveState", "Record", "Config",
)


@dataclass(frozen=True)
class FieldDecl:
    path: str
    line: int
    type_name: str
    field_type: str
    name: str
    unity_serialized: bool
    carrier_type: bool

    @property
    def key(self) -> str:
        return f"{self.type_name}.{self.name}"


@dataclass
class Evidence:
    writes: list[str] = field(default_factory=list)
    behavioral_reads: list[str] = field(default_factory=list)
    transport_reads: list[str] = field(default_factory=list)


@dataclass(frozen=True)
class Finding:
    declaration: FieldDecl
    writes: tuple[str, ...]
    transport_reads: tuple[str, ...]

    @property
    def category(self) -> str:
        if self.declaration.unity_serialized:
            return "unity-serialized"
        if self.transport_reads:
            return "transport-only"
        return "snapshot/state-carrier"


def is_production_cs(path: Path, src_root: Path) -> bool:
    try:
        rel = path.relative_to(src_root)
    except ValueError:
        return False
    if path.suffix != ".cs":
        return False
    lowered = {part.lower() for part in rel.parts}
    if lowered & TEST_PARTS:
        return False
    name = path.name.lower()
    if name.endswith("tests.cs") or name.endswith("test.cs"):
        return False
    return True


def iter_cs_files(repo: Path) -> Iterator[Path]:
    src = repo / "src"
    if not src.is_dir():
        return
    for path in sorted(src.rglob("*.cs")):
        if is_production_cs(path, src):
            yield path


def strip_line_comments(line: str) -> str:
    """Remove // comments while preserving strings well enough for field syntax."""
    out: list[str] = []
    in_string = False
    verbatim = False
    escaped = False
    i = 0
    while i < len(line):
        ch = line[i]
        nxt = line[i + 1] if i + 1 < len(line) else ""
        if not in_string and ch == "/" and nxt == "/":
            break
        if not in_string and ch == '"' and i > 0 and line[i - 1] == "@":
            in_string = True
            verbatim = True
            out.append(ch)
        elif not in_string and ch == '"':
            in_string = True
            verbatim = False
            out.append(ch)
        elif in_string:
            out.append(ch)
            if verbatim and ch == '"' and nxt == '"':
                out.append(nxt)
                i += 1
            elif ch == '"' and (verbatim or not escaped):
                in_string = False
                verbatim = False
            escaped = (ch == "\\" and not escaped) if not verbatim else False
            i += 1
            continue
        else:
            out.append(ch)
        escaped = (ch == "\\" and not escaped) if in_string and not verbatim else False
        i += 1
    return "".join(out)


def brace_delta(line: str) -> int:
    clean = strip_line_comments(line)
    return clean.count("{") - clean.count("}")


def declarations_in_file(path: Path, repo: Path) -> list[FieldDecl]:
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    result: list[FieldDecl] = []
    # Stack entries are (type name, brace depth *inside* that type).
    type_stack: list[tuple[str, int]] = []
    pending_type: str | None = None
    depth = 0
    pending_serialize = False

    for idx, raw in enumerate(lines, start=1):
        line = strip_line_comments(raw)
        stripped = line.strip()

        # Close types whose body ended on the previous line.
        while type_stack and depth < type_stack[-1][1]:
            type_stack.pop()

        tm = TYPE_RE.search(line)
        if tm:
            pending_type = tm.group("name")

        # A conventional type body may open on the declaration line or a later line.
        opens = line.count("{")
        if pending_type is not None and opens:
            # The first opening brace on/after the type declaration begins the type body.
            type_stack.append((pending_type, depth + 1))
            pending_type = None

        if SERIALIZE_ATTR_RE.search(line):
            pending_serialize = True

        fm = FIELD_RE.match(line)
        if fm and type_stack:
            type_name = type_stack[-1][0]
            unity_serialized = pending_serialize or bool(SERIALIZE_ATTR_RE.search(line))
            carrier_type = type_name.endswith(CARRIER_TYPE_SUFFIXES)
            if unity_serialized or carrier_type:
                result.append(
                    FieldDecl(
                        path=str(path.relative_to(repo)).replace("\\", "/"),
                        line=idx,
                        type_name=type_name,
                        field_type=fm.group("type"),
                        name=fm.group("name"),
                        unity_serialized=unity_serialized,
                        carrier_type=carrier_type,
                    )
                )
            pending_serialize = False
        elif stripped and not ATTRIBUTE_ONLY_RE.match(line):
            # Any real statement/declaration breaks attribute association, except a
            # type-opening brace between an attribute and its field (not legal C# anyway).
            if not (pending_serialize and stripped == "{"):
                pending_serialize = False

        depth += brace_delta(line)
        while type_stack and depth < type_stack[-1][1]:
            type_stack.pop()

    return result


def discover_fields(repo: Path) -> list[FieldDecl]:
    fields: list[FieldDecl] = []
    for path in iter_cs_files(repo):
        fields.extend(declarations_in_file(path, repo))
    return fields


def method_names_by_line(lines: Sequence[str]) -> list[str | None]:
    current: str | None = None
    method_depth: int | None = None
    pending: str | None = None
    depth = 0
    result: list[str | None] = []

    for raw in lines:
        line = strip_line_comments(raw)
        stripped = line.strip()

        # End a method whose closing brace was on the previous line.
        if current is not None and method_depth is not None and depth < method_depth:
            current = None
            method_depth = None

        mm = METHOD_RE.match(line)
        if mm and not stripped.endswith(";"):
            pending = mm.group("name")

        if pending is not None and "{" in line:
            current = pending
            method_depth = depth + 1
            pending = None

        result.append(current)
        depth += brace_delta(line)

        if current is not None and method_depth is not None and depth < method_depth:
            current = None
            method_depth = None

    return result


def is_transport_context(path: str, method: str | None, line: str) -> bool:
    base = Path(path).stem.lower()
    if any(marker in base for marker in TRANSPORT_PATH_MARKERS):
        return True
    if method:
        lower_method = method.lower()
        if any(marker in lower_method for marker in TRANSPORT_METHOD_MARKERS):
            return True
    # Canonical serializer / binary reader-writer calls are transport even when
    # wrapped in a generically named helper.
    lowered = line.lower()
    return (
        "canonicalserializer" in lowered
        or "binarywriter" in lowered
        or "binaryreader" in lowered
        or ".write(" in lowered
        or ".read" in lowered and "(" in lowered
    )


def occurrence_kind(line: str, field_name: str) -> str | None:
    """Classify one line occurrence as write/read/none.

    Conservative rule: if a line writes and also reads the same field (e.g. x += 1),
    it counts as a read. Plain assignment/object-initializer/ref-out destinations are
    writes only.
    """
    code = strip_line_comments(line)
    if not re.search(IDENT_RE_TEMPLATE.format(re.escape(field_name)), code):
        return None

    # Ignore the field declaration itself.
    if FIELD_RE.match(code) and re.search(
        rf"\b{re.escape(field_name)}\b\s*(?:=|;)", code
    ):
        return None

    # Compound assignments and ++/-- consume the prior value.
    if re.search(rf"\b{re.escape(field_name)}\b\s*(?:\+\+|--|[+\-*/%&|^]=)", code):
        return "read"

    # ref can be both; out is destination-only.
    if re.search(rf"\bref\s+(?:[\w.]+\.)?{re.escape(field_name)}\b", code):
        return "read"
    if re.search(rf"\bout\s+(?:[\w.]+\.)?{re.escape(field_name)}\b", code):
        return "write"

    # Qualified or bare assignment LHS, including object initializers.
    if re.search(
        rf"(?<![=!<>])\b(?:[A-Za-z_]\w*\.)*{re.escape(field_name)}\s*=(?!=)",
        code,
    ):
        # If the RHS also contains the field name, count consumption.
        lhs_match = re.search(
            rf"\b(?:[A-Za-z_]\w*\.)*{re.escape(field_name)}\s*=(?!=)", code
        )
        if lhs_match:
            rhs = code[lhs_match.end():]
            if re.search(IDENT_RE_TEMPLATE.format(re.escape(field_name)), rhs):
                return "read"
        return "write"

    return "read"


def collect_evidence(repo: Path, fields: Sequence[FieldDecl]) -> dict[str, Evidence]:
    by_name: dict[str, list[FieldDecl]] = {}
    for decl in fields:
        by_name.setdefault(decl.name, []).append(decl)

    evidence = {decl.key: Evidence() for decl in fields}

    for path in iter_cs_files(repo):
        rel = str(path.relative_to(repo)).replace("\\", "/")
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        methods = method_names_by_line(lines)

        for lineno, (raw, method) in enumerate(zip(lines, methods), start=1):
            for name, decls in by_name.items():
                kind = occurrence_kind(raw, name)
                if kind is None:
                    continue
                location = f"{rel}:{lineno}"
                for decl in decls:
                    # If the same field name exists on several carrier types this lexical
                    # sweep cannot type-resolve cross-file accesses. To avoid false proof,
                    # only propagate unqualified same-file uses to the declaring type;
                    # qualified cross-file uses conservatively count for every same-name
                    # candidate. Findings remain candidates, never proof.
                    same_file = rel == decl.path
                    qualified = bool(re.search(
                        rf"\.\s*{re.escape(name)}\b", strip_line_comments(raw)
                    ))
                    if not same_file and not qualified:
                        continue
                    target = evidence[decl.key]
                    if kind == "write":
                        target.writes.append(location)
                    elif is_transport_context(rel, method, raw):
                        target.transport_reads.append(location)
                    else:
                        target.behavioral_reads.append(location)
    return evidence


def find_candidates(repo: Path) -> list[Finding]:
    fields = discover_fields(repo)
    evidence = collect_evidence(repo, fields)
    findings: list[Finding] = []
    for decl in fields:
        ev = evidence[decl.key]
        writes = list(ev.writes)
        # Unity populates [SerializeField] from scene/prefab data, so the attribute is
        # itself a write-side activation surface even when no C# assignment exists.
        if decl.unity_serialized and not writes:
            writes.append("<Unity serialization>")
        if not writes:
            continue
        if ev.behavioral_reads:
            continue
        findings.append(
            Finding(
                declaration=decl,
                writes=tuple(sorted(set(writes))),
                transport_reads=tuple(sorted(set(ev.transport_reads))),
            )
        )
    return sorted(
        findings,
        key=lambda f: (f.declaration.path, f.declaration.line, f.declaration.name),
    )


def render_markdown(findings: Sequence[Finding]) -> str:
    out = [
        "# Static unread serialized/snapshot field sweep",
        "",
        "Diagnostic candidates only. A field is listed when production code writes it and the",
        "lexical sweep finds no behavioral read. Serialization/capture/restore/codec reads are",
        "reported as transport-only and intentionally do not clear the finding.",
        "",
        f"**Candidates:** {len(findings)}",
        "",
        "| Category | Field | Declaration | Writes | Transport-only reads |",
        "|---|---|---|---:|---:|",
    ]
    for item in findings:
        d = item.declaration
        out.append(
            f"| {item.category} | `{d.key}` | `{d.path}:{d.line}` | "
            f"{len(item.writes)} | {len(item.transport_reads)} |"
        )
    out.append("")
    for item in findings:
        d = item.declaration
        out.extend(
            [
                f"## `{d.key}`",
                "",
                f"- Declaration: `{d.path}:{d.line}`",
                f"- Category: `{item.category}`",
                "- Writes: " + (", ".join(f"`{x}`" for x in item.writes) or "none"),
                "- Transport-only reads: "
                + (", ".join(f"`{x}`" for x in item.transport_reads) or "none"),
                "",
            ]
        )
    return "\n".join(out)


def render_json(findings: Sequence[Finding]) -> str:
    payload = []
    for item in findings:
        d = item.declaration
        payload.append(
            {
                "field": d.key,
                "category": item.category,
                "declaration": f"{d.path}:{d.line}",
                "unity_serialized": d.unity_serialized,
                "carrier_type": d.carrier_type,
                "writes": list(item.writes),
                "transport_only_reads": list(item.transport_reads),
            }
        )
    return json.dumps(
        {"candidate_count": len(payload), "candidates": payload},
        indent=2,
        sort_keys=True,
    )


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path("."))
    parser.add_argument("--format", choices=("markdown", "json"), default="markdown")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--fail-on-findings", action="store_true")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    repo = args.repo.resolve()
    findings = find_candidates(repo)
    text = render_json(findings) if args.format == "json" else render_markdown(findings)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(text + "\n", encoding="utf-8")
    else:
        print(text)
    return 1 if args.fail_on_findings and findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
