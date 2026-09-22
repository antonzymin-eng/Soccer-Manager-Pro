from __future__ import annotations

import hashlib
import importlib.util
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "dotnet-ci" / "check_evidence_manifests.py"


def _load_checker():
    spec = importlib.util.spec_from_file_location("check_evidence_manifests", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _errors(checker, root: Path) -> list[str]:
    _, errors = checker.validate(root)
    return errors


def _register(checker, directory: str, manifest: str) -> None:
    checker.DIRECTORY_CONTRACTS = {directory: ("manifest", manifest)}
    checker.ROOT_FILE_ALLOWLIST = set()
    checker.AUXILIARY_SHA_MANIFEST_ALLOWLIST = set()


def _git(repo: Path, *args: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=True,
        capture_output=True,
        text=True,
    )
    return completed.stdout.strip()


class EvidenceManifestTests(unittest.TestCase):
    def test_current_repository_evidence_contracts_pass(self) -> None:
        completed = subprocess.run(
            [sys.executable, str(SCRIPT), "--repo", str(ROOT)],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertIn("Evidence integrity contracts: PASS", completed.stdout)
        self.assertIn("6 registered directories", completed.stdout)

    def test_full_manifest_fails_on_digest_mismatch(self) -> None:
        checker = _load_checker()
        _register(checker, "case", checker.FULL_MANIFEST)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            payload = b"expected"
            (evidence / "payload.txt").write_bytes(payload)
            (evidence / checker.FULL_MANIFEST).write_text(
                f"{_digest(payload)}  payload.txt\n",
                encoding="utf-8",
            )
            (evidence / "payload.txt").write_bytes(b"tampered")
            errors = _errors(checker, root)
        self.assertTrue(any("SHA-256 mismatch for payload.txt" in e for e in errors), errors)

    def test_full_manifest_fails_on_uncovered_file(self) -> None:
        checker = _load_checker()
        _register(checker, "case", checker.FULL_MANIFEST)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            payload = b"covered"
            (evidence / "covered.txt").write_bytes(payload)
            (evidence / "uncovered.txt").write_bytes(b"uncovered")
            (evidence / checker.FULL_MANIFEST).write_text(
                f"{_digest(payload)}  covered.txt\n",
                encoding="utf-8",
            )
            errors = _errors(checker, root)
        self.assertTrue(
            any(
                "uncovered tracked in-scope files" in e and "uncovered.txt" in e
                for e in errors
            ),
            errors,
        )

    def test_full_manifest_fails_on_missing_listed_file(self) -> None:
        checker = _load_checker()
        _register(checker, "case", checker.FULL_MANIFEST)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / checker.FULL_MANIFEST).write_text(
                f"{'0' * 64}  missing.txt\n",
                encoding="utf-8",
            )
            errors = _errors(checker, root)
        self.assertTrue(any("listed file is missing: missing.txt" in e for e in errors), errors)

    def test_manifest_rejects_path_escape(self) -> None:
        checker = _load_checker()
        _register(checker, "case", checker.PARTIAL_MANIFEST)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / checker.PARTIAL_MANIFEST).write_text(
                f"{'0' * 64}  ../outside.txt\n",
                encoding="utf-8",
            )
            errors = _errors(checker, root)
        self.assertTrue(any("unsafe manifest path" in e for e in errors), errors)

    def test_artifact_manifest_is_explicitly_partial(self) -> None:
        checker = _load_checker()
        _register(checker, "case", checker.PARTIAL_MANIFEST)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            artifact = b"artifact"
            (evidence / "artifact.bin").write_bytes(artifact)
            (evidence / "README.md").write_text(
                "outside artifact digest scope\n",
                encoding="utf-8",
            )
            (evidence / checker.PARTIAL_MANIFEST).write_text(
                f"{_digest(artifact)}  artifact.bin\n",
                encoding="utf-8",
            )
            errors = _errors(checker, root)
        self.assertEqual([], errors)

    def test_full_manifest_ignores_untracked_gitignored_files(self) -> None:
        checker = _load_checker()
        _register(checker, "case", checker.FULL_MANIFEST)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            subprocess.run(["git", "init", "-b", "main", str(root)], check=True, capture_output=True)
            _git(root, "config", "user.name", "Test")
            _git(root, "config", "user.email", "test@example.com")
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            payload = b"tracked"
            (evidence / "payload.txt").write_bytes(payload)
            (evidence / checker.FULL_MANIFEST).write_text(
                f"{_digest(payload)}  payload.txt\n",
                encoding="utf-8",
            )
            (root / ".gitignore").write_text(".DS_Store\n", encoding="utf-8")
            _git(root, "add", ".gitignore", checker.EVIDENCE_ROOT.as_posix())
            _git(root, "commit", "-m", "fixture")
            (evidence / ".DS_Store").write_bytes(b"local junk")
            errors = _errors(checker, root)
        self.assertEqual([], errors)

    def test_unregistered_evidence_directory_fails_closed(self) -> None:
        checker = _load_checker()
        checker.DIRECTORY_CONTRACTS = {}
        checker.ROOT_FILE_ALLOWLIST = set()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            rogue = root / checker.EVIDENCE_ROOT / "rogue"
            rogue.mkdir(parents=True)
            (rogue / "payload.txt").write_text("evidence\n", encoding="utf-8")
            errors = _errors(checker, root)
        self.assertTrue(
            any("lack an integrity contract" in e and "rogue" in e for e in errors),
            errors,
        )

    def test_unrecognized_sha256sum_style_name_fails_closed(self) -> None:
        checker = _load_checker()
        checker.DIRECTORY_CONTRACTS = {"case": ("external", "owner.py")}
        checker.ROOT_FILE_ALLOWLIST = set()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "owner.py").write_text("# owner\n", encoding="utf-8")
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / "SHA256SUMS.txt").write_text("claim\n", encoding="utf-8")
            errors = _errors(checker, root)
        self.assertTrue(
            any("unrecognized SHA256SUMS-style manifest name" in e for e in errors),
            errors,
        )

    def test_lowercase_sha256sum_style_name_fails_closed(self) -> None:
        checker = _load_checker()
        checker.DIRECTORY_CONTRACTS = {"case": ("external", "owner.py")}
        checker.ROOT_FILE_ALLOWLIST = set()
        checker.AUXILIARY_SHA_MANIFEST_ALLOWLIST = set()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "owner.py").write_text("# owner\n", encoding="utf-8")
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / "sha256sums.txt").write_text("claim\n", encoding="utf-8")
            errors = _errors(checker, root)
        self.assertTrue(
            any("unrecognized SHA256SUMS-style manifest name" in e for e in errors),
            errors,
        )

    def test_full_manifest_rejects_symlink_target(self) -> None:
        checker = _load_checker()
        _register(checker, "case", checker.FULL_MANIFEST)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            payload = b"payload"
            (evidence / "payload.txt").write_bytes(payload)
            (evidence / "link.txt").symlink_to("payload.txt")
            (evidence / checker.FULL_MANIFEST).write_text(
                f"{_digest(payload)}  payload.txt\n"
                f"{_digest(payload)}  link.txt\n",
                encoding="utf-8",
            )
            errors = _errors(checker, root)
        self.assertTrue(
            any("symlink targets are not allowed: link.txt" in e for e in errors),
            errors,
        )

    def test_registered_root_file_is_accepted(self) -> None:
        checker = _load_checker()
        checker.DIRECTORY_CONTRACTS = {}
        checker.ROOT_FILE_ALLOWLIST = {"note.md"}
        checker.AUXILIARY_SHA_MANIFEST_ALLOWLIST = set()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT
            evidence.mkdir(parents=True)
            (evidence / "note.md").write_text("registered\n", encoding="utf-8")
            errors = _errors(checker, root)
        self.assertEqual([], errors)

    def test_missing_external_verifier_fails_closed(self) -> None:
        checker = _load_checker()
        checker.DIRECTORY_CONTRACTS = {"case": ("external", "missing-owner.py")}
        checker.ROOT_FILE_ALLOWLIST = set()
        checker.AUXILIARY_SHA_MANIFEST_ALLOWLIST = set()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / "payload.txt").write_text("evidence\n", encoding="utf-8")
            errors = _errors(checker, root)
        self.assertTrue(
            any("external integrity verifier is missing" in e for e in errors),
            errors,
        )

    def test_unknown_contract_kind_fails_closed(self) -> None:
        checker = _load_checker()
        checker.DIRECTORY_CONTRACTS = {"case": ("mystery", "owner")}
        checker.ROOT_FILE_ALLOWLIST = set()
        checker.AUXILIARY_SHA_MANIFEST_ALLOWLIST = set()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / "payload.txt").write_text("evidence\n", encoding="utf-8")
            errors = _errors(checker, root)
        self.assertTrue(
            any("unknown integrity contract kind 'mystery'" in e for e in errors),
            errors,
        )

    def test_stale_registered_directory_fails_closed(self) -> None:
        checker = _load_checker()
        checker.DIRECTORY_CONTRACTS = {
            "case": ("external", "owner.py"),
            "ghost": ("external", "owner.py"),
        }
        checker.ROOT_FILE_ALLOWLIST = set()
        checker.AUXILIARY_SHA_MANIFEST_ALLOWLIST = set()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "owner.py").write_text("# owner\n", encoding="utf-8")
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / "payload.txt").write_text("evidence\n", encoding="utf-8")
            errors = _errors(checker, root)
        self.assertTrue(
            any("registered integrity contracts have no tracked directory" in e
                and "ghost" in e for e in errors),
            errors,
        )

    def test_git_scope_failure_does_not_fall_back_to_disk(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / "payload.txt").write_text("local\n", encoding="utf-8")

            calls = 0
            original_git = checker._git

            def failing_git(repo, *args):
                nonlocal calls
                calls += 1
                if calls == 1:
                    return subprocess.CompletedProcess(
                        ["git"], 0, stdout=b"true\n", stderr=b""
                    )
                return subprocess.CompletedProcess(
                    ["git"], 128, stdout=b"", stderr=b"simulated ls-files failure"
                )

            checker._git = failing_git
            try:
                _, errors = checker.validate(root)
            finally:
                checker._git = original_git

        self.assertTrue(
            any("git ls-files failed" in e
                and "simulated ls-files failure" in e for e in errors),
            errors,
        )

    def test_empty_evidence_tree_is_not_itself_an_error(self) -> None:
        checker = _load_checker()
        checker.DIRECTORY_CONTRACTS = {}
        checker.ROOT_FILE_ALLOWLIST = set()
        with tempfile.TemporaryDirectory() as tmp:
            manifests, errors = checker.validate(Path(tmp))
        self.assertEqual([], manifests)
        self.assertEqual([], errors)


if __name__ == "__main__":
    unittest.main()
