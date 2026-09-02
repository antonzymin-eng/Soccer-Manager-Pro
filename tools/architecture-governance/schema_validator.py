#!/usr/bin/env python3
# File: tools/architecture-governance/schema_validator.py
# Created: September 1, 2026
# Purpose: A2 bounded JSON Schema Draft 2020-12 validator covering exactly the
#          keyword subset the ten canonical governance schemas use. It exists so
#          the frozen schemas and the executable reference semantics can be
#          differentially tested against identical fixtures without adding a
#          third-party dependency to the pure-stdlib Spec hygiene job.
#
# This is deliberately NOT a general-purpose validator. Any keyword outside
# SUPPORTED_KEYWORDS raises UnsupportedKeyword rather than being ignored: a
# silently skipped keyword would make every differential test pass vacuously,
# which is the exact failure mode this module is meant to prevent.

import json
import re
from pathlib import Path
from urllib.parse import urljoin

# Keywords that constrain an instance. Every one of these is implemented below.
SUPPORTED_KEYWORDS = frozenset({
    "$ref",
    "type", "const", "enum",
    "pattern", "minLength",
    "minimum",
    "items", "minItems", "maxItems", "uniqueItems",
    "properties", "required", "additionalProperties",
    "allOf", "oneOf", "not", "if", "then", "else",
})

# Keywords that carry identity, documentation, or subschema storage only. They
# are traversed or ignored deliberately, never treated as assertions.
ANNOTATION_KEYWORDS = frozenset({
    "$schema", "$id", "title", "description", "$defs",
    "x-governance-control-data",
})


class SchemaValidatorError(Exception):
    pass


class UnsupportedKeyword(SchemaValidatorError):
    pass


def _json_type(value):
    if value is None:
        return "null"
    if isinstance(value, bool):
        return "boolean"
    if isinstance(value, int):
        return "integer"
    if isinstance(value, float):
        return "number"
    if isinstance(value, str):
        return "string"
    if isinstance(value, list):
        return "array"
    if isinstance(value, dict):
        return "object"
    raise SchemaValidatorError("value is not JSON data: %r" % (value,))


def _matches_type(value, declared):
    actual = _json_type(value)
    names = declared if isinstance(declared, list) else [declared]
    if actual in names:
        return True
    # An integer is a valid number; a boolean is neither.
    return actual == "integer" and "number" in names


class SchemaSet:
    """The committed canonical schema set, addressed by `$id`."""

    def __init__(self, directory):
        self.by_id = {}
        self.by_name = {}
        for path in sorted(Path(directory).glob("*.json")):
            document = json.loads(path.read_text(encoding="utf-8"))
            schema_id = document.get("$id")
            if not schema_id:
                raise SchemaValidatorError(
                    "%s has no $id; relative $ref cannot resolve" % path.name)
            self.by_id[schema_id] = document
            self.by_name[path.name] = document
        self._assert_only_supported_keywords()

    def _assert_only_supported_keywords(self):
        """Fail loudly rather than silently under-validating."""
        allowed = SUPPORTED_KEYWORDS | ANNOTATION_KEYWORDS
        for name, document in sorted(self.by_name.items()):
            for keyword in sorted(self.used_keywords(document)):
                if keyword not in allowed:
                    raise UnsupportedKeyword(
                        "%s uses keyword %r, which this bounded validator does "
                        "not implement" % (name, keyword))

    @classmethod
    def used_keywords(cls, schema):
        """Every schema keyword reachable in `schema`, excluding property names.

        Only positions that JSON Schema treats as a schema are descended into,
        so an object *property* called "type" is never mistaken for a keyword.
        """
        found = set()
        if not isinstance(schema, dict):
            return found
        for keyword, value in schema.items():
            found.add(keyword)
            if keyword in {"properties", "$defs"} and isinstance(value, dict):
                for sub in value.values():
                    found |= cls.used_keywords(sub)
            elif keyword in {"allOf", "oneOf", "anyOf"} and isinstance(value, list):
                for sub in value:
                    found |= cls.used_keywords(sub)
            elif keyword in {"items", "not", "if", "then", "else"}:
                found |= cls.used_keywords(value)
            elif keyword == "additionalProperties" and isinstance(value, dict):
                found |= cls.used_keywords(value)
        return found

    def resolve(self, ref, base_id):
        """Resolve `ref` against `base_id` per RFC 3986, then apply the pointer."""
        target, _, fragment = ref.partition("#")
        resolved_id = urljoin(base_id, target) if target else base_id
        document = self.by_id.get(resolved_id)
        if document is None:
            raise SchemaValidatorError(
                "$ref %r resolves to %r, which is not in the schema set"
                % (ref, resolved_id))
        node = document
        if fragment:
            if not fragment.startswith("/"):
                raise SchemaValidatorError("only JSON-pointer fragments: %r" % ref)
            for token in fragment[1:].split("/"):
                token = token.replace("~1", "/").replace("~0", "~")
                if not isinstance(node, dict) or token not in node:
                    raise SchemaValidatorError(
                        "$ref %r has no target at %r" % (ref, token))
                node = node[token]
        return node, resolved_id

    def validate(self, instance, schema_name):
        """Return a list of human-readable violations; empty means valid."""
        document = self.by_name[schema_name]
        errors = []
        self._check(instance, document, document["$id"], schema_name, errors)
        return errors

    def _valid(self, instance, schema, base_id):
        probe = []
        self._check(instance, schema, base_id, "", probe)
        return not probe

    def _check(self, instance, schema, base_id, path, errors):
        if not isinstance(schema, dict):
            raise SchemaValidatorError("schema at %s is not an object" % path)

        if "$ref" in schema:
            target, target_id = self.resolve(schema["$ref"], base_id)
            self._check(instance, target, target_id, path, errors)
            # Draft 2020-12 applies sibling keywords alongside $ref.

        if "type" in schema and not _matches_type(instance, schema["type"]):
            errors.append(
                "%s: expected type %s, got %s"
                % (path, schema["type"], _json_type(instance)))
            return

        if "const" in schema and instance != schema["const"]:
            errors.append(
                "%s: expected const %r, got %r" % (path, schema["const"], instance))
        if "enum" in schema and instance not in schema["enum"]:
            errors.append("%s: %r is not one of %r" % (path, instance, schema["enum"]))

        for keyword in ("allOf",):
            for index, sub in enumerate(schema.get(keyword, [])):
                self._check(instance, sub, base_id, "%s/%s[%d]" % (path, keyword, index), errors)

        if "oneOf" in schema:
            matched = sum(
                1 for sub in schema["oneOf"] if self._valid(instance, sub, base_id))
            if matched != 1:
                errors.append("%s: matched %d oneOf branches, expected 1" % (path, matched))

        if "not" in schema and self._valid(instance, schema["not"], base_id):
            errors.append("%s: must not match the prohibited subschema" % path)

        if "if" in schema:
            branch = "then" if self._valid(instance, schema["if"], base_id) else "else"
            if branch in schema:
                self._check(instance, schema[branch], base_id, path + "/" + branch, errors)

        if isinstance(instance, str):
            if "minLength" in schema and len(instance) < schema["minLength"]:
                errors.append("%s: shorter than minLength %d" % (path, schema["minLength"]))
            if "pattern" in schema and re.search(schema["pattern"], instance) is None:
                errors.append("%s: %r fails pattern %s" % (path, instance, schema["pattern"]))

        if isinstance(instance, (int, float)) and not isinstance(instance, bool):
            if "minimum" in schema and instance < schema["minimum"]:
                errors.append("%s: below minimum %r" % (path, schema["minimum"]))

        if isinstance(instance, list):
            if "items" in schema:
                for index, item in enumerate(instance):
                    self._check(
                        item, schema["items"], base_id, "%s[%d]" % (path, index), errors)
            if "minItems" in schema and len(instance) < schema["minItems"]:
                errors.append("%s: fewer than minItems %d" % (path, schema["minItems"]))
            if "maxItems" in schema and len(instance) > schema["maxItems"]:
                errors.append("%s: more than maxItems %d" % (path, schema["maxItems"]))
            if schema.get("uniqueItems") and not _unique(instance):
                errors.append("%s: items must be unique" % path)

        if isinstance(instance, dict):
            for name in schema.get("required", []):
                if name not in instance:
                    errors.append("%s: missing required %r" % (path, name))
            properties = schema.get("properties", {})
            additional = schema.get("additionalProperties", True)
            for key in sorted(instance):
                if key in properties:
                    self._check(
                        instance[key], properties[key], base_id,
                        "%s.%s" % (path, key), errors)
                elif additional is False:
                    errors.append("%s: additional property %r is not allowed" % (path, key))
                elif isinstance(additional, dict):
                    self._check(
                        instance[key], additional, base_id,
                        "%s.%s" % (path, key), errors)


def _unique(items):
    seen = []
    for item in items:
        if item in seen:
            return False
        seen.append(item)
    return True


DEFAULT_SCHEMA_DIR = (
    Path(__file__).resolve().parents[2]
    / "docs" / "tracking" / "architecture-governance" / "schemas"
)


def default_schema_set():
    return SchemaSet(DEFAULT_SCHEMA_DIR)
