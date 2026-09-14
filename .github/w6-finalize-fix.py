from pathlib import Path
p = Path('.github/w6-finalize.py')
s = p.read_text()
old = """'''        /// Transitions the ball to Controlled state.\n        /// Called by the agent system after CheckPossession returns a valid agent.\n''',\n'''        /// Transitions the ball to Controlled state.\n        /// Called by the host after its possession mechanic adjudicates a successful touch;\n        /// CheckPossession remains available to hosts that use Ball Physics acquisition geometry.\n''', p)"""
new = """'''        /// Transitions ball to Controlled state. Called by agent system after CheckPossession.\n        /// Does NOT record which agent has possession (Option B — agent system owns that).\n''',\n'''        /// Transitions ball to Controlled state after the host possession mechanic adjudicates\n        /// a successful touch. CheckPossession remains available to hosts using Ball Physics'\n        /// own acquisition geometry. Does NOT record which agent has possession (Option B).\n''', p)"""
if s.count(old) != 1:
    raise SystemExit(f'expected stale finalizer anchor once, found {s.count(old)}')
s = s.replace(old, new, 1)
old_status = "s = one(s, '**Status:** IMPLEMENTATION / VALIDATION', '**Status:** WIRED / VALIDATED (PR #412)', p)"
new_status = "s = one(s, '**Status:** IMPLEMENTATION / VALIDATION  ', '**Status:** WIRED / VALIDATED (PR #412)', p)"
if s.count(old_status) != 1:
    raise SystemExit(f'expected W6 status finalizer line once, found {s.count(old_status)}')
s = s.replace(old_status, new_status, 1)
p.write_text(s)
