#!/usr/bin/env python3
"""Find serialized/snapshot carrier fields written but never behaviorally read.

This is a conservative lexical candidate generator, not a C# compiler. Transport-only
reads in serialization/capture/restore/codec code do not count as behavioral consumption.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass, field
import json
from pathlib import Path
import re
from typing import Iterator, Sequence

FIELD_RE = re.compile(
    r"^\s*(?P<access>public|internal|private|protected)\s+"
    r"(?:(?:static|readonly|volatile|const|new|unsafe)\s+)*"
    r"(?P<type>[A-Za-z_][\w<>\[\],?.:]*)\s+(?P<name>[A-Za-z_]\w*)\s*(?:=[^;]*)?;\s*$"
)
TYPE_RE = re.compile(r"\b(?:class|struct|record)\s+(?:struct\s+|class\s+)?(?P<name>[A-Za-z_]\w*)")
METHOD_RE = re.compile(
    r"^\s*(?:(?:public|internal|private|protected|static|virtual|override|sealed|async|unsafe|new|partial)\s+)*"
    r"[A-Za-z_][\w<>\[\],?.:]*\s+(?P<name>[A-Za-z_]\w*)\s*\([^;]*\)\s*"
    r"(?:where\b[^{}]*)?(?P<brace>\{)?\s*$"
)
SERIALIZE_ATTR_RE = re.compile(r"\[\s*(?:UnityEngine\.)?SerializeField\s*\]")
ATTRIBUTE_ONLY_RE = re.compile(r"^\s*\[[^\]]+\]\s*$")
IDENT_RE_TEMPLATE = r"\b{}\b"
TEST_PARTS = {"test", "tests", "testing", "testdata", "test-data", "editor-tests"}
TRANSPORT_PATH_MARKERS = ("serializer", "serialization", "codec", "save")
TRANSPORT_METHOD_MARKERS = (
    "serialize", "deserialize", "capture", "restore", "write", "read",
    "encode", "decode", "pack", "unpack", "save", "load",
)
CARRIER_TYPE_SUFFIXES = ("Snapshot", "State", "SaveData", "SaveState", "Record", "Config")


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
    return not (lowered & TEST_PARTS) and not path.name.lower().endswith(("tests.cs", "test.cs"))


def iter_cs_files(repo: Path) -> Iterator[Path]:
    src = repo / "src"
    if not src.is_dir():
        return
    for path in sorted(src.rglob("*.cs")):
        if is_production_cs(path, src):
            yield path


def strip_line_comments(line: str) -> str:
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
        if not in_string and ch == '"':
            in_string = True
            verbatim = i > 0 and line[i - 1] == "@"
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
    type_stack: list[tuple[str, int]] = []
    pending_type: str | None = None
    pending_serialize = False
    depth = 0

    for idx, raw in enumerate(lines, start=1):
        line = strip_line_comments(raw)
        stripped = line.strip()
        while type_stack and depth < type_stack[-1][1]:
            type_stack.pop()
        match = TYPE_RE.search(line)
        if match:
            pending_type = match.group("name")
        if pending_type is not None and "{" in line:
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
                result.append(FieldDecl(
                    path=str(path.relative_to(repo)).replace("\\", "/"),
                    line=idx,
                    type_name=type_name,
                    field_type=fm.group("type"),
                    name=fm.group("name"),
                    unity_serialized=unity_serialized,
                    carrier_type=carrier_type,
                ))
            pending_serialize = False
        elif stripped and not ATTRIBUTE_ONLY_RE.match(line):
            if not (pending_serialize and stripped == "{"):
                pending_serialize = False

        depth += brace_delta(line)
        while type_stack and depth < type_stack[-1][1]:
            type_stack.pop()
    return result


def discover_fields(repo: Path) -> list[FieldDecl]:
    result: list[FieldDecl] = []
    for path in iter_cs_files(repo):
        result.extend(declarations_in_file(path, repo))
    return result


def method_names_by_line(lines: Sequence[str]) -> list[str | None]:
    current: str | None = None
    method_depth: int | None = None
    pending: str | None = None
    depth = 0
    result: list[str | None] = []
    for raw in lines:
        line = strip_line_comments(raw)
        if current is not None and method_depth is not None and depth < method_depth:
            current = None
            method_depth = None
        mm = METHOD_RE.match(line)
        if mm and not line.strip().endswith(";"):
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


def initializer_types_by_line(lines: Sequence[str], known_types: set[str]) -> list[str | None]:
    if not known_types:
        return [None] * len(lines)
    names = "|".join(re.escape(x) for x in sorted(known_types, key=lambda x: (-len(x), x)))
    pattern = re.compile(rf"\bnew\s+(?:[A-Za-z_]\w*\.)*(?P<type>{names})\b")
    stack: list[tuple[str, int]] = []
    pending: str | None = None
    depth = 0
    result: list[str | None] = []
    for raw in lines:
        line = strip_line_comments(raw)
        while stack and depth < stack[-1][1]:
            stack.pop()
        match = pattern.search(line)
        if match:
            pending = match.group("type")
        if pending is not None and "{" in line:
            stack.append((pending, depth + 1))
            pending = None
        result.append(stack[-1][0] if stack else None)
        depth += brace_delta(line)
        while stack and depth < stack[-1][1]:
            stack.pop()
    return result


def strip_type(type_name: str) -> str:
    value = type_name.rstrip("?")
    while value.endswith("[]"):
        value = value[:-2]
    return value.rsplit(".", 1)[-1]


def learn_variable_types(code: str, known_types: set[str], mapping: dict[str, str]) -> None:
    """Learn simple local/parameter variable types for known carrier/snapshot types."""
    for type_name in known_types:
        # Handles parameters, locals, ref readonly locals, and arrays.
        pattern = re.compile(
            rf"(?:\b(?:ref|readonly|in|out)\s+)*\b{re.escape(type_name)}(?P<array>\[\])?\s+(?P<var>[A-Za-z_]\w*)\b"
        )
        for match in pattern.finditer(code):
            mapping[match.group("var")] = type_name + ("[]" if match.group("array") else "")


def receiver_type_for_field(
    code: str,
    field_name: str,
    variable_types: dict[str, str],
    field_types: dict[tuple[str, str], str],
) -> str | None:
    """Resolve common `receiver.Field` and `root.Member[i].Field` shapes lexically."""
    # root.Member[index].Field: resolve root type, then member element type.
    chain = re.search(
        rf"\b(?P<root>[A-Za-z_]\w*)\s*\.\s*(?P<member>[A-Za-z_]\w*)\s*\[[^\]]+\]\s*\.\s*{re.escape(field_name)}\b",
        code,
    )
    if chain:
        root_type = strip_type(variable_types.get(chain.group("root"), ""))
        member_type = field_types.get((root_type, chain.group("member")))
        if member_type:
            return strip_type(member_type)

    # array[index].Field.
    array_access = re.search(
        rf"\b(?P<root>[A-Za-z_]\w*)\s*\[[^\]]+\]\s*\.\s*{re.escape(field_name)}\b",
        code,
    )
    if array_access:
        root_type = variable_types.get(array_access.group("root"))
        if root_type and root_type.endswith("[]"):
            return strip_type(root_type)

    # receiver.Field.
    simple = re.search(
        rf"\b(?P<root>[A-Za-z_]\w*)\s*\.\s*{re.escape(field_name)}\b",
        code,
    )
    if simple:
        root_type = variable_types.get(simple.group("root"))
        if root_type:
            return strip_type(root_type)
    return None


def is_transport_context(path: str, method: str | None, line: str) -> bool:
    base = Path(path).stem.lower()
    if any(marker in base for marker in TRANSPORT_PATH_MARKERS):
        return True
    if method and any(marker in method.lower() for marker in TRANSPORT_METHOD_MARKERS):
        return True
    lower = line.lower()
    return (
        "canonicalserializer" in lower
        or "binarywriter" in lower
        or "binaryreader" in lower
        or ".write(" in lower
        or (".read" in lower and "(" in lower)
    )


def occurrence_kind(line: str, field_name: str) -> str | None:
    code = strip_line_comments(line)
    if not re.search(IDENT_RE_TEMPLATE.format(re.escape(field_name)), code):
        return None
    if FIELD_RE.match(code) and re.search(rf"\b{re.escape(field_name)}\b\s*(?:=|;)", code):
        return None
    if re.search(rf"\b{re.escape(field_name)}\b\s*(?:\+\+|--|[+\-*/%&|^]=)", code):
        return "read"
    if re.search(rf"\bref\s+(?:[\w.]+\.)?{re.escape(field_name)}\b", code):
        return "read"
    if re.search(rf"\bout\s+(?:[\w.]+\.)?{re.escape(field_name)}\b", code):
        return "write"
    lhs = re.search(
        rf"(?<![=!<>])\b(?:[A-Za-z_]\w*(?:\[[^\]]+\])?\.)*{re.escape(field_name)}\s*=(?!=)", code
    )
    if lhs:
        rhs = code[lhs.end():]
        if re.search(IDENT_RE_TEMPLATE.format(re.escape(field_name)), rhs):
            return "read"
        return "write"
    return "read"


def collect_evidence(repo: Path, fields: Sequence[FieldDecl]) -> dict[str, Evidence]:
    by_name: dict[str, list[FieldDecl]] = {}
    for decl in fields:
        by_name.setdefault(decl.name, []).append(decl)
    evidence = {decl.key: Evidence() for decl in fields}
    if not by_name:
        return evidence

    known_types = {decl.type_name for decl in fields}
    field_types = {(decl.type_name, decl.name): decl.field_type for decl in fields}
    names = "|".join(re.escape(x) for x in sorted(by_name, key=lambda x: (-len(x), x)))
    name_pattern = re.compile(rf"\b(?:{names})\b")

    for path in iter_cs_files(repo):
        rel = str(path.relative_to(repo)).replace("\\", "/")
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        methods = method_names_by_line(lines)
        initializer_types = initializer_types_by_line(lines, known_types)
        variable_types: dict[str, str] = {}

        for lineno, (raw, method, initializer_type) in enumerate(zip(lines, methods, initializer_types), start=1):
            code = strip_line_comments(raw)
            learn_variable_types(code, known_types, variable_types)
            present = {match.group(0) for match in name_pattern.finditer(code)}
            for name in present:
                kind = occurrence_kind(raw, name)
                if kind is None:
                    continue
                decls = by_name[name]
                location = f"{rel}:{lineno}"
                receiver_type = receiver_type_for_field(code, name, variable_types, field_types)
                for decl in decls:
                    same_file = rel == decl.path
                    applies = False
                    if receiver_type is not None:
                        applies = receiver_type == decl.type_name
                    elif initializer_type is not None:
                        applies = initializer_type == decl.type_name
                    elif len(decls) == 1:
                        applies = True
                    elif same_file:
                        # Unqualified access inside the declaration file is the least-bad
                        # fallback when the name is ambiguous and no receiver can be resolved.
                        applies = True
                    if not applies:
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
        if decl.unity_serialized and not writes:
            writes.append("<Unity serialization>")
        if not writes or ev.behavioral_reads:
            continue
        findings.append(Finding(
            declaration=decl,
            writes=tuple(sorted(set(writes))),
            transport_reads=tuple(sorted(set(ev.transport_reads))),
        ))
    return sorted(findings, key=lambda item: (item.declaration.path, item.declaration.line, item.declaration.name))


def render_markdown(findings: Sequence[Finding]) -> str:
    out = [
        "# Static unread serialized/snapshot field sweep", "",
        "Diagnostic candidates only. A field is listed when production code writes it and the",
        "lexical sweep finds no behavioral read. Serialization/capture/restore/codec reads are",
        "reported as transport-only and intentionally do not clear the finding.", "",
        f"**Candidates:** {len(findings)}", "",
        "| Category | Field | Declaration | Writes | Transport-only reads |",
        "|---|---|---|---:|---:|",
    ]
    for item in findings:
        d = item.declaration
        out.append(
            f"| {item.category} | `{d.key}` | `{d.path}:{d.line}` | {len(item.writes)} | {len(item.transport_reads)} |"
        )
    out.append("")
    for item in findings:
        d = item.declaration
        out.extend([
            f"## `{d.key}`", "",
            f"- Declaration: `{d.path}:{d.line}`",
            f"- Category: `{item.category}`",
            "- Writes: " + (", ".join(f"`{x}`" for x in item.writes) or "none"),
            "- Transport-only reads: " + (", ".join(f"`{x}`" for x in item.transport_reads) or "none"),
            "",
        ])
    return "\n".join(out)


def render_json(findings: Sequence[Finding]) -> str:
    payload = []
    for item in findings:
        d = item.declaration
        payload.append({
            "field": d.key,
            "category": item.category,
            "declaration": f"{d.path}:{d.line}",
            "unity_serialized": d.unity_serialized,
            "carrier_type": d.carrier_type,
            "writes": list(item.writes),
            "transport_only_reads": list(item.transport_reads),
        })
    return json.dumps({"candidate_count": len(payload), "candidates": payload}, indent=2, sort_keys=True)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path("."))
    parser.add_argument("--format", choices=("markdown", "json"), default="markdown")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--fail-on-findings", action="store_true")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    findings = find_candidates(args.repo.resolve())
    text = render_json(findings) if args.format == "json" else render_markdown(findings)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(text + "\n", encoding="utf-8")
    else:
        print(text)
    return 1 if args.fail_on_findings and findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
