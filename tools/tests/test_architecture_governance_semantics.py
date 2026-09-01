# File: tools/tests/test_architecture_governance_semantics.py
# Created: August 31, 2026
# Purpose: A2 fixtures for selector/identity/activation, applicability, and proof freshness semantics.

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

    def test_seven_seed_artifacts_validate(self):
        sem.validate_runtime_surface_classifications_document(
            self.load("runtime-surface-classifications.json"))
        sem.validate_integration_contracts_document(
            self.load("integration-contracts.json"))
        sem.validate_applicability_rules_document(
            self.load("applicability-rules.json"))
        properties = self.load("property-registry.json")
        sem.validate_property_registry(properties, None)
        sem.validate_exception_registry(self.load("exceptions.json"), properties)
        sem.validate_review_ledger(self.load("review-ledger.json"))
        sem.validate_temporary_activation_baseline(
            self.load("temporary-activation-baseline.json"))

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


class ReviewLedgerTests(unittest.TestCase):
    def test_clean_zero_finding_final_review_can_converge(self):
        run = review_run()
        sem.validate_review_ledger(
            review_ledger([run]),
            current_subject_digests={run["review_run_id"]: run["subject_scope_digest"]},
            strict_freshness=True,
        )

    def test_open_low_blocker_prevents_convergence(self):
        with self.assertRaises(sem.ReviewLedgerError):
            sem.validate_review_ledger(review_ledger([review_run()], [finding()]))

    def test_terminal_high_tradeoff_does_not_gate_by_severity(self):
        accepted = finding("Accepted Tradeoff", "Accepted", severity="High")
        sem.validate_review_ledger(review_ledger([review_run()], [accepted]))

    def test_invalid_disposition_status_pairing_is_rejected(self):
        invalid = finding("Blocker", "Resolved")
        invalid["status"] = "Accepted"
        invalid["status_history"][-1]["transition_to"] = "Accepted"
        with self.assertRaises(sem.ReviewLedgerError):
            sem.validate_review_ledger(
                review_ledger([review_run(convergence="NON-CONVERGED")], [invalid]))

    def test_round_budget_with_open_finding_records_non_converged(self):
        run = review_run(
            convergence="NON-CONVERGED", final=True, budget_exhausted=True)
        sem.validate_review_ledger(review_ledger([run], [finding()]))
        run["convergence_state"] = "CONVERGED"
        with self.assertRaises(sem.ReviewLedgerError):
            sem.validate_review_ledger(review_ledger([run], [finding()]))

    def test_stale_final_review_digest_and_missing_strict_context_fail_closed(self):
        run = review_run()
        ledger = review_ledger([run])
        with self.assertRaises(sem.ReviewStateUncertainty):
            sem.validate_review_ledger(ledger, strict_freshness=True)
        with self.assertRaises(sem.ReviewLedgerError):
            sem.validate_review_ledger(
                ledger,
                current_subject_digests={run["review_run_id"]: sem.digest({"subject": "changed"})},
                strict_freshness=True,
            )

    def test_review_runs_and_finding_status_history_are_append_only(self):
        initial_run = review_run(convergence="IN_PROGRESS", final=False)
        open_finding = finding()
        prior = review_ledger([initial_run], [open_finding])
        resolved = finding("Blocker", "Resolved")
        final_run = review_run("RUN-002", 2)
        current = review_ledger([initial_run, final_run], [resolved])
        sem.validate_review_ledger(current, prior_ledger=prior)
        rewritten = copy.deepcopy(current)
        rewritten["findings"][0]["status_history"][0]["actor"] = "rewriter"
        with self.assertRaises(sem.ReviewLedgerError):
            sem.validate_review_ledger(rewritten, prior_ledger=prior)

    def test_finding_cannot_be_retroactively_attached_to_a_later_round(self):
        first = review_run(convergence="IN_PROGRESS", final=False)
        second = review_run("RUN-002", 2, convergence="NON-CONVERGED")
        retroactive = finding()
        retroactive["parent_review_run_id"] = "RUN-002"
        with self.assertRaises(sem.ReviewLedgerError):
            sem.validate_review_ledger(
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


class TemporaryActivationBaselineTests(unittest.TestCase):
    def test_strict_activation_requires_mechanically_empty_strict_baseline(self):
        sem.validate_temporary_activation_baseline(
            baseline("strict", True), strict_activation=True)
        with self.assertRaises(sem.ActivationBaselineError):
            sem.validate_temporary_activation_baseline(
                baseline("migration", True, [baseline_item()]),
                strict_activation=True,
            )

    def test_new_violation_is_not_silently_baselined(self):
        current = baseline("migration", True, [baseline_item()])
        with self.assertRaises(sem.ActivationBaselineError):
            sem.validate_temporary_activation_baseline(
                current,
                current_violation_ids=["VIOLATION-001", "VIOLATION-NEW"],
            )

    def test_sealed_baseline_can_shrink_but_cannot_grow_or_rewrite(self):
        prior = baseline("migration", True, [baseline_item()])
        strict = baseline("strict", True)
        sem.validate_temporary_activation_baseline(strict, prior_baseline=prior)

        grown = baseline(
            "migration", True,
            [baseline_item(), baseline_item("VIOLATION-002")],
        )
        with self.assertRaises(sem.ActivationBaselineError):
            sem.validate_temporary_activation_baseline(grown, prior_baseline=prior)

        rewritten = copy.deepcopy(prior)
        rewritten["items"][0]["owner"] = "different-owner"
        with self.assertRaises(sem.ActivationBaselineError):
            sem.validate_temporary_activation_baseline(rewritten, prior_baseline=prior)


if __name__ == "__main__":
    unittest.main()
