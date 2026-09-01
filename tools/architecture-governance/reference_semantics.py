#!/usr/bin/env python3
# File: tools/architecture-governance/reference_semantics.py
# Created: August 31, 2026
# Purpose: A2 executable reference semantics for typed compiler-fact selectors,
#          stable component identity, activation/KD-W1, deterministic applicability,
#          and proof dependency-closure/freshness. Consumes typed facts only;
#          this module never parses C# source.

import hashlib
import json
import math

REFERENCE_SEMANTICS_VERSION = "1.7.0"

_SELECTOR_KINDS = {"namespace", "type", "constructor", "method", "field", "property", "event"}
_ACTIVATION_STATES = {"active", "intentionally-disabled", "pending-integration", "unresolved"}
_VALUE_TYPES = {"boolean", "integer", "number", "string", "enum", "null"}
_ANCHOR_OPERATORS = {"equals", "not-equals"}


class SemanticsError(ValueError):
    pass


class SelectorError(SemanticsError):
    pass


class IdentityError(SemanticsError):
    pass


class ActivationError(SemanticsError):
    pass


def canonical_json(value):
    try:
        return json.dumps(
            value,
            ensure_ascii=False,
            sort_keys=True,
            separators=(",", ":"),
            allow_nan=False,
        )
    except (TypeError, ValueError) as exc:
        raise SemanticsError("value is not canonical JSON: %s" % exc) from exc


def digest(value):
    return hashlib.sha256(canonical_json(value).encode("utf-8")).hexdigest()


def _nonempty(value, field):
    item = value.get(field)
    if not isinstance(item, str) or not item.strip():
        raise SelectorError("selector.%s must be a non-empty string" % field)
    return item.strip()


def _exact(value, allowed):
    unknown = sorted(set(value) - set(allowed))
    if unknown:
        raise SelectorError("selector contains unknown field(s): %s" % ", ".join(unknown))


def _type_ids(value):
    raw = value.get("parameter_type_ids")
    if not isinstance(raw, list):
        raise SelectorError("selector.parameter_type_ids must be a list")
    result = []
    for index, item in enumerate(raw):
        if not isinstance(item, str) or not item.strip():
            raise SelectorError("selector.parameter_type_ids[%d] must be non-empty" % index)
        result.append(item.strip())
    return result


def normalize_selector(selector):
    """Validate selector-v1.

    All type ids are compiler-canonical ids. This reference implementation
    compares typed compiler facts and does not infer symbols from source text.
    Constructors carry is_static so .cctor cannot collide with .ctor.
    Properties carry parameter_type_ids so indexer overloads are addressable.
    """
    if not isinstance(selector, dict):
        raise SelectorError("selector must be an object")
    kind = selector.get("kind")
    if kind not in _SELECTOR_KINDS:
        raise SelectorError("selector.kind is invalid: %r" % kind)
    out = {"assembly": _nonempty(selector, "assembly"), "kind": kind}

    if kind == "namespace":
        _exact(selector, {"assembly", "kind", "namespace"})
        out["namespace"] = _nonempty(selector, "namespace")
        return out
    if kind == "type":
        _exact(selector, {"assembly", "kind", "type_id"})
        out["type_id"] = _nonempty(selector, "type_id")
        return out
    if kind == "constructor":
        _exact(selector, {
            "assembly", "kind", "containing_type_id",
            "parameter_type_ids", "is_static",
        })
        out["containing_type_id"] = _nonempty(selector, "containing_type_id")
        out["parameter_type_ids"] = _type_ids(selector)
        is_static = selector.get("is_static")
        if not isinstance(is_static, bool):
            raise SelectorError("selector.is_static must be boolean")
        if is_static and out["parameter_type_ids"]:
            raise SelectorError("static constructor selector cannot have parameters")
        out["is_static"] = is_static
        return out

    out["containing_type_id"] = _nonempty(selector, "containing_type_id")
    out["member_name"] = _nonempty(selector, "member_name")
    if kind == "method":
        _exact(selector, {
            "assembly", "kind", "containing_type_id", "member_name",
            "parameter_type_ids", "generic_arity", "is_static",
        })
        out["parameter_type_ids"] = _type_ids(selector)
        arity = selector.get("generic_arity")
        if not isinstance(arity, int) or isinstance(arity, bool) or arity < 0:
            raise SelectorError("selector.generic_arity must be an integer >= 0")
        out["generic_arity"] = arity
    elif kind == "property":
        _exact(selector, {
            "assembly", "kind", "containing_type_id", "member_name",
            "parameter_type_ids", "is_static",
        })
        out["parameter_type_ids"] = _type_ids(selector)
    else:
        _exact(selector, {
            "assembly", "kind", "containing_type_id", "member_name", "is_static",
        })
    is_static = selector.get("is_static")
    if not isinstance(is_static, bool):
        raise SelectorError("selector.is_static must be boolean")
    out["is_static"] = is_static
    return out


def selector_key(selector):
    return "selector-v1:" + digest(normalize_selector(selector))


class SemanticFactIndex:
    """Validated reusable lookup for one typed compiler-fact universe."""

    def __init__(self, semantic_facts):
        self.by_selector = {}
        symbol_owner = {}
        for position, fact in enumerate(semantic_facts):
            if not isinstance(fact, dict) or "selector" not in fact:
                raise SelectorError("semantic fact[%d] must contain selector" % position)
            normalized = normalize_selector(fact["selector"])
            key = selector_key(normalized)
            symbol_key = fact.get("symbol_key")
            if not isinstance(symbol_key, str) or not symbol_key.strip():
                raise SelectorError("semantic fact[%d] has no symbol_key" % position)
            symbol_key = symbol_key.strip()
            previous_key = symbol_owner.get(symbol_key)
            if previous_key is not None and previous_key != key:
                raise SelectorError(
                    "symbol_key %s is claimed by multiple selectors" % symbol_key)
            symbol_owner[symbol_key] = key
            self.by_selector.setdefault(key, []).append(fact)


def _index_semantic_facts(semantic_facts):
    if isinstance(semantic_facts, SemanticFactIndex):
        return semantic_facts
    return SemanticFactIndex(semantic_facts)


def _resolve_from_index(selector, fact_index, allow_missing=False):
    target = normalize_selector(selector)
    matches = fact_index.by_selector.get(selector_key(target), [])
    if not matches:
        if allow_missing:
            return None
        raise SelectorError("selector does not resolve: %s" % canonical_json(target))
    if len(matches) != 1:
        raise SelectorError("selector resolves ambiguously to %d facts" % len(matches))
    return matches[0]


def resolve_selector(selector, semantic_facts):
    """Resolve against raw facts or a reusable SemanticFactIndex."""
    return _resolve_from_index(selector, _index_semantic_facts(semantic_facts))


def validate_component_identities(records, semantic_facts):
    """Bind current selectors while preserving non-resolving historical selectors."""
    if not isinstance(records, list):
        raise IdentityError("component records must be a list")
    try:
        fact_index = _index_semantic_facts(semantic_facts)
    except SelectorError as exc:
        raise IdentityError("invalid semantic fact universe: %s" % exc) from exc
    component_ids = set()
    selector_owner = {}
    symbol_owner = {}
    bindings = {}
    for index, record in enumerate(records):
        if not isinstance(record, dict):
            raise IdentityError("component[%d] must be an object" % index)
        component_id = record.get("component_id")
        if not isinstance(component_id, str) or not component_id.strip():
            raise IdentityError("component_id must be non-empty")
        component_id = component_id.strip()
        if component_id in component_ids:
            raise IdentityError("duplicate component_id: %s" % component_id)
        component_ids.add(component_id)
        if "current_selector" not in record:
            raise IdentityError("%s requires current_selector" % component_id)
        current = normalize_selector(record["current_selector"])
        history = record.get("selector_history", [])
        if not isinstance(history, list):
            raise IdentityError("selector_history must be a list")

        selectors = [current]
        for item in history:
            if not isinstance(item, dict) or "selector" not in item:
                raise IdentityError("selector_history entries require selector")
            unknown = sorted(set(item) - {"selector", "superseded_reason"})
            if unknown:
                raise IdentityError(
                    "selector_history contains unknown field(s): %s" % ", ".join(unknown))
            reason = item.get("superseded_reason")
            if not isinstance(reason, str) or not reason.strip():
                raise IdentityError("selector_history entries require superseded_reason")
            selectors.append(normalize_selector(item["selector"]))

        local = set()
        for item in selectors:
            key = selector_key(item)
            if key in local:
                raise IdentityError("%s repeats a current/history selector" % component_id)
            local.add(key)
            owner = selector_owner.get(key)
            if owner is not None and owner != component_id:
                raise IdentityError(
                    "selector is claimed by both %s and %s" % (owner, component_id))
            selector_owner[key] = component_id

        try:
            resolved = _resolve_from_index(current, fact_index)
        except SelectorError as exc:
            raise IdentityError(
                "%s current_selector does not resolve uniquely: %s"
                % (component_id, exc)) from exc
        symbol_key = resolved["symbol_key"].strip()
        owner = symbol_owner.get(symbol_key)
        if owner is not None and owner != component_id:
            raise IdentityError(
                "symbol_key %s is bound by both %s and %s"
                % (symbol_key, owner, component_id))
        symbol_owner[symbol_key] = component_id
        bindings[component_id] = symbol_key
    return bindings


def normalize_typed_value(value):
    if not isinstance(value, dict):
        raise ActivationError("typed value must be an object")
    unknown = sorted(set(value) - {"value_type", "value", "enum_type_id"})
    if unknown:
        raise ActivationError("typed value contains unknown field(s): %s" % ", ".join(unknown))
    kind = value.get("value_type")
    if kind not in _VALUE_TYPES:
        raise ActivationError("invalid value_type: %r" % kind)
    if "value" not in value:
        raise ActivationError("typed value requires value")
    raw = value["value"]
    if kind == "null" and raw is not None:
        raise ActivationError("null typed value requires value: null")
    if kind == "boolean" and not isinstance(raw, bool):
        raise ActivationError("boolean typed value requires bool")
    if kind == "integer" and (not isinstance(raw, int) or isinstance(raw, bool)):
        raise ActivationError("integer typed value requires int")
    if kind == "number":
        if not isinstance(raw, (int, float)) or isinstance(raw, bool):
            raise ActivationError("number typed value requires int or float")
        if not math.isfinite(float(raw)):
            raise ActivationError("number typed value must be finite")
    if kind == "string" and not isinstance(raw, str):
        raise ActivationError("string typed value requires string")
    out = {"value_type": kind, "value": raw}
    if kind == "enum":
        enum_type = value.get("enum_type_id")
        if not isinstance(enum_type, str) or not enum_type.strip():
            raise ActivationError("enum typed value requires enum_type_id")
        if not isinstance(raw, str) or not raw.strip():
            raise ActivationError("enum typed value requires named string value")
        out["enum_type_id"] = enum_type.strip()
    elif "enum_type_id" in value:
        raise ActivationError("enum_type_id is valid only for enum values")
    return out


def validate_activation_contract(contract):
    if not isinstance(contract, dict):
        raise ActivationError("integration contract must be an object")
    state = contract.get("activation_state")
    if state not in _ACTIVATION_STATES:
        raise ActivationError("invalid activation_state: %r" % state)
    if state == "intentionally-disabled":
        for field in ("activation_owner", "decision_ref", "reactivation_condition"):
            value = contract.get(field)
            if not isinstance(value, str) or not value.strip():
                raise ActivationError("intentionally-disabled requires %s" % field)
        if not isinstance(contract.get("disable_anchor"), dict):
            raise ActivationError("intentionally-disabled requires disable_anchor")
    elif state == "pending-integration":
        for field in ("activation_owner", "integration_gap", "activation_condition"):
            value = contract.get(field)
            if not isinstance(value, str) or not value.strip():
                raise ActivationError("pending-integration requires %s" % field)
    return state


def evaluate_disable_anchor(contract, semantic_facts):
    state = validate_activation_contract(contract)
    if state != "intentionally-disabled":
        raise ActivationError("disable-anchor evaluation requires intentionally-disabled state")
    anchor = contract["disable_anchor"]
    unknown = sorted(set(anchor) - {"selector", "operator", "expected"})
    if unknown:
        raise ActivationError("disable_anchor contains unknown field(s): %s" % ", ".join(unknown))
    if "selector" not in anchor or "expected" not in anchor:
        raise ActivationError("disable_anchor requires selector and expected")
    operator = anchor.get("operator")
    if operator not in _ANCHOR_OPERATORS:
        raise ActivationError("invalid disable-anchor operator: %r" % operator)
    expected = normalize_typed_value(anchor["expected"])
    try:
        fact = resolve_selector(anchor["selector"], semantic_facts)
    except SelectorError as exc:
        raise ActivationError(
            "disable_anchor selector does not resolve uniquely: %s" % exc) from exc
    if "value" not in fact:
        raise ActivationError("disable-anchor fact exposes no typed value")
    actual = normalize_typed_value(fact["value"])
    equal = actual == expected
    return {
        "passed": equal if operator == "equals" else not equal,
        "symbol_key": fact["symbol_key"],
        "operator": operator,
        "expected": expected,
        "actual": actual,
    }


def _selector_key_set(selectors):
    if not isinstance(selectors, list):
        raise ActivationError("selector collection must be a list")
    return {selector_key(item) for item in selectors}


def kd_w1_violations(changed_selectors, contracts, semantic_facts, exception_scopes=None):
    """Return typed KD-W1/contract-integrity findings.

    Two finding kinds are emitted and never conflated:
      * stale-tuning-selector — a contract tuning selector no longer resolves;
        this is contract integrity and applies to every activation state.
      * inactive-tuning-change — an inactive component had an unauthorized
        changed tuning surface; this is the KD-W1 tuning prohibition.

    Changed-surface matching uses canonical selector identity.
    """
    changed = _selector_key_set(changed_selectors)
    fact_index = _index_semantic_facts(semantic_facts)

    scopes = []
    for scope in exception_scopes or []:
        if not isinstance(scope, dict):
            raise ActivationError("exception scope must be an object")
        unknown = sorted(
            set(scope) - {"component_id", "approval_ref", "tuning_surface_selectors"})
        if unknown:
            raise ActivationError(
                "exception scope contains unknown field(s): %s" % ", ".join(unknown))
        component_id = scope.get("component_id")
        approval_ref = scope.get("approval_ref")
        selectors = scope.get("tuning_surface_selectors")
        if not isinstance(component_id, str) or not component_id.strip():
            raise ActivationError("exception scope requires component_id")
        if not isinstance(approval_ref, str) or not approval_ref.strip():
            raise ActivationError("exception scope requires approval_ref")
        if not isinstance(selectors, list) or not selectors:
            raise ActivationError("exception scope requires tuning_surface_selectors")
        scopes.append((component_id.strip(), _selector_key_set(selectors)))

    if not isinstance(contracts, list):
        raise ActivationError("integration contracts must be a list")

    seen_components = set()
    findings = []
    for contract in contracts:
        state = validate_activation_contract(contract)
        component_id = contract.get("component_id")
        if not isinstance(component_id, str) or not component_id.strip():
            raise ActivationError("integration contract requires component_id")
        component_id = component_id.strip()
        if component_id in seen_components:
            raise ActivationError("duplicate integration contract component_id: %s" % component_id)
        seen_components.add(component_id)

        tuning = contract.get("tuning_surface_selectors", [])
        if not isinstance(tuning, list):
            raise ActivationError("tuning_surface_selectors must be a list")
        tuning_by_key = {selector_key(item): item for item in tuning}
        if len(tuning_by_key) != len(tuning):
            raise ActivationError(
                "tuning_surface_selectors contains duplicate canonical selectors")

        unresolved = sorted(
            key for key, selector in tuning_by_key.items()
            if _resolve_from_index(selector, fact_index, allow_missing=True) is None
        )
        if unresolved:
            findings.append({
                "finding_kind": "stale-tuning-selector",
                "component_id": component_id,
                "activation_state": state,
                "selector_keys": unresolved,
            })

        if state == "active":
            continue

        affected = changed & set(tuning_by_key)
        authorized = set()
        for scoped_component, scoped_keys in scopes:
            if scoped_component == component_id:
                authorized.update(affected & scoped_keys)
        unauthorized = sorted(affected - authorized)
        if not unauthorized:
            continue

        changed_symbol_keys = []
        unresolved_changed_keys = []
        for key in unauthorized:
            fact = _resolve_from_index(tuning_by_key[key], fact_index, allow_missing=True)
            if fact is None:
                unresolved_changed_keys.append(key)
            else:
                changed_symbol_keys.append(fact["symbol_key"].strip())

        findings.append({
            "finding_kind": "inactive-tuning-change",
            "component_id": component_id,
            "activation_state": state,
            "selector_keys": unauthorized,
            "changed_symbol_keys": sorted(changed_symbol_keys),
            "unresolved_selector_keys": sorted(unresolved_changed_keys),
        })

    return sorted(
        findings,
        key=lambda item: (
            item["component_id"],
            item["finding_kind"],
            tuple(item["selector_keys"]),
        ),
    )


_STRUCTURAL_CLASSIFICATIONS = {
    "production-runtime-root",
    "contracted-child",
    "test-only",
    "tooling-only",
    "generated-or-external",
    "non-runtime-bearing",
}
_FALLBACK_SCOPES = {"repository", "runtime-bearing", "non-runtime-bearing"}
_FALLBACK_CLASSIFICATIONS = {
    "repository": _STRUCTURAL_CLASSIFICATIONS,
    "runtime-bearing": {"production-runtime-root", "contracted-child"},
    "non-runtime-bearing": {
        "test-only",
        "tooling-only",
        "generated-or-external",
        "non-runtime-bearing",
    },
}
_FALLBACK_PRECEDENCE = {
    "repository": 0,
    "runtime-bearing": 1,
    "non-runtime-bearing": 1,
}
_PROOF_CLASSES = {
    "structural-reachability",
    "lifecycle-order",
    "failure-injection",
    "mutation",
}
_CHANGE_TYPES = {
    "pure-local-calculation",
    "new-public-cross-assembly-api",
    "new-runtime-service",
    "new-composition-root-registration",
    "host-bootstrap-change",
    "static-initialization-change",
    "persistence-boundary",
    "external-resource-dependency",
    "testhost-runtime-divergence-fix",
    "dependency-graph-only-refactor",
    "pure-data-schema-no-runtime-behavior",
}
_PERSISTENCE_CHANGE_TYPES = {
    "persistence-boundary",
    "external-resource-dependency",
}
_EXECUTION_STATES = {
    "passed",
    "failed",
    "skipped",
    "excluded",
    "unavailable",
    "not-run",
    "runner-failed",
}
_DEPENDENCY_KINDS = {
    "requirement",
    "property",
    "contract",
    "runtime-root",
    "symbol",
    "public-surface",
    "bypass-surface",
    "asmdef",
    "lifecycle",
    "synchronization",
    "testhost",
    "serializer",
    "schema",
    "resource",
    "configuration",
    "test",
    "fixture",
    "runner",
    "environment",
    "tool",
    "extractor",
}
_COMMON_RELATIONS = {
    "requires",
    "contract",
    "root",
    "tool-semantic",
    "extractor-semantic",
    "configuration",
}
_STRUCTURAL_RELATIONS = {
    "construction",
    "registration",
    "public-surface",
    "bypass-surface",
    "assembly-reference",
}
_LIFECYCLE_RELATIONS = {
    "lifecycle-member",
    "ordering",
    "synchronization",
    "thread-affinity",
    "testhost-equivalent",
}
_PERSISTENCE_RELATIONS = {"serializer", "schema", "resource"}
_EXECUTABLE_RELATIONS = {"target", "test", "fixture", "runner", "environment"}
_ALL_DEPENDENCY_RELATIONS = (
    _COMMON_RELATIONS
    | _STRUCTURAL_RELATIONS
    | _LIFECYCLE_RELATIONS
    | _PERSISTENCE_RELATIONS
    | _EXECUTABLE_RELATIONS
)
_EXECUTABLE_PROOF_RELATIONS = (
    _COMMON_RELATIONS
    | _STRUCTURAL_RELATIONS
    | _LIFECYCLE_RELATIONS
    | _EXECUTABLE_RELATIONS
)
_PROOF_RELATIONS = {
    "structural-reachability": _COMMON_RELATIONS | _STRUCTURAL_RELATIONS,
    "lifecycle-order": _COMMON_RELATIONS | _STRUCTURAL_RELATIONS | _LIFECYCLE_RELATIONS,
    "failure-injection": _EXECUTABLE_PROOF_RELATIONS,
    "mutation": _EXECUTABLE_PROOF_RELATIONS,
}


class ApplicabilityError(SemanticsError):
    pass


class ClosureError(SemanticsError):
    pass


class FreshnessError(SemanticsError):
    pass


class ExecutionError(SemanticsError):
    pass


def _text(value, field, error_type):
    item = value.get(field)
    if not isinstance(item, str) or not item.strip():
        raise error_type("%s must be a non-empty string" % field)
    return item.strip()


def _text_list(value, field, error_type, required=True):
    raw = value.get(field)
    if raw is None and not required:
        return []
    if not isinstance(raw, list) or (required and not raw):
        raise error_type("%s must be a non-empty list" % field)
    result = []
    for index, item in enumerate(raw):
        if not isinstance(item, str) or not item.strip():
            raise error_type("%s[%d] must be a non-empty string" % (field, index))
        result.append(item.strip())
    if len(result) != len(set(result)):
        raise error_type("%s contains duplicates" % field)
    return sorted(result)


def _normalize_na_reasons(rule):
    raw = rule.get("allowed_na_reasons", [])
    if not isinstance(raw, list):
        raise ApplicabilityError("allowed_na_reasons must be a list")
    out = []
    seen = set()
    for index, item in enumerate(raw):
        if not isinstance(item, dict):
            raise ApplicabilityError("allowed_na_reasons[%d] must be an object" % index)
        unknown = sorted(set(item) - {"reason_code", "approval_required"})
        if unknown:
            raise ApplicabilityError(
                "allowed_na_reasons contains unknown field(s): %s" % ", ".join(unknown))
        code = _text(item, "reason_code", ApplicabilityError)
        approval_required = item.get("approval_required")
        if not isinstance(approval_required, bool):
            raise ApplicabilityError("allowed_na_reasons.approval_required must be boolean")
        if code in seen:
            raise ApplicabilityError("duplicate N/A reason_code: %s" % code)
        seen.add(code)
        out.append({"reason_code": code, "approval_required": approval_required})
    return sorted(out, key=lambda item: item["reason_code"])


def _specificity(rule):
    # Surface specificity preserves the existing ordering. Change context is
    # orthogonal and contributes the least-significant bit so an otherwise
    # identical change-type-specific rule outranks its generic counterpart
    # without changing surface precedence.
    fallback_scope = rule.get("fallback_scope")
    if fallback_scope is not None:
        surface_score = _FALLBACK_PRECEDENCE[fallback_scope]
    else:
        surface_score = 32
        if rule["selectors"]:
            surface_score |= 16
        if rule["component_ids"]:
            surface_score |= 8
        if rule["assemblies"]:
            surface_score |= 4
        if rule["classifications"]:
            surface_score |= 2
        if rule["activation_states"]:
            surface_score |= 1
    return (surface_score * 2) + (1 if rule["change_types"] else 0)


def normalize_applicability_rule(rule):
    if not isinstance(rule, dict):
        raise ApplicabilityError("applicability rule must be an object")
    allowed = {
        "rule_id",
        "selectors",
        "component_ids",
        "assemblies",
        "classifications",
        "activation_states",
        "trigger_ref",
        "change_types",
        "requirement_refs",
        "proof_classes",
        "gate_classes",
        "allowed_na_reasons",
        "precedence",
        "fallback_scope",
    }
    unknown = sorted(set(rule) - allowed)
    if unknown:
        raise ApplicabilityError(
            "applicability rule contains unknown field(s): %s" % ", ".join(unknown))

    selectors = rule.get("selectors", [])
    if not isinstance(selectors, list):
        raise ApplicabilityError("selectors must be a list")
    normalized_selectors = [normalize_selector(item) for item in selectors]
    selector_keys = [selector_key(item) for item in normalized_selectors]
    if len(selector_keys) != len(set(selector_keys)):
        raise ApplicabilityError("selectors contains duplicates")

    component_ids = _text_list(rule, "component_ids", ApplicabilityError, required=False)
    assemblies = _text_list(rule, "assemblies", ApplicabilityError, required=False)
    classifications = _text_list(
        rule, "classifications", ApplicabilityError, required=False)
    activation_states = _text_list(
        rule, "activation_states", ApplicabilityError, required=False)
    if any(item not in _STRUCTURAL_CLASSIFICATIONS for item in classifications):
        raise ApplicabilityError("classifications contains an invalid value")
    if any(item not in _ACTIVATION_STATES for item in activation_states):
        raise ApplicabilityError("activation_states contains an invalid value")

    fallback_scope = rule.get("fallback_scope")
    if fallback_scope is not None and fallback_scope not in _FALLBACK_SCOPES:
        raise ApplicabilityError("invalid fallback_scope: %r" % fallback_scope)
    explicit = bool(
        normalized_selectors
        or component_ids
        or assemblies
        or classifications
        or activation_states
    )
    if explicit and fallback_scope is not None:
        raise ApplicabilityError(
            "fallback_scope is valid only when no explicit applicability selector is present")
    if not explicit and fallback_scope is None:
        raise ApplicabilityError(
            "rule requires at least one explicit applicability selector or fallback_scope")

    change_types = _text_list(
        rule, "change_types", ApplicabilityError, required=False)
    if any(item not in _CHANGE_TYPES for item in change_types):
        raise ApplicabilityError("change_types contains an invalid value")

    proof_classes = _text_list(rule, "proof_classes", ApplicabilityError)
    if any(item not in _PROOF_CLASSES for item in proof_classes):
        raise ApplicabilityError("proof_classes contains an invalid value")

    out = {
        "rule_id": _text(rule, "rule_id", ApplicabilityError),
        "selectors": sorted(normalized_selectors, key=selector_key),
        "component_ids": component_ids,
        "assemblies": assemblies,
        "classifications": classifications,
        "activation_states": activation_states,
        "trigger_ref": _text(rule, "trigger_ref", ApplicabilityError),
        "change_types": change_types,
        "requirement_refs": _text_list(rule, "requirement_refs", ApplicabilityError),
        "proof_classes": proof_classes,
        "gate_classes": _text_list(rule, "gate_classes", ApplicabilityError),
        "allowed_na_reasons": _normalize_na_reasons(rule),
        "fallback_scope": fallback_scope,
    }
    expected_precedence = _specificity(out)
    precedence = rule.get("precedence")
    if not isinstance(precedence, int) or isinstance(precedence, bool):
        raise ApplicabilityError("precedence must be an integer")
    if precedence != expected_precedence:
        raise ApplicabilityError(
            "precedence %d does not match schema-derived specificity %d"
            % (precedence, expected_precedence))
    out["precedence"] = precedence
    return out


def normalize_applicability_subject(subject):
    if not isinstance(subject, dict):
        raise ApplicabilityError("applicability subject must be an object")
    allowed = {
        "selector",
        "component_id",
        "assembly",
        "classification",
        "activation_state",
        "change_type",
    }
    unknown = sorted(set(subject) - allowed)
    if unknown:
        raise ApplicabilityError(
            "applicability subject contains unknown field(s): %s" % ", ".join(unknown))
    out = {}
    if "selector" in subject:
        out["selector"] = normalize_selector(subject["selector"])
    for field in ("component_id", "assembly"):
        if field in subject:
            out[field] = _text(subject, field, ApplicabilityError)
    if "classification" in subject:
        classification = subject["classification"]
        if classification not in _STRUCTURAL_CLASSIFICATIONS:
            raise ApplicabilityError("invalid subject classification: %r" % classification)
        out["classification"] = classification
    if "activation_state" in subject:
        state = subject["activation_state"]
        if state not in _ACTIVATION_STATES:
            raise ApplicabilityError("invalid subject activation_state: %r" % state)
        out["activation_state"] = state
    if "change_type" in subject:
        change_type = subject["change_type"]
        if change_type not in _CHANGE_TYPES:
            raise ApplicabilityError("invalid subject change_type: %r" % change_type)
        out["change_type"] = change_type
    return out


def _fallback_matches(scope, subject):
    classification = subject.get("classification")
    if scope == "repository":
        return True
    if classification is None:
        return False
    return classification in _FALLBACK_CLASSIFICATIONS[scope]


def _rule_matches(rule, subject):
    if rule["change_types"] and subject.get("change_type") not in rule["change_types"]:
        return False
    if rule["fallback_scope"] is not None:
        return _fallback_matches(rule["fallback_scope"], subject)
    if rule["selectors"]:
        current = subject.get("selector")
        if current is None:
            return False
        current_key = selector_key(current)
        if current_key not in {selector_key(item) for item in rule["selectors"]}:
            return False
    for field, subject_field in (
        ("component_ids", "component_id"),
        ("assemblies", "assembly"),
        ("classifications", "classification"),
        ("activation_states", "activation_state"),
    ):
        if rule[field] and subject.get(subject_field) not in rule[field]:
            return False
    return True


def _rule_payload(rule):
    return {
        "requirement_refs": rule["requirement_refs"],
        "proof_classes": rule["proof_classes"],
        "gate_classes": rule["gate_classes"],
        "allowed_na_reasons": rule["allowed_na_reasons"],
    }


def _normalize_na_requests(na_requests):
    if na_requests is None:
        return {}
    if not isinstance(na_requests, list):
        raise ApplicabilityError("na_requests must be a list")
    out = {}
    for index, request in enumerate(na_requests):
        if not isinstance(request, dict):
            raise ApplicabilityError("na_requests[%d] must be an object" % index)
        unknown = sorted(set(request) - {"trigger_ref", "reason_code", "approval_ref"})
        if unknown:
            raise ApplicabilityError(
                "na_requests contains unknown field(s): %s" % ", ".join(unknown))
        trigger = _text(request, "trigger_ref", ApplicabilityError)
        reason = _text(request, "reason_code", ApplicabilityError)
        approval = request.get("approval_ref")
        if approval is not None and (not isinstance(approval, str) or not approval.strip()):
            raise ApplicabilityError("approval_ref must be non-empty when provided")
        if trigger in out:
            raise ApplicabilityError("duplicate N/A request for trigger_ref: %s" % trigger)
        out[trigger] = {
            "reason_code": reason,
            "approval_ref": approval.strip() if isinstance(approval, str) else None,
        }
    return out


def resolve_applicability(subject, rules, na_requests=None, strict=True):
    """Resolve every matching trigger before any changed-surface optimization.

    Rules compete only within the same trigger_ref. Schema-derived specificity
    selects the highest-precedence match. Equal-precedence matches must carry
    identical obligation payloads or strict resolution fails.
    """
    normalized_subject = normalize_applicability_subject(subject)
    if strict and "change_type" not in normalized_subject:
        raise ApplicabilityError(
            "strict applicability resolution requires subject.change_type")
    if not isinstance(rules, list) or not rules:
        raise ApplicabilityError("rules must be a non-empty list")
    normalized_rules = [normalize_applicability_rule(item) for item in rules]
    rule_ids = [item["rule_id"] for item in normalized_rules]
    if len(rule_ids) != len(set(rule_ids)):
        raise ApplicabilityError("rule_id values must be unique")

    matching = [item for item in normalized_rules if _rule_matches(item, normalized_subject)]
    by_trigger = {}
    for item in matching:
        by_trigger.setdefault(item["trigger_ref"], []).append(item)
    if strict and not by_trigger:
        raise ApplicabilityError("no applicability rule matches the subject")

    requests = _normalize_na_requests(na_requests)
    obligations = []
    selected_rule_ids = []
    for trigger in sorted(by_trigger):
        candidates = by_trigger[trigger]
        precedence = max(item["precedence"] for item in candidates)
        winners = sorted(
            (item for item in candidates if item["precedence"] == precedence),
            key=lambda item: item["rule_id"],
        )
        payload = _rule_payload(winners[0])
        for winner in winners[1:]:
            if _rule_payload(winner) != payload:
                raise ApplicabilityError(
                    "equal-precedence applicability conflict for trigger_ref %s" % trigger)
        winner_ids = [item["rule_id"] for item in winners]
        selected_rule_ids.extend(winner_ids)

        na = None
        if trigger in requests:
            request = requests.pop(trigger)
            allowed = {
                item["reason_code"]: item["approval_required"]
                for item in payload["allowed_na_reasons"]
            }
            if request["reason_code"] not in allowed:
                raise ApplicabilityError(
                    "N/A reason %s is not allowed for trigger_ref %s"
                    % (request["reason_code"], trigger))
            if allowed[request["reason_code"]] and not request["approval_ref"]:
                raise ApplicabilityError(
                    "N/A reason %s requires approval_ref" % request["reason_code"])
            na = request

        obligations.append({
            "trigger_ref": trigger,
            "rule_ids": winner_ids,
            "precedence": precedence,
            "requirement_refs": payload["requirement_refs"],
            "proof_classes": payload["proof_classes"],
            "gate_classes": payload["gate_classes"],
            "na": na,
        })

    if requests:
        raise ApplicabilityError(
            "N/A request names unmatched trigger_ref(s): %s" % ", ".join(sorted(requests)))

    active = [item for item in obligations if item["na"] is None]
    result = {
        "subject": normalized_subject,
        "selected_rule_ids": sorted(selected_rule_ids),
        "obligations": obligations,
        "requirement_refs": sorted({
            ref for item in active for ref in item["requirement_refs"]
        }),
        "proof_classes": sorted({
            proof for item in active for proof in item["proof_classes"]
        }),
        "gate_classes": sorted({
            gate for item in active for gate in item["gate_classes"]
        }),
    }
    result["applicability_digest"] = digest({
        "semantics_version": REFERENCE_SEMANTICS_VERSION,
        "subject": result["subject"],
        "obligations": result["obligations"],
    })
    return result


def evaluate_execution_truth(
        execution_state, bounded_substitute=None, bounded_substitute_permitted=False):
    """Evaluate the A2 execution-truth state machine.

    A bounded substitute is an approved replacement for omitted/uneconomic
    proof, never a waiver of a proof that executed and failed. Accordingly:
      * passed satisfies directly;
      * failed, skipped, and runner-failed can never be converted to satisfied;
      * excluded, unavailable, and not-run may satisfy only through an explicitly
        permitted and complete bounded-substitute record.
    """
    if execution_state not in _EXECUTION_STATES:
        raise ExecutionError("invalid execution_state: %r" % execution_state)
    if not isinstance(bounded_substitute_permitted, bool):
        raise ExecutionError("bounded_substitute_permitted must be boolean")

    if execution_state == "passed":
        if bounded_substitute is not None:
            raise ExecutionError(
                "passed execution cannot also claim a bounded substitute")
        return {
            "execution_state": execution_state,
            "satisfied": True,
            "basis": "passed",
        }

    if bounded_substitute is None:
        return {
            "execution_state": execution_state,
            "satisfied": False,
            "basis": "unsatisfied",
        }

    if execution_state in {"failed", "skipped", "runner-failed"}:
        raise ExecutionError(
            "%s cannot be satisfied by a bounded substitute" % execution_state)

    if not isinstance(bounded_substitute, dict):
        raise ExecutionError("bounded_substitute must be an object")
    allowed = {
        "authority_ref",
        "approval_ref",
        "justification",
        "omitted_surface_or_uncertainty",
    }
    unknown = sorted(set(bounded_substitute) - allowed)
    if unknown:
        raise ExecutionError(
            "bounded_substitute contains unknown field(s): %s" % ", ".join(unknown))
    normalized = {}
    for field in sorted(allowed):
        value = bounded_substitute.get(field)
        if not isinstance(value, str) or not value.strip():
            raise ExecutionError("bounded_substitute requires %s" % field)
        normalized[field] = value.strip()

    if not bounded_substitute_permitted:
        return {
            "execution_state": execution_state,
            "satisfied": False,
            "basis": "bounded-substitute-not-permitted",
            "bounded_substitute": normalized,
        }

    return {
        "execution_state": execution_state,
        "satisfied": True,
        "basis": "bounded-substitute",
        "bounded_substitute": normalized,
    }


def _validate_fingerprint(value, field):
    if not isinstance(value, str) or len(value) != 64:
        raise ClosureError("%s must be a lowercase SHA-256 hex digest" % field)
    lowered = value.lower()
    if value != lowered or any(ch not in "0123456789abcdef" for ch in value):
        raise ClosureError("%s must be a lowercase SHA-256 hex digest" % field)
    return value


def normalize_dependency_graph(graph):
    if not isinstance(graph, dict):
        raise ClosureError("dependency graph must be an object")
    unknown = sorted(set(graph) - {"nodes", "edges"})
    if unknown:
        raise ClosureError(
            "dependency graph contains unknown field(s): %s" % ", ".join(unknown))
    raw_nodes = graph.get("nodes")
    raw_edges = graph.get("edges")
    if not isinstance(raw_nodes, list) or not isinstance(raw_edges, list):
        raise ClosureError("dependency graph requires nodes and edges lists")

    nodes = {}
    requirements = {}
    for index, node in enumerate(raw_nodes):
        if not isinstance(node, dict):
            raise ClosureError("nodes[%d] must be an object" % index)
        unknown_node = sorted(
            set(node) - {"dependency_id", "kind", "fingerprint", "requirement_ref"})
        if unknown_node:
            raise ClosureError(
                "dependency node contains unknown field(s): %s" % ", ".join(unknown_node))
        dependency_id = _text(node, "dependency_id", ClosureError)
        if dependency_id in nodes:
            raise ClosureError("duplicate dependency_id: %s" % dependency_id)
        kind = node.get("kind")
        if kind not in _DEPENDENCY_KINDS:
            raise ClosureError("invalid dependency kind: %r" % kind)
        normalized = {
            "dependency_id": dependency_id,
            "kind": kind,
            "fingerprint": _validate_fingerprint(
                node.get("fingerprint"), "dependency fingerprint"),
        }
        if "requirement_ref" in node:
            requirement_ref = _text(node, "requirement_ref", ClosureError)
            if kind not in {"requirement", "property"}:
                raise ClosureError("requirement_ref is valid only on requirement/property nodes")
            if requirement_ref in requirements:
                raise ClosureError("duplicate requirement_ref binding: %s" % requirement_ref)
            requirements[requirement_ref] = dependency_id
            normalized["requirement_ref"] = requirement_ref
        nodes[dependency_id] = normalized

    edges = []
    seen_edges = set()
    for index, edge in enumerate(raw_edges):
        if not isinstance(edge, dict):
            raise ClosureError("edges[%d] must be an object" % index)
        unknown_edge = sorted(set(edge) - {"source", "target", "relation"})
        if unknown_edge:
            raise ClosureError(
                "dependency edge contains unknown field(s): %s" % ", ".join(unknown_edge))
        source = _text(edge, "source", ClosureError)
        target = _text(edge, "target", ClosureError)
        relation = _text(edge, "relation", ClosureError)
        if source not in nodes or target not in nodes:
            raise ClosureError("dependency edge references an unknown node")
        if relation not in _ALL_DEPENDENCY_RELATIONS:
            raise ClosureError("invalid dependency relation: %s" % relation)
        key = (source, target, relation)
        if key in seen_edges:
            raise ClosureError("duplicate dependency edge: %s" % (key,))
        seen_edges.add(key)
        edges.append({"source": source, "target": target, "relation": relation})
    edges.sort(key=lambda item: (item["source"], item["relation"], item["target"]))
    return {"nodes": nodes, "edges": edges, "requirements": requirements}


def _proof_obligations(proof_class, resolution):
    if proof_class not in _PROOF_CLASSES:
        raise ClosureError("invalid proof_class: %r" % proof_class)
    if not isinstance(resolution, dict) or not isinstance(
            resolution.get("obligations"), list):
        raise ClosureError("resolution must be an applicability result")
    if "subject" not in resolution:
        raise ClosureError("applicability result is missing subject")
    try:
        normalized_subject = normalize_applicability_subject(resolution["subject"])
    except ApplicabilityError as exc:
        raise ClosureError("applicability result has invalid subject: %s" % exc) from exc
    if "change_type" not in normalized_subject:
        raise ClosureError(
            "proof closure requires applicability subject.change_type")
    recorded_digest = resolution.get("applicability_digest")
    _validate_fingerprint(recorded_digest, "applicability_digest")
    expected_digest = digest({
        "semantics_version": REFERENCE_SEMANTICS_VERSION,
        "subject": normalized_subject,
        "obligations": resolution["obligations"],
    })
    if recorded_digest != expected_digest:
        raise ClosureError(
            "applicability_digest does not match the resolved subject/obligations")

    obligations = []
    for index, item in enumerate(resolution["obligations"]):
        if not isinstance(item, dict):
            raise ClosureError("obligations[%d] must be an object" % index)
        required = {
            "trigger_ref",
            "rule_ids",
            "precedence",
            "requirement_refs",
            "proof_classes",
            "gate_classes",
            "na",
        }
        if set(item) != required:
            raise ClosureError(
                "obligations[%d] does not have the canonical applicability fields" % index)
        if not isinstance(item["rule_ids"], list) or not item["rule_ids"]:
            raise ClosureError("obligation rule_ids must be a non-empty list")
        if not isinstance(item["requirement_refs"], list) or not item["requirement_refs"]:
            raise ClosureError("obligation requirement_refs must be a non-empty list")
        if not isinstance(item["proof_classes"], list) or not item["proof_classes"]:
            raise ClosureError("obligation proof_classes must be a non-empty list")
        if item["na"] is None and proof_class in item["proof_classes"]:
            obligations.append(item)
    if not obligations:
        raise ClosureError(
            "no active applicability obligation requires proof_class %s" % proof_class)
    return obligations


def derive_proof_closure(proof_class, resolution, graph):
    """Derive the minimum proof dependency closure from resolved obligations."""
    obligations = _proof_obligations(proof_class, resolution)
    normalized = normalize_dependency_graph(graph)
    requirement_refs = sorted({
        ref for item in obligations for ref in item["requirement_refs"]
    })
    rule_ids = sorted({
        rule_id for item in obligations for rule_id in item["rule_ids"]
    })
    root_ids = []
    for ref in requirement_refs:
        dependency_id = normalized["requirements"].get(ref)
        if dependency_id is None:
            raise ClosureError("no dependency node binds requirement_ref %s" % ref)
        root_ids.append(dependency_id)

    allowed_relations = set(_PROOF_RELATIONS[proof_class])
    change_type = resolution["subject"]["change_type"]
    persistence_triggered = change_type in _PERSISTENCE_CHANGE_TYPES
    if persistence_triggered:
        allowed_relations.update(_PERSISTENCE_RELATIONS)

    outgoing = {}
    for edge in normalized["edges"]:
        outgoing.setdefault(edge["source"], []).append(edge)

    reached = set(root_ids)
    queue = list(root_ids)
    included_edges = []
    while queue:
        source = queue.pop(0)
        for edge in outgoing.get(source, []):
            if edge["relation"] not in allowed_relations:
                continue
            included_edges.append(edge)
            if edge["target"] not in reached:
                reached.add(edge["target"])
                queue.append(edge["target"])

    included_edges.sort(
        key=lambda item: (item["source"], item["relation"], item["target"]))
    nodes = [normalized["nodes"][item] for item in sorted(reached)]
    subject = {
        "semantics_version": REFERENCE_SEMANTICS_VERSION,
        "proof_class": proof_class,
        "applicability_digest": resolution.get("applicability_digest"),
        "relation_policy": sorted(allowed_relations),
        "change_type": change_type,
        "persistence_triggered": persistence_triggered,
        "requirement_refs": requirement_refs,
        "applicability_rule_ids": rule_ids,
        "nodes": nodes,
        "edges": included_edges,
    }
    return {
        "semantics_version": REFERENCE_SEMANTICS_VERSION,
        "proof_class": proof_class,
        "requirement_refs": requirement_refs,
        "applicability_rule_ids": rule_ids,
        "dependency_ids": sorted(reached),
        "dependency_fingerprints": {
            item["dependency_id"]: item["fingerprint"] for item in nodes
        },
        "edges": included_edges,
        "relation_policy_digest": digest({
            "relations": sorted(allowed_relations),
            "persistence_triggered": persistence_triggered,
        }),
        "change_type": change_type,
        "persistence_triggered": persistence_triggered,
        "applicability_digest": resolution.get("applicability_digest"),
        "subject_scope_digest": digest(subject),
    }


def capture_proof_snapshot(
        proof_class, resolution, graph, provenance_revision=None, provenance_tree=None):
    snapshot = derive_proof_closure(proof_class, resolution, graph)
    if provenance_revision is not None:
        if not isinstance(provenance_revision, str) or not provenance_revision.strip():
            raise ClosureError("provenance_revision must be non-empty when provided")
        snapshot["provenance_revision"] = provenance_revision.strip()
    if provenance_tree is not None:
        if not isinstance(provenance_tree, str) or not provenance_tree.strip():
            raise ClosureError("provenance_tree must be non-empty when provided")
        snapshot["provenance_tree"] = provenance_tree.strip()
    return snapshot


def assess_proof_freshness(recorded, current_resolution, current_graph):
    """Recompute material scope. Provenance metadata is intentionally ignored."""
    if not isinstance(recorded, dict):
        raise FreshnessError("recorded proof snapshot must be an object")
    proof_class = recorded.get("proof_class")
    if proof_class not in _PROOF_CLASSES:
        raise FreshnessError("recorded proof_class is invalid")
    recorded_digest = recorded.get("subject_scope_digest")
    try:
        _validate_fingerprint(recorded_digest, "subject_scope_digest")
        current = derive_proof_closure(proof_class, current_resolution, current_graph)
    except ClosureError as exc:
        raise FreshnessError("cannot establish current proof closure: %s" % exc) from exc

    if current["subject_scope_digest"] == recorded_digest:
        return {"fresh": True, "reasons": [], "current": current}

    reasons = []
    if recorded.get("requirement_refs") != current["requirement_refs"]:
        reasons.append("requirements-changed")
    if recorded.get("applicability_rule_ids") != current["applicability_rule_ids"]:
        reasons.append("applicability-changed")
    if recorded.get("dependency_ids") != current["dependency_ids"]:
        reasons.append("dependency-set-changed")
    if recorded.get("dependency_fingerprints") != current["dependency_fingerprints"]:
        reasons.append("dependency-content-changed")
    if recorded.get("edges") != current["edges"]:
        reasons.append("dependency-topology-changed")
    if (recorded.get("semantics_version") != current["semantics_version"] or
            recorded.get("relation_policy_digest") != current["relation_policy_digest"]):
        reasons.append("proof-semantics-changed")
    if recorded.get("applicability_digest") != current["applicability_digest"]:
        reasons.append("applicability-subject-changed")
    if not reasons:
        reasons.append("subject-scope-changed")
    return {"fresh": False, "reasons": reasons, "current": current}


def changed_proof_decision(
        recorded, current_resolution, current_graph, changed_dependency_ids):
    """Conservative --changed optimization after full applicability/closure resolution.

    A known changed surface inside the derived proof closure always requires the
    full relevant proof run. Skipping is allowed only when every changed surface
    is mapped, none belongs to the closure, and the recorded proof is otherwise
    fresh.
    """
    if not isinstance(changed_dependency_ids, list):
        raise FreshnessError("changed_dependency_ids must be a list")
    changed = []
    for index, item in enumerate(changed_dependency_ids):
        if not isinstance(item, str) or not item.strip():
            raise FreshnessError(
                "changed_dependency_ids[%d] must be a non-empty string" % index)
        changed.append(item.strip())
    normalized = normalize_dependency_graph(current_graph)
    unknown = sorted(set(changed) - set(normalized["nodes"]))
    if unknown:
        return {
            "run_required": True,
            "reason": "unmapped-changed-surface",
            "unmapped_dependency_ids": unknown,
        }

    freshness = assess_proof_freshness(recorded, current_resolution, current_graph)
    if not freshness["fresh"]:
        return {
            "run_required": True,
            "reason": "proof-stale",
            "freshness_reasons": freshness["reasons"],
        }

    in_scope = sorted(
        set(changed) & set(freshness["current"]["dependency_ids"]))
    if in_scope:
        return {
            "run_required": True,
            "reason": "changed-surface-in-proof-closure",
            "changed_dependency_ids": in_scope,
        }

    return {
        "run_required": False,
        "reason": "proven-non-impact",
    }
