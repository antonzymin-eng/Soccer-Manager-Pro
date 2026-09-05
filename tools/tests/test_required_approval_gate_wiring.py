from __future__ import annotations

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
CI = ROOT / ".github" / "workflows" / "ci.yml"
HELPER = ROOT / "tools" / "run-testing-strategy-approval-audit.sh"


class RequiredApprovalGateWiringTests(unittest.TestCase):
    def test_required_spec_hygiene_job_owns_approval_transition_enforcement(self) -> None:
        text = CI.read_text(encoding="utf-8")
        start = text.index("  spec-hygiene:\n")
        end = text.index("\n  file-manifest-check:\n", start)
        block = text[start:end]

        self.assertIn("name: Spec hygiene checks", block)
        self.assertIn("fetch-depth: 0", block)
        self.assertIn("name: Testing Strategy approval-transition enforcement", block)
        self.assertIn("github.event_name == 'pull_request'", block)
        self.assertIn("TD_APPROVAL_BASE_REF: ${{ github.event.pull_request.base.sha }}", block)
        self.assertIn(
            'bash tools/run-testing-strategy-approval-audit.sh "$TD_APPROVAL_BASE_REF"',
            block,
        )

    def test_reusable_approval_helper_is_fail_closed_and_runs_both_auditors(self) -> None:
        text = HELPER.read_text(encoding="utf-8")
        self.assertIn('set -euo pipefail', text)
        self.assertIn('approval-transition audit requires the PR base commit SHA', text)
        self.assertIn('testing-strategy-approval-scope.py', text)
        self.assertIn('checklist-auditor.py', text)
        self.assertIn('--execute-checks', text)
        self.assertIn('spec5-schema-auditor.py', text)
        self.assertNotIn('--survey-only', text)


if __name__ == "__main__":
    unittest.main()
