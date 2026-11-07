"""선택 몬스터 데이터의 의존성·입력 완전성과 요청 변환을 검사한다."""

from copy import deepcopy
import json
from pathlib import Path
import tempfile
import unittest

import enemy_decision_request as enemy
import laya_worker as worker
import package_enemy_data as package


class EnemyDataTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.pack = enemy.read(enemy.PACK / "unity-export.json")
        cls.snapshot = enemy.read(enemy.PACK / "examples/Golem_Normal-near.json")

    def test_all_data_references_resolve_and_ai_motions_are_unambiguous(self):
        package.verify_graph(self.pack)
        self.assertEqual(len(self.pack["actors"]), 2)
        self.assertEqual(len(self.pack["objects"]), 486)
        cards = enemy.read(enemy.PACK / "actors.json")
        self.assertEqual([len(actor["attacks"]) for actor in cards], [4, 5])

    def test_transferred_file_hashes(self):
        manifest = enemy.verify_pack()
        self.assertGreater(len(manifest["files"]), 400)

    def test_complete_examples_preserve_actual_context_and_candidates(self):
        paths = sorted((enemy.PACK / "examples").glob("*.json"))
        self.assertEqual(len(paths), 12)
        native = {row["request_id"]: row for row in enemy.read(enemy.PACK / "native-score-fixtures.json")}
        for path in paths:
            with self.subTest(path=path.name):
                snapshot = enemy.read(path)
                request = enemy.build_request(snapshot)
                self.assertEqual(request["observation"]["context"], snapshot["context"])
                self.assertEqual(request["observation"]["source"], "synthetic_fixture")
                self.assertEqual(snapshot["candidate_intents"], native[snapshot["request_id"]]["candidate_pool"])
                self.assertEqual(snapshot["context"], native[snapshot["request_id"]]["context"])
                worker.validate_request(request)

    def test_runtime_templates_cannot_masquerade_as_complete_observations(self):
        for path in (enemy.PACK / "templates").glob("*.json"):
            with self.assertRaises(worker.WorkerError):
                enemy.build_request(enemy.read(path))

    def test_missing_state_and_untyped_fields_fail_closed(self):
        mutations = [
            lambda row: row["context"].pop("IsPlayerRecovering"),
            lambda row: row["context"].update(ActionDelayElapsed=None),
            lambda row: row["context"].update(Distance=float("nan")),
            lambda row: row["blackboard"].update({"Target.Distance": 99}),
            lambda row: row.update(runtime_level=None),
            lambda row: row.update(actor_id=[]),
            lambda row: row.update(candidate_intents=["InventedAction", "Attack"]),
            lambda row: row["ability_states"].pop(),
            lambda row: row["ability_states"][0].update(runtime_eligible=True, cooldown_remaining_s=1),
        ]
        for mutate in mutations:
            with self.subTest(mutate=mutate):
                snapshot = deepcopy(self.snapshot)
                mutate(snapshot)
                with self.assertRaises(worker.WorkerError):
                    enemy.build_request(snapshot)

    def test_utf8_export_contains_hangul_and_no_replacement_characters(self):
        text = (enemy.PACK / "unity-export.json").read_text(encoding="utf-8")
        self.assertNotIn("\ufffd", text)
        self.assertTrue(any("\uac00" <= character <= "\ud7a3" for character in text))


if __name__ == "__main__":
    unittest.main()
