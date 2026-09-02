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
from pathlib import Path

REFERENCE_SEMANTICS_VERSION = "2.0.0"

_CONTROL_SCHEMA_PATH = (
    Path(__file__).resolve().parents[2]
    / "docs" / "tracking" / "architecture-governance" / "schemas" / "common.schema.json"
)
try:
    with _CONTROL_SCHEMA_PATH.open(encoding="utf-8") as _control_schema_file:
        _CONTROL_SCHEMA = json.load(_control_schema_file)
    _CONTROL_DEFS = _CONTROL_SCHEMA["$defs"]
    _CONTROL_DATA = _CONTROL_SCHEMA["x-governance-control-data"]
except (OSError, KeyError, TypeError, ValueError) as exc:
    raise RuntimeError(
        "cannot load canonical architecture-governance control schema: %s" % exc
    ) from exc


def _schema_enum(name):
    values = _CONTROL_DEFS.get(name, {}).get("enum")
    if not isinstance(values, list) or not values or any(
            not isinstance(item, str) for item in values):
        raise RuntimeError("canonical schema enum %s is missing or invalid" % name)
    if len(values) != len(set(values)):
        raise RuntimeError("canonical schema enum %s contains duplicates" % name)
    return frozenset(values)


SCHEMA_VERSION = _CONTROL_DEFS["currentSchemaVersion"]["const"]
_SELECTOR_KINDS = _schema_enum("selectorKind")
_ACTIVATION_STATES = _schema_enum("activationState")
_VALUE_TYPES = _schema_enum("valueType")
_ANCHOR_OPERATORS = _schema_enum("anchorOperator")
_PROPERTY_STATES = _schema_enum("propertyState")
_PROPERTY_TRANSITIONS = frozenset(
    tuple(item) for item in _CONTROL_DATA["property_transitions"])
_ENFORCEMENT_CLASSES = _schema_enum("enforcementClass")
_PROPERTY_ACTIVATIONS = _schema_enum("propertyActivation")
_DISPOSITION_TERMINAL_STATUS = dict(
    _CONTROL_DATA["disposition_terminal_status"])
_DISPOSITIONS = _schema_enum("disposition")
_FINDING_STATUSES = _schema_enum("findingStatus")
_REVIEW_STATES = _schema_enum("reviewState")
_BASELINE_MODES = _schema_enum("baselineMode")
_EXPIRY_TRIGGER_TYPES = _schema_enum("expiryTriggerType")
_EXCEPTION_STATUSES = _schema_enum("exceptionStatus")
_PROPERTY_RESULTS = _schema_enum("propertyResult")
_PROOF_RESULTS = _schema_enum("proofResult")
_REVALIDATION_OUTCOMES = _schema_enum("revalidationOutcome")
_SEVERITIES = _schema_enum("severity")
_FOREIGN_REQUIREMENT_PREFIXES = tuple(
    _CONTROL_DATA["foreign_requirement_prefixes"])
if not _FOREIGN_REQUIREMENT_PREFIXES or any(
        not isinstance(item, str) or not item.strip()
        for item in _FOREIGN_REQUIREMENT_PREFIXES):
    raise RuntimeError("canonical foreign requirement prefixes are invalid")
_APPROVED_LIMITATION_FIELDS = frozenset({
    "authority_ref",
    "approval_ref",
    "justification",
    "omitted_surface_or_uncertainty",
})
_BASELINE_MODE_TRANSITIONS = frozenset(
    tuple(item) for item in _CONTROL_DATA["baseline_mode_transitions"])
if any(
        len(item) != 2 or item[0] not in _PROPERTY_STATES or item[1] not in _PROPERTY_STATES
        for item in _PROPERTY_TRANSITIONS):
    raise RuntimeError("canonical property transition control data is invalid")
if (
        set(_DISPOSITION_TERMINAL_STATUS) != _DISPOSITIONS
        or not set(_DISPOSITION_TERMINAL_STATUS.values()) < _FINDING_STATUSES
        or "Open" not in _FINDING_STATUSES):
    raise RuntimeError("canonical disposition/status control data is invalid")
if any(
        len(item) != 2 or item[0] not in _BASELINE_MODES or item[1] not in _BASELINE_MODES
        for item in _BASELINE_MODE_TRANSITIONS):
    raise RuntimeError("canonical baseline transition control data is invalid")

_NOT_PROVIDED = object()


class SemanticsError(ValueError):
    pass


class SelectorError(SemanticsError):
    pass


class IdentityError(SemanticsError):
    pass


class ActivationError(SemanticsError):
    pass


class SchemaError(SemanticsError):
    pass


class PropertyRegistryError(SchemaError):
    pass


class PropertyRegistryUncertainty(PropertyRegistryError):
    pass


class ExceptionRegistryError(SchemaError):
    pass


class ReviewLedgerError(SchemaError):
    pass


class ReviewStateUncertainty(ReviewLedgerError):
    pass


class ActivationBaselineError(SchemaError):
    pass


class ActivationBaselineUncertainty(ActivationBaselineError):
    pass


class ProofArtifactError(SchemaError):
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

    All selector type ids use the C# XML documentation ID type-signature
    spelling (the type portion used inside member IDs), emitted from compiler
    symbols rather than inferred from source text. In particular, by-reference
    parameters use the XML-doc `@` suffix, so M(System.Int32) and
    M(System.Int32@) are distinct selector signatures. The same convention
    canonically carries generic parameters/types, arrays, pointers, and nested
    type structure; producers MUST NOT substitute display names or plain type
    names that erase those distinctions.

    Constructors carry is_static so .cctor cannot collide with .ctor.
    Properties carry parameter_type_ids so indexer overloads are addressable.
    """
    if not isinstance(selector, dict):
        raise SelectorError("selector must be an object")
    kind = _enum(
        selector.get("kind"), _SELECTOR_KINDS, "selector.kind", SelectorError)
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
    kind = _enum(
        value.get("value_type"), _VALUE_TYPES, "value_type", ActivationError)
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


def _normalize_disable_anchor(anchor):
    """Validate a disable anchor's complete typed shape.

    Single owner for the rule, called from both the contract validator and the
    evaluator, so the two cannot disagree about what a usable anchor is.
    """
    if not isinstance(anchor, dict):
        raise ActivationError("intentionally-disabled requires disable_anchor")
    unknown = sorted(set(anchor) - {"selector", "operator", "expected"})
    if unknown:
        raise ActivationError(
            "disable_anchor contains unknown field(s): %s" % ", ".join(unknown))
    if "selector" not in anchor or "expected" not in anchor:
        raise ActivationError("disable_anchor requires selector and expected")
    operator = _enum(
        anchor.get("operator"), _ANCHOR_OPERATORS,
        "disable_anchor.operator", ActivationError)
    try:
        selector = normalize_selector(anchor["selector"])
    except SelectorError as exc:
        raise ActivationError("disable_anchor selector is invalid: %s" % exc) from exc
    return {
        "selector": selector,
        "operator": operator,
        "expected": normalize_typed_value(anchor["expected"]),
    }


def validate_activation_contract(contract):
    if not isinstance(contract, dict):
        raise ActivationError("integration contract must be an object")
    state = _enum(
        contract.get("activation_state"),
        _ACTIVATION_STATES,
        "activation_state",
        ActivationError,
    )
    if state == "intentionally-disabled":
        for field in ("activation_owner", "decision_ref", "reactivation_condition"):
            value = contract.get(field)
            if not isinstance(value, str) or not value.strip():
                raise ActivationError("intentionally-disabled requires %s" % field)
        # The anchor's SHAPE is validated here, not only where it is evaluated:
        # a document validator that accepts {} approves a disabled contract whose
        # asserted anchor can never be evaluated, which FR-CS-081 requires to be
        # verifiable. The canonical schema requires all three fields; this is the
        # executable half of that contract.
        _normalize_disable_anchor(contract.get("disable_anchor"))
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
    normalized = _normalize_disable_anchor(anchor)
    operator = normalized["operator"]
    expected = normalized["expected"]
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


_STRUCTURAL_CLASSIFICATIONS = _schema_enum("structuralClassification")
_FALLBACK_SCOPES = _schema_enum("fallbackScope")
_FALLBACK_CLASSIFICATIONS = {
    scope: frozenset(classifications)
    for scope, classifications in _CONTROL_DATA["fallback_classifications"].items()
}
_FALLBACK_PRECEDENCE = dict(_CONTROL_DATA["fallback_precedence"])
_PROOF_CLASSES = _schema_enum("proofClass")
_CHANGE_TYPES = _schema_enum("changeType")
_PERSISTENCE_CHANGE_TYPES = frozenset(_CONTROL_DATA["persistence_change_types"])
_EXECUTION_STATES = _schema_enum("executionState")
_DEPENDENCY_KINDS = _schema_enum("dependencyKind")
_RELATION_GROUPS = {
    name: frozenset(values)
    for name, values in _CONTROL_DATA["dependency_relation_groups"].items()
}
_COMMON_RELATIONS = _RELATION_GROUPS["common"]
_STRUCTURAL_RELATIONS = _RELATION_GROUPS["structural"]
_LIFECYCLE_RELATIONS = _RELATION_GROUPS["lifecycle"]
_PERSISTENCE_RELATIONS = _RELATION_GROUPS["persistence"]
_EXECUTABLE_RELATIONS = _RELATION_GROUPS["executable"]
_ALL_DEPENDENCY_RELATIONS = (
    _COMMON_RELATIONS
    | _STRUCTURAL_RELATIONS
    | _LIFECYCLE_RELATIONS
    | _PERSISTENCE_RELATIONS
    | _EXECUTABLE_RELATIONS
)
if _ALL_DEPENDENCY_RELATIONS != _schema_enum("dependencyRelation"):
    raise RuntimeError(
        "canonical dependency relation enum and relation groups disagree")
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


def _enum(value, allowed, field, error_type):
    """Validate an untrusted scalar enum without leaking TypeError."""
    if not isinstance(value, str) or value not in allowed:
        raise error_type("%s is invalid: %r" % (field, value))
    return value


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
    # orthogonal: among otherwise-identical rules, a smaller non-empty matching
    # change_types set is more specific than a broader set, and every restricted
    # set is more specific than a generic rule. The multiplier prevents context
    # rank from ever overtaking one surface-specificity step.
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

    context_width = len(_CHANGE_TYPES) + 1
    context_rank = (
        len(_CHANGE_TYPES) - len(rule["change_types"]) + 1
        if rule["change_types"] else 0
    )
    return (surface_score * context_width) + context_rank


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
    for item in classifications:
        _enum(item, _STRUCTURAL_CLASSIFICATIONS, "classifications[]", ApplicabilityError)
    for item in activation_states:
        _enum(item, _ACTIVATION_STATES, "activation_states[]", ApplicabilityError)

    fallback_scope = rule.get("fallback_scope")
    if fallback_scope is not None:
        fallback_scope = _enum(
            fallback_scope, _FALLBACK_SCOPES, "fallback_scope", ApplicabilityError)
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
    for item in change_types:
        _enum(item, _CHANGE_TYPES, "change_types[]", ApplicabilityError)

    proof_classes = _text_list(rule, "proof_classes", ApplicabilityError)
    for item in proof_classes:
        _enum(item, _PROOF_CLASSES, "proof_classes[]", ApplicabilityError)

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
        out["classification"] = _enum(
            subject["classification"],
            _STRUCTURAL_CLASSIFICATIONS,
            "subject.classification",
            ApplicabilityError,
        )
    if "activation_state" in subject:
        out["activation_state"] = _enum(
            subject["activation_state"],
            _ACTIVATION_STATES,
            "subject.activation_state",
            ApplicabilityError,
        )
    if "change_type" in subject:
        out["change_type"] = _enum(
            subject["change_type"],
            _CHANGE_TYPES,
            "subject.change_type",
            ApplicabilityError,
        )
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
    context_complete = "change_type" in normalized_subject
    if strict and not context_complete:
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
        "context_complete": context_complete,
        "diagnostics": [] if context_complete else ["missing-change-type"],
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
    execution_state = _enum(
        execution_state, _EXECUTION_STATES, "execution_state", ExecutionError)
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
    allowed = _APPROVED_LIMITATION_FIELDS
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
        kind = _enum(
            node.get("kind"), _DEPENDENCY_KINDS, "dependency.kind", ClosureError)
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
        relation = _enum(
            relation,
            _ALL_DEPENDENCY_RELATIONS,
            "dependency.relation",
            ClosureError,
        )
        key = (source, target, relation)
        if key in seen_edges:
            raise ClosureError("duplicate dependency edge: %s" % (key,))
        seen_edges.add(key)
        edges.append({"source": source, "target": target, "relation": relation})
    edges.sort(key=lambda item: (item["source"], item["relation"], item["target"]))
    return {"nodes": nodes, "edges": edges, "requirements": requirements}


def _proof_obligations(proof_class, resolution):
    proof_class = _enum(
        proof_class, _PROOF_CLASSES, "proof_class", ClosureError)
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
    proof_class = _enum(
        proof_class, _PROOF_CLASSES, "recorded proof_class", FreshnessError)
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


def _schema_version(document, error_type):
    if not isinstance(document, dict):
        raise error_type("artifact must be an object")
    version = document.get("schema_version")
    if not isinstance(version, str):
        raise error_type("schema_version must be a semantic-version string")
    parts = version.split(".")
    if len(parts) != 3 or any(not part.isdigit() for part in parts):
        raise error_type("schema_version must use MAJOR.MINOR.PATCH")
    if int(parts[0]) != int(SCHEMA_VERSION.split(".")[0]):
        raise error_type("unsupported schema_version major: %s" % parts[0])
    return version


def _exact_record(value, required, optional, label, error_type):
    if not isinstance(value, dict):
        raise error_type("%s must be an object" % label)
    missing = sorted(set(required) - set(value))
    if missing:
        raise error_type("%s missing field(s): %s" % (label, ", ".join(missing)))
    unknown = sorted(set(value) - set(required) - set(optional))
    if unknown:
        raise error_type("%s contains unknown field(s): %s" % (label, ", ".join(unknown)))


def _optional_text(value, field, error_type):
    if field not in value:
        return None
    item = value[field]
    if not isinstance(item, str) or not item.strip():
        raise error_type("%s must be non-empty when provided" % field)
    return item.strip()


def _sha256(value, field, error_type):
    if not isinstance(value, str) or len(value) != 64:
        raise error_type("%s must be a lowercase SHA-256 hex digest" % field)
    if value != value.lower() or any(ch not in "0123456789abcdef" for ch in value):
        raise error_type("%s must be a lowercase SHA-256 hex digest" % field)
    return value


def _normalize_scope(scope, error_type):
    if not isinstance(scope, list) or not scope:
        raise error_type("scope must be a non-empty list")
    normalized = []
    for index, item in enumerate(scope):
        if not isinstance(item, dict):
            raise error_type("scope[%d] must be an object" % index)
        keys = set(item)
        if len(keys) != 1 or not keys <= {"component_id", "selector", "source_path", "host_id"}:
            raise error_type(
                "scope[%d] must contain exactly one component_id, selector, source_path, or host_id"
                % index)
        key = next(iter(keys))
        if key == "selector":
            try:
                normalized.append({"selector": normalize_selector(item[key])})
            except SelectorError as exc:
                raise error_type("scope[%d] has invalid selector: %s" % (index, exc)) from exc
        else:
            text_value = item[key]
            if not isinstance(text_value, str) or not text_value.strip():
                raise error_type("scope[%d].%s must be non-empty" % (index, key))
            normalized.append({key: text_value.strip()})
    keys = [canonical_json(item) for item in normalized]
    if len(keys) != len(set(keys)):
        raise error_type("scope contains duplicate bindings")
    return normalized


def validate_runtime_surface_classifications_document(document):
    """Validate the committed classification-intent registry envelope."""
    _schema_version(document, SchemaError)
    _exact_record(document, {"schema_version", "surfaces"}, set(), "classification registry", SchemaError)
    records = document["surfaces"]
    if not isinstance(records, list):
        raise SchemaError("surfaces must be a list")
    ids = set()
    symbols = set()
    for index, record in enumerate(records):
        required = {
            "surface_id", "symbol_key", "kind", "source_path", "signature",
            "assembly", "classification",
        }
        _exact_record(
            record, required, {"component_id", "contract_id"},
            "surfaces[%d]" % index, SchemaError)
        surface_id = _text(record, "surface_id", SchemaError)
        symbol_key = _text(record, "symbol_key", SchemaError)
        if surface_id in ids:
            raise SchemaError("duplicate surface_id: %s" % surface_id)
        if symbol_key in symbols:
            raise SchemaError("duplicate classification symbol_key: %s" % symbol_key)
        ids.add(surface_id)
        symbols.add(symbol_key)
        _enum(record.get("kind"), _SELECTOR_KINDS, "surface.kind", SchemaError)
        _enum(
            record.get("classification"), _STRUCTURAL_CLASSIFICATIONS,
            "surface.classification", SchemaError)
        for field in ("source_path", "signature", "assembly"):
            _text(record, field, SchemaError)
        _optional_text(record, "component_id", SchemaError)
        _optional_text(record, "contract_id", SchemaError)
    return document


def validate_integration_contracts_document(document):
    """Validate the versioned contract envelope and activation invariants."""
    _schema_version(document, SchemaError)
    _exact_record(document, {"schema_version", "contracts"}, set(), "contract registry", SchemaError)
    contracts = document["contracts"]
    if not isinstance(contracts, list):
        raise SchemaError("contracts must be a list")
    ids = set()
    components = set()
    for index, contract in enumerate(contracts):
        if not isinstance(contract, dict):
            raise SchemaError("contracts[%d] must be an object" % index)
        contract_id = _text(contract, "contract_id", SchemaError)
        component_id = _text(contract, "component_id", SchemaError)
        if contract_id in ids or component_id in components:
            raise SchemaError("duplicate contract_id or component_id")
        ids.add(contract_id)
        components.add(component_id)
        for field in (
                "owning_host", "owning_assembly", "composition_root",
                "construction_path", "activation_phase", "update_use_owner",
                "teardown_owner", "relevant_testhost_path"):
            _text(contract, field, SchemaError)
        for field in (
                "alternate_supported_paths", "prohibited_bypass_paths",
                "lifecycle_ordering_requirements"):
            _text_list(contract, field, SchemaError, required=False)
        if not isinstance(contract.get("static_initialization_involved"), bool):
            raise SchemaError("static_initialization_involved must be boolean")
        if not isinstance(contract.get("na_fields", []), list):
            raise SchemaError("na_fields must be a list")
        try:
            validate_activation_contract(contract)
            normalize_selector(contract.get("current_selector"))
            _selector_key_set(contract.get("tuning_surface_selectors", []))
        except (ActivationError, SelectorError) as exc:
            raise SchemaError("contracts[%d] is invalid: %s" % (index, exc)) from exc
    return document


def validate_applicability_rules_document(document):
    _schema_version(document, SchemaError)
    _exact_record(document, {"schema_version", "rules"}, set(), "applicability registry", SchemaError)
    rules = document["rules"]
    if not isinstance(rules, list):
        raise SchemaError("rules must be a list")
    normalized = [normalize_applicability_rule(item) for item in rules]
    ids = [item["rule_id"] for item in normalized]
    if len(ids) != len(set(ids)):
        raise SchemaError("applicability rules contain duplicate rule_id")
    return document


def _normalize_property_decisions(record, property_id):
    decisions = record.get("decision_history")
    if not isinstance(decisions, list) or not decisions:
        raise PropertyRegistryError("%s requires non-empty decision_history" % property_id)
    normalized = []
    ids = set()
    previous = None
    for index, item in enumerate(decisions):
        required = {
            "decision_id", "decision_actor", "transition_from", "transition_to",
            "decision_rationale", "decided_at",
        }
        _exact_record(
            item, required, {"decision_provenance_revision"},
            "%s.decision_history[%d]" % (property_id, index), PropertyRegistryError)
        decision_id = _text(item, "decision_id", PropertyRegistryError)
        if decision_id in ids:
            raise PropertyRegistryError("%s repeats decision_id %s" % (property_id, decision_id))
        ids.add(decision_id)
        actor = _text(item, "decision_actor", PropertyRegistryError)
        rationale = _text(item, "decision_rationale", PropertyRegistryError)
        decided_at = _text(item, "decided_at", PropertyRegistryError)
        transition_to = _enum(
            item.get("transition_to"), _PROPERTY_STATES,
            "transition_to", PropertyRegistryError)
        transition_from = item.get("transition_from")
        if index == 0:
            if transition_from is not None or transition_to != "Candidate":
                raise PropertyRegistryError(
                    "%s initial decision must establish Candidate from null" % property_id)
        else:
            transition_from = _enum(
                transition_from, _PROPERTY_STATES,
                "transition_from", PropertyRegistryError)
            if transition_from != previous:
                raise PropertyRegistryError(
                    "%s decision history is not contiguous" % property_id)
            if (transition_from, transition_to) not in _PROPERTY_TRANSITIONS:
                raise PropertyRegistryError(
                    "%s has illegal Governance §3.1 transition %s -> %s"
                    % (property_id, transition_from, transition_to))
        normalized_item = {
            "decision_id": decision_id,
            "decision_actor": actor,
            "transition_from": transition_from,
            "transition_to": transition_to,
            "decision_rationale": rationale,
            "decided_at": decided_at,
        }
        provenance = _optional_text(
            item, "decision_provenance_revision", PropertyRegistryError)
        if provenance is not None:
            normalized_item["decision_provenance_revision"] = provenance
        normalized.append(normalized_item)
        previous = transition_to
    return normalized


def _normalize_revalidation_history(record, property_id):
    history = record.get("revalidation_history")
    if not isinstance(history, list):
        raise PropertyRegistryError("%s.revalidation_history must be a list" % property_id)
    normalized = []
    ids = set()
    for index, item in enumerate(history):
        required = {
            "revalidation_id", "decision_actor", "reviewed_at",
            "subject_scope_digest", "outcome", "decision_rationale",
        }
        _exact_record(
            item, required, {"decision_provenance_revision"},
            "%s.revalidation_history[%d]" % (property_id, index), PropertyRegistryError)
        revalidation_id = _text(item, "revalidation_id", PropertyRegistryError)
        if revalidation_id in ids:
            raise PropertyRegistryError(
                "%s repeats revalidation_id %s" % (property_id, revalidation_id))
        ids.add(revalidation_id)
        normalized_item = {
            "revalidation_id": revalidation_id,
            "decision_actor": _text(item, "decision_actor", PropertyRegistryError),
            "reviewed_at": _text(item, "reviewed_at", PropertyRegistryError),
            "subject_scope_digest": _sha256(
                item.get("subject_scope_digest"), "subject_scope_digest",
                PropertyRegistryError),
            "outcome": _enum(
                item.get("outcome"), _REVALIDATION_OUTCOMES,
                "revalidation outcome", PropertyRegistryError),
            "decision_rationale": _text(
                item, "decision_rationale", PropertyRegistryError),
        }
        provenance = _optional_text(
            item, "decision_provenance_revision", PropertyRegistryError)
        if provenance is not None:
            normalized_item["decision_provenance_revision"] = provenance
        normalized.append(normalized_item)
    return normalized


def _normalize_property_record(record, index):
    required = {
        "property_id", "title", "state", "statement", "failure_mode", "scope",
        "non_scope", "authority", "evidence", "enforcement_class", "activation",
        "exceptions_allowed", "supersedes", "decision_rationale", "last_reviewed",
        "decision_history", "revalidation_history",
    }
    _exact_record(record, required, {"exception_mechanism"}, "properties[%d]" % index, PropertyRegistryError)
    property_id = _text(record, "property_id", PropertyRegistryError)
    # The FR-CS-/FR-TS- namespaces belong to #20 and #19. A property registered
    # under one of their ids captures `exception_route`'s property branch, which
    # is evaluated first, and silently moves that requirement's waiver authority
    # into exceptions.json — the exact crossing §3.6 forbids. §3.6's carve-out for
    # an obligation that is ALSO an admitted AP is preserved: the AP cites the FR
    # requirement, it does not take its identifier.
    if property_id.startswith(_FOREIGN_REQUIREMENT_PREFIXES):
        raise PropertyRegistryError(
            "%s claims an id in a #19/#20 requirement namespace; an admitted "
            "property cites an FR requirement, it does not take its id" % property_id)
    normalized = {
        "property_id": property_id,
        "title": _text(record, "title", PropertyRegistryError),
        "state": _enum(record.get("state"), _PROPERTY_STATES, "state", PropertyRegistryError),
        "statement": _text(record, "statement", PropertyRegistryError),
        "failure_mode": _text(record, "failure_mode", PropertyRegistryError),
        "scope": _text_list(record, "scope", PropertyRegistryError),
        "non_scope": _text_list(record, "non_scope", PropertyRegistryError, required=False),
        "authority": _text(record, "authority", PropertyRegistryError),
        "evidence": _text_list(record, "evidence", PropertyRegistryError),
        "enforcement_class": _enum(
            record.get("enforcement_class"), _ENFORCEMENT_CLASSES,
            "enforcement_class", PropertyRegistryError),
        "activation": _enum(
            record.get("activation"), _PROPERTY_ACTIVATIONS,
            "activation", PropertyRegistryError),
        "exceptions_allowed": record.get("exceptions_allowed"),
        "supersedes": record.get("supersedes"),
        "decision_rationale": _text(record, "decision_rationale", PropertyRegistryError),
        "last_reviewed": _text(record, "last_reviewed", PropertyRegistryError),
    }
    if not isinstance(normalized["exceptions_allowed"], bool):
        raise PropertyRegistryError("exceptions_allowed must be boolean")
    if normalized["supersedes"] is not None and (
            not isinstance(normalized["supersedes"], str) or not normalized["supersedes"].strip()):
        raise PropertyRegistryError("supersedes must be null or a non-empty property_id")
    if normalized["exceptions_allowed"]:
        normalized["exception_mechanism"] = _text(
            record, "exception_mechanism", PropertyRegistryError)
        if normalized["exception_mechanism"] != "governance-exception":
            raise PropertyRegistryError(
                "exception_mechanism must be governance-exception")
    elif "exception_mechanism" in record:
        raise PropertyRegistryError(
            "exception_mechanism is invalid when exceptions_allowed is false")
    normalized["decision_history"] = _normalize_property_decisions(record, property_id)
    normalized["revalidation_history"] = _normalize_revalidation_history(record, property_id)
    if normalized["state"] != normalized["decision_history"][-1]["transition_to"]:
        raise PropertyRegistryError(
            "%s state does not match its final decision transition" % property_id)
    return normalized


def _property_map(document):
    _schema_version(document, PropertyRegistryError)
    _exact_record(
        document, {"schema_version", "properties"}, set(),
        "property registry", PropertyRegistryError)
    raw = document["properties"]
    if not isinstance(raw, list):
        raise PropertyRegistryError("properties must be a list")
    records = [_normalize_property_record(item, index) for index, item in enumerate(raw)]
    ids = [item["property_id"] for item in records]
    if len(ids) != len(set(ids)):
        raise PropertyRegistryError("property registry contains duplicate property_id")
    return records, {item["property_id"]: item for item in records}


def validate_property_registry(
        registry, merge_base_registry=_NOT_PROVIDED, strict=True):
    """Validate property state/history and trusted-merge-base immutability.

    Pass ``None`` when the trusted merge base proves that no registry existed.
    Omitting the merge-base value in strict mode is uncertainty, not approval.
    """
    current_records, current = _property_map(registry)
    if merge_base_registry is _NOT_PROVIDED:
        if strict:
            raise PropertyRegistryUncertainty(
                "trusted merge-base property registry was not provided")
        return registry
    if merge_base_registry is None:
        return registry
    prior_records, prior = _property_map(merge_base_registry)
    prior_ids = [item["property_id"] for item in prior_records]
    current_ids = [item["property_id"] for item in current_records]
    if current_ids[:len(prior_ids)] != prior_ids:
        raise PropertyRegistryError(
            "properties are append-only and existing order/records cannot be removed")
    for property_id in prior_ids:
        old = prior[property_id]
        new = current[property_id]
        old_decisions = old["decision_history"]
        new_decisions = new["decision_history"]
        if new_decisions[:len(old_decisions)] != old_decisions:
            raise PropertyRegistryError(
                "%s decision history was rewritten" % property_id)
        old_revalidation = old["revalidation_history"]
        new_revalidation = new["revalidation_history"]
        if new_revalidation[:len(old_revalidation)] != old_revalidation:
            raise PropertyRegistryError(
                "%s revalidation history was rewritten" % property_id)
        state_changed = old["state"] != new["state"]
        decisions_appended = len(new_decisions) > len(old_decisions)
        if state_changed and not decisions_appended:
            raise PropertyRegistryError(
                "%s state changes require an appended legal decision transition"
                % property_id)
        material_keys = (set(old) | set(new)) - {
            "state", "decision_history", "revalidation_history",
        }
        material_changed = any(old.get(key) != new.get(key) for key in material_keys)
        history_appended = (
            decisions_appended or len(new_revalidation) > len(old_revalidation))
        if material_changed and not history_appended:
            raise PropertyRegistryError(
                "%s material amendment requires appended decision or revalidation history"
                % property_id)
    return registry


def _normalize_exception_record(record, index):
    required = {
        "exception_id", "property_id", "scope", "reason", "risk", "mitigation",
        "owner", "expiry_trigger", "approval", "status",
    }
    _exact_record(record, required, set(), "exceptions[%d]" % index, ExceptionRegistryError)
    expiry = record["expiry_trigger"]
    _exact_record(expiry, {"type", "value"}, set(), "expiry_trigger", ExceptionRegistryError)
    approval = record["approval"]
    _exact_record(
        approval, {"decision_id", "decision_actor", "decided_at"},
        {"decision_provenance_revision"}, "approval", ExceptionRegistryError)
    return {
        "exception_id": _text(record, "exception_id", ExceptionRegistryError),
        "property_id": _text(record, "property_id", ExceptionRegistryError),
        "scope": _normalize_scope(record["scope"], ExceptionRegistryError),
        "reason": _text(record, "reason", ExceptionRegistryError),
        "risk": _text(record, "risk", ExceptionRegistryError),
        "mitigation": _text(record, "mitigation", ExceptionRegistryError),
        "owner": _text(record, "owner", ExceptionRegistryError),
        "expiry_trigger": {
            "type": _enum(
                expiry.get("type"), _EXPIRY_TRIGGER_TYPES,
                "expiry_trigger.type", ExceptionRegistryError),
            "value": _text(expiry, "value", ExceptionRegistryError),
        },
        "approval": {
            "decision_id": _text(approval, "decision_id", ExceptionRegistryError),
            "decision_actor": _text(approval, "decision_actor", ExceptionRegistryError),
            "decided_at": _text(approval, "decided_at", ExceptionRegistryError),
            **({"decision_provenance_revision": approval["decision_provenance_revision"].strip()}
               if _optional_text(approval, "decision_provenance_revision", ExceptionRegistryError)
               is not None else {}),
        },
        "status": _enum(
            record.get("status"), _EXCEPTION_STATUSES,
            "exception.status", ExceptionRegistryError),
    }


def validate_exception_registry(registry, property_registry):
    _schema_version(registry, ExceptionRegistryError)
    _exact_record(
        registry, {"schema_version", "exceptions"}, set(),
        "exception registry", ExceptionRegistryError)
    raw = registry["exceptions"]
    if not isinstance(raw, list):
        raise ExceptionRegistryError("exceptions must be a list")
    records = [_normalize_exception_record(item, index) for index, item in enumerate(raw)]
    ids = [item["exception_id"] for item in records]
    if len(ids) != len(set(ids)):
        raise ExceptionRegistryError("exception registry contains duplicate exception_id")
    _, properties = _property_map(property_registry)
    for item in records:
        property_record = properties.get(item["property_id"])
        if property_record is None:
            raise ExceptionRegistryError(
                "%s does not route to an admitted property" % item["exception_id"])
        if property_record["state"] != "Admitted":
            raise ExceptionRegistryError(
                "%s targets a property that is not Admitted" % item["exception_id"])
        if not property_record["exceptions_allowed"]:
            raise ExceptionRegistryError(
                "%s targets a property that forbids exceptions" % item["exception_id"])
    return registry


def exception_route(requirement_ref, property_registry):
    """Return the exclusive exception owner; routes never cross authority.

    ``governance_exception_allowed`` says only whether ``exceptions.json`` may
    carry the waiver. It deliberately does not judge an owning #19/#20
    exception mechanism.
    """
    if not isinstance(requirement_ref, str) or not requirement_ref.strip():
        raise ExceptionRegistryError("requirement_ref must be non-empty")
    requirement_ref = requirement_ref.strip()
    _, properties = _property_map(property_registry)
    if requirement_ref in properties:
        record = properties[requirement_ref]
        return {
            "route": "governance-property",
            "governance_exception_allowed": (
                record["state"] == "Admitted" and record["exceptions_allowed"]),
        }
    if requirement_ref.startswith("FR-CS-"):
        return {
            "route": "code-standards-owner",
            "governance_exception_allowed": False,
        }
    if requirement_ref.startswith("FR-TS-"):
        return {
            "route": "testing-strategy-owner",
            "governance_exception_allowed": False,
        }
    return {"route": "not-waivable", "governance_exception_allowed": False}


def _normalize_status_history(record, finding_id):
    history = record.get("status_history")
    if not isinstance(history, list) or not history:
        raise ReviewLedgerError("%s requires non-empty status_history" % finding_id)
    normalized = []
    previous = None
    ids = set()
    disposition = record["disposition"]
    terminal = _DISPOSITION_TERMINAL_STATUS[disposition]
    for index, item in enumerate(history):
        required = {"event_id", "transition_from", "transition_to", "actor", "at", "evidence"}
        _exact_record(
            item, required, {"approval_ref"},
            "%s.status_history[%d]" % (finding_id, index), ReviewLedgerError)
        event_id = _text(item, "event_id", ReviewLedgerError)
        if event_id in ids:
            raise ReviewLedgerError("%s repeats status event %s" % (finding_id, event_id))
        ids.add(event_id)
        transition_to = _enum(
            item.get("transition_to"), _FINDING_STATUSES,
            "transition_to", ReviewLedgerError)
        transition_from = item.get("transition_from")
        if index == 0:
            if transition_from is not None or transition_to != "Open":
                raise ReviewLedgerError(
                    "%s must begin with null -> Open" % finding_id)
        else:
            transition_from = _enum(
                transition_from, _FINDING_STATUSES,
                "transition_from", ReviewLedgerError)
            if transition_from != previous:
                raise ReviewLedgerError("%s status history is not contiguous" % finding_id)
            if transition_from != "Open" or transition_to != terminal:
                raise ReviewLedgerError(
                    "%s has illegal %s transition %s -> %s"
                    % (finding_id, disposition, transition_from, transition_to))
        evidence = item.get("evidence")
        if not isinstance(evidence, list):
            raise ReviewLedgerError("status history evidence must be a list")
        _text_list(item, "evidence", ReviewLedgerError, required=False)
        normalized.append(item)
        previous = transition_to
    return normalized


def _normalize_review_run(record, index):
    required = {
        "review_run_id", "review_series_id", "review_scope", "subject_scope_digest",
        "review_round", "reviewer_identity", "coverage", "unverified_surfaces",
        "applicable_properties", "convergence_state", "final_review",
        "round_budget_exhausted",
    }
    _exact_record(
        record, required, {"provenance_revision", "provenance_tree"},
        "review_runs[%d]" % index, ReviewLedgerError)
    review_round = record["review_round"]
    if not isinstance(review_round, int) or isinstance(review_round, bool) or review_round < 1:
        raise ReviewLedgerError("review_round must be an integer >= 1")
    if not isinstance(record["final_review"], bool) or not isinstance(
            record["round_budget_exhausted"], bool):
        raise ReviewLedgerError("final_review and round_budget_exhausted must be boolean")
    for field in ("review_run_id", "review_series_id", "reviewer_identity"):
        _text(record, field, ReviewLedgerError)
    _optional_text(record, "provenance_revision", ReviewLedgerError)
    _optional_text(record, "provenance_tree", ReviewLedgerError)
    _enum(
        record.get("convergence_state"), _REVIEW_STATES,
        "convergence_state", ReviewLedgerError)
    _text_list(record, "review_scope", ReviewLedgerError)
    _text_list(record, "coverage", ReviewLedgerError)
    _text_list(record, "unverified_surfaces", ReviewLedgerError, required=False)
    properties = record["applicable_properties"]
    if not isinstance(properties, list):
        raise ReviewLedgerError("applicable_properties must be a list")
    seen = set()
    for item in properties:
        _exact_record(
            item, {"property_id", "result", "evidence_refs"}, {"approval_ref"},
            "applicable property", ReviewLedgerError)
        property_id = _text(item, "property_id", ReviewLedgerError)
        if property_id in seen:
            raise ReviewLedgerError("duplicate applicable property: %s" % property_id)
        seen.add(property_id)
        result = _enum(
            item.get("result"), _PROPERTY_RESULTS,
            "property result", ReviewLedgerError)
        _text_list(item, "evidence_refs", ReviewLedgerError, required=result == "pass")
        if result == "na":
            _text(item, "approval_ref", ReviewLedgerError)
        elif "approval_ref" in item:
            raise ReviewLedgerError("approval_ref is valid only for an na property result")
    return record


def _normalize_finding(record, index):
    required = {
        "finding_id", "stable_key", "review_series_id", "parent_review_run_id",
        "summary", "evidence", "severity", "requirement_property", "disposition",
        "required_action", "owner", "status", "round_introduced",
        "resolution_evidence", "status_history",
    }
    optional = {"disposition_approval", "resolution_property_id"}
    _exact_record(record, required, optional, "findings[%d]" % index, ReviewLedgerError)
    disposition = _enum(
        record.get("disposition"), _DISPOSITIONS,
        "disposition", ReviewLedgerError)
    status = _enum(record.get("status"), _FINDING_STATUSES, "status", ReviewLedgerError)
    if status not in {"Open", _DISPOSITION_TERMINAL_STATUS[disposition]}:
        raise ReviewLedgerError(
            "invalid Disposition/Status pairing: %s / %s" % (disposition, status))
    round_introduced = record["round_introduced"]
    if not isinstance(round_introduced, int) or isinstance(round_introduced, bool) or round_introduced < 1:
        raise ReviewLedgerError("round_introduced must be an integer >= 1")
    finding_id = _text(record, "finding_id", ReviewLedgerError)
    for field in (
            "stable_key", "review_series_id", "parent_review_run_id", "summary",
            "required_action", "owner"):
        _text(record, field, ReviewLedgerError)
    _text_list(record, "evidence", ReviewLedgerError)
    requirements = _text_list(
        record, "requirement_property", ReviewLedgerError,
        required=disposition == "Blocker")
    _enum(record.get("severity"), _SEVERITIES, "severity", ReviewLedgerError)
    resolution = _text_list(
        record, "resolution_evidence", ReviewLedgerError,
        required=status != "Open")
    history = _normalize_status_history(record, finding_id)
    if history[-1]["transition_to"] != status:
        raise ReviewLedgerError("%s status does not match status_history" % finding_id)
    if disposition in {"Accepted Tradeoff", "Residual Risk"} and status != "Open":
        _text(record, "disposition_approval", ReviewLedgerError)
    if disposition == "Candidate Property" and status == "In property process":
        _text(record, "resolution_property_id", ReviewLedgerError)
    return {
        **record,
        "finding_id": finding_id,
        "requirement_property": requirements,
        "resolution_evidence": resolution,
        "status_history": history,
    }


def validate_review_ledger(
        ledger, current_subject_digests=None, prior_ledger=_NOT_PROVIDED, strict=True):
    """Validate review-run/finding state, convergence, and append-only history.

    Pass ``prior_ledger=None`` when the trusted merge base proves that no ledger
    existed. Omitting it in strict mode is uncertainty, not approval, and a final
    review whose current subject digest is not supplied is likewise uncertainty.
    """
    _schema_version(ledger, ReviewLedgerError)
    _exact_record(
        ledger, {"schema_version", "legacy_policy", "review_runs", "findings"},
        set(), "review ledger", ReviewLedgerError)
    if ledger["legacy_policy"] != "read-only-no-inference":
        raise ReviewLedgerError("legacy_policy must be read-only-no-inference")
    runs_raw = ledger["review_runs"]
    findings_raw = ledger["findings"]
    if not isinstance(runs_raw, list) or not isinstance(findings_raw, list):
        raise ReviewLedgerError("review_runs and findings must be lists")
    runs = [_normalize_review_run(item, index) for index, item in enumerate(runs_raw)]
    findings = [_normalize_finding(item, index) for index, item in enumerate(findings_raw)]
    run_ids = [item["review_run_id"] for item in runs]
    finding_ids = [item["finding_id"] for item in findings]
    stable_keys = [(item["review_series_id"], item["stable_key"]) for item in findings]
    if len(run_ids) != len(set(run_ids)):
        raise ReviewLedgerError("duplicate review_run_id")
    if len(finding_ids) != len(set(finding_ids)):
        raise ReviewLedgerError("duplicate finding_id")
    if len(stable_keys) != len(set(stable_keys)):
        raise ReviewLedgerError("duplicate (review_series_id, stable_key)")
    runs_by_id = {item["review_run_id"]: item for item in runs}
    for finding in findings:
        parent = runs_by_id.get(finding["parent_review_run_id"])
        if parent is None or parent["review_series_id"] != finding["review_series_id"]:
            raise ReviewLedgerError(
                "%s has missing or cross-series parent review" % finding["finding_id"])
        if finding["round_introduced"] != parent["review_round"]:
            raise ReviewLedgerError(
                "finding round_introduced must equal its parent review round")
    for run in runs:
        series_findings = [
            item for item in findings
            if item["review_series_id"] == run["review_series_id"]
            and item["round_introduced"] <= run["review_round"]
        ]
        open_findings = [item for item in series_findings if item["status"] == "Open"]
        failed_properties = [
            item for item in run["applicable_properties"] if item["result"] == "fail"]
        if run["convergence_state"] == "CONVERGED":
            if not run["final_review"]:
                raise ReviewLedgerError("CONVERGED requires final_review")
            if open_findings or failed_properties or run["unverified_surfaces"]:
                raise ReviewLedgerError(
                    "CONVERGED requires terminal findings, satisfied properties, and full coverage")
        if run["final_review"] and run["convergence_state"] == "IN_PROGRESS":
            raise ReviewLedgerError("a final review cannot remain IN_PROGRESS")
        if run["round_budget_exhausted"] and open_findings and (
                run["convergence_state"] != "NON-CONVERGED"):
            raise ReviewLedgerError(
                "round budget with open findings must record NON-CONVERGED")
        if run["final_review"]:
            expected = None if current_subject_digests is None else current_subject_digests.get(
                run["review_run_id"])
            if expected is None and strict:
                raise ReviewStateUncertainty(
                    "current subject digest missing for final review %s" % run["review_run_id"])
            if expected is not None:
                _sha256(expected, "current subject digest", ReviewLedgerError)
                if expected != run["subject_scope_digest"]:
                    raise ReviewLedgerError("final review subject is stale")
        _sha256(run["subject_scope_digest"], "subject_scope_digest", ReviewLedgerError)
    if prior_ledger is _NOT_PROVIDED:
        if strict:
            raise ReviewStateUncertainty(
                "trusted prior review ledger was not provided")
    elif prior_ledger is not None:
        validate_review_ledger(prior_ledger, None, None, strict=False)
        if runs_raw[:len(prior_ledger["review_runs"])] != prior_ledger["review_runs"]:
            raise ReviewLedgerError("review runs are append-only")
        old_findings = {item["finding_id"]: item for item in prior_ledger["findings"]}
        new_findings = {item["finding_id"]: item for item in findings_raw}
        if not set(old_findings) <= set(new_findings):
            raise ReviewLedgerError("findings cannot be removed")
        for finding_id, old in old_findings.items():
            new = new_findings[finding_id]
            immutable = set(old) - {
                "status", "resolution_evidence", "status_history",
                "disposition_approval", "resolution_property_id",
            }
            if any(old[key] != new.get(key) for key in immutable):
                raise ReviewLedgerError("%s immutable finding fields changed" % finding_id)
            if new["status_history"][:len(old["status_history"])] != old["status_history"]:
                raise ReviewLedgerError("%s status history was rewritten" % finding_id)
    return ledger


def _normalize_baseline_item(item, index):
    required = {
        "violation_id", "binding", "baseline_subject_scope_digest",
        "creation_provenance_revision", "owner", "disposition",
        "required_action", "expiry_trigger",
    }
    _exact_record(item, required, set(), "items[%d]" % index, ActivationBaselineError)
    binding = item["binding"]
    _exact_record(
        binding, {"component_id", "selector"}, set(),
        "baseline binding", ActivationBaselineError)
    expiry = item["expiry_trigger"]
    _exact_record(
        expiry, {"type", "value"}, set(),
        "baseline expiry_trigger", ActivationBaselineError)
    try:
        normalized_selector = normalize_selector(binding["selector"])
    except SelectorError as exc:
        raise ActivationBaselineError("invalid baseline selector: %s" % exc) from exc
    return {
        **item,
        "violation_id": _text(item, "violation_id", ActivationBaselineError),
        "binding": {
            "component_id": _text(binding, "component_id", ActivationBaselineError),
            "selector": normalized_selector,
        },
        "baseline_subject_scope_digest": _sha256(
            item.get("baseline_subject_scope_digest"),
            "baseline_subject_scope_digest", ActivationBaselineError),
        "creation_provenance_revision": _text(
            item, "creation_provenance_revision", ActivationBaselineError),
        "owner": _text(item, "owner", ActivationBaselineError),
        "disposition": _enum(
            item.get("disposition"), _DISPOSITIONS,
            "baseline disposition", ActivationBaselineError),
        "required_action": _text(item, "required_action", ActivationBaselineError),
        "expiry_trigger": {
            "type": _enum(
                expiry.get("type"), _EXPIRY_TRIGGER_TYPES,
                "expiry_trigger.type", ActivationBaselineError),
            "value": _text(expiry, "value", ActivationBaselineError),
        },
    }


def validate_temporary_activation_baseline(
        baseline, strict_activation=False, prior_baseline=_NOT_PROVIDED,
        current_violation_ids=None, strict=True):
    """Validate finite baseline shape, transitions, and IP-4 coverage.

    Pass ``prior_baseline=None`` when the trusted merge base proves that no
    baseline existed. Omitting it in strict mode is uncertainty, not approval.
    ``current_violation_ids`` carries the live violation set; omitting it in
    strict mode is uncertainty, because absent discovery evidence is not the
    same claim as the empty list.

    ``strict_activation`` is deliberately not part of ``strict``: it asserts
    that this call is the final activation check and adds a requirement rather
    than relaxing one.
    """
    _schema_version(baseline, ActivationBaselineError)
    _exact_record(
        baseline, {"schema_version", "baseline_id", "mode", "sealed", "items"},
        set(), "activation baseline", ActivationBaselineError)
    _text(baseline, "baseline_id", ActivationBaselineError)
    mode = _enum(baseline.get("mode"), _BASELINE_MODES, "baseline.mode", ActivationBaselineError)
    if not isinstance(baseline["sealed"], bool):
        raise ActivationBaselineError("baseline.sealed must be boolean")
    raw_items = baseline["items"]
    if not isinstance(raw_items, list):
        raise ActivationBaselineError("baseline.items must be a list")
    items = [_normalize_baseline_item(item, index) for index, item in enumerate(raw_items)]
    ids = [item["violation_id"] for item in items]
    if len(ids) != len(set(ids)):
        raise ActivationBaselineError("baseline contains duplicate violation_id")
    if mode == "inactive" and items:
        raise ActivationBaselineError("inactive baseline must be empty")
    if mode == "strict" and (items or not baseline["sealed"]):
        raise ActivationBaselineError("strict baseline must be sealed and mechanically empty")
    if strict_activation and (mode != "strict" or items):
        raise ActivationBaselineError(
            "strict activation requires mode strict and zero baseline items")
    if current_violation_ids is None:
        if strict:
            raise ActivationBaselineUncertainty(
                "current violation identifiers were not provided")
    else:
        if not isinstance(current_violation_ids, list) or any(
                not isinstance(item, str) or not item.strip()
                for item in current_violation_ids):
            raise ActivationBaselineError("current_violation_ids must be a list of non-empty strings")
        current = {item.strip() for item in current_violation_ids}
        baseline_ids = set(ids)
        new = sorted(current - baseline_ids)
        stale = sorted(baseline_ids - current)
        if new:
            raise ActivationBaselineError(
                "new violations are not baseline-covered: %s" % ", ".join(new))
        if stale:
            raise ActivationBaselineError(
                "resolved violations must be removed from the baseline: %s" % ", ".join(stale))
    if prior_baseline is _NOT_PROVIDED:
        if strict:
            raise ActivationBaselineUncertainty(
                "trusted prior activation baseline was not provided")
    elif prior_baseline is not None:
        validate_temporary_activation_baseline(
            prior_baseline, False, None, None, strict=False)
        prior_mode = prior_baseline["mode"]
        if mode != prior_mode and (prior_mode, mode) not in _BASELINE_MODE_TRANSITIONS:
            raise ActivationBaselineError(
                "illegal baseline mode transition %s -> %s" % (prior_mode, mode))
        if prior_baseline["sealed"] and not baseline["sealed"]:
            raise ActivationBaselineError("a sealed baseline cannot be unsealed")
        old_items = {item["violation_id"]: item for item in prior_baseline["items"]}
        new_items = {item["violation_id"]: item for item in raw_items}
        for violation_id in set(old_items) & set(new_items):
            if old_items[violation_id] != new_items[violation_id]:
                raise ActivationBaselineError(
                    "%s baseline record is immutable" % violation_id)
        # §3.9 states "New violations fail" WITHOUT qualification. Guarding this
        # only after sealing let an unsealed migration baseline absorb a new
        # violation and its own live-set entry in one revision -- the coverage
        # check then sees nothing new and the ratchet never engages. Additions
        # are measured against the TRUSTED PRIOR, whatever its seal state.
        added = sorted(set(new_items) - set(old_items))
        if added:
            raise ActivationBaselineError(
                "baseline cannot admit new violations: %s" % ", ".join(added))
    return baseline


_PROOF_EXECUTION_REQUIRED = frozenset({
    "execution_id", "command_or_test", "runner", "environment",
    "subject_scope_digest", "execution_state", "started_at", "ended_at",
})
_PROOF_REQUIRED = frozenset({
    "schema_version", "proof_id", "proof_class", "requirement_property_refs",
    "applicability_rule_ids", "result", "subject_scope_digest", "dependency_closure",
    "content_fingerprints", "configuration_fingerprints", "tool_identities",
    "execution_records", "created", "revalidation_history",
})
_PROOF_OPTIONAL = frozenset({
    "na", "bounded_substitute", "provenance_revision", "provenance_tree",
    "inventory_digest", "asmdef_digest", "failure_injection", "mutation",
})
_PERTURBATION_REQUIRED = frozenset({
    "condition_or_input", "target_selector", "expected_path",
    "executed_command_or_test", "observed_result", "tool_environment_identity",
})
_MUTATION_TEXT_FIELDS = frozenset({
    "operator_or_mutant_digest", "baseline_execution", "mutant_execution",
    "expected_detector", "observed_detector_failure", "tool_identity",
})


def _approved_limitation(value, label):
    """Validate an `na` / bounded-substitute record against the frozen shape."""
    _exact_record(
        value, _APPROVED_LIMITATION_FIELDS, set(), label, ProofArtifactError)
    return {
        field: _text(value, field, ProofArtifactError)
        for field in sorted(_APPROVED_LIMITATION_FIELDS)
    }


def _digest_map(value, field):
    if not isinstance(value, dict):
        raise ProofArtifactError("%s must be an object" % field)
    out = {}
    for key in sorted(value):
        if not isinstance(key, str) or not key.strip():
            raise ProofArtifactError("%s keys must be non-empty strings" % field)
        out[key.strip()] = _sha256(
            value[key], "%s[%s]" % (field, key), ProofArtifactError)
    return out


def _proof_execution(record, index):
    label = "execution_records[%d]" % index
    _exact_record(
        record, _PROOF_EXECUTION_REQUIRED, {"result_artifact"},
        label, ProofArtifactError)
    out = {
        field: _text(record, field, ProofArtifactError)
        for field in sorted(_PROOF_EXECUTION_REQUIRED - {
            "subject_scope_digest", "execution_state"})
    }
    out["subject_scope_digest"] = _sha256(
        record.get("subject_scope_digest"),
        label + ".subject_scope_digest", ProofArtifactError)
    out["execution_state"] = _enum(
        record.get("execution_state"), _EXECUTION_STATES,
        label + ".execution_state", ProofArtifactError)
    if "result_artifact" in record:
        out["result_artifact"] = _text(record, "result_artifact", ProofArtifactError)
    return out


def _proof_failure_injection(record, semantic_facts):
    _exact_record(
        record, _PERTURBATION_REQUIRED, set(), "failure_injection", ProofArtifactError)
    for field in sorted(_PERTURBATION_REQUIRED - {"target_selector"}):
        _text(record, field, ProofArtifactError)
    return _proof_selector(record["target_selector"], "failure_injection", semantic_facts)


def _proof_mutation(record, semantic_facts):
    required = _MUTATION_TEXT_FIELDS | {
        "base_subject_digest", "target_selector", "restoration_clean_state"}
    _exact_record(record, required, set(), "mutation", ProofArtifactError)
    for field in sorted(_MUTATION_TEXT_FIELDS):
        _text(record, field, ProofArtifactError)
    _sha256(
        record.get("base_subject_digest"),
        "mutation.base_subject_digest", ProofArtifactError)
    if record.get("restoration_clean_state") is not True:
        raise ProofArtifactError(
            "mutation.restoration_clean_state must record a restored clean state")
    return _proof_selector(record["target_selector"], "mutation", semantic_facts)


def _proof_selector(selector, label, semantic_facts):
    try:
        normalized = normalize_selector(selector)
    except SelectorError as exc:
        raise ProofArtifactError("%s.target_selector is invalid: %s" % (label, exc))
    if semantic_facts is not None:
        try:
            resolve_selector(normalized, semantic_facts)
        except SemanticsError as exc:
            raise ProofArtifactError(
                "%s.target_selector does not resolve: %s" % (label, exc))
    return normalized


def validate_proof_artifact(
        artifact, semantic_facts=None, bounded_substitute_permitted=False):
    """Validate one reusable proof record against the frozen §3.7 contract.

    Shape mirrors `schemas/proof-artifact.schema.json`. Beyond the shape this
    binds the record to A2 execution truth: a `pass` result requires every
    execution record to have passed, and a `bounded` result may only convert the
    states `evaluate_execution_truth` permits, through an approved substitute
    that #19 explicitly allows (`bounded_substitute_permitted`).

    `semantic_facts` is optional; when supplied, failure-injection and mutation
    target selectors must resolve against it rather than merely parse.
    """
    _schema_version(artifact, ProofArtifactError)
    _exact_record(
        artifact, _PROOF_REQUIRED, _PROOF_OPTIONAL,
        "proof artifact", ProofArtifactError)
    _text(artifact, "proof_id", ProofArtifactError)
    proof_class = _enum(
        artifact.get("proof_class"), _PROOF_CLASSES,
        "proof_class", ProofArtifactError)
    result = _enum(
        artifact.get("result"), _PROOF_RESULTS, "result", ProofArtifactError)
    _text_list(artifact, "requirement_property_refs", ProofArtifactError)
    _text_list(artifact, "applicability_rule_ids", ProofArtifactError)
    _sha256(
        artifact.get("subject_scope_digest"),
        "subject_scope_digest", ProofArtifactError)
    for field in ("provenance_revision", "provenance_tree"):
        if field in artifact:
            _text(artifact, field, ProofArtifactError)
    for field in ("inventory_digest", "asmdef_digest"):
        if field in artifact:
            _sha256(artifact[field], field, ProofArtifactError)
    _digest_map(artifact["content_fingerprints"], "content_fingerprints")
    _digest_map(artifact["configuration_fingerprints"], "configuration_fingerprints")

    closure = artifact["dependency_closure"]
    _exact_record(
        closure,
        {"dependency_ids", "edges", "relation_policy_digest", "change_type"},
        set(), "dependency_closure", ProofArtifactError)
    _text_list(closure, "dependency_ids", ProofArtifactError, required=False)
    if not isinstance(closure["edges"], list) or any(
            not isinstance(item, dict) for item in closure["edges"]):
        raise ProofArtifactError("dependency_closure.edges must be a list of objects")
    _sha256(
        closure.get("relation_policy_digest"),
        "dependency_closure.relation_policy_digest", ProofArtifactError)
    _enum(
        closure.get("change_type"), _CHANGE_TYPES,
        "dependency_closure.change_type", ProofArtifactError)

    tools_raw = artifact["tool_identities"]
    if not isinstance(tools_raw, list) or not tools_raw:
        raise ProofArtifactError("tool_identities must be a non-empty list")
    for index, tool in enumerate(tools_raw):
        label = "tool_identities[%d]" % index
        _exact_record(
            tool, {"tool_id", "semantic_version", "content_digest"},
            set(), label, ProofArtifactError)
        _text(tool, "tool_id", ProofArtifactError)
        _text(tool, "semantic_version", ProofArtifactError)
        _sha256(tool.get("content_digest"), label + ".content_digest", ProofArtifactError)

    created = artifact["created"]
    _exact_record(created, {"actor", "at"}, set(), "created", ProofArtifactError)
    _text(created, "actor", ProofArtifactError)
    _text(created, "at", ProofArtifactError)
    if not isinstance(artifact["revalidation_history"], list) or any(
            not isinstance(item, dict) for item in artifact["revalidation_history"]):
        raise ProofArtifactError("revalidation_history must be a list of objects")

    executions_raw = artifact["execution_records"]
    if not isinstance(executions_raw, list):
        raise ProofArtifactError("execution_records must be a list")
    executions = [
        _proof_execution(item, index) for index, item in enumerate(executions_raw)]
    execution_ids = [item["execution_id"] for item in executions]
    if len(execution_ids) != len(set(execution_ids)):
        raise ProofArtifactError("duplicate execution_id")

    # Result/limitation exclusivity, mirroring the schema's allOf branches.
    na = None
    if result == "na":
        if "na" not in artifact:
            raise ProofArtifactError("an na result requires an approved na record")
        na = _approved_limitation(artifact["na"], "na")
    elif "na" in artifact:
        raise ProofArtifactError("na is only valid for an na result")

    substitute = None
    if result == "bounded":
        if "bounded_substitute" not in artifact:
            raise ProofArtifactError(
                "a bounded result requires an approved bounded_substitute record")
        substitute = _approved_limitation(
            artifact["bounded_substitute"], "bounded_substitute")
    elif "bounded_substitute" in artifact:
        raise ProofArtifactError(
            "bounded_substitute is only valid for a bounded result")

    if proof_class == "failure-injection":
        if "failure_injection" not in artifact:
            raise ProofArtifactError(
                "a failure-injection proof requires a failure_injection record")
        _proof_failure_injection(artifact["failure_injection"], semantic_facts)
    elif "failure_injection" in artifact:
        raise ProofArtifactError(
            "failure_injection belongs to a failure-injection proof")

    if proof_class == "mutation":
        if "mutation" not in artifact:
            raise ProofArtifactError("a mutation proof requires a mutation record")
        _proof_mutation(artifact["mutation"], semantic_facts)
    elif "mutation" in artifact:
        raise ProofArtifactError("mutation belongs to a mutation proof")

    # Execution truth: the proof result may never outrun what actually executed.
    #
    # Whether a given proof class REQUIRES an execution at all is an applicability
    # question, not a property of this record, so an empty execution list is not
    # rejected here — that rule would be invented, and A2 is freezing the contract.
    # What is checked is that every execution actually recorded is consistent with
    # the claimed result.
    if result in {"pass", "bounded"}:
        for record in executions:
            # Bind the evidence to the subject. Without this the execution's own
            # subject_scope_digest is decorative and a passing record copied from
            # an unrelated or older subject certifies this proof.
            #
            # NARROWING, recorded deliberately: the plan defines no subsumption
            # relation between scopes, so equality is the only mechanically
            # defined binding available at freeze time. If a broader execution
            # must certify a narrower subject, that is a schema-evolution
            # decision for A5/A6 to take explicitly -- not a gap to leave open.
            if record["subject_scope_digest"] != artifact["subject_scope_digest"]:
                raise ProofArtifactError(
                    "execution %s ran against a different subject scope"
                    % record["execution_id"])
            passed = record["execution_state"] == "passed"
            # A substitute covers only the record it stands in for; offering one
            # alongside a passed execution is itself an error.
            truth = evaluate_execution_truth(
                record["execution_state"],
                None if result == "pass" or passed else substitute,
                False if result == "pass" or passed
                else bool(bounded_substitute_permitted))
            if not truth["satisfied"]:
                raise ProofArtifactError(
                    "%s is not satisfied by execution %s (%s)" % (
                        result, record["execution_id"], truth["basis"]))

    normalized = {
        **artifact,
        "proof_class": proof_class,
        "result": result,
        "execution_records": executions,
    }
    # Absent limitation records stay absent: re-validating the returned document
    # must not trip the result/limitation exclusivity rule above.
    if na is not None:
        normalized["na"] = na
    if substitute is not None:
        normalized["bounded_substitute"] = substitute
    return normalized
