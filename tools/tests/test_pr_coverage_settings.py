from __future__ import annotations

import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
MODULE_PATH = ROOT / "tools" / "dotnet-ci" / "pr_coverage_settings.py"
SPEC = importlib.util.spec_from_file_location("pr_coverage_settings", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
PCS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PCS)


class PrCoverageSettingsTests(unittest.TestCase):
    def write_asmdef(
        self,
        path: Path,
        name: str,
        references: list[str] | None = None,
        guid: str | None = None,
    ) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(
            json.dumps({"name": name, "references": references or []}, indent=2) + "\n",
            encoding="utf-8",
        )
        if guid is not None:
            path.with_name(path.name + ".meta").write_text(
                f"fileFormatVersion: 2\nguid: {guid}\n",
                encoding="utf-8",
            )

    def git(self, root: Path, *args: str) -> str:
        return subprocess.check_output(["git", *args], cwd=root, text=True).strip()

    def test_production_change_selects_owning_assembly(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            self.write_asmdef(root / "src" / "alpha" / "alpha.asmdef", "TacticalDirector.Alpha")
            source = root / "src" / "alpha" / "Thing.cs"
            source.write_text("class Thing {}\n", encoding="utf-8")
            selected = PCS.coverage_assemblies(root, ["src/alpha/Thing.cs"])
            self.assertEqual(selected, ["TacticalDirector.Alpha"])

    def test_changed_test_assembly_resolves_guid_and_named_production_references(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            guid = "0123456789abcdef0123456789abcdef"
            self.write_asmdef(root / "src" / "alpha" / "alpha.asmdef", "TacticalDirector.Alpha")
            self.write_asmdef(
                root / "src" / "shared" / "shared.asmdef",
                "TacticalDirector.Shared",
                guid=guid,
            )
            self.write_asmdef(
                root / "src" / "alpha" / "tests" / "alpha-tests.asmdef",
                "TacticalDirector.Alpha.Tests",
                [
                    "TacticalDirector.Alpha",
                    f"GUID:{guid}",
                    "TacticalDirector.Other.Tests",
                ],
            )
            test_source = root / "src" / "alpha" / "tests" / "AlphaTests.cs"
            test_source.write_text("class AlphaTests {}\n", encoding="utf-8")
            selected = PCS.coverage_assemblies(root, ["src/alpha/tests/AlphaTests.cs"])
            self.assertEqual(selected, ["TacticalDirector.Alpha", "TacticalDirector.Shared"])

    def test_unresolved_guid_reference_fails_loud(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            self.write_asmdef(
                root / "src" / "alpha" / "tests" / "alpha-tests.asmdef",
                "TacticalDirector.Alpha.Tests",
                ["GUID:missing"],
            )
            test_source = root / "src" / "alpha" / "tests" / "AlphaTests.cs"
            test_source.write_text("class AlphaTests {}\n", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "cannot resolve asmdef GUID reference"):
                PCS.coverage_assemblies(root, ["src/alpha/tests/AlphaTests.cs"])

    def test_no_source_delta_uses_non_matching_sentinel_and_single_hit(self) -> None:
        settings = PCS.render_settings([])
        self.assertIn("[__NoProductionCoverageDelta__]*", settings)
        self.assertIn("<SingleHit>true</SingleHit>", settings)
        self.assertIn("[*.Tests]*,[UnityShim*]*", settings)

    def test_unowned_changed_source_fails_loud(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            source = root / "src" / "orphan" / "Thing.cs"
            source.parent.mkdir(parents=True)
            source.write_text("class Thing {}\n", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "no asmdef owner"):
                PCS.coverage_assemblies(root, ["src/orphan/Thing.cs"])

    def test_main_head_without_explicit_base_uses_first_parent(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            subprocess.run(["git", "init", "-q", "-b", "main"], cwd=root, check=True)
            subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=root, check=True)
            subprocess.run(["git", "config", "user.name", "Test"], cwd=root, check=True)
            tracked = root / "tracked.txt"
            tracked.write_text("base\n", encoding="utf-8")
            subprocess.run(["git", "add", "tracked.txt"], cwd=root, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=root, check=True)
            parent = self.git(root, "rev-parse", "HEAD")
            tracked.write_text("next\n", encoding="utf-8")
            subprocess.run(["git", "commit", "-qam", "next"], cwd=root, check=True)

            self.assertEqual(PCS.resolve_base(root, None), parent)

    def test_explicit_base_wins_over_main_push_detection(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            subprocess.run(["git", "init", "-q", "-b", "main"], cwd=root, check=True)
            subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=root, check=True)
            subprocess.run(["git", "config", "user.name", "Test"], cwd=root, check=True)
            tracked = root / "tracked.txt"
            tracked.write_text("base\n", encoding="utf-8")
            subprocess.run(["git", "add", "tracked.txt"], cwd=root, check=True)
            subprocess.run(["git", "commit", "-qm", "base"], cwd=root, check=True)
            base = self.git(root, "rev-parse", "HEAD")
            tracked.write_text("next\n", encoding="utf-8")
            subprocess.run(["git", "commit", "-qam", "next"], cwd=root, check=True)

            self.assertEqual(PCS.resolve_base(root, base), base)

    def test_lower_gate_accepts_explicit_coverage_settings_only_with_coverage(self) -> None:
        gate = ROOT / "tools" / "dotnet-ci" / "run-gate.sh"
        with tempfile.TemporaryDirectory() as td:
            settings = Path(td) / "coverage.runsettings"
            settings.write_text(PCS.render_settings(["TacticalDirector.Alpha"]), encoding="utf-8")
            env = os.environ.copy()
            env["TD_GATE_DRY_RUN"] = "1"

            accepted = subprocess.run(
                ["bash", str(gate), "--coverage", "--coverage-settings", str(settings)],
                cwd=ROOT,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
            self.assertEqual(accepted.returncode, 0, accepted.stdout)
            self.assertIn(f"coverage_settings={settings}", accepted.stdout)

            rejected = subprocess.run(
                ["bash", str(gate), "--coverage-settings", str(settings)],
                cwd=ROOT,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
            self.assertEqual(rejected.returncode, 2, rejected.stdout)
            self.assertIn("requires --coverage", rejected.stdout)


if __name__ == "__main__":
    unittest.main()
