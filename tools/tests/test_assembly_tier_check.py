# File: tools/tests/test_assembly_tier_check.py
# Created: August 29, 2026
# Purpose: Regression tests for assembly-tier-check machine-report semantics.

import contextlib
import importlib.util
import io
import json
import tempfile
import unittest
from pathlib import Path


TOOL_PATH = Path(__file__).resolve().parents[1] / "assembly-tier-check.py"
SPEC = importlib.util.spec_from_file_location("assembly_tier_check", TOOL_PATH)
checker = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(checker)

TIER_NAMES = [
    "Foundation",
    "Physics",
    "Configuration",
    "Mechanics",
    "AI",
    "Data",
    "Composition",
    "Management",
    "Presentation",
    "Client",
]


class RepoFixture:
    def __init__(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        (self.root / "src").mkdir(parents=True)
        (self.root / checker.SECTION2_PATH).parent.mkdir(parents=True)
        self.write_specs()
        for tier in range(10):
            self.asmdef(
                "tier-%d/tier-%d.asmdef" % (tier, tier),
                "Example.Tier%d" % tier)
        self.asmdef("infra-a/infra-a.asmdef", "Example.InfraA")
        self.asmdef("infra-b/infra-b.asmdef", "Example.InfraB")

    def close(self):
        self.temp.cleanup()

    def write_specs(self, heading="### 3.5.2 Tier Order and Dependency Arrows",
                    infra_cell="— **Infrastructure**",
                    swap_six_seven=False):
        sequence = " → ".join(TIER_NAMES)
        section2 = (
            "| FR-CS-046 | Assembly references use the ten-tier order defined "
            "in §3.5.2 (%s). |\n"
            "| FR-CS-046b | The out-of-band **Infrastructure** assembly "
            "(`infra-a`, `infra-b`) is separately bound. |\n"
        ) % sequence
        (self.root / checker.SECTION2_PATH).write_text(
            section2, encoding="utf-8")

        folders = ["tier-%d" % tier for tier in range(10)]
        if swap_six_seven:
            folders[6], folders[7] = folders[7], folders[6]
        rows = [
            "| %d **%s** | `%s` | fixture |"
            % (tier, TIER_NAMES[tier], folders[tier])
            for tier in range(10)
        ]
        rows.append(
            "| %s | `infra-a`, `infra-b` | fixture |" % infra_cell)
        section3 = "\n".join([
            heading,
            "",
            "| Tier | Assemblies | Why this tier |",
            "|---|---|---|",
            *rows,
            "",
            "### 3.5.3 Next",
            "",
        ])
        (self.root / checker.SPEC_PATH).write_text(
            section3, encoding="utf-8")

    def asmdef(self, rel, name, references=None):
        path = self.root / "src" / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(
            json.dumps({
                "name": name,
                "references": references or [],
            }),
            encoding="utf-8")
        return path


class AssemblyTierReportTests(unittest.TestCase):
    def setUp(self):
        self.fx = RepoFixture()

    def tearDown(self):
        self.fx.close()

    def test_subject_digest_changes_when_classification_changes(self):
        baseline = checker.analyze(self.fx.root)
        graph_digest = baseline["digests"]["graph_sha256"]
        subject_digest = baseline["digests"]["subject_sha256"]

        self.fx.write_specs(swap_six_seven=True)
        changed = checker.analyze(self.fx.root)

        self.assertEqual(graph_digest, changed["digests"]["graph_sha256"])
        self.assertNotEqual(
            baseline["digests"]["classification_sha256"],
            changed["digests"]["classification_sha256"])
        self.assertNotEqual(
            subject_digest, changed["digests"]["subject_sha256"])

    def test_heading_title_can_change_without_losing_section_binding(self):
        self.fx.write_specs(
            heading="### 3.5.2 Renamed Without Changing Section Number")
        report = checker.analyze(self.fx.root)

        self.assertEqual("pass", report["status"])
        self.assertEqual(
            10, report["summary"]["classification_counts"]["production"])
        self.assertEqual(
            2, report["summary"]["classification_counts"]["out-of-band"])

    def test_folding_infrastructure_into_tier_ten_is_a_policy_failure(self):
        baseline = checker.analyze(self.fx.root)
        self.fx.write_specs(infra_cell="10 **Infrastructure**")
        report = checker.analyze(self.fx.root)

        self.assertEqual("fail", report["status"])
        self.assertTrue(any(
            "Infrastructure row no longer names" in failure
            for failure in report["policy_failures"]))
        self.assertNotEqual(
            baseline["digests"]["subject_sha256"],
            report["digests"]["subject_sha256"])

    def test_all_graph_reports_test_cycle_without_promoting_it_to_policy_cycle(self):
        self.fx.asmdef(
            "tier-0/Tests/a.asmdef",
            "Example.TestA",
            ["Example.TestB"])
        self.fx.asmdef(
            "tier-0/Tests/b.asmdef",
            "Example.TestB",
            ["Example.TestA"])

        report = checker.analyze(self.fx.root)

        self.assertEqual("pass", report["status"])
        self.assertEqual(1, report["summary"]["all_cycle_component_count"])
        self.assertEqual(
            0, report["summary"]["production_cycle_component_count"])

    def test_test_only_external_reference_is_not_a_production_unknown(self):
        self.fx.asmdef(
            "tier-0/Tests/external.asmdef",
            "Example.ExternalTest",
            ["UnityEngine.TestRunner"])

        report = checker.analyze(self.fx.root)

        self.assertEqual(1, report["summary"]["external_reference_count"])
        self.assertEqual(
            0, report["summary"]["production_unknown_reference_count"])
        self.assertEqual("test", report["external_references"][0][
            "source_classification"])

    def test_stray_src_asmdef_is_visible_and_fails_classification(self):
        path = self.fx.root / "src" / "stray.asmdef"
        path.write_text(
            json.dumps({"name": "Example.Stray", "references": []}),
            encoding="utf-8")

        report = checker.analyze(self.fx.root)

        self.assertEqual("fail", report["status"])
        stray = next(
            node for node in report["assemblies"]
            if node["name"] == "Example.Stray")
        self.assertEqual("unresolved", stray["classification"])
        self.assertTrue(any(
            "directly under src/" in failure
            for failure in report["policy_failures"]))

    def test_json_cli_emits_the_same_subject_digest(self):
        direct = checker.analyze(self.fx.root)
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            rc = checker.main([
                "--repo", str(self.fx.root),
                "--json",
            ])
        parsed = json.loads(output.getvalue())

        self.assertEqual(0, rc)
        self.assertEqual(
            direct["digests"]["subject_sha256"],
            parsed["digests"]["subject_sha256"])


if __name__ == "__main__":
    unittest.main()
