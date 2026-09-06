from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
CHECKLIST = ROOT / "tools" / "checklist-auditor.py"
SCHEMA = ROOT / "tools" / "spec5-schema-auditor.py"


class TestingStrategyAuditorHardeningTests(unittest.TestCase):
    def run_auditor(self, script: Path, repo: Path, spec: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3",
                str(script),
                "--root",
                str(repo / "docs" / "specs"),
                "--repo-root",
                str(repo),
                "--changed-scope",
                "--enforce-dir",
                str(spec),
            ],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=15,
        )

    def write_index(self, repo: Path, *, folder: str, status: str) -> None:
        specs = repo / "docs" / "specs"
        specs.mkdir(parents=True, exist_ok=True)
        (specs / "SPEC_INDEX.md").write_text(
            "# SPEC_INDEX.md — Canonical Specification Registry\n\n"
            "| # | Specification | Folder | Priority | Status | Approved |\n"
            "|---|---|---|---|---|---|\n"
            f"| 9 | Spec Nine | `{folder}/` | 1 | {status} | — |\n",
            encoding="utf-8",
        )

    def test_blockquoted_backticked_approved_status_is_recognized_without_index(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "> **Status:** `APPROVED` (May 15, 2026)\n\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | moon is cheese | prose only |\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("BLOCK", proc.stdout)

    def test_spec_index_approved_overrides_local_in_review_status(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo, folder="spec-nine", status="APPROVED")
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "**Status:** IN REVIEW\n\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | moon is cheese | prose only |\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("BLOCK", proc.stdout)

    def test_checkbox_rows_are_audited_and_section_must_resolve(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-1.md").write_text(
                "# Spec #9 — Section 1\n"
                "## 1.1 Representation\n"
                "Q32.32 representation uses int64 raw storage.\n",
                encoding="utf-8",
            )
            checklist = spec / "section-9-approval-checklist.md"
            checklist.write_text(
                "# Spec #9 — Approval Checklist\n"
                "> **Status:** `APPROVED`\n\n"
                "## 9.1 Representation\n"
                "- [x] Q32.32 representation and int64 raw storage defined. Evidence: `section-1.md` §1.1.\n",
                encoding="utf-8",
            )
            good = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(good.returncode, 0, good.stdout)

            checklist.write_text(
                "# Spec #9 — Approval Checklist\n"
                "> **Status:** `APPROVED`\n\n"
                "## 9.1 Representation\n"
                "- [x] claim reviewed manually. Evidence: `section-1.md` §1.999.\n",
                encoding="utf-8",
            )
            bad = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(bad.returncode, 1, bad.stdout)
            self.assertIn("unresolved evidence section", bad.stdout)

    def test_checkbox_without_resolved_evidence_blocks_approval(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "**Status:** APPROVED\n\n"
                "## 9.1 Gate\n"
                "- [ ] implementation evidence deferred\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("checkbox is not checked", proc.stdout)
            self.assertIn("prose only", proc.stdout)

    def test_natural_language_value_comparison_is_stage0_not_automation(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-1.md").write_text(
                "# Spec #9 — Section 1\n## 1.1 Timeout\nThe timeout is 600 seconds.\n",
                encoding="utf-8",
            )
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n**Status:** APPROVED\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | Timeout is 60 seconds | `section-1.md` §1.1 |\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 0, proc.stdout)
            self.assertNotIn("supporting the claim", proc.stdout)

    def test_enforced_approved_spec_without_checklist_blocks(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo, folder="spec-nine", status="APPROVED")
            (spec / "section-1.md").write_text("# Spec #9 — Section 1\n", encoding="utf-8")
            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("missing required approval-checklist file", proc.stdout)

    def test_appendix_c_taxonomy_requires_determinism_and_end_to_end_rows(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-5.md").write_text(
                "# Spec #9 — Section 5: Test Plan\n"
                "**Status:** APPROVED\n\n"
                "## 5.1 Test Count by Taxonomy Layer\n"
                "| Layer | Count | Notes |\n"
                "|---|---:|---|\n"
                "| Unit | 3 | |\n"
                "| Integration | 1 | |\n"
                "| Simulation | 1 | |\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(SCHEMA, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("missing Determinism (consumed from #16 §5) row", proc.stdout)
            self.assertIn("missing End-to-end / soak row", proc.stdout)

    def test_schema_uses_canonical_registry_status(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo, folder="spec-nine", status="APPROVED")
            (spec / "section-5.md").write_text(
                "# Spec #9 — Section 5\n"
                "**Status:** IN REVIEW\n"
                "Unit only.\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(SCHEMA, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("BLOCK", proc.stdout)

    def test_enforced_approved_spec_without_section5_blocks(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo, folder="spec-nine", status="APPROVED")
            (spec / "section-1.md").write_text(
                "# Spec #9 — Section 1\nPurpose only.\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(SCHEMA, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("missing required section-5 test-plan file", proc.stdout)
            self.assertIn("BLOCK", proc.stdout)

    def test_section5_approval_link_must_resolve_to_real_checklist_row(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            scenario = repo / "tests" / "scenarios" / "spec-nine" / "smoke.json"
            scenario.parent.mkdir(parents=True)
            scenario.write_text("{}\n", encoding="utf-8")
            spec.mkdir(parents=True)
            self.write_index(repo, folder="spec-nine", status="APPROVED")
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n## 9.1 Real row\n",
                encoding="utf-8",
            )
            (spec / "section-5.md").write_text(
                "# Spec #9 — Section 5: Test Plan\n**Status:** APPROVED\n\n"
                "## 5.1 Test Count by Taxonomy Layer\n"
                "| Layer | Count | Notes |\n|---|---:|---|\n"
                "| Unit | 1 | |\n| Integration | 1 | |\n| Simulation | 1 | |\n"
                "| Determinism (consumed from #16 §5) | — | |\n| End-to-end / soak | 1 | |\n\n"
                "## 5.2 Property Test List\n| Property | Tier | Owning Module |\n|---|---|---|\n| prop | A | Core |\n\n"
                "## 5.3 Scenario List\n| Scenario | Manifest Path | Tier |\n|---|---|---|\n| smoke | `tests/scenarios/spec-nine/smoke.json` | B |\n\n"
                "## 5.4 Coverage Targets\n| Tier | Line | Branch |\n|---|---|---|\n| A | 98% | 95% |\n| B | 90% | 80% |\n| C | lint-only | — |\n\n"
                "## 5.5 Determinism-Tier Classification\n| Field | Tier | Source |\n|---|---|---|\n| Core.Value | A | #16 §1.1.1 |\n\n"
                "## 5.6 Approval-Checklist Linkage\n| Test ID | Verifies §9 Row |\n|---|---|\n| unit_core | §9.999 |\n\n"
                "## 5.7 Version History\n- v1\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(SCHEMA, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("references missing approval-checklist row §9.999", proc.stdout)


if __name__ == "__main__":
    unittest.main()
