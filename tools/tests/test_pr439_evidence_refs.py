from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHECKER = ROOT / "tools" / "dotnet-ci" / "check_pr439_evidence_refs.py"
sys.path.insert(0, str(CHECKER.parent))
import check_pr439_evidence_refs as verifier  # noqa: E402


class Pr439EvidenceRefArchiveTests(unittest.TestCase):
    def test_current_archive_integrity_passes(self) -> None:
        completed = subprocess.run(
            [sys.executable, str(CHECKER), "--repo", str(ROOT), "--mode", "archive"],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertIn("PR439 evidence-ref reconciliation: PASS", completed.stdout)

    def test_only_complete_pre_or_post_delete_topologies_are_allowed(self) -> None:
        refs = {"evidence/pr439-one", "evidence/pr439-two"}
        self.assertEqual(("pre-delete", []), verifier.resolve_live_state(refs, refs, True, "auto"))
        self.assertEqual(("post-delete", []), verifier.resolve_live_state(set(), refs, True, "auto"))
        for actual in ({"evidence/pr439-one"}, {"evidence/pr439-unknown"}):
            state, errors = verifier.resolve_live_state(actual, refs, True, "auto")
            self.assertIsNone(state)
            self.assertTrue(errors)

    def test_post_delete_requires_explicit_authorization(self) -> None:
        refs = {"evidence/pr439-one", "evidence/pr439-two"}
        for requested in ("auto", "post-delete"):
            state, errors = verifier.resolve_live_state(set(), refs, False, requested)
            self.assertIsNone(state)
            self.assertTrue(errors)
        state, errors = verifier.resolve_live_state(refs, refs, True, "post-delete")
        self.assertIsNone(state)
        self.assertTrue(errors)


if __name__ == "__main__":
    unittest.main()
