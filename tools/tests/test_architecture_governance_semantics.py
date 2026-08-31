# File: tools/tests/test_architecture_governance_semantics.py
# Created: August 31, 2026
# Purpose: A2 fixtures for selector/identity/activation, applicability, and proof freshness semantics.

import copy
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
    def test_reference_semantics_version_is_pinned(self):
        self.assertEqual("1.3.0", sem.REFERENCE_SEMANTICS_VERSION)

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


    def test_deleted_tuning_surface_is_reported_without_crashing(self):
        violations = sem.kd_w1_violations(
            [self.other["selector"]],
            [self.contract()],
            [self.disabled],
        )
        self.assertEqual(1, len(violations))
        self.assertEqual([], violations[0]["changed_symbol_keys"])
        self.assertEqual(
            [sem.selector_key(self.other["selector"])],
            violations[0]["unresolved_selector_keys"],
        )

    def test_stale_tuning_selector_is_reported_without_changed_surface(self):
        violations = sem.kd_w1_violations(
            [],
            [self.contract()],
            [self.disabled],
        )
        self.assertEqual(1, len(violations))
        self.assertEqual([], violations[0]["changed_selector_keys"])
        self.assertEqual(
            [sem.selector_key(self.other["selector"])],
            violations[0]["unresolved_selector_keys"],
        )

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

    def test_non_finite_numbers_are_rejected(self):
        with self.assertRaises(sem.ActivationError):
            sem.normalize_typed_value({"value_type": "number", "value": float("nan")})
        with self.assertRaises(sem.SemanticsError):
            sem.digest({"value": float("inf")})


def applicability_rule(
        rule_id, trigger_ref, requirement_ref, proof_class="structural-reachability",
        selectors=None, component_ids=None, assemblies=None, classifications=None,
        activation_states=None, fallback_scope=None, allowed_na_reasons=None):
    selectors = selectors or []
    component_ids = component_ids or []
    assemblies = assemblies or []
    classifications = classifications or []
    activation_states = activation_states or []
    precedence = (
        (16 if selectors else 0)
        | (8 if component_ids else 0)
        | (4 if assemblies else 0)
        | (2 if classifications else 0)
        | (1 if activation_states else 0)
    )
    return {
        "rule_id": rule_id,
        "selectors": selectors,
        "component_ids": component_ids,
        "assemblies": assemblies,
        "classifications": classifications,
        "activation_states": activation_states,
        "trigger_ref": trigger_ref,
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


def proof_resolution(subject=None, proof_class="structural-reachability"):
    rule = applicability_rule(
        "AR-X",
        "TRIGGER-X",
        "FR-X",
        proof_class=proof_class,
        fallback_scope="repository",
    )
    return sem.resolve_applicability(
        subject or {"classification": "production-runtime-root"},
        [rule],
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
    def test_structural_closure_includes_structural_and_tool_semantics_only(self):
        closure = sem.derive_proof_closure(
            "structural-reachability",
            proof_resolution(),
            proof_graph(),
        )
        self.assertIn("symbol:child", closure["dependency_ids"])
        self.assertIn("asmdef:runtime", closure["dependency_ids"])
        self.assertIn("tool:extractor", closure["dependency_ids"])
        self.assertNotIn("life:start", closure["dependency_ids"])
        self.assertNotIn("serializer:save", closure["dependency_ids"])
        self.assertNotIn("test:proof", closure["dependency_ids"])

    def test_lifecycle_and_persistence_expand_the_structural_closure(self):
        lifecycle = sem.derive_proof_closure(
            "lifecycle-order",
            proof_resolution(proof_class="lifecycle-order"),
            proof_graph(),
        )
        persistence = sem.derive_proof_closure(
            "persistence-external-resource",
            proof_resolution(proof_class="persistence-external-resource"),
            proof_graph(),
        )
        self.assertIn("life:start", lifecycle["dependency_ids"])
        self.assertNotIn("serializer:save", lifecycle["dependency_ids"])
        self.assertIn("life:start", persistence["dependency_ids"])
        self.assertIn("serializer:save", persistence["dependency_ids"])

    def test_executable_closure_adds_test_runner_and_environment_path(self):
        closure = sem.derive_proof_closure(
            "failure-injection",
            proof_resolution(proof_class="failure-injection"),
            proof_graph(),
        )
        self.assertIn("test:proof", closure["dependency_ids"])
        self.assertIn("runner:dotnet", closure["dependency_ids"])

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
        })
        result = sem.assess_proof_freshness(
            self.snapshot(), current, proof_graph())
        self.assertFalse(result["fresh"])
        self.assertIn("applicability-subject-changed", result["reasons"])

    def test_changed_optimization_falls_back_for_unmapped_surface(self):
        decision = sem.changed_proof_decision(
            self.snapshot(),
            proof_resolution(),
            proof_graph(),
            ["new:file:not-in-inventory"],
        )
        self.assertTrue(decision["run_required"])
        self.assertEqual("unmapped-changed-surface", decision["reason"])

    def test_changed_optimization_can_skip_known_unrelated_material(self):
        decision = sem.changed_proof_decision(
            self.snapshot(),
            proof_resolution(),
            proof_graph(),
            ["unrelated:docs"],
        )
        self.assertFalse(decision["run_required"])
        self.assertEqual("material-scope-unchanged", decision["reason"])


if __name__ == "__main__":
    unittest.main()
