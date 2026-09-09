import importlib.util
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "round-resolution-fit.py"
SPEC = importlib.util.spec_from_file_location("round_resolution_fit", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class RoundResolutionFitInputValidationTests(unittest.TestCase):
    def assert_invalid(self, contents, message):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "corpus.csv"
            path.write_text(contents, encoding="utf-8")
            with self.assertRaisesRegex(MODULE.CorpusError, message):
                MODULE.read_rows([path])

    def test_rejects_all_seven_missing_required_column_combinations(self):
        required = ("dSquad", "homeGoals", "awayGoals")
        for mask in range(1, 8):
            present = [name for index, name in enumerate(required) if not mask & (1 << index)]
            with self.subTest(missing_mask=mask):
                self.assert_invalid(",".join(present) + "\n", "missing required column")

    def test_rejects_each_blank_required_value(self):
        rows = (
            ",1,1",
            "0,,1",
            "0,1,",
        )
        for row in rows:
            with self.subTest(row=row):
                self.assert_invalid("dSquad,homeGoals,awayGoals\n" + row + "\n", "must not be blank")

    def test_rejects_each_non_finite_rating(self):
        for value in ("nan", "inf", "-inf"):
            with self.subTest(value=value):
                self.assert_invalid(
                    f"dSquad,homeGoals,awayGoals\n{value},1,1\n",
                    "dSquad must be finite",
                )

    def test_rejects_malformed_values_in_each_numeric_field(self):
        rows = (
            "not-a-rating,1,1",
            "0,not-a-score,1",
            "0,1,not-a-score",
        )
        for row in rows:
            with self.subTest(row=row):
                self.assert_invalid("dSquad,homeGoals,awayGoals\n" + row + "\n", "invalid|integer")

    def test_rejects_fractional_goal_counts_for_both_sides(self):
        for row in ("0,1.5,1", "0,1,1.5"):
            with self.subTest(row=row):
                self.assert_invalid("dSquad,homeGoals,awayGoals\n" + row + "\n", "must be an integer")

    def test_rejects_negative_goal_counts_for_both_sides(self):
        for row in ("0,-1,1", "0,1,-1"):
            with self.subTest(row=row):
                self.assert_invalid("dSquad,homeGoals,awayGoals\n" + row + "\n", "must be >= 0")

    def test_accepts_engine_scores_above_surrogate_model_cap(self):
        excessive = MODULE.MAX_GOALS_PER_SIDE + 1
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "corpus.csv"
            path.write_text(
                f"dSquad,homeGoals,awayGoals\n0,{excessive},{excessive + 1}\n",
                encoding="utf-8",
            )
            self.assertEqual(
                MODULE.read_rows([path]),
                [{"d": 0.0, "h": excessive, "a": excessive + 1}],
            )

    def test_rejects_rows_wider_than_header(self):
        self.assert_invalid("dSquad,homeGoals,awayGoals\n0,1,1,unexpected\n", "more values")

    def test_missing_file_is_a_corpus_error(self):
        with self.assertRaisesRegex(MODULE.CorpusError, "cannot read corpus"):
            MODULE.read_rows([Path("/definitely/missing/corpus.csv")])

    def test_rejects_empty_file(self):
        self.assert_invalid("", "empty or has no CSV header")

    def test_accepts_bom_and_canonical_rows(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "corpus.csv"
            path.write_text("\ufeffdSquad,homeGoals,awayGoals\n-0.5,0,20\n", encoding="utf-8")
            self.assertEqual(MODULE.read_rows([path]), [{"d": -0.5, "h": 0, "a": 20}])


if __name__ == "__main__":
    unittest.main()
