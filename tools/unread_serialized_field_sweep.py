#!/usr/bin/env python3
"""Find serialized/snapshot carrier fields written but never behaviorally read.

Conservative lexical candidate generator, not a C# compiler. Transport-only reads in
serialization/capture/restore/codec code do not count as behavioral consumption.
"""
from __future__ import annotations
import argparse
from dataclasses import dataclass, field
import json
from pathlib import Path
import re
from typing import Iterator, Sequence

FIELD_RE = re.compile(r"^\s*(?P<access>public|internal|private|protected)\s+(?:(?:static|readonly|volatile|const|new|unsafe)\s+)*(?P<type>[A-Za-z_][\w<>\[\],?.:]*)\s+(?P<name>[A-Za-z_]\w*)\s*(?:=[^;]*)?;\s*$")
TYPE_RE = re.compile(r"\b(?:class|struct|record)\s+(?:struct\s+|class\s+)?(?P<name>[A-Za-z_]\w*)")
METHOD_RE = re.compile(r"^\s*(?:(?:public|internal|private|protected|static|virtual|override|sealed|async|unsafe|new|partial)\s+)*[A-Za-z_][\w<>\[\],?.:]*\s+(?P<name>[A-Za-z_]\w*)\s*\([^;]*\)\s*(?:where\b[^{}]*)?(?P<brace>\{)?\s*$")
SERIALIZE_ATTR_RE = re.compile(r"\[\s*(?:UnityEngine\.)?SerializeField\s*\]")
ATTRIBUTE_ONLY_RE = re.compile(r"^\s*\[[^\]]+\]\s*$")
TEST_PARTS = {"test", "tests", "testing", "testdata", "test-data", "editor-tests"}
TRANSPORT_PATH_MARKERS = ("serializer", "serialization", "codec", "save")
TRANSPORT_METHOD_MARKERS = ("serialize", "deserialize", "capture", "restore", "write", "read", "encode", "decode", "pack", "unpack", "save", "load")
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
    def key(self) -> str: return f"{self.type_name}.{self.name}"

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
        if self.declaration.unity_serialized: return "unity-serialized"
        if self.transport_reads: return "transport-only"
        return "snapshot/state-carrier"

def is_production_cs(path: Path, src_root: Path) -> bool:
    try: rel = path.relative_to(src_root)
    except ValueError: return False
    lowered = {p.lower() for p in rel.parts}
    return path.suffix == ".cs" and not (lowered & TEST_PARTS) and not path.name.lower().endswith(("tests.cs", "test.cs"))

def iter_cs_files(repo: Path) -> Iterator[Path]:
    src = repo / "src"
    if not src.is_dir(): return
    for path in sorted(src.rglob("*.cs")):
        if is_production_cs(path, src): yield path

def strip_line_comments(line: str) -> str:
    out=[]; in_string=False; verbatim=False; escaped=False; i=0
    while i < len(line):
        ch=line[i]; nxt=line[i+1] if i+1 < len(line) else ""
        if not in_string and ch=="/" and nxt=="/": break
        if not in_string and ch=='"':
            in_string=True; verbatim=i>0 and line[i-1]=="@"; out.append(ch)
        elif in_string:
            out.append(ch)
            if verbatim and ch=='"' and nxt=='"': out.append(nxt); i+=1
            elif ch=='"' and (verbatim or not escaped): in_string=False; verbatim=False
            escaped=(ch=="\\" and not escaped) if not verbatim else False; i+=1; continue
        else: out.append(ch)
        escaped=(ch=="\\" and not escaped) if in_string and not verbatim else False; i+=1
    return "".join(out)

def brace_delta(line: str) -> int:
    clean=strip_line_comments(line); return clean.count("{")-clean.count("}")

def declarations_in_file(path: Path, repo: Path) -> list[FieldDecl]:
    lines=path.read_text(encoding="utf-8-sig").splitlines(); result=[]; stack=[]; pending_type=None; pending_serialize=False; depth=0
    for idx, raw in enumerate(lines,1):
        line=strip_line_comments(raw); stripped=line.strip()
        while stack and depth < stack[-1][1]: stack.pop()
        tm=TYPE_RE.search(line)
        if tm: pending_type=tm.group("name")
        if pending_type is not None and "{" in line: stack.append((pending_type,depth+1)); pending_type=None
        if SERIALIZE_ATTR_RE.search(line): pending_serialize=True
        fm=FIELD_RE.match(line)
        if fm and stack:
            tn=stack[-1][0]; us=pending_serialize or bool(SERIALIZE_ATTR_RE.search(line)); carrier=tn.endswith(CARRIER_TYPE_SUFFIXES)
            if us or carrier:
                result.append(FieldDecl(str(path.relative_to(repo)).replace("\\","/"),idx,tn,fm.group("type"),fm.group("name"),us,carrier))
            pending_serialize=False
        elif stripped and not ATTRIBUTE_ONLY_RE.match(line):
            if not (pending_serialize and stripped=="{"): pending_serialize=False
        depth += brace_delta(line)
        while stack and depth < stack[-1][1]: stack.pop()
    return result

def discover_fields(repo: Path) -> list[FieldDecl]:
    out=[]
    for path in iter_cs_files(repo): out.extend(declarations_in_file(path,repo))
    return out

def method_names_by_line(lines: Sequence[str]) -> list[str|None]:
    current=None; method_depth=None; pending=None; depth=0; out=[]
    for raw in lines:
        line=strip_line_comments(raw)
        if current is not None and method_depth is not None and depth < method_depth: current=None; method_depth=None
        mm=METHOD_RE.match(line)
        if mm and not line.strip().endswith(";"): pending=mm.group("name")
        if pending is not None and "{" in line: current=pending; method_depth=depth+1; pending=None
        out.append(current); depth += brace_delta(line)
        if current is not None and method_depth is not None and depth < method_depth: current=None; method_depth=None
    return out

def initializer_types_by_line(lines: Sequence[str], known_types: set[str]) -> list[str|None]:
    if not known_types: return [None]*len(lines)
    names="|".join(re.escape(x) for x in sorted(known_types,key=lambda x:(-len(x),x)))
    pattern=re.compile(rf"\bnew\s+(?:[A-Za-z_]\w*\.)*(?P<type>{names})\b")
    stack=[]; pending=None; depth=0; out=[]
    for raw in lines:
        line=strip_line_comments(raw)
        while stack and depth < stack[-1][1]: stack.pop()
        m=pattern.search(line)
        if m: pending=m.group("type")
        if pending is not None and "{" in line: stack.append((pending,depth+1)); pending=None
        out.append(stack[-1][0] if stack else None); depth += brace_delta(line)
        while stack and depth < stack[-1][1]: stack.pop()
    return out

def strip_type(value: str) -> str:
    value=value.rstrip("?")
    while value.endswith("[]"): value=value[:-2]
    return value.rsplit(".",1)[-1]

def build_variable_pattern(known_types: set[str]) -> re.Pattern[str]:
    names="|".join(re.escape(x) for x in sorted(known_types,key=lambda x:(-len(x),x)))
    return re.compile(rf"(?:\b(?:ref|readonly|in|out)\s+)*\b(?P<type>{names})(?P<array>\[\])?\s+(?P<var>[A-Za-z_]\w*)\b")

def learn_variable_types(code: str, pattern: re.Pattern[str], mapping: dict[str,str]) -> None:
    for m in pattern.finditer(code): mapping[m.group("var")]=m.group("type")+("[]" if m.group("array") else "")

def receiver_type_for_field(code: str, name: str, vars: dict[str,str], field_types: dict[tuple[str,str],str]) -> str|None:
    m=re.search(rf"\b(?P<root>[A-Za-z_]\w*)\s*\.\s*(?P<member>[A-Za-z_]\w*)\s*\[[^\]]+\]\s*\.\s*{re.escape(name)}\b",code)
    if m:
        root=strip_type(vars.get(m.group("root"),"")); member=field_types.get((root,m.group("member")))
        if member: return strip_type(member)
    m=re.search(rf"\b(?P<root>[A-Za-z_]\w*)\s*\[[^\]]+\]\s*\.\s*{re.escape(name)}\b",code)
    if m:
        rt=vars.get(m.group("root"));
        if rt and rt.endswith("[]"): return strip_type(rt)
    m=re.search(rf"\b(?P<root>[A-Za-z_]\w*)\s*\.\s*{re.escape(name)}\b",code)
    if m and m.group("root") in vars: return strip_type(vars[m.group("root")])
    return None

def is_transport_context(path: str, method: str|None, line: str) -> bool:
    base=Path(path).stem.lower()
    if any(x in base for x in TRANSPORT_PATH_MARKERS): return True
    if method and any(x in method.lower() for x in TRANSPORT_METHOD_MARKERS): return True
    low=line.lower(); return "canonicalserializer" in low or "binarywriter" in low or "binaryreader" in low or ".write(" in low or (".read" in low and "(" in low)

def occurrence_kind(line: str, name: str) -> str|None:
    code=strip_line_comments(line)
    if not re.search(rf"\b{re.escape(name)}\b",code): return None
    if FIELD_RE.match(code) and re.search(rf"\b{re.escape(name)}\b\s*(?:=|;)",code): return None
    if re.search(rf"\b{re.escape(name)}\b\s*(?:\+\+|--|[+\-*/%&|^]=)",code): return "read"
    if re.search(rf"\bref\s+(?:[\w.]+\.)?{re.escape(name)}\b",code): return "read"
    if re.search(rf"\bout\s+(?:[\w.]+\.)?{re.escape(name)}\b",code): return "write"
    lhs=re.search(rf"(?<![=!<>])\b(?:[A-Za-z_]\w*(?:\[[^\]]+\])?\.)*{re.escape(name)}\s*=(?!=)",code)
    if lhs:
        if re.search(rf"\b{re.escape(name)}\b",code[lhs.end():]): return "read"
        return "write"
    return "read"

def collect_evidence(repo: Path, fields: Sequence[FieldDecl]) -> dict[str,Evidence]:
    by_name={}
    for d in fields: by_name.setdefault(d.name,[]).append(d)
    ev={d.key:Evidence() for d in fields}
    if not by_name: return ev
    known={d.type_name for d in fields}; field_types={(d.type_name,d.name):d.field_type for d in fields}
    names="|".join(re.escape(x) for x in sorted(by_name,key=lambda x:(-len(x),x))); name_pattern=re.compile(rf"\b(?:{names})\b"); var_pattern=build_variable_pattern(known)
    for path in iter_cs_files(repo):
        rel=str(path.relative_to(repo)).replace("\\","/"); lines=path.read_text(encoding="utf-8-sig").splitlines(); methods=method_names_by_line(lines); init_types=initializer_types_by_line(lines,known); vars={}
        for lineno,(raw,method,init_type) in enumerate(zip(lines,methods,init_types),1):
            code=strip_line_comments(raw); learn_variable_types(code,var_pattern,vars)
            for name in {m.group(0) for m in name_pattern.finditer(code)}:
                kind=occurrence_kind(raw,name)
                if kind is None: continue
                decls=by_name[name]; location=f"{rel}:{lineno}"; receiver=receiver_type_for_field(code,name,vars,field_types)
                for d in decls:
                    applies=(receiver==d.type_name) if receiver is not None else ((init_type==d.type_name) if init_type is not None else (len(decls)==1 or rel==d.path))
                    if not applies: continue
                    target=ev[d.key]
                    if kind=="write": target.writes.append(location)
                    elif is_transport_context(rel,method,raw): target.transport_reads.append(location)
                    else: target.behavioral_reads.append(location)
    return ev

def find_candidates(repo: Path) -> list[Finding]:
    fields=discover_fields(repo); evidence=collect_evidence(repo,fields); out=[]
    for d in fields:
        e=evidence[d.key]; writes=list(e.writes)
        if d.unity_serialized and not writes: writes.append("<Unity serialization>")
        if writes and not e.behavioral_reads: out.append(Finding(d,tuple(sorted(set(writes))),tuple(sorted(set(e.transport_reads)))))
    return sorted(out,key=lambda x:(x.declaration.path,x.declaration.line,x.declaration.name))

def render_markdown(findings: Sequence[Finding]) -> str:
    out=["# Static unread serialized/snapshot field sweep","","Diagnostic candidates only. A field is listed when production code writes it and the","lexical sweep finds no behavioral read. Serialization/capture/restore/codec reads are","reported as transport-only and intentionally do not clear the finding.","",f"**Candidates:** {len(findings)}","","| Category | Field | Declaration | Writes | Transport-only reads |","|---|---|---|---:|---:|"]
    for f in findings:
        d=f.declaration; out.append(f"| {f.category} | `{d.key}` | `{d.path}:{d.line}` | {len(f.writes)} | {len(f.transport_reads)} |")
    out.append("")
    for f in findings:
        d=f.declaration; out += [f"## `{d.key}`","",f"- Declaration: `{d.path}:{d.line}`",f"- Category: `{f.category}`","- Writes: "+(", ".join(f"`{x}`" for x in f.writes) or "none"),"- Transport-only reads: "+(", ".join(f"`{x}`" for x in f.transport_reads) or "none"),""]
    return "\n".join(out)

def render_json(findings: Sequence[Finding]) -> str:
    rows=[]
    for f in findings:
        d=f.declaration; rows.append({"field":d.key,"category":f.category,"declaration":f"{d.path}:{d.line}","unity_serialized":d.unity_serialized,"carrier_type":d.carrier_type,"writes":list(f.writes),"transport_only_reads":list(f.transport_reads)})
    return json.dumps({"candidate_count":len(rows),"candidates":rows},indent=2,sort_keys=True)

def main(argv: Sequence[str]|None=None) -> int:
    p=argparse.ArgumentParser(description=__doc__); p.add_argument("--repo",type=Path,default=Path(".")); p.add_argument("--format",choices=("markdown","json"),default="markdown"); p.add_argument("--output",type=Path); p.add_argument("--fail-on-findings",action="store_true"); a=p.parse_args(argv)
    findings=find_candidates(a.repo.resolve()); text=render_json(findings) if a.format=="json" else render_markdown(findings)
    if a.output: a.output.parent.mkdir(parents=True,exist_ok=True); a.output.write_text(text+"\n",encoding="utf-8")
    else: print(text)
    return 1 if a.fail_on_findings and findings else 0
if __name__=="__main__": raise SystemExit(main())
