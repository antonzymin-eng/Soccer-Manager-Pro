"""Regression tests for the unread serialized/snapshot field sweep."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
import tempfile
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[2]
MODULE_PATH = ROOT / "tools" / "unread_serialized_field_sweep.py"
SPEC = importlib.util.spec_from_file_location("unread_serialized_field_sweep", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
sweep = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = sweep
SPEC.loader.exec_module(sweep)


class UnreadSerializedFieldSweepTests(unittest.TestCase):
    def _repo(self, files: dict[str, str]) -> Path:
        tmp = tempfile.TemporaryDirectory()
        root = Path(tmp.name)
        for rel, body in files.items():
            path = root / rel
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(textwrap.dedent(body), encoding="utf-8")
        self.addCleanup(tmp.cleanup)
        return root

    def test_snapshot_write_without_read_is_candidate(self) -> None:
        repo = self._repo(
            {
                "src/a/DefensiveAgentSnapshot.cs": """
                    namespace X
                    {
                        public struct DefensiveAgentSnapshot
                        {
                            public bool HasBall;
                            public int Used;
                        }

                        public sealed class Host
                        {
                            public void Fill()
                            {
                                var x = new DefensiveAgentSnapshot { HasBall = true, Used = 3 };
                                if (x.Used > 0) { }
                            }
                        }
                    }
                """,
            }
        )
        findings = {f.declaration.key: f for f in sweep.find_candidates(repo)}
        self.assertIn("DefensiveAgentSnapshot.HasBall", findings)
        self.assertNotIn("DefensiveAgentSnapshot.Used", findings)

    def test_transport_only_read_does_not_clear_candidate(self) -> None:
        repo = self._repo(
            {
                "src/a/FooState.cs": """
                    namespace X
                    {
                        public struct FooState
                        {
                            public int Dormant;
                        }

                        public sealed class Runtime
                        {
                            public FooState Build()
                            {
                                return new FooState { Dormant = 4 };
                            }
                        }
                    }
                """,
                "src/a/FooSerializer.cs": """
                    namespace X
                    {
                        public sealed class FooSerializer
                        {
                            public void Serialize(FooState value)
                            {
                                Sink(value.Dormant);
                            }

                            private void Sink(int value) { }
                        }
                    }
                """,
            }
        )
        findings = {f.declaration.key: f for f in sweep.find_candidates(repo)}
        item = findings["FooState.Dormant"]
        self.assertTrue(item.transport_reads)
        self.assertEqual("transport-only", item.category)

    def test_unity_serialize_field_is_in_scope(self) -> None:
        repo = self._repo(
            {
                "src/client/View.cs": """
                    namespace X
                    {
                        public sealed class View
                        {
                            [UnityEngine.SerializeField]
                            private float _unusedScale;

                            [SerializeField]
                            private float _usedScale;

                            public float Scale() => _usedScale;
                        }
                    }
                """,
            }
        )
        findings = {f.declaration.key: f for f in sweep.find_candidates(repo)}
        self.assertIn("View._unusedScale", findings)
        self.assertNotIn("View._usedScale", findings)

    def test_tests_do_not_count_as_behavioral_read(self) -> None:
        repo = self._repo(
            {
                "src/a/FooSnapshot.cs": """
                    namespace X
                    {
                        public struct FooSnapshot
                        {
                            public int Dormant;
                        }
                        public sealed class Runtime
                        {
                            public FooSnapshot Build() => new FooSnapshot { Dormant = 1 };
                        }
                    }
                """,
                "src/a/Tests/FooTests.cs": """
                    namespace X
                    {
                        public sealed class FooTests
                        {
                            public int Read(FooSnapshot value) => value.Dormant;
                        }
                    }
                """,
            }
        )
        findings = {f.declaration.key: f for f in sweep.find_candidates(repo)}
        self.assertIn("FooSnapshot.Dormant", findings)

    def test_compound_assignment_is_a_behavioral_read(self) -> None:
        repo = self._repo(
            {
                "src/a/FooState.cs": """
                    namespace X
                    {
                        public sealed class FooState
                        {
                            public int Count;
                            public void Step()
                            {
                                Count += 1;
                            }
                        }
                    }
                """,
            }
        )
        findings = {f.declaration.key: f for f in sweep.find_candidates(repo)}
        self.assertNotIn("FooState.Count", findings)


if __name__ == "__main__":
    unittest.main()
