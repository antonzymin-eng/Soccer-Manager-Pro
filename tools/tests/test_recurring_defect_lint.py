# File: tools/tests/test_recurring_defect_lint.py
# Created: September 1, 2026
# Purpose: Regression fixtures for recurring-defect-lint's ERR-041-012
#          phantom-stream class, with MIXED positive/negative context rather
#          than isolated cases. The negation matching is a line-window
#          heuristic; isolated positives cannot show where that window
#          over-suppresses, so the known bound is pinned here explicitly
#          instead of being left as an undocumented gap.

import importlib.util
import tempfile
import unittest
from pathlib import Path

TOOL_PATH = Path(__file__).resolve().parents[1] / "recurring-defect-lint.py"
SPEC = importlib.util.spec_from_file_location("recurring_defect_lint", TOOL_PATH)
lint = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(lint)


class PhantomStreamContextTests(unittest.TestCase):
    """The class must flag positive claims and stay quiet on real negations."""

    def flags(self, body):
        with tempfile.TemporaryDirectory() as directory:
            docs = Path(directory) / "docs" / "tracking"
            docs.mkdir(parents=True)
            (docs / "probe.md").write_text("# Probe\n\n" + body + "\n", encoding="utf-8")
            findings = []
            lint.lint_phantom_stream(directory, findings)
            return [item for item in findings if item.severity == "ERROR"]

    # ---- positives that must always fire -------------------------------

    def test_positive_claims_fire(self):
        for body in (
                "#41 registers the `injuries.occurrence` stream at startup.",
                "The medical subsystem has a registered stream for FR-MD-027 draws.",
                "#41 uses `injuries.occurrence` directly.",
                "#41 MUST register a stream for medical draws.",
        ):
            self.assertTrue(self.flags(body), body)

    def test_positive_fires_when_a_negation_is_outside_the_window(self):
        """Mixed context: the negation is present but too far to apply."""
        self.assertTrue(self.flags(
            "The mixer does not convert #41 into a registered stream.\n\n"
            "filler.\n\nfiller.\n\nfiller.\n\n"
            "#41 registers the `injuries.occurrence` stream at startup."))

    def test_positive_fires_inside_a_neutral_list(self):
        """A list lead-in is inherited only when it actually negates."""
        self.assertTrue(self.flags(
            "This pass covers:\n\n"
            "- change domain tags;\n"
            "- #41 registers the `injuries.occurrence` stream."))

    # ---- negations that must stay quiet --------------------------------

    def test_markdown_emphasised_negation_is_silent(self):
        self.assertFalse(self.flags(
            "The #41 draw uses **no** registered stream, ERR-041-002."))

    def test_does_not_clause_is_silent(self):
        self.assertFalse(self.flags(
            "Centralizing the mixer does not convert #41 into a registered "
            "`DeterministicRngService` stream."))

    def test_bullet_inherits_a_negating_list_lead_in(self):
        """A non-goals bullet is elliptical: it means "we do NOT do this"."""
        self.assertFalse(self.flags(
            "This architectural pass does **not** itself:\n\n"
            "- change domain tags;\n"
            "- add a registered #41 RNG stream;\n"
            "- change save formats."))

    def test_wrapped_negation_still_suppresses(self):
        """Why the window exists: markdown prose wraps mid-sentence.

        Dropping the window to zero re-raises three genuinely wrapped
        negations elsewhere in docs/, so the window is load-bearing.
        """
        self.assertFalse(self.flags(
            "The design registers no\n"
            "stream for #41; the draw is keyed instead."))

    # ---- the known bound, pinned rather than hidden ---------------------

    def test_known_limitation_adjacent_negation_suppresses_a_positive(self):
        """A line-window heuristic cannot separate adjacent claims.

        A positive assertion within PHANTOM_NEGATION_WINDOW lines of an
        unrelated negation is suppressed. This PRE-DATES the September 2026
        negation work — the window has always been +/-2 — and is the price of
        matching negations that wrap. Narrowing the window to zero fixes this
        case and breaks three real ones, so the trade is deliberate.

        This test asserts the CURRENT behaviour. If a future pass separates
        claims by sentence rather than by line, invert it — do not delete it.
        """
        self.assertFalse(self.flags(
            "The mixer does not convert #41 into a registered stream.\n"
            "#41 registers the `injuries.occurrence` stream at startup."))

    def test_window_constant_matches_the_documented_bound(self):
        self.assertEqual(2, lint.PHANTOM_NEGATION_WINDOW)


if __name__ == "__main__":
    unittest.main()
