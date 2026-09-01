# File: tools/tests/test_architecture_governance_semantics.py
# Created: August 31, 2026
# Purpose: A2 fixtures for selector/identity/activation, applicability, and proof freshness semantics.

import ast
import copy
import importlib.util
import json
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


def namespace_fact(name, symbol_key):
    return {
        "selector": {
            "assembly": "Example.Runtime",
            "kind": "namespace",
            "namespace": name,
        },
        "symbol_key": symbol_key,
    }


def type_fact(type_id, symbol_key):
    return {
        "selector": {
            "assembly": "Example.Runtime",
            "kind": "type",
            "type_id": type_id,
        },
        "symbol_key": symbol_key,
    }


def constructor(type_id, params, symbol_key, is_static=False):
    return {
        "selector": {
            "assembly": "Example.Runtime",
            "kind": "constructor",
            "containing_type_id": type_id,
            "parameter_type_ids": params,
            "is_static": is_static,
        },
        "symbol_key": symbol_key,
    }


def property_fact(name, params, symbol_key, is_static=False):
    return {
        "selector": {
            "assembly": "Example.Runtime",
            "kind": "property",
            "containing_type_id": "Example.Component",
            "member_name": name,
            "parameter_type_ids": params,
            "is_static": is_static,
        },
        "symbol_key": symbol_key,
    }


def event_fact(name, symbol_key, is_static=False):
    return {
        "selector": {
            "assembly": "Example.Runtime",
            "kind": "event",
            "containing_type_id": "Example.Component",
            "member_name": name,
            "is_static": is_static,
        },
        "symbol_key": symbol_key,
    }


class SelectorTests(unittest.TestCase):
    def test_unhashable_selector_kind_is_selector_error(self):
        with self.assertRaises(sem.SelectorError):
            sem.normalize_selector({
                "assembly": "Example.Runtime",
                "kind": ["method"],
            })

    def test_reference_semantics_version_is_pinned(self):
        self.assertEqual("2.0.0", sem.REFERENCE_SEMANTICS_VERSION)
        self.assertEqual("1.0.0", sem.SCHEMA_VERSION)

    def test_reusable_fact_index_avoids_reindexing_contract(self):
        fact = method("Start", [], "M:Start()")
        index = sem.SemanticFactIndex([fact])
        self.assertEqual(
            "M:Start()",
            sem.resolve_selector(fact["selector"], index)["symbol_key"],
        )

    def test_fact_universe_rejects_one_symbol_key_for_multiple_selectors(self):
        first = method("Start", [], "M:Same")
        second = method("Other", [], "M:Same")
        with self.assertRaises(sem.SelectorError):
            sem.SemanticFactIndex([first, second])

    def test_overloads_resolve_by_parameter_types(self):
        no_arg = method("Start", [], "M:Start()")
        int_arg = method("Start", ["System.Int32"], "M:Start(Int32)")
        resolved = sem.resolve_selector(int_arg["selector"], [no_arg, int_arg])
        self.assertEqual("M:Start(Int32)", resolved["symbol_key"])
        self.assertNotEqual(
            sem.selector_key(no_arg["selector"]),
            sem.selector_key(int_arg["selector"]),
        )

    def test_value_and_byref_overloads_resolve_by_xml_doc_type_ids(self):
        by_value = method(
            "Mutate", ["System.Int32"], "M:Mutate(System.Int32)")
        by_ref = method(
            "Mutate", ["System.Int32@"], "M:Mutate(System.Int32@)")
        self.assertNotEqual(
            sem.selector_key(by_value["selector"]),
            sem.selector_key(by_ref["selector"]),
        )
        self.assertEqual(
            "M:Mutate(System.Int32)",
            sem.resolve_selector(
                by_value["selector"], [by_value, by_ref])["symbol_key"],
        )
        self.assertEqual(
            "M:Mutate(System.Int32@)",
            sem.resolve_selector(
                by_ref["selector"], [by_value, by_ref])["symbol_key"],
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


    def test_namespace_and_type_selectors_resolve(self):
        ns = namespace_fact("Example", "N:Example")
        typ = type_fact("Example.Component", "T:Example.Component")
        self.assertEqual(
            "N:Example",
            sem.resolve_selector(ns["selector"], [ns, typ])["symbol_key"],
        )
        self.assertEqual(
            "T:Example.Component",
            sem.resolve_selector(typ["selector"], [ns, typ])["symbol_key"],
        )

    def test_static_constructor_is_distinct_from_parameterless_instance_constructor(self):
        instance = constructor("Example.Component", [], "M:.ctor", False)
        static = constructor("Example.Component", [], "M:.cctor", True)
        self.assertNotEqual(
            sem.selector_key(instance["selector"]),
            sem.selector_key(static["selector"]),
        )
        self.assertEqual(
            "M:.cctor",
            sem.resolve_selector(static["selector"], [instance, static])["symbol_key"],
        )

    def test_static_constructor_cannot_declare_parameters(self):
        bad = constructor("Example.Component", ["System.Int32"], "M:.cctor", True)
        with self.assertRaises(sem.SelectorError):
            sem.normalize_selector(bad["selector"])

    def test_indexer_overloads_are_distinguished_by_parameter_types(self):
        by_int = property_fact("Item", ["System.Int32"], "P:Item(Int32)")
        by_string = property_fact("Item", ["System.String"], "P:Item(String)")
        self.assertNotEqual(
            sem.selector_key(by_int["selector"]),
            sem.selector_key(by_string["selector"]),
        )

    def test_event_selector_is_addressable(self):
        event = event_fact("Changed", "E:Changed")
        self.assertEqual(
            "E:Changed",
            sem.resolve_selector(event["selector"], [event])["symbol_key"],
        )


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
                "selector_history": [{
                    "selector": fact["selector"],
                    "superseded_reason": "previous binding",
                }],
            },
        ]
        with self.assertRaises(sem.IdentityError):
            sem.validate_component_identities(records, [fact, other])

    def test_two_components_cannot_bind_distinct_selectors_to_one_symbol_key(self):
        first = method("Start", [], "M:Same")
        second = method("Other", [], "M:Same")
        records = [
            {"component_id": "component:a", "current_selector": first["selector"]},
            {"component_id": "component:b", "current_selector": second["selector"]},
        ]
        with self.assertRaises(sem.IdentityError):
            sem.validate_component_identities(records, [first, second])

    def test_selector_history_requires_reason(self):
        old = method("Old", [], "M:Old")
        current = method("Current", [], "M:Current")
        records = [{
            "component_id": "component:a",
            "current_selector": current["selector"],
            "selector_history": [{"selector": old["selector"]}],
        }]
        with self.assertRaises(sem.IdentityError):
            sem.validate_component_identities(records, [current])


    def test_deleted_current_selector_is_identity_error(self):
        missing = method("Missing", [], "M:Missing")
        records = [{
            "component_id": "component:a",
            "current_selector": missing["selector"],
        }]
        with self.assertRaises(sem.IdentityError):
            sem.validate_component_identities(records, [])


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

    def test_disable_anchor_resolution_failure_is_activation_error(self):
        with self.assertRaises(sem.ActivationError):
            sem.evaluate_disable_anchor(self.contract(), [self.other])

    def test_intentionally_disabled_requires_decision_metadata(self):
        contract = self.contract()
        del contract["decision_ref"]
        with self.assertRaises(sem.ActivationError):
            sem.validate_activation_contract(contract)

    def test_kd_w1_exact_exception_scope_only(self):
        findings = sem.kd_w1_violations(
            [self.disabled["selector"], self.other["selector"]],
            [self.contract()],
            [self.disabled, self.other],
            exception_scopes=[{
                "component_id": "component:tackling",
                "approval_ref": "EX-TS-001",
                "tuning_surface_selectors": [self.disabled["selector"]],
            }],
        )
        self.assertEqual(1, len(findings))
        self.assertEqual("inactive-tuning-change", findings[0]["finding_kind"])
        self.assertEqual(["F:PressureThreshold"], findings[0]["changed_symbol_keys"])

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


    def test_deleted_changed_tuning_surface_emits_two_typed_findings(self):
        findings = sem.kd_w1_violations(
            [self.other["selector"]],
            [self.contract()],
            [self.disabled],
        )
        self.assertEqual(
            ["inactive-tuning-change", "stale-tuning-selector"],
            sorted(item["finding_kind"] for item in findings),
        )
        stale = next(
            item for item in findings
            if item["finding_kind"] == "stale-tuning-selector")
        change = next(
            item for item in findings
            if item["finding_kind"] == "inactive-tuning-change")
        key = sem.selector_key(self.other["selector"])
        self.assertEqual([key], stale["selector_keys"])
        self.assertEqual([key], change["selector_keys"])
        self.assertEqual([key], change["unresolved_selector_keys"])
        self.assertEqual([], change["changed_symbol_keys"])

    def test_stale_tuning_selector_is_distinct_without_changed_surface(self):
        findings = sem.kd_w1_violations(
            [],
            [self.contract()],
            [self.disabled],
        )
        self.assertEqual(1, len(findings))
        self.assertEqual("stale-tuning-selector", findings[0]["finding_kind"])
        self.assertEqual(
            [sem.selector_key(self.other["selector"])],
            findings[0]["selector_keys"],
        )

    def test_active_component_still_reports_contract_staleness(self):
        contract = self.contract()
        contract["activation_state"] = "active"
        findings = sem.kd_w1_violations(
            [],
            [contract],
            [self.disabled],
        )
        self.assertEqual(1, len(findings))
        self.assertEqual("stale-tuning-selector", findings[0]["finding_kind"])

    def test_duplicate_contract_component_ids_fail_closed(self):
        duplicate = self.contract()
        duplicate["component_id"] = " component:tackling "
        with self.assertRaises(sem.ActivationError):
            sem.kd_w1_violations(
                [],
                [self.contract(), duplicate],
                [self.disabled, self.other],
            )

    def test_exception_scope_rejects_unknown_fields(self):
        with self.assertRaises(sem.ActivationError):
            sem.kd_w1_violations(
                [self.other["selector"]],
                [self.contract()],
                [self.disabled, self.other],
                exception_scopes=[{
                    "component_id": "component:tackling",
                    "approval_ref": "EX-TS-001",
                    "tuning_surface_selectors": [self.other["selector"]],
                    "revoked": True,
                }],
            )

    def test_contract_component_id_is_trimmed_before_exception_matching(self):
        contract = self.contract()
        contract["component_id"] = "component:tackling "
        self.assertEqual(
            [],
            sem.kd_w1_violations(
                [self.other["selector"]],
                [contract],
                [self.disabled, self.other],
                exception_scopes=[{
                    "component_id": "component:tackling",
                    "approval_ref": "EX-TS-001",
                    "tuning_surface_selectors": [self.other["selector"]],
                }],
            ),
        )

    def test_pending_integration_requires_gap_and_activation_condition(self):
        contract = {
            "component_id": "component:x",
            "activation_state": "pending-integration",
            "activation_owner": "owner",
            "integration_gap": "missing production caller",
            "activation_condition": "caller lands",
        }
        self.assertEqual("pending-integration", sem.validate_activation_contract(contract))
        del contract["integration_gap"]
        with self.assertRaises(sem.ActivationError):
            sem.validate_activation_contract(contract)

    def test_enum_typed_value_is_canonical(self):
        self.assertEqual(
            {
                "value_type": "enum",
                "value": "Disabled",
                "enum_type_id": "Example.Mode",
            },
            sem.normalize_typed_value({
                "value_type": "enum",
                "value": "Disabled",
                "enum_type_id": "Example.Mode",
            }),
        )

    def test_not_equals_disable_anchor(self):
        contract = self.contract()
        contract["disable_anchor"]["operator"] = "not-equals"
        contract["disable_anchor"]["expected"] = {
            "value_type": "number",
            "value": 1,
        }
        self.assertTrue(
            sem.evaluate_disable_anchor(
                contract, [self.disabled, self.other])["passed"])

    def test_unhashable_activation_enums_are_typed_errors(self):
        with self.assertRaises(sem.ActivationError):
            sem.normalize_typed_value({
                "value_type": ["number"],
                "value": 1,
            })
        with self.assertRaises(sem.ActivationError):
            sem.validate_activation_contract({
                "activation_state": ["active"],
            })

        contract = self.contract()
        contract["disable_anchor"] = dict(contract["disable_anchor"])
        contract["disable_anchor"]["operator"] = ["equals"]
        with self.assertRaises(sem.ActivationError):
            sem.evaluate_disable_anchor(
                contract, [self.disabled, self.other])

    def test_non_finite_numbers_are_rejected(self):
        with self.assertRaises(sem.ActivationError):
            sem.normalize_typed_value({"value_type": "number", "value": float("nan")})
        with self.assertRaises(sem.SemanticsError):
            sem.digest({"value": float("inf")})


def applicability_rule(
        rule_id, trigger_ref, requirement_ref, proof_class="structural-reachability",
        selectors=None, component_ids=None, assemblies=None, classifications=None,
        activation_states=None, fallback_scope=None, allowed_na_reasons=None,
        change_types=None):
    selectors = selectors or []
    component_ids = component_ids or []
    assemblies = assemblies or []
    classifications = classifications or []
    activation_states = activation_states or []
    change_types = change_types or []
    if fallback_scope is not None:
        surface_precedence = 0 if fallback_scope == "repository" else 1
    else:
        surface_precedence = (
            32
            | (16 if selectors else 0)
            | (8 if component_ids else 0)
            | (4 if assemblies else 0)
            | (2 if classifications else 0)
            | (1 if activation_states else 0)
        )
    context_width = len(sem._CHANGE_TYPES) + 1
    context_rank = (
        len(sem._CHANGE_TYPES) - len(change_types) + 1
        if change_types else 0
    )
    precedence = (surface_precedence * context_width) + context_rank
    return {
        "rule_id": rule_id,
        "selectors": selectors,
        "component_ids": component_ids,
        "assemblies": assemblies,
        "classifications": classifications,
        "activation_states": activation_states,
        "trigger_ref": trigger_ref,
        "change_types": change_types,
        "requirement_refs": [requirement_ref],
        "proof_classes": [proof_class],
        "gate_classes": ["merge"],
        "allowed_na_reasons": allowed_na_reasons or [],
        "precedence": precedence,
        "fallback_scope": fallback_scope,
    }


def dep_node(dependency_id, kind, tag, requirement_ref=None):
    result = {
        "dependency_id": dependency_id,
        "kind": kind,
        "fingerprint": sem.digest({"tag": tag}),
    }
    if requirement_ref is not None:
        result["requirement_ref"] = requirement_ref
    return result


def proof_graph():
    return {
        "nodes": [
            dep_node("req:FR-X", "requirement", "requirement-v1", "FR-X"),
            dep_node("contract:host", "contract", "contract-v1"),
            dep_node("root:host", "runtime-root", "root-v1"),
            dep_node("symbol:child", "symbol", "symbol-v1"),
            dep_node("asmdef:runtime", "asmdef", "asmdef-v1"),
            dep_node("life:start", "lifecycle", "lifecycle-v1"),
            dep_node("serializer:save", "serializer", "serializer-v1"),
            dep_node("test:proof", "test", "test-v1"),
            dep_node("runner:dotnet", "runner", "runner-v1"),
            dep_node("tool:extractor", "extractor", "extractor-v1"),
            dep_node("unrelated:docs", "schema", "unrelated-v1"),
        ],
        "edges": [
            {"source": "req:FR-X", "target": "contract:host", "relation": "contract"},
            {"source": "contract:host", "target": "root:host", "relation": "root"},
            {"source": "contract:host", "target": "tool:extractor", "relation": "extractor-semantic"},
            {"source": "root:host", "target": "symbol:child", "relation": "construction"},
            {"source": "symbol:child", "target": "asmdef:runtime", "relation": "assembly-reference"},
            {"source": "root:host", "target": "life:start", "relation": "lifecycle-member"},
            {"source": "root:host", "target": "serializer:save", "relation": "serializer"},
            {"source": "root:host", "target": "test:proof", "relation": "test"},
            {"source": "test:proof", "target": "runner:dotnet", "relation": "runner"},
            {"source": "runner:dotnet", "target": "tool:extractor", "relation": "tool-semantic"},
        ],
    }


def proof_resolution(
        subject=None, proof_class="structural-reachability",
        change_type="pure-local-calculation", rule_change_types=None):
    resolved_subject = dict(
        subject or {"classification": "production-runtime-root"})
    if change_type is not None:
        resolved_subject.setdefault("change_type", change_type)
    rule = applicability_rule(
        "AR-X",
        "TRIGGER-X",
        "FR-X",
        proof_class=proof_class,
        fallback_scope="repository",
        change_types=rule_change_types,
    )
    return sem.resolve_applicability(
        resolved_subject,
        [rule],
        strict=(change_type is not None),
    )


class ApplicabilityTests(unittest.TestCase):
    def subject(self):
        fact = method("Start", [], "M:Start()")
        return {
            "selector": fact["selector"],
            "component_id": "component:host",
            "assembly": "Example.Runtime",
            "classification": "production-runtime-root",
            "activation_state": "active",
            "change_type": "pure-local-calculation",
        }

    def test_schema_derived_specificity_beats_fallback_and_broader_matches(self):
        rules = [
            applicability_rule(
                "fallback", "T", "FR-FALLBACK", fallback_scope="repository"),
            applicability_rule(
                "assembly", "T", "FR-ASSEMBLY", assemblies=["Example.Runtime"]),
            applicability_rule(
                "component", "T", "FR-COMPONENT", component_ids=["component:host"]),
            applicability_rule(
                "selector", "T", "FR-SELECTOR",
                selectors=[method("Start", [], "M:Start()")["selector"]]),
        ]
        result = sem.resolve_applicability(self.subject(), rules)
        self.assertEqual(["FR-SELECTOR"], result["requirement_refs"])
        self.assertEqual(["selector"], result["selected_rule_ids"])

    def test_equal_precedence_conflict_fails_closed(self):
        rules = [
            applicability_rule(
                "a", "T", "FR-A", component_ids=["component:host"]),
            applicability_rule(
                "b", "T", "FR-B", component_ids=["component:host"]),
        ]
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(self.subject(), rules)

    def test_equal_precedence_identical_payload_coalesces_deterministically(self):
        rules = [
            applicability_rule(
                "b", "T", "FR-X", component_ids=["component:host"]),
            applicability_rule(
                "a", "T", "FR-X", component_ids=["component:host"]),
        ]
        forward = sem.resolve_applicability(self.subject(), rules)
        reverse = sem.resolve_applicability(self.subject(), list(reversed(rules)))
        self.assertEqual(["a", "b"], forward["selected_rule_ids"])
        self.assertEqual(
            forward["applicability_digest"], reverse["applicability_digest"])

    def test_stored_precedence_cannot_override_specificity(self):
        rule = applicability_rule(
            "a", "T", "FR-X", component_ids=["component:host"])
        rule["precedence"] = 31
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(self.subject(), [rule])

    def test_unmatched_subject_fails_closed_in_strict_mode(self):
        rule = applicability_rule(
            "a", "T", "FR-X", component_ids=["component:other"])
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(self.subject(), [rule])

    def test_category_fallback_outranks_repository_default(self):
        rules = [
            applicability_rule(
                "repo", "T", "FR-REPO", fallback_scope="repository"),
            applicability_rule(
                "runtime", "T", "FR-RUNTIME", fallback_scope="runtime-bearing"),
        ]
        result = sem.resolve_applicability(self.subject(), rules)
        self.assertEqual(["FR-RUNTIME"], result["requirement_refs"])
        self.assertEqual(["runtime"], result["selected_rule_ids"])

    def test_non_runtime_fallback_covers_all_four_non_runtime_classifications(self):
        rule = applicability_rule(
            "nonruntime", "T", "FR-NONRUNTIME", fallback_scope="non-runtime-bearing")
        for classification in (
                "test-only", "tooling-only", "generated-or-external",
                "non-runtime-bearing"):
            result = sem.resolve_applicability(
                {
                    "classification": classification,
                    "change_type": "pure-local-calculation",
                },
                [rule])
            self.assertEqual(["FR-NONRUNTIME"], result["requirement_refs"])

    def test_explicit_rule_outranks_category_fallback(self):
        rules = [
            applicability_rule(
                "runtime", "T", "FR-RUNTIME", fallback_scope="runtime-bearing"),
            applicability_rule(
                "activation", "T", "FR-ACTIVE", activation_states=["active"]),
        ]
        result = sem.resolve_applicability(self.subject(), rules)
        self.assertEqual(["FR-ACTIVE"], result["requirement_refs"])
        self.assertEqual(["activation"], result["selected_rule_ids"])

    def test_unhashable_applicability_enums_are_typed_errors(self):
        for field, value in (
                ("classification", ["production-runtime-root"]),
                ("activation_state", {"value": "active"}),
                ("change_type", ["persistence-boundary"])):
            subject = self.subject()
            subject[field] = value
            with self.subTest(field=field):
                with self.assertRaises(sem.ApplicabilityError):
                    sem.resolve_applicability(
                        subject,
                        [applicability_rule(
                            "a", "T", "FR-X",
                            component_ids=["component:host"])],
                    )

        rule = applicability_rule(
            "a", "T", "FR-X", component_ids=["component:host"])
        rule["fallback_scope"] = ["repository"]
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(self.subject(), [rule])

    def test_invalid_subject_change_type_fails_closed(self):
        subject = self.subject()
        subject["change_type"] = "persistence-ish"
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(
                subject,
                [applicability_rule(
                    "a", "T", "FR-X", component_ids=["component:host"])],
            )

    def test_invalid_rule_change_type_fails_closed(self):
        rule = applicability_rule(
            "a", "T", "FR-X", component_ids=["component:host"],
            change_types=["persistence-ish"])
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(self.subject(), [rule])

    def test_strict_resolution_requires_current_change_type(self):
        subject = self.subject()
        del subject["change_type"]
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(
                subject,
                [applicability_rule(
                    "a", "T", "FR-X", component_ids=["component:host"])],
            )

    def test_change_type_specific_rule_matches_only_current_change_context(self):
        rule = applicability_rule(
            "persistence", "T", "FR-X", component_ids=["component:host"],
            change_types=["persistence-boundary"])
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(self.subject(), [rule])

        persistence_subject = self.subject()
        persistence_subject["change_type"] = "persistence-boundary"
        result = sem.resolve_applicability(persistence_subject, [rule])
        self.assertEqual(["persistence"], result["selected_rule_ids"])

    def test_change_specific_rule_outranks_equivalent_generic_rule(self):
        rules = [
            applicability_rule(
                "generic", "T", "FR-GENERIC", component_ids=["component:host"]),
            applicability_rule(
                "persistence", "T", "FR-PERSIST", component_ids=["component:host"],
                change_types=["persistence-boundary"]),
        ]
        subject = self.subject()
        subject["change_type"] = "persistence-boundary"
        result = sem.resolve_applicability(subject, rules)
        self.assertEqual(["FR-PERSIST"], result["requirement_refs"])
        self.assertEqual(["persistence"], result["selected_rule_ids"])

    def test_narrower_change_type_set_outranks_broader_matching_set(self):
        rules = [
            applicability_rule(
                "broad", "T", "FR-BROAD", component_ids=["component:host"],
                change_types=[
                    "persistence-boundary",
                    "external-resource-dependency",
                ]),
            applicability_rule(
                "narrow", "T", "FR-NARROW", component_ids=["component:host"],
                change_types=["persistence-boundary"]),
        ]
        subject = self.subject()
        subject["change_type"] = "persistence-boundary"
        result = sem.resolve_applicability(subject, rules)
        self.assertEqual(["FR-NARROW"], result["requirement_refs"])
        self.assertEqual(["narrow"], result["selected_rule_ids"])

    def test_non_strict_missing_context_is_explicitly_diagnostic(self):
        subject = self.subject()
        del subject["change_type"]
        rule = applicability_rule(
            "persistence", "T", "FR-X", component_ids=["component:host"],
            change_types=["persistence-boundary"])
        result = sem.resolve_applicability(
            subject, [rule], strict=False)
        self.assertFalse(result["context_complete"])
        self.assertEqual(["missing-change-type"], result["diagnostics"])
        self.assertEqual([], result["obligations"])

    def test_na_requires_enumerated_reason_and_approval_when_declared(self):
        rule = applicability_rule(
            "a",
            "T",
            "FR-X",
            component_ids=["component:host"],
            allowed_na_reasons=[{
                "reason_code": "external-owner",
                "approval_required": True,
            }],
        )
        with self.assertRaises(sem.ApplicabilityError):
            sem.resolve_applicability(
                self.subject(),
                [rule],
                na_requests=[{
                    "trigger_ref": "T",
                    "reason_code": "external-owner",
                }],
            )
        result = sem.resolve_applicability(
            self.subject(),
            [rule],
            na_requests=[{
                "trigger_ref": "T",
                "reason_code": "external-owner",
                "approval_ref": "APPROVAL-1",
            }],
        )
        self.assertEqual([], result["requirement_refs"])
        self.assertEqual("external-owner", result["obligations"][0]["na"]["reason_code"])


class ProofClosureTests(unittest.TestCase):
    def test_unhashable_dependency_kind_and_proof_class_are_closure_errors(self):
        graph = proof_graph()
        graph["nodes"][0]["kind"] = ["requirement"]
        with self.assertRaises(sem.ClosureError):
            sem.normalize_dependency_graph(graph)

        with self.assertRaises(sem.ClosureError):
            sem.derive_proof_closure(
                ["structural-reachability"],
                proof_resolution(),
                proof_graph(),
            )

    def test_structural_closure_excludes_persistence_without_matching_change_type(self):
        closure = sem.derive_proof_closure(
            "structural-reachability",
            proof_resolution(),
            proof_graph(),
        )
        self.assertIn("symbol:child", closure["dependency_ids"])
        self.assertIn("asmdef:runtime", closure["dependency_ids"])
        self.assertIn("tool:extractor", closure["dependency_ids"])
        self.assertNotIn("serializer:save", closure["dependency_ids"])
        self.assertNotIn("life:start", closure["dependency_ids"])
        self.assertNotIn("test:proof", closure["dependency_ids"])
        self.assertFalse(closure["persistence_triggered"])

    def test_persistence_change_type_adds_persistence_edges_to_structural_closure(self):
        closure = sem.derive_proof_closure(
            "structural-reachability",
            proof_resolution(change_type="persistence-boundary"),
            proof_graph(),
        )
        self.assertIn("serializer:save", closure["dependency_ids"])
        self.assertTrue(closure["persistence_triggered"])

    def test_external_resource_change_type_adds_persistence_edges_to_lifecycle_closure(self):
        lifecycle = sem.derive_proof_closure(
            "lifecycle-order",
            proof_resolution(
                proof_class="lifecycle-order",
                change_type="external-resource-dependency",
            ),
            proof_graph(),
        )
        self.assertIn("life:start", lifecycle["dependency_ids"])
        self.assertIn("serializer:save", lifecycle["dependency_ids"])
        self.assertTrue(lifecycle["persistence_triggered"])

    def test_proof_closure_rejects_resolution_without_change_context(self):
        resolution = proof_resolution(change_type=None)
        with self.assertRaises(sem.ClosureError):
            sem.derive_proof_closure(
                "structural-reachability",
                resolution,
                proof_graph(),
            )

    def test_persistence_is_not_a_fifth_proof_class(self):
        with self.assertRaises(sem.ApplicabilityError):
            proof_resolution(proof_class="persistence-external-resource")

    def test_executable_closure_adds_test_runner_without_untriggered_persistence(self):
        closure = sem.derive_proof_closure(
            "failure-injection",
            proof_resolution(proof_class="failure-injection"),
            proof_graph(),
        )
        self.assertIn("test:proof", closure["dependency_ids"])
        self.assertIn("runner:dotnet", closure["dependency_ids"])
        self.assertNotIn("serializer:save", closure["dependency_ids"])

    def test_executable_persistence_trigger_adds_persistence_edges(self):
        closure = sem.derive_proof_closure(
            "failure-injection",
            proof_resolution(
                proof_class="failure-injection",
                change_type="persistence-boundary",
            ),
            proof_graph(),
        )
        self.assertIn("serializer:save", closure["dependency_ids"])
        self.assertTrue(closure["persistence_triggered"])

    def test_tampered_applicability_result_cannot_seed_proof_closure(self):
        resolution = proof_resolution()
        resolution["obligations"][0]["requirement_refs"] = ["FR-TAMPERED"]
        with self.assertRaises(sem.ClosureError):
            sem.derive_proof_closure(
                "structural-reachability",
                resolution,
                proof_graph(),
            )

    def test_missing_requirement_binding_fails_closed(self):
        graph = proof_graph()
        graph["nodes"] = [
            item for item in graph["nodes"] if item["dependency_id"] != "req:FR-X"
        ]
        graph["edges"] = [
            item for item in graph["edges"] if item["source"] != "req:FR-X"
        ]
        with self.assertRaises(sem.ClosureError):
            sem.derive_proof_closure(
                "structural-reachability",
                proof_resolution(),
                graph,
            )


class FreshnessTests(unittest.TestCase):
    def snapshot(self):
        return sem.capture_proof_snapshot(
            "structural-reachability",
            proof_resolution(),
            proof_graph(),
            provenance_revision="commit-old",
            provenance_tree="tree-old",
        )

    def test_unrelated_dependency_change_does_not_stale_proof(self):
        graph = proof_graph()
        changed = copy.deepcopy(graph)
        node = next(
            item for item in changed["nodes"]
            if item["dependency_id"] == "unrelated:docs")
        node["fingerprint"] = sem.digest({"tag": "unrelated-v2"})
        result = sem.assess_proof_freshness(
            self.snapshot(), proof_resolution(), changed)
        self.assertTrue(result["fresh"])

    def test_reachable_dependency_content_change_stales_proof(self):
        changed = proof_graph()
        node = next(
            item for item in changed["nodes"]
            if item["dependency_id"] == "root:host")
        node["fingerprint"] = sem.digest({"tag": "root-v2"})
        result = sem.assess_proof_freshness(
            self.snapshot(), proof_resolution(), changed)
        self.assertFalse(result["fresh"])
        self.assertIn("dependency-content-changed", result["reasons"])

    def test_new_reachable_dependency_stales_proof(self):
        changed = proof_graph()
        changed["nodes"].append(
            dep_node("config:new", "configuration", "config-v1"))
        changed["edges"].append({
            "source": "root:host",
            "target": "config:new",
            "relation": "configuration",
        })
        result = sem.assess_proof_freshness(
            self.snapshot(), proof_resolution(), changed)
        self.assertFalse(result["fresh"])
        self.assertIn("dependency-set-changed", result["reasons"])
        self.assertIn("dependency-topology-changed", result["reasons"])

    def test_provenance_does_not_participate_in_freshness(self):
        snapshot = self.snapshot()
        snapshot["provenance_revision"] = "different-container-commit"
        snapshot["provenance_tree"] = "different-container-tree"
        self.assertTrue(
            sem.assess_proof_freshness(
                snapshot, proof_resolution(), proof_graph())["fresh"])

    def test_applicability_subject_change_stales_proof_even_with_same_rule(self):
        current = proof_resolution(subject={
            "classification": "production-runtime-root",
            "component_id": "component:renamed-host",
            "change_type": "pure-local-calculation",
        })
        result = sem.assess_proof_freshness(
            self.snapshot(), current, proof_graph())
        self.assertFalse(result["fresh"])
        self.assertIn("applicability-subject-changed", result["reasons"])

    def test_change_context_change_stales_and_expands_proof_scope(self):
        result = sem.assess_proof_freshness(
            self.snapshot(),
            proof_resolution(change_type="persistence-boundary"),
            proof_graph(),
        )
        self.assertFalse(result["fresh"])
        self.assertIn("applicability-subject-changed", result["reasons"])
        self.assertIn("serializer:save", result["current"]["dependency_ids"])

    def test_changed_optimization_falls_back_for_unmapped_surface(self):
        decision = sem.changed_proof_decision(
            self.snapshot(),
            proof_resolution(),
            proof_graph(),
            ["new:file:not-in-inventory"],
        )
        self.assertTrue(decision["run_required"])
        self.assertEqual("unmapped-changed-surface", decision["reason"])

    def test_changed_surface_inside_closure_requires_full_relevant_run(self):
        decision = sem.changed_proof_decision(
            self.snapshot(),
            proof_resolution(),
            proof_graph(),
            ["root:host"],
        )
        self.assertTrue(decision["run_required"])
        self.assertEqual(
            "changed-surface-in-proof-closure", decision["reason"])
        self.assertEqual(["root:host"], decision["changed_dependency_ids"])

    def test_changed_optimization_can_skip_only_proven_unrelated_material(self):
        decision = sem.changed_proof_decision(
            self.snapshot(),
            proof_resolution(),
            proof_graph(),
            ["unrelated:docs"],
        )
        self.assertFalse(decision["run_required"])
        self.assertEqual("proven-non-impact", decision["reason"])


class ExecutionTruthTests(unittest.TestCase):
    def test_unhashable_execution_state_is_execution_error(self):
        with self.assertRaises(sem.ExecutionError):
            sem.evaluate_execution_truth(["passed"])

    def substitute(self):
        return {
            "authority_ref": "FR-TS-BOUND-001",
            "approval_ref": "APPROVAL-1",
            "justification": "exhaustive execution is disproportionate",
            "omitted_surface_or_uncertainty": "rare platform branch remains unexecuted",
        }

    def test_only_passed_satisfies_unqualified_required_execution(self):
        for state in (
                "failed", "skipped", "excluded", "unavailable",
                "not-run", "runner-failed"):
            result = sem.evaluate_execution_truth(state)
            self.assertFalse(result["satisfied"])
            self.assertEqual("unsatisfied", result["basis"])
        self.assertTrue(sem.evaluate_execution_truth("passed")["satisfied"])

    def test_bounded_substitute_requires_explicit_permission_and_complete_record(self):
        denied = sem.evaluate_execution_truth(
            "unavailable", self.substitute(), bounded_substitute_permitted=False)
        self.assertFalse(denied["satisfied"])
        self.assertEqual("bounded-substitute-not-permitted", denied["basis"])

        allowed = sem.evaluate_execution_truth(
            "unavailable", self.substitute(), bounded_substitute_permitted=True)
        self.assertTrue(allowed["satisfied"])
        self.assertEqual("bounded-substitute", allowed["basis"])

        incomplete = self.substitute()
        del incomplete["approval_ref"]
        with self.assertRaises(sem.ExecutionError):
            sem.evaluate_execution_truth(
                "unavailable", incomplete, bounded_substitute_permitted=True)

    def test_unknown_execution_state_and_passed_plus_bounded_fail_closed(self):
        with self.assertRaises(sem.ExecutionError):
            sem.evaluate_execution_truth("green")
        with self.assertRaises(sem.ExecutionError):
            sem.evaluate_execution_truth(
                "passed", self.substitute(), bounded_substitute_permitted=True)

    def test_failed_skipped_and_runner_failed_cannot_be_waived(self):
        for state in ("failed", "skipped", "runner-failed"):
            with self.assertRaises(sem.ExecutionError):
                sem.evaluate_execution_truth(
                    state,
                    self.substitute(),
                    bounded_substitute_permitted=True,
                )

    def test_excluded_unavailable_and_not_run_may_use_approved_bounded_substitute(self):
        for state in ("excluded", "unavailable", "not-run"):
            result = sem.evaluate_execution_truth(
                state,
                self.substitute(),
                bounded_substitute_permitted=True,
            )
            self.assertTrue(result["satisfied"])
            self.assertEqual("bounded-substitute", result["basis"])


def property_record(state="Candidate", exceptions_allowed=True):
    decisions = [{
        "decision_id": "DEC-AP-001",
        "decision_actor": "architecture-owner",
        "transition_from": None,
        "transition_to": "Candidate",
        "decision_rationale": "establish the candidate",
        "decided_at": "2026-09-01",
    }]
    if state != "Candidate":
        decisions.append({
            "decision_id": "DEC-AP-002",
            "decision_actor": "architecture-owner",
            "transition_from": "Candidate",
            "transition_to": state,
            "decision_rationale": "complete the admission decision",
            "decided_at": "2026-09-01",
        })
    record = {
        "property_id": "AP-001",
        "title": "Runtime ownership is explicit",
        "state": state,
        "statement": "Every runtime service has one construction owner.",
        "failure_mode": "A service can exist without production activation.",
        "scope": ["runtime-bearing"],
        "non_scope": [],
        "authority": "Project Architecture Governance",
        "evidence": ["structural-reachability"],
        "enforcement_class": "Hybrid",
        "activation": "Staged",
        "exceptions_allowed": exceptions_allowed,
        "supersedes": None,
        "decision_rationale": "Prevents structurally dormant services.",
        "last_reviewed": "2026-09-01",
        "decision_history": decisions,
        "revalidation_history": [],
    }
    if exceptions_allowed:
        record["exception_mechanism"] = "governance-exception"
    return record


def property_registry(record=None):
    return {
        "schema_version": "1.0.0",
        "properties": [] if record is None else [record],
    }


class CanonicalArtifactSchemaTests(unittest.TestCase):
    ROOT = Path(__file__).resolve().parents[2] / "docs" / "tracking" / "architecture-governance"

    def load(self, name):
        return json.loads((self.ROOT / name).read_text(encoding="utf-8"))

    def test_all_canonical_schema_documents_are_machine_readable_and_versioned(self):
        expected = {
            "applicability-rules.schema.json",
            "bootstrap-runtime-surfaces.schema.json",
            "common.schema.json",
            "exceptions.schema.json",
            "integration-contracts.schema.json",
            "proof-artifact.schema.json",
            "property-registry.schema.json",
            "review-ledger.schema.json",
            "runtime-surface-classifications.schema.json",
            "temporary-activation-baseline.schema.json",
        }
        actual = {path.name for path in (self.ROOT / "schemas").glob("*.json")}
        self.assertEqual(expected, actual)
        for name in expected:
            schema = self.load("schemas/" + name)
            self.assertEqual(
                "https://json-schema.org/draft/2020-12/schema",
                schema["$schema"],
            )

    def test_all_schema_references_resolve_inside_the_canonical_schema_set(self):
        schemas = {
            path.name: json.loads(path.read_text(encoding="utf-8"))
            for path in (self.ROOT / "schemas").glob("*.json")
        }

        def resolve_pointer(document, fragment):
            current = document
            if not fragment:
                return current
            self.assertTrue(fragment.startswith("/"))
            for part in fragment[1:].split("/"):
                part = part.replace("~1", "/").replace("~0", "~")
                current = current[part]
            return current

        def walk(value, owner):
            if isinstance(value, dict):
                for key, item in value.items():
                    if key == "$ref":
                        file_name, _, fragment = item.partition("#")
                        target = schemas[owner] if not file_name else schemas[file_name]
                        resolve_pointer(target, fragment)
                    walk(item, owner)
            elif isinstance(value, list):
                for item in value:
                    walk(item, owner)

        for name, schema in schemas.items():
            walk(schema, name)

    def test_common_schema_is_the_single_enum_control_source(self):
        common = self.load("schemas/common.schema.json")
        expected_runtime_bindings = {
            "selectorKind": sem._SELECTOR_KINDS,
            "structuralClassification": sem._STRUCTURAL_CLASSIFICATIONS,
            "activationState": sem._ACTIVATION_STATES,
            "valueType": sem._VALUE_TYPES,
            "anchorOperator": sem._ANCHOR_OPERATORS,
            "propertyState": sem._PROPERTY_STATES,
            "enforcementClass": sem._ENFORCEMENT_CLASSES,
            "propertyActivation": sem._PROPERTY_ACTIVATIONS,
            "disposition": sem._DISPOSITIONS,
            "findingStatus": sem._FINDING_STATUSES,
            "reviewState": sem._REVIEW_STATES,
            "baselineMode": sem._BASELINE_MODES,
            "proofClass": sem._PROOF_CLASSES,
            "changeType": sem._CHANGE_TYPES,
            "executionState": sem._EXECUTION_STATES,
            "dependencyKind": sem._DEPENDENCY_KINDS,
        }
        for name, runtime_values in expected_runtime_bindings.items():
            self.assertEqual(set(common["$defs"][name]["enum"]), set(runtime_values))

        for path in (self.ROOT / "schemas").glob("*.json"):
            if path.name == "common.schema.json":
                continue
            self.assertNotIn(
                '"enum"', path.read_text(encoding="utf-8"),
                "%s duplicates canonical enum control data" % path.name,
            )

        selector_branches = common["$defs"]["selector"]["oneOf"]
        branch_kinds = set()
        for branch in selector_branches:
            kind = branch["properties"]["kind"]
            branch_kinds.update([kind["const"]] if "const" in kind else kind["enum"])
        self.assertEqual(set(common["$defs"]["selectorKind"]["enum"]), branch_kinds)

    def test_reference_semantics_keeps_the_ci_pure_stdlib(self):
        tree = ast.parse(TOOL_PATH.read_text(encoding="utf-8"))
        imported_roots = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                imported_roots.update(alias.name.split(".")[0] for alias in node.names)
            elif isinstance(node, ast.ImportFrom):
                imported_roots.add(node.module.split(".")[0])
        self.assertEqual(
            {"hashlib", "json", "math", "pathlib"}, imported_roots)

    def test_seven_seed_artifacts_validate(self):
        """The seeds satisfy the SEMANTIC validators, strictly.

        Every cross-document input is supplied explicitly: the seeds are the
        first landing, so the trusted prior is genuinely absent rather than
        merely unsupplied.
        """
        sem.validate_runtime_surface_classifications_document(
            self.load("runtime-surface-classifications.json"))
        sem.validate_integration_contracts_document(
            self.load("integration-contracts.json"))
        sem.validate_applicability_rules_document(
            self.load("applicability-rules.json"))
        properties = self.load("property-registry.json")
        sem.validate_property_registry(properties, None)
        sem.validate_exception_registry(self.load("exceptions.json"), properties)
        sem.validate_review_ledger(self.load("review-ledger.json"), prior_ledger=None)
        sem.validate_temporary_activation_baseline(
            self.load("temporary-activation-baseline.json"),
            prior_baseline=None, current_violation_ids=[])

    def test_unknown_schema_major_fails_closed(self):
        document = self.load("property-registry.json")
        document["schema_version"] = "2.0.0"
        with self.assertRaises(sem.PropertyRegistryError):
            sem.validate_property_registry(document, None)


class PropertyRegistryTests(unittest.TestCase):
    def test_known_absent_merge_base_allows_initial_candidate(self):
        sem.validate_property_registry(property_registry(property_record()), None)

    def test_strict_validation_without_trusted_merge_base_reports_uncertainty(self):
        with self.assertRaises(sem.PropertyRegistryUncertainty):
            sem.validate_property_registry(property_registry(property_record()))

    def test_legal_candidate_to_admitted_transition_is_append_only(self):
        prior = property_registry(property_record())
        current = property_registry(property_record("Admitted"))
        sem.validate_property_registry(current, prior)

    def test_illegal_governance_transition_is_rejected(self):
        record = property_record()
        record["state"] = "Retired"
        record["decision_history"].append({
            "decision_id": "DEC-AP-ILLEGAL",
            "decision_actor": "architecture-owner",
            "transition_from": "Candidate",
            "transition_to": "Retired",
            "decision_rationale": "skip admission",
            "decided_at": "2026-09-01",
        })
        with self.assertRaises(sem.PropertyRegistryError):
            sem.validate_property_registry(property_registry(record), None)

    def test_merge_base_history_rewrite_is_rejected(self):
        prior = property_registry(property_record())
        current = copy.deepcopy(prior)
        current["properties"][0]["decision_history"][0]["decision_rationale"] = "rewritten"
        with self.assertRaises(sem.PropertyRegistryError):
            sem.validate_property_registry(current, prior)

    def test_material_amendment_requires_appended_revalidation(self):
        prior = property_registry(property_record())
        current = copy.deepcopy(prior)
        current["properties"][0]["statement"] = "Every runtime service has exactly one owner."
        with self.assertRaises(sem.PropertyRegistryError):
            sem.validate_property_registry(current, prior)
        current["properties"][0]["revalidation_history"].append({
            "revalidation_id": "REVAL-001",
            "decision_actor": "architecture-owner",
            "reviewed_at": "2026-09-01",
            "subject_scope_digest": sem.digest({"property": "AP-001-v2"}),
            "outcome": "amended",
            "decision_rationale": "clarify cardinality",
        })
        sem.validate_property_registry(current, prior)

    def test_top_level_decision_metadata_cannot_change_without_history(self):
        prior = property_registry(property_record())
        current = copy.deepcopy(prior)
        current["properties"][0]["decision_rationale"] = "silently rewritten"
        current["properties"][0]["last_reviewed"] = "2026-09-02"
        with self.assertRaises(sem.PropertyRegistryError):
            sem.validate_property_registry(current, prior)


def exception_record(property_id="AP-001"):
    return {
        "exception_id": "EX-AP-001",
        "property_id": property_id,
        "scope": [{"component_id": "component:match-host"}],
        "reason": "Migration cannot complete in one change.",
        "risk": "The component may remain structurally dormant.",
        "mitigation": "A focused activation test runs on every change.",
        "owner": "match-engine",
        "expiry_trigger": {"type": "milestone", "value": "A8 strict activation"},
        "approval": {
            "decision_id": "DEC-EX-001",
            "decision_actor": "architecture-owner",
            "decided_at": "2026-09-01",
        },
        "status": "active",
    }


class ExceptionRegistryTests(unittest.TestCase):
    def test_exception_requires_admitted_property_that_allows_it(self):
        properties = property_registry(property_record("Admitted"))
        registry = {"schema_version": "1.0.0", "exceptions": [exception_record()]}
        sem.validate_exception_registry(registry, properties)

        denied = property_registry(property_record("Admitted", exceptions_allowed=False))
        with self.assertRaises(sem.ExceptionRegistryError):
            sem.validate_exception_registry(registry, denied)

    def test_fr_waiver_cannot_be_routed_into_governance_exceptions(self):
        properties = property_registry(property_record("Admitted"))
        registry = {
            "schema_version": "1.0.0",
            "exceptions": [exception_record("FR-TS-097")],
        }
        with self.assertRaises(sem.ExceptionRegistryError):
            sem.validate_exception_registry(registry, properties)

    def test_exception_routes_are_exclusive(self):
        properties = property_registry(property_record("Admitted"))
        self.assertEqual(
            {"route": "governance-property", "governance_exception_allowed": True},
            sem.exception_route("AP-001", properties),
        )
        self.assertEqual(
            {"route": "testing-strategy-owner", "governance_exception_allowed": False},
            sem.exception_route("FR-TS-097", properties),
        )
        self.assertEqual(
            {"route": "code-standards-owner", "governance_exception_allowed": False},
            sem.exception_route("FR-CS-076", properties),
        )


def review_run(
        run_id="RUN-001", round_number=1, convergence="CONVERGED",
        final=True, budget_exhausted=False):
    return {
        "review_run_id": run_id,
        "review_series_id": "SERIES-001",
        "review_scope": ["A2 schema freeze"],
        "subject_scope_digest": sem.digest({"subject": "A2"}),
        "review_round": round_number,
        "reviewer_identity": "reviewer-1",
        "coverage": ["schemas", "reference semantics"],
        "unverified_surfaces": [],
        "applicable_properties": [],
        "convergence_state": convergence,
        "final_review": final,
        "round_budget_exhausted": budget_exhausted,
    }


def finding(disposition="Blocker", status="Open", severity="Low"):
    terminal = {
        "Blocker": "Resolved",
        "Accepted Tradeoff": "Accepted",
        "Residual Risk": "Recorded",
        "Candidate Property": "In property process",
    }[disposition]
    history = [{
        "event_id": "STATUS-001",
        "transition_from": None,
        "transition_to": "Open",
        "actor": "reviewer-1",
        "at": "2026-09-01",
        "evidence": [],
    }]
    resolution = []
    if status != "Open":
        history.append({
            "event_id": "STATUS-002",
            "transition_from": "Open",
            "transition_to": status,
            "actor": "architecture-owner",
            "at": "2026-09-01",
            "evidence": ["resolution accepted"],
        })
        resolution = ["resolution accepted"]
    record = {
        "finding_id": "FINDING-001",
        "stable_key": "schema-transition-gap",
        "review_series_id": "SERIES-001",
        "parent_review_run_id": "RUN-001",
        "summary": "A schema transition is not enforced.",
        "evidence": ["bad fixture passes"],
        "severity": severity,
        "requirement_property": ["FR-AG-017"] if disposition == "Blocker" else [],
        "disposition": disposition,
        "required_action": "Implement the missing transition check.",
        "owner": "architecture-governance",
        "status": status,
        "round_introduced": 1,
        "resolution_evidence": resolution,
        "status_history": history,
    }
    if disposition in {"Accepted Tradeoff", "Residual Risk"} and status == terminal:
        record["disposition_approval"] = "DEC-REVIEW-001"
    if disposition == "Candidate Property" and status == terminal:
        record["resolution_property_id"] = "AP-002"
    return record


def review_ledger(runs=None, findings=None):
    return {
        "schema_version": "1.0.0",
        "legacy_policy": "read-only-no-inference",
        "review_runs": [] if runs is None else runs,
        "findings": [] if findings is None else findings,
    }


def check_ledger(ledger, **kwargs):
    """Validate a ledger while exercising a rule other than the strict gates.

    The strict gates have their own tests; call sites that are probing
    convergence or append-only behaviour state the relaxation explicitly so a
    strict-mode uncertainty can never masquerade as the rule under test.
    """
    kwargs.setdefault("prior_ledger", None)
    kwargs.setdefault("strict", False)
    return sem.validate_review_ledger(ledger, **kwargs)


class ReviewLedgerTests(unittest.TestCase):
    def test_clean_zero_finding_final_review_can_converge(self):
        run = review_run()
        sem.validate_review_ledger(
            review_ledger([run]),
            current_subject_digests={run["review_run_id"]: run["subject_scope_digest"]},
            prior_ledger=None,
        )

    def test_open_low_blocker_prevents_convergence(self):
        with self.assertRaises(sem.ReviewLedgerError):
            check_ledger(review_ledger([review_run()], [finding()]))

    def test_terminal_high_tradeoff_does_not_gate_by_severity(self):
        accepted = finding("Accepted Tradeoff", "Accepted", severity="High")
        check_ledger(review_ledger([review_run()], [accepted]))

    def test_invalid_disposition_status_pairing_is_rejected(self):
        invalid = finding("Blocker", "Resolved")
        invalid["status"] = "Accepted"
        invalid["status_history"][-1]["transition_to"] = "Accepted"
        with self.assertRaises(sem.ReviewLedgerError):
            check_ledger(
                review_ledger([review_run(convergence="NON-CONVERGED")], [invalid]))

    def test_round_budget_with_open_finding_records_non_converged(self):
        run = review_run(
            convergence="NON-CONVERGED", final=True, budget_exhausted=True)
        check_ledger(review_ledger([run], [finding()]))
        run["convergence_state"] = "CONVERGED"
        with self.assertRaises(sem.ReviewLedgerError):
            check_ledger(review_ledger([run], [finding()]))

    def test_stale_final_review_digest_and_missing_strict_context_fail_closed(self):
        run = review_run()
        ledger = review_ledger([run])
        with self.assertRaises(sem.ReviewStateUncertainty):
            sem.validate_review_ledger(ledger, prior_ledger=None)
        with self.assertRaises(sem.ReviewLedgerError):
            sem.validate_review_ledger(
                ledger,
                current_subject_digests={run["review_run_id"]: sem.digest({"subject": "changed"})},
                prior_ledger=None,
            )

    def test_review_runs_and_finding_status_history_are_append_only(self):
        initial_run = review_run(convergence="IN_PROGRESS", final=False)
        open_finding = finding()
        prior = review_ledger([initial_run], [open_finding])
        resolved = finding("Blocker", "Resolved")
        final_run = review_run("RUN-002", 2)
        current = review_ledger([initial_run, final_run], [resolved])
        sem.validate_review_ledger(current, prior_ledger=prior, strict=False)
        rewritten = copy.deepcopy(current)
        rewritten["findings"][0]["status_history"][0]["actor"] = "rewriter"
        with self.assertRaises(sem.ReviewLedgerError):
            sem.validate_review_ledger(rewritten, prior_ledger=prior, strict=False)

    def test_finding_cannot_be_retroactively_attached_to_a_later_round(self):
        first = review_run(convergence="IN_PROGRESS", final=False)
        second = review_run("RUN-002", 2, convergence="NON-CONVERGED")
        retroactive = finding()
        retroactive["parent_review_run_id"] = "RUN-002"
        with self.assertRaises(sem.ReviewLedgerError):
            check_ledger(
                review_ledger([first, second], [retroactive]))


def baseline_item(violation_id="VIOLATION-001"):
    return {
        "violation_id": violation_id,
        "binding": {
            "component_id": "component:match-host",
            "selector": method("Start", [], "unused")["selector"],
        },
        "baseline_subject_scope_digest": sem.digest({"violation": violation_id}),
        "creation_provenance_revision": "commit-1",
        "owner": "match-engine",
        "disposition": "Blocker",
        "required_action": "Wire the host.",
        "expiry_trigger": {"type": "milestone", "value": "A8 strict activation"},
    }


def baseline(mode="inactive", sealed=False, items=None):
    return {
        "schema_version": "1.0.0",
        "baseline_id": "architecture-governance-activation",
        "mode": mode,
        "sealed": sealed,
        "items": [] if items is None else items,
    }


def check_baseline(document, **kwargs):
    """Validate a baseline while exercising a rule other than the strict gates."""
    kwargs.setdefault("prior_baseline", None)
    kwargs.setdefault("current_violation_ids", None)
    kwargs.setdefault("strict", False)
    return sem.validate_temporary_activation_baseline(document, **kwargs)


class TemporaryActivationBaselineTests(unittest.TestCase):
    def test_strict_activation_requires_mechanically_empty_strict_baseline(self):
        check_baseline(baseline("strict", True), strict_activation=True)
        with self.assertRaises(sem.ActivationBaselineError):
            check_baseline(
                baseline("migration", True, [baseline_item()]),
                strict_activation=True,
            )

    def test_new_violation_is_not_silently_baselined(self):
        current = baseline("migration", True, [baseline_item()])
        with self.assertRaises(sem.ActivationBaselineError):
            sem.validate_temporary_activation_baseline(
                current,
                prior_baseline=None,
                current_violation_ids=["VIOLATION-001", "VIOLATION-NEW"],
            )

    def test_sealed_baseline_can_shrink_but_cannot_grow_or_rewrite(self):
        prior = baseline("migration", True, [baseline_item()])
        strict = baseline("strict", True)
        check_baseline(strict, prior_baseline=prior)

        grown = baseline(
            "migration", True,
            [baseline_item(), baseline_item("VIOLATION-002")],
        )
        with self.assertRaises(sem.ActivationBaselineError):
            check_baseline(grown, prior_baseline=prior)

        rewritten = copy.deepcopy(prior)
        rewritten["items"][0]["owner"] = "different-owner"
        with self.assertRaises(sem.ActivationBaselineError):
            check_baseline(rewritten, prior_baseline=prior)


if __name__ == "__main__":
    unittest.main()


def approved_limitation():
    return {
        "authority_ref": "#19 FR-TS-094",
        "approval_ref": "owner-approval-2026-09-01",
        "justification": "The surface cannot execute until A8 provisions the SDK.",
        "omitted_surface_or_uncertainty": "GkHeadingWorldAdapter.ApplyKick",
    }


def proof_execution(execution_id="EXEC-001", state="passed"):
    return {
        "execution_id": execution_id,
        "command_or_test": "dotnet test MatchEngine.Tests",
        "runner": "linux-shim-gate",
        "environment": "ubuntu-24.04/dotnet-8.0",
        "subject_scope_digest": sem.digest({"subject": "proof"}),
        "execution_state": state,
        "started_at": "2026-09-01T00:00:00Z",
        "ended_at": "2026-09-01T00:01:00Z",
    }


def proof_artifact(
        result="pass", proof_class="structural-reachability", executions=None, **extra):
    artifact = {
        "schema_version": "1.0.0",
        "proof_id": "PROOF-001",
        "proof_class": proof_class,
        "requirement_property_refs": ["AP-001"],
        "applicability_rule_ids": ["RULE-001"],
        "result": result,
        "subject_scope_digest": sem.digest({"subject": "proof"}),
        "dependency_closure": {
            "dependency_ids": ["component:match-host"],
            "edges": [{"from": "component:match-host", "to": "component:match-engine"}],
            "relation_policy_digest": sem.digest({"relations": "v1"}),
            "change_type": "pure-local-calculation",
        },
        "content_fingerprints": {"src/match-engine/MatchEngine.cs": sem.digest({"file": 1})},
        "configuration_fingerprints": {"tools/dotnet-ci/pins.json": sem.digest({"pins": 1})},
        "tool_identities": [{
            "tool_id": "architecture-governance-reference-semantics",
            "semantic_version": sem.REFERENCE_SEMANTICS_VERSION,
            "content_digest": sem.digest({"tool": 1}),
        }],
        "execution_records": [proof_execution()] if executions is None else executions,
        "created": {"actor": "governance-tooling", "at": "2026-09-01T00:02:00Z"},
        "revalidation_history": [],
    }
    artifact.update(extra)
    return artifact


class FailClosedDefaultTests(unittest.TestCase):
    """Omitting a cross-document input is uncertainty, never approval.

    `validate_property_registry` already established this posture; these lock the
    review-ledger and baseline validators to the same contract so a caller that
    forgets an argument cannot receive a silent pass.
    """

    def test_review_ledger_without_trusted_prior_reports_uncertainty(self):
        ledger = review_ledger([review_run(convergence="IN_PROGRESS", final=False)])
        with self.assertRaises(sem.ReviewStateUncertainty):
            sem.validate_review_ledger(ledger)
        # A trusted merge base proving no prior ledger existed is a real answer.
        sem.validate_review_ledger(ledger, prior_ledger=None)

    def test_final_review_without_current_digest_reports_uncertainty(self):
        run = review_run()
        with self.assertRaises(sem.ReviewStateUncertainty):
            sem.validate_review_ledger(review_ledger([run]), prior_ledger=None)

    def test_baseline_without_trusted_prior_reports_uncertainty(self):
        with self.assertRaises(sem.ActivationBaselineUncertainty):
            sem.validate_temporary_activation_baseline(
                baseline(), current_violation_ids=[])

    def test_baseline_without_current_violations_reports_uncertainty(self):
        with self.assertRaises(sem.ActivationBaselineUncertainty):
            sem.validate_temporary_activation_baseline(
                baseline(), prior_baseline=None)

    def test_absent_violation_discovery_is_not_the_empty_violation_set(self):
        """IP-4's core rule cannot be satisfied by supplying nothing."""
        current = baseline("migration", True, [baseline_item()])
        sem.validate_temporary_activation_baseline(
            current, prior_baseline=None, current_violation_ids=["VIOLATION-001"])
        with self.assertRaises(sem.ActivationBaselineUncertainty):
            sem.validate_temporary_activation_baseline(
                current, prior_baseline=None, current_violation_ids=None)

    def test_strict_activation_is_not_folded_into_strict(self):
        """`strict_activation` adds a requirement; it never relaxes one."""
        sem.validate_temporary_activation_baseline(
            baseline(), prior_baseline=None, current_violation_ids=[])
        with self.assertRaises(sem.ActivationBaselineError):
            sem.validate_temporary_activation_baseline(
                baseline(), prior_baseline=None, current_violation_ids=[],
                strict_activation=True)


class ProofArtifactTests(unittest.TestCase):
    def test_representative_pass_artifact_validates(self):
        sem.validate_proof_artifact(proof_artifact())

    def test_unknown_field_and_bad_digest_fail_closed(self):
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(proof_artifact(stowaway="x"))
        artifact = proof_artifact()
        artifact["subject_scope_digest"] = "not-a-digest"
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(artifact)

    def test_na_and_bounded_records_are_exclusive_to_their_result(self):
        sem.validate_proof_artifact(proof_artifact("na", na=approved_limitation()))
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(proof_artifact("na"))
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(
                proof_artifact("pass", na=approved_limitation()))
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(
                proof_artifact("pass", bounded_substitute=approved_limitation()))

    def test_pass_result_cannot_outrun_a_non_passing_execution(self):
        for state in ("failed", "skipped", "not-run", "runner-failed"):
            with self.assertRaises(sem.ProofArtifactError):
                sem.validate_proof_artifact(
                    proof_artifact(executions=[proof_execution(state=state)]))

    def test_bounded_result_converts_only_the_permitted_states(self):
        artifact = proof_artifact(
            "bounded",
            executions=[proof_execution(state="unavailable")],
            bounded_substitute=approved_limitation(),
        )
        sem.validate_proof_artifact(artifact, bounded_substitute_permitted=True)
        # #19 permission is not implied by the record's own presence.
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(artifact)
        # An execution that ran and failed is never convertible.
        failed = proof_artifact(
            "bounded",
            executions=[proof_execution(state="failed")],
            bounded_substitute=approved_limitation(),
        )
        with self.assertRaises(sem.ExecutionError):
            sem.validate_proof_artifact(failed, bounded_substitute_permitted=True)

    def test_bounded_result_admits_a_mixed_execution_set(self):
        """The substitute covers the omitted record, not the ones that ran."""
        mixed = proof_artifact(
            "bounded",
            executions=[
                proof_execution("EXEC-001", "passed"),
                proof_execution("EXEC-002", "excluded"),
            ],
            bounded_substitute=approved_limitation(),
        )
        sem.validate_proof_artifact(mixed, bounded_substitute_permitted=True)

    def test_empty_execution_set_is_not_rejected_by_the_record_contract(self):
        """Whether a proof class requires an execution is an applicability
        question; the frozen record contract does not invent that rule."""
        sem.validate_proof_artifact(proof_artifact(executions=[]))

    def test_proof_class_binds_its_evidence_record(self):
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(proof_artifact(proof_class="failure-injection"))
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(proof_artifact(proof_class="mutation"))
        injection = proof_artifact(
            proof_class="failure-injection",
            failure_injection={
                "condition_or_input": "force the composition root to throw",
                "target_selector": method("Start", [], "unused")["selector"],
                "expected_path": "MatchHost bootstrap aborts",
                "executed_command_or_test": "dotnet test --filter Bootstrap",
                "observed_result": "aborted as expected",
                "tool_environment_identity": "linux-shim-gate",
            },
        )
        sem.validate_proof_artifact(injection)
        # The evidence record cannot ride along on an unrelated proof class.
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(
                proof_artifact(failure_injection=injection["failure_injection"]))

    def test_mutation_requires_a_restored_clean_state(self):
        def mutation_artifact(restored):
            return proof_artifact(
                proof_class="mutation",
                mutation={
                    "base_subject_digest": sem.digest({"subject": "proof"}),
                    "target_selector": method("Start", [], "unused")["selector"],
                    "operator_or_mutant_digest": "negate-conditional",
                    "baseline_execution": "EXEC-001",
                    "mutant_execution": "EXEC-002",
                    "expected_detector": "BootstrapTests.Start_registers_host",
                    "observed_detector_failure": "failed as expected",
                    "tool_identity": "mutation-harness-1.0",
                    "restoration_clean_state": restored,
                },
            )
        sem.validate_proof_artifact(mutation_artifact(True))
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(mutation_artifact(False))

    def test_target_selector_must_resolve_when_facts_are_supplied(self):
        fact = method("Start", [], "M:Start()")
        injection = proof_artifact(
            proof_class="failure-injection",
            failure_injection={
                "condition_or_input": "force a throw",
                "target_selector": fact["selector"],
                "expected_path": "aborts",
                "executed_command_or_test": "dotnet test",
                "observed_result": "aborted",
                "tool_environment_identity": "linux-shim-gate",
            },
        )
        sem.validate_proof_artifact(injection, semantic_facts=[fact])
        with self.assertRaises(sem.ProofArtifactError):
            sem.validate_proof_artifact(
                injection, semantic_facts=[method("Other", [], "M:Other()")])

    def test_validated_artifact_survives_revalidation(self):
        normalized = sem.validate_proof_artifact(proof_artifact())
        sem.validate_proof_artifact(normalized)


VALIDATOR_PATH = (
    Path(__file__).resolve().parents[1] / "architecture-governance" / "schema_validator.py")
_VALIDATOR_SPEC = importlib.util.spec_from_file_location(
    "architecture_governance_schema_validator", VALIDATOR_PATH)
jsv = importlib.util.module_from_spec(_VALIDATOR_SPEC)
_VALIDATOR_SPEC.loader.exec_module(jsv)


def surface_document():
    return {
        "schema_version": "1.0.0",
        "surfaces": [{
            "surface_id": "SURFACE-001",
            "symbol_key": "M:Example.Component.Start()",
            "kind": "method",
            "source_path": "src/match-engine/MatchEngine.cs",
            "signature": "public void Start()",
            "assembly": "Example.Runtime",
            "classification": "production-runtime-root",
            "component_id": "component:match-engine",
        }],
    }


def contract_document():
    disabled = field(
        "TackleContactRadiusM", "F:TackleContactRadiusM",
        {"value_type": "number", "value": 0})
    return {
        "schema_version": "1.0.0",
        "contracts": [{
            "contract_id": "CONTRACT-001",
            "component_id": "component:tackling",
            "current_selector": disabled["selector"],
            "selector_history": [],
            "owning_host": "MatchHost",
            "owning_assembly": "Example.Runtime",
            "composition_root": "MatchHost.Compose",
            "construction_path": "MatchHost.Compose -> Tackling",
            "activation_phase": "startup",
            "update_use_owner": "MatchEngine.Tick",
            "teardown_owner": "MatchHost.Dispose",
            "relevant_testhost_path": "MatchEngine.Tests",
            "alternate_supported_paths": [],
            "prohibited_bypass_paths": [],
            "static_initialization_involved": False,
            "lifecycle_ordering_requirements": [],
            "na_fields": [],
            "activation_state": "intentionally-disabled",
            "activation_owner": "match-engine",
            "decision_ref": "KD-TACKLE-001",
            "disable_anchor": {
                "selector": disabled["selector"],
                "operator": "equals",
                "expected": {"value_type": "number", "value": 0},
            },
            "reactivation_condition": "integration contract is completed",
            "tuning_surface_selectors": [disabled["selector"]],
        }],
    }


class BoundedSchemaValidatorTests(unittest.TestCase):
    ROOT = Path(__file__).resolve().parents[2] / "docs" / "tracking" / "architecture-governance"

    def setUp(self):
        self.schemas = jsv.default_schema_set()

    def test_every_schema_declares_an_id_so_relative_refs_resolve(self):
        """Without `$id` a relative `$ref` has no base URI outside file loading."""
        for name, document in sorted(self.schemas.by_name.items()):
            self.assertEqual(
                "https://schemas.tactical-director.internal/architecture-governance/" + name,
                document["$id"],
                "%s must declare its canonical $id" % name,
            )
        # Cross-file refs resolve through real URI resolution, not filename lookup.
        target, _ = self.schemas.resolve(
            "common.schema.json#/$defs/sha256",
            self.schemas.by_name["proof-artifact.schema.json"]["$id"])
        self.assertEqual("^[0-9a-f]{64}$", target["pattern"])

    def test_validator_implements_every_keyword_the_schemas_use(self):
        """A silently unimplemented keyword would make every differential vacuous."""
        used = set()
        for document in self.schemas.by_name.values():
            used |= jsv.SchemaSet.used_keywords(document)
        unimplemented = used - jsv.SUPPORTED_KEYWORDS - jsv.ANNOTATION_KEYWORDS
        self.assertEqual(set(), unimplemented)

    def test_unimplemented_keyword_and_missing_id_are_rejected_loudly(self):
        import shutil
        import tempfile
        directory = tempfile.mkdtemp()
        try:
            for path in (self.ROOT / "schemas").glob("*.json"):
                shutil.copy(path, directory)
            target = Path(directory) / "exceptions.schema.json"
            document = json.loads(target.read_text(encoding="utf-8"))
            document["properties"]["exceptions"]["contains"] = {"type": "object"}
            target.write_text(json.dumps(document), encoding="utf-8")
            with self.assertRaises(jsv.UnsupportedKeyword):
                jsv.SchemaSet(directory)

            target.write_text(json.dumps(
                json.loads((self.ROOT / "schemas" / "exceptions.schema.json").read_text(
                    encoding="utf-8"))), encoding="utf-8")
            common = Path(directory) / "common.schema.json"
            document = json.loads(common.read_text(encoding="utf-8"))
            del document["$id"]
            common.write_text(json.dumps(document), encoding="utf-8")
            with self.assertRaises(jsv.SchemaValidatorError):
                jsv.SchemaSet(directory)
        finally:
            shutil.rmtree(directory)

    def test_validator_is_not_vacuous(self):
        """Each implemented keyword class actually rejects a violating document."""
        cases = [
            ({"schema_version": "1.0.0"}, "property-registry.schema.json"),
            ({"schema_version": "9.0.0", "properties": []},
             "property-registry.schema.json"),
            ({"schema_version": "1.0.0", "properties": [], "stowaway": 1},
             "property-registry.schema.json"),
            ({"schema_version": "1.0.0", "baseline_id": "b", "mode": "strict",
              "sealed": False, "items": []},
             "temporary-activation-baseline.schema.json"),
        ]
        for document, schema in cases:
            self.assertTrue(
                self.schemas.validate(document, schema),
                "%s should have been rejected" % schema)

    def test_seed_artifacts_satisfy_their_schemas(self):
        for name, schema in (
                ("runtime-surface-classifications.json",
                 "runtime-surface-classifications.schema.json"),
                ("integration-contracts.json", "integration-contracts.schema.json"),
                ("applicability-rules.json", "applicability-rules.schema.json"),
                ("property-registry.json", "property-registry.schema.json"),
                ("exceptions.json", "exceptions.schema.json"),
                ("review-ledger.json", "review-ledger.schema.json"),
                ("temporary-activation-baseline.json",
                 "temporary-activation-baseline.schema.json"),
        ):
            document = json.loads((self.ROOT / name).read_text(encoding="utf-8"))
            self.assertEqual([], self.schemas.validate(document, schema), name)

    def test_semantically_valid_fixtures_also_satisfy_their_schemas(self):
        """The frozen shape and the executable semantics must not drift apart.

        The implication is deliberately ONE-directional. The semantic validators
        enforce cross-record rules — append-only history, legal transitions,
        dependency closure, Disposition x Status — that JSON Schema cannot
        express, so a schema-valid document the semantics reject is correct
        behaviour, not drift. What must never happen is the reverse: a document
        the semantics bless that violates the contract producers code against.
        """
        cases = []

        def case(label, schema, document, semantic):
            cases.append((label, schema, document, semantic))

        properties = property_registry(property_record("Admitted", True))
        case("property registry", "property-registry.schema.json", properties,
             lambda doc: sem.validate_property_registry(doc, None))
        case("exception registry", "exceptions.schema.json",
             {"schema_version": "1.0.0", "exceptions": [exception_record()]},
             lambda doc: sem.validate_exception_registry(doc, properties))
        case("applicability rules", "applicability-rules.schema.json",
             {"schema_version": "1.0.0",
              "rules": [applicability_rule(
                  "RULE-001", "trigger:a", "AP-001",
                  classifications=["production-runtime-root"])]},
             sem.validate_applicability_rules_document)
        case("runtime surfaces", "runtime-surface-classifications.schema.json",
             surface_document(), sem.validate_runtime_surface_classifications_document)
        case("integration contracts", "integration-contracts.schema.json",
             contract_document(), sem.validate_integration_contracts_document)

        run = review_run()
        case("review ledger", "review-ledger.schema.json",
             review_ledger([run], [finding("Accepted Tradeoff", "Accepted")]),
             lambda doc: sem.validate_review_ledger(
                 doc,
                 current_subject_digests={
                     run["review_run_id"]: run["subject_scope_digest"]},
                 prior_ledger=None))
        case("activation baseline", "temporary-activation-baseline.schema.json",
             baseline("migration", True, [baseline_item()]),
             lambda doc: sem.validate_temporary_activation_baseline(
                 doc, prior_baseline=None, current_violation_ids=["VIOLATION-001"]))
        case("proof artifact", "proof-artifact.schema.json",
             proof_artifact(), sem.validate_proof_artifact)
        case("proof artifact (na)", "proof-artifact.schema.json",
             proof_artifact("na", na=approved_limitation()), sem.validate_proof_artifact)

        for label, schema, document, semantic in cases:
            semantic(copy.deepcopy(document))
            self.assertEqual(
                [], self.schemas.validate(document, schema),
                "%s: accepted by the semantics but violates %s" % (label, schema))

    def test_schema_validator_keeps_the_ci_pure_stdlib(self):
        tree = ast.parse(VALIDATOR_PATH.read_text(encoding="utf-8"))
        roots = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                roots.update(alias.name.split(".")[0] for alias in node.names)
            elif isinstance(node, ast.ImportFrom):
                roots.add(node.module.split(".")[0])
        self.assertEqual({"json", "re", "pathlib", "urllib"}, roots)


class ExceptionAuthorityBoundaryTests(unittest.TestCase):
    """A2-FR-001: a property may not squat a #19/#20 requirement namespace.

    `exception_route` evaluates its property branch first, so a property
    registered as `FR-CS-046` captured that requirement's routing and reported
    `governance_exception_allowed` — silently moving a Code Standards waiver into
    `exceptions.json`, the crossing integration-plan §3.6 forbids.
    """

    def test_property_cannot_claim_a_foreign_requirement_id(self):
        for foreign in ("FR-CS-046", "FR-TS-094"):
            record = property_record("Admitted", True)
            record["property_id"] = foreign
            with self.assertRaises(sem.PropertyRegistryError):
                sem.validate_property_registry(property_registry(record), None)

    def test_foreign_requirements_still_route_to_their_owners(self):
        properties = property_registry(property_record("Admitted", True))
        self.assertEqual(
            {"route": "code-standards-owner", "governance_exception_allowed": False},
            sem.exception_route("FR-CS-046", properties))
        self.assertEqual(
            {"route": "testing-strategy-owner", "governance_exception_allowed": False},
            sem.exception_route("FR-TS-094", properties))

    def test_an_admitted_property_cites_a_requirement_without_taking_its_id(self):
        """§3.6's carve-out survives: the AP keeps its own id and cites the FR."""
        record = property_record("Admitted", True)
        record["property_id"] = "AP-046"
        record["authority"] = "FR-CS-046"
        properties = property_registry(record)
        sem.validate_property_registry(properties, None)
        self.assertTrue(
            sem.exception_route("AP-046", properties)["governance_exception_allowed"])

    def test_schema_and_semantics_agree_on_the_foreign_namespaces(self):
        schemas = jsv.default_schema_set()
        for prefix in sem._FOREIGN_REQUIREMENT_PREFIXES:
            record = property_record("Admitted", True)
            record["property_id"] = prefix + "001"
            self.assertTrue(
                schemas.validate(
                    property_registry(record), "property-registry.schema.json"),
                "%s must be rejected by the schema as well" % prefix)


class DurableReviewLedgerTests(unittest.TestCase):
    """The committed A2 review ledger is real evidence, not a seed.

    Integration-plan §3.8 requires new governance-aware reviews to use the durable
    ledger prospectively, and a final-review marker to bind the MATERIAL review
    subject. These lock that the committed record actually validates and that its
    recorded digest recomputes -- otherwise the closure record's digest bundle is
    prose rather than something a later reviewer can check.
    """

    ROOT = Path(__file__).resolve().parents[2]
    LEDGER = ROOT / "docs" / "tracking" / "architecture-governance" / "review-ledger.json"

    # One definition of the material subject, used by both the working-tree and
    # the historical-tree digests so they cannot drift apart.
    SUBJECT_DIRS = (
        "docs/tracking/architecture-governance/schemas/",
        "docs/tracking/architecture-governance/",
        "tools/architecture-governance/",
    )
    SUBJECT_FILE = "tools/tests/test_architecture_governance_semantics.py"
    SUBJECT_EXCLUDED = "docs/tracking/architecture-governance/review-ledger.json"

    @classmethod
    def in_material_subject(cls, rel):
        """The frozen contract itself -- not the record of reviewing it.

        review-ledger.json is excluded deliberately: §3.8 says recording the
        review run must not recursively invalidate the subject it records.
        Tracking prose is excluded for the same reason -- it is not the contract.
        """
        if rel == cls.SUBJECT_EXCLUDED:
            return False
        if rel == cls.SUBJECT_FILE:
            return True
        if not (rel.endswith(".json") or rel.endswith(".py")):
            return False
        return any(
            rel.startswith(prefix) and "/" not in rel[len(prefix):]
            for prefix in cls.SUBJECT_DIRS)

    @classmethod
    def material_subject_digest(cls):
        """Material subject digest of the working tree."""
        import hashlib
        files = {}
        for path in sorted(cls.ROOT.rglob("*")):
            if not path.is_file():
                continue
            rel = path.relative_to(cls.ROOT).as_posix()
            if cls.in_material_subject(rel):
                files[rel] = hashlib.sha256(path.read_bytes()).hexdigest()
        return sem.digest(files)

    def ledger(self):
        return json.loads(self.LEDGER.read_text(encoding="utf-8"))

    def test_committed_ledger_validates_against_the_frozen_contract(self):
        """Structural validation only -- freshness is proved elsewhere.

        An earlier version fed the ledger digests taken FROM the ledger, which
        could only ever agree with itself. It was doubly empty: the freshness
        branch fires only for a `final_review` run and no run carries one. The
        real binding is test_every_round_digest_recomputes_from_the_tree_it_names.
        """
        ledger = self.ledger()
        sem.validate_review_ledger(ledger, prior_ledger=None)
        self.assertEqual(
            [], jsv.default_schema_set().validate(ledger, "review-ledger.schema.json"))

    def test_every_round_digest_recomputes_from_the_tree_it_names(self):
        """Prove each digest IS its named tree -- all of them, or none.

        Verification is deliberately ALL-OR-NOTHING. An earlier version skipped
        unavailable revisions one at a time and skipped the test only when none
        resolved, so a partial-history checkout could verify one digest of five,
        ignore the rest, and report a green tick under a name asserting all of
        them. The default CI checkout is shallow, so that is the expected
        environment rather than an edge case: a partial result must never be
        able to present itself as a complete one.
        """
        import subprocess
        runs = self.ledger()["review_runs"]
        revisions = {run["review_run_id"]: self.named_revision(run) for run in runs}
        missing = sorted({
            revision for revision in revisions.values()
            if subprocess.run(
                ["git", "-C", str(self.ROOT), "cat-file", "-e", revision + "^{commit}"],
                capture_output=True).returncode != 0})
        if missing:
            self.skipTest(
                "history absent for %s -- verification is all-or-nothing"
                % ", ".join(missing))
        for run in runs:
            revision = revisions[run["review_run_id"]]
            self.assertEqual(
                self.subject_digest_at(revision), run["subject_scope_digest"],
                "%s digest does not match %s" % (run["review_run_id"], revision))

    def test_every_round_names_the_revision_it_reviewed(self):
        """Git-independent: the ledger must stay self-describing.

        Deliberately NOT a distinctness check. Two rounds may legitimately
        review an unchanged material subject and correctly carry the same
        digest; governance requires each digest to match its named subject, not
        to differ from its neighbours.
        """
        for run in self.ledger()["review_runs"]:
            self.assertIsNotNone(
                self.named_revision(run),
                "%s does not name the revision it reviewed" % run["review_run_id"])

    @classmethod
    def named_revision(cls, run):
        import re
        match = re.search(r"\bat ([0-9a-f]{7,40})\b", " ".join(run["review_scope"]))
        return match.group(1) if match else None

    def test_status_history_is_neither_future_dated_nor_out_of_order(self):
        """A durable record cannot contain events that have not happened.

        Round 4's events were once stamped 69 minutes after the commit that
        asserted they were complete. Timestamps now derive from real commits: a
        finding is raised at the commit time of the artifact reviewed and
        resolved at the commit time that carried the fix.
        """
        import datetime
        now = datetime.datetime.now(datetime.timezone.utc)
        for item in self.ledger()["findings"]:
            previous = None
            for event in item["status_history"]:
                stamp = datetime.datetime.fromisoformat(
                    event["at"].replace("Z", "+00:00"))
                self.assertLessEqual(
                    stamp, now,
                    "%s records a future event at %s"
                    % (item["finding_id"], event["at"]))
                if previous is not None:
                    self.assertGreaterEqual(
                        stamp, previous,
                        "%s status history runs backwards" % item["finding_id"])
                previous = stamp

    def test_closure_condition_4_is_only_claimed_with_a_review_of_this_tree(self):
        """Mechanise the gate condition instead of guessing at its shape.

        Condition 4 requires a fresh review of the candidate as pushed. So the
        rule is not "the current tree must be unreviewed" -- a round may
        legitimately review it -- but "row 4 may say Complete only if some
        recorded round's digest IS the current material subject".
        """
        record = (self.ROOT / "docs" / "tracking"
                  / "a2-schema-semantics-closure.md").read_text(encoding="utf-8")
        row = next(
            line for line in record.splitlines()
            if line.startswith("| 4 | Fresh review over pushed current candidate"))
        recorded = {run["subject_scope_digest"] for run in self.ledger()["review_runs"]}
        reviewed = self.material_subject_digest() in recorded
        if "**Complete**" in row:
            self.assertTrue(
                reviewed,
                "row 4 claims Complete but no recorded round reviewed this tree")
        else:
            self.assertIn("**PENDING**", row, "row 4 must be Complete or PENDING")

    @classmethod
    def subject_digest_at(cls, revision):
        """Material subject digest of a committed tree, by the same rule."""
        import hashlib
        import subprocess
        listing = subprocess.run(
            ["git", "-C", str(cls.ROOT), "ls-tree", "-r", "--name-only", revision],
            capture_output=True, text=True, check=True).stdout.split()
        files = {}
        for rel in sorted(listing):
            if not cls.in_material_subject(rel):
                continue
            blob = subprocess.run(
                ["git", "-C", str(cls.ROOT), "show", "%s:%s" % (revision, rel)],
                capture_output=True, check=True).stdout
            files[rel] = hashlib.sha256(blob).hexdigest()
        return sem.digest(files)

    def test_every_recorded_finding_is_terminal(self):
        """Closure condition 5: no open or invalid finding remains."""
        ledger = self.ledger()
        self.assertTrue(ledger["findings"], "the ledger must carry the A2 findings")
        for item in ledger["findings"]:
            self.assertNotEqual("Open", item["status"], item["finding_id"])
            self.assertEqual(
                sem._DISPOSITION_TERMINAL_STATUS[item["disposition"]],
                item["status"], item["finding_id"])

    def test_no_run_claims_convergence_while_the_owner_gate_is_open(self):
        """A2 closure conditions 6 and 7 are the owner's; an agent cannot self-close."""
        for run in self.ledger()["review_runs"]:
            self.assertFalse(run["final_review"], run["review_run_id"])
            self.assertNotEqual("CONVERGED", run["convergence_state"], run["review_run_id"])
