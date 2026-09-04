from __future__ import annotations

import importlib.util
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
MODULE = ROOT / "tools" / "testing_strategy_audit.py"


def load_module():
    spec = importlib.util.spec_from_file_location("testing_strategy_audit_under_test", MODULE)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


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
