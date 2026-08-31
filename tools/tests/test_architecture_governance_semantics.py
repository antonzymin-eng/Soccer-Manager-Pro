# File: tools/tests/test_architecture_governance_semantics.py
# Created: August 31, 2026
# Purpose: A2 fixtures for selector/identity/activation/KD-W1 semantics.

import importlib.util
import unittest
from pathlib import Path

TOOL_PATH = Path(__file__).resolve().parents[1] / "architecture-governance" / "reference_semantics.py"
SPEC = importlib.util.spec_from_file_location("architecture_governance_reference_semantics", TOOL_PATH)
sem = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(sem)


def method(name, params, symbol_key, is_static=False):
    return {
        "selector": {
            "assembly": "Example.Runtime",
            "kind": "method",
            "containing_type_id": "Example.Component",
            "member_name": name,
            "parameter_type_ids": params,
            "generic_arity": 0,
            "is_static": is_static,
        },
        "symbol_key": symbol_key,
    }


def field(name, symbol_key, value):
    return {
        "selector": {
            "assembly": "Example.Runtime",
            "kind": "field",
            "containing_type_id": "Example.Config",
            "member_name": name,
            "is_static": True,
        },
        "symbol_key": symbol_key,
        "value": value,
    }


class SelectorTests(unittest.TestCase):
    def test_overloads_resolve_by_parameter_types(self):
        no_arg = method("Start", [], "M:Start()")
        int_arg = method("Start", ["System.Int32"], "M:Start(Int32)")
        resolved = sem.resolve_selector(int_arg["selector"], [no_arg, int_arg])
        self.assertEqual("M:Start(Int32)", resolved["symbol_key"])
        self.assertNotEqual(
            sem.selector_key(no_arg["selector"]),
            sem.selector_key(int_arg["selector"]),
        )

    def test_static_and_instance_members_are_distinct(self):
        static = method("Create", [], "M:Create:static", True)
        instance = method("Create", [], "M:Create:instance", False)
        self.assertEqual(
            "M:Create:static",
            sem.resolve_selector(static["selector"], [static, instance])["symbol_key"],
        )

    def test_missing_and_ambiguous_resolution_fail_closed(self):
        fact = method("Start", [], "M:Start()")
        with self.assertRaises(sem.SelectorError):
            sem.resolve_selector(method("Stop", [], "unused")["selector"], [fact])
        with self.assertRaises(sem.SelectorError):
            sem.resolve_selector(fact["selector"], [fact, dict(fact)])

    def test_unknown_selector_fields_are_rejected(self):
        fact = method("Start", [], "M:Start()")
        bad = dict(fact["selector"])
        bad["source_path"] = "src/Component.cs"
        with self.assertRaises(sem.SelectorError):
            sem.normalize_selector(bad)


class IdentityTests(unittest.TestCase):
    def test_component_id_survives_move_via_history(self):
        old = method("Start", [], "M:Old.Start")
        moved = {
            "selector": {**old["selector"], "containing_type_id": "Example.NewComponent"},
            "symbol_key": "M:New.Start",
        }
        records = [{
            "component_id": "component:match-host",
            "current_selector": moved["selector"],
            "selector_history": [{
                "selector": old["selector"],
                "superseded_reason": "type moved",
            }],
        }]
        self.assertEqual(
            {"component:match-host": "M:New.Start"},
            sem.validate_component_identities(records, [moved]),
        )

    def test_selector_cannot_be_claimed_by_two_components(self):
        fact = method("Start", [], "M:Start()")
        other = method("Other", [], "M:Other")
        records = [
            {"component_id": "component:a", "current_selector": fact["selector"]},
            {
                "component_id": "component:b",
                "current_selector": other["selector"],
                "selector_history": [{"selector": fact["selector"]}],
            },
        ]
        with self.assertRaises(sem.IdentityError):
            sem.validate_component_identities(records, [fact, other])


class ActivationTests(unittest.TestCase):
    def setUp(self):
        self.disabled = field(
            "TackleContactRadiusM",
            "F:TackleContactRadiusM",
            {"value_type": "number", "value": 0},
        )
        self.other = field(
            "PressureThreshold",
            "F:PressureThreshold",
            {"value_type": "number", "value": 0.4},
        )

    def contract(self):
        return {
            "component_id": "component:tackling",
            "activation_state": "intentionally-disabled",
            "activation_owner": "match-engine",
            "decision_ref": "KD-TACKLE-001",
            "disable_anchor": {
                "selector": self.disabled["selector"],
                "operator": "equals",
                "expected": {"value_type": "number", "value": 0},
            },
            "reactivation_condition": "integration contract is completed",
            "tuning_surface_selectors": [
                self.disabled["selector"],
                self.other["selector"],
            ],
        }

    def test_disable_anchor_must_resolve_and_match(self):
        result = sem.evaluate_disable_anchor(self.contract(), [self.disabled, self.other])
        self.assertTrue(result["passed"])
        drifted = dict(self.disabled)
        drifted["value"] = {"value_type": "number", "value": 0.1}
        self.assertFalse(
            sem.evaluate_disable_anchor(self.contract(), [drifted, self.other])["passed"]
        )

    def test_intentionally_disabled_requires_decision_metadata(self):
        contract = self.contract()
        del contract["decision_ref"]
        with self.assertRaises(sem.ActivationError):
            sem.validate_activation_contract(contract)

    def test_kd_w1_exact_exception_scope_only(self):
        violations = sem.kd_w1_violations(
            [self.disabled["selector"], self.other["selector"]],
            [self.contract()],
            [self.disabled, self.other],
            exception_scopes=[{
                "component_id": "component:tackling",
                "approval_ref": "EX-TS-001",
                "tuning_surface_selectors": [self.disabled["selector"]],
            }],
        )
        self.assertEqual(1, len(violations))
        self.assertEqual(["F:PressureThreshold"], violations[0]["changed_symbol_keys"])

    def test_kd_w1_allows_active_owner(self):
        contract = self.contract()
        contract["activation_state"] = "active"
        self.assertEqual(
            [],
            sem.kd_w1_violations(
                [self.other["selector"]],
                [contract],
                [self.disabled, self.other],
            ),
        )


if __name__ == "__main__":
    unittest.main()
