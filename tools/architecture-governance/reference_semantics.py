#!/usr/bin/env python3
# File: tools/architecture-governance/reference_semantics.py
# Created: August 31, 2026
# Purpose: A2 executable reference semantics for typed compiler-fact selectors,
#          stable component identity, disable anchors, and KD-W1 matching.
#          This module consumes compiler facts; it never parses C# source.

import hashlib
import json

REFERENCE_SEMANTICS_VERSION = "1.0.0"

_SELECTOR_KINDS = {"namespace", "type", "constructor", "method", "field", "property"}
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
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


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
        _exact(selector, {"assembly", "kind", "containing_type_id", "parameter_type_ids"})
        out["containing_type_id"] = _nonempty(selector, "containing_type_id")
        out["parameter_type_ids"] = _type_ids(selector)
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


def resolve_selector(selector, semantic_facts):
    target = normalize_selector(selector)
    matches = []
    for fact in semantic_facts:
        if not isinstance(fact, dict) or "selector" not in fact:
            raise SelectorError("semantic fact must contain selector")
        if normalize_selector(fact["selector"]) == target:
            matches.append(fact)
    if not matches:
        raise SelectorError("selector does not resolve: %s" % canonical_json(target))
    if len(matches) != 1:
        raise SelectorError("selector resolves ambiguously to %d facts" % len(matches))
    symbol_key = matches[0].get("symbol_key")
    if not isinstance(symbol_key, str) or not symbol_key.strip():
        raise SelectorError("resolved semantic fact has no symbol_key")
    return matches[0]


def validate_component_identities(records, semantic_facts):
    """Bind current selectors while preserving non-resolving historical selectors."""
    if not isinstance(records, list):
        raise IdentityError("component records must be a list")
    component_ids = set()
    selector_owner = {}
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
                raise IdentityError("selector_history contains unknown field(s): %s" % ", ".join(unknown))
            selectors.append(normalize_selector(item["selector"]))

        local = set()
        for item in selectors:
            key = selector_key(item)
            if key in local:
                raise IdentityError("%s repeats a current/history selector" % component_id)
            local.add(key)
            owner = selector_owner.get(key)
            if owner is not None and owner != component_id:
                raise IdentityError("selector is claimed by both %s and %s" % (owner, component_id))
            selector_owner[key] = component_id
        bindings[component_id] = resolve_selector(current, semantic_facts)["symbol_key"]
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
    if kind == "number" and (not isinstance(raw, (int, float)) or isinstance(raw, bool)):
        raise ActivationError("number typed value requires int or float")
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
    fact = resolve_selector(anchor["selector"], semantic_facts)
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


def _symbol_keys(selectors, semantic_facts):
    return {resolve_selector(item, semantic_facts)["symbol_key"] for item in selectors}


def kd_w1_violations(changed_selectors, contracts, semantic_facts, exception_scopes=None):
    """Return inactive-owner tuning changes not covered by an exact approved scope."""
    changed = _symbol_keys(changed_selectors, semantic_facts)
    scopes = []
    for scope in exception_scopes or []:
        if not isinstance(scope, dict):
            raise ActivationError("exception scope must be an object")
        component_id = scope.get("component_id")
        approval_ref = scope.get("approval_ref")
        selectors = scope.get("tuning_surface_selectors")
        if not isinstance(component_id, str) or not component_id.strip():
            raise ActivationError("exception scope requires component_id")
        if not isinstance(approval_ref, str) or not approval_ref.strip():
            raise ActivationError("exception scope requires approval_ref")
        if not isinstance(selectors, list) or not selectors:
            raise ActivationError("exception scope requires tuning_surface_selectors")
        scopes.append((component_id.strip(), _symbol_keys(selectors, semantic_facts)))

    violations = []
    for contract in contracts:
        state = validate_activation_contract(contract)
        component_id = contract.get("component_id")
        if not isinstance(component_id, str) or not component_id.strip():
            raise ActivationError("integration contract requires component_id")
        tuning = contract.get("tuning_surface_selectors", [])
        if not isinstance(tuning, list):
            raise ActivationError("tuning_surface_selectors must be a list")
        affected = changed & _symbol_keys(tuning, semantic_facts)
        if not affected or state == "active":
            continue
        authorized = set()
        for scoped_component, scoped_keys in scopes:
            if scoped_component == component_id:
                authorized.update(affected & scoped_keys)
        unauthorized = sorted(affected - authorized)
        if unauthorized:
            violations.append({
                "component_id": component_id,
                "activation_state": state,
                "changed_symbol_keys": unauthorized,
            })
    return sorted(violations, key=lambda item: (item["component_id"], tuple(item["changed_symbol_keys"])))
