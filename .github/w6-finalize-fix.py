from pathlib import Path
p = Path('.github/w6-finalize.py')
s = p.read_text()
old = """'''        /// Transitions the ball to Controlled state.\n        /// Called by the agent system after CheckPossession returns a valid agent.\n''',\n'''        /// Transitions the ball to Controlled state.\n        /// Called by the host after its possession mechanic adjudicates a successful touch;\n        /// CheckPossession remains available to hosts that use Ball Physics acquisition geometry.\n''', p)"""
new = """'''        /// Transitions ball to Controlled state. Called by agent system after CheckPossession.\n        /// Does NOT record which agent has possession (Option B — agent system owns that).\n''',\n'''        /// Transitions ball to Controlled state after the host possession mechanic adjudicates\n        /// a successful touch. CheckPossession remains available to hosts using Ball Physics'\n        /// own acquisition geometry. Does NOT record which agent has possession (Option B).\n''', p)"""
if s.count(old) != 1:
    raise SystemExit(f'expected stale finalizer anchor once, found {s.count(old)}')
p.write_text(s.replace(old, new, 1))
