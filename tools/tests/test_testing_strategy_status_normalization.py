from __future__ import annotations

import importlib
from pathlib import Path
import sys
import unittest


ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "tools"


def load_module():
    tools = str(TOOLS)
    if tools not in sys.path:
        sys.path.insert(0, tools)
    return importlib.import_module("testing_strategy_audit")


class TestingStrategyStatusNormalizationTests(unittest.TestCase):
    def test_supported_decorative_approved_forms_are_approved(self) -> None:
        module = load_module()
        for value in (
            "APPROVED",
            "`APPROVED`",
            "✅ APPROVED",
            "✅ `APPROVED` (May 15, 2026)",
        ):
            with self.subTest(value=value):
                self.assertTrue(module.is_approved_status(value))

    def test_amendment_draft_is_not_approved(self) -> None:
        module = load_module()
        self.assertFalse(module.is_approved_status("✅ APPROVED — AMENDMENT DRAFT"))


if __name__ == "__main__":
    unittest.main()
