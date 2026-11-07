"""평가 실행기의 입력 격리와 채점을 모델 없이 검증한다."""

import unittest

import evaluate_mac as evaluation


class EvaluationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan = evaluation.read(evaluation.ROOT / "evaluation/plan.json")
        cls.cases = evaluation.read(evaluation.ROOT / cls.plan["suite"])["cases"]

    def test_fixed_plan_produces_960_deterministic_isolated_requests(self):
        profiles, jobs = evaluation.prepare(self.plan, self.cases, "test")
        self.assertEqual((profiles, jobs), evaluation.prepare(self.plan, self.cases, "test"))
        self.assertEqual(len({request["request_id"] for _, request in jobs}), 960)
        self.assertEqual(len({request["state_seq"] for _, request in jobs}), 960)
        lookup = {case["case_id"]: case for case in self.cases}
        for index, request in jobs:
            case = lookup[index["case_id"]]
            self.assertEqual(set(request), {"protocol_version", "session_id", "request_id", "state_seq",
                                            "observation", "candidates"})
            self.assertEqual(request["observation"], {**case["shared_observation"],
                                                      "objective": case["objective"][index["language"]]})
            original = [item["id"] for item in case["candidates"]]
            expected_order = {"original": original, "reverse": original[::-1],
                              "rotate_left_one": original[1:] + original[:1]}
            self.assertEqual([item["id"] for item in request["candidates"]], expected_order[index["order"]])

    def test_rule_baseline_and_ties(self):
        rules, invalid = evaluation.rules_and_invalid(self.cases)
        self.assertEqual(len(rules), 40)
        self.assertTrue(all(row["correct"] for row in rules))
        self.assertEqual({row["case_id"] for row in rules if len(row["selected_actions"]) > 1}, {"C03", "A03"})
        self.assertFalse(any(row["reason"] == "rule_expectation_mismatch" for row in invalid))

    def test_errors_and_not_run_stay_in_denominators(self):
        variants = [{"case_id": "R01", "profile": "baseline", "language": "en", "repeat": 1,
                     "order": order, "request_id": order, "state_seq": seq}
                    for seq, order in enumerate(self.plan["candidate_orders"], 1)]
        requests = [{**variant, "session_id": "test", "candidates": [{"id": "strike"}]} for variant in variants]
        predictions = {"original": {**requests[0], "selected_action_id": "strike", "decision_source": "laya"}}
        scored = evaluation.score_jobs(list(zip(variants, requests)), predictions, {"original", "reverse"},
                                       {"R01": ["strike"]}, {})
        self.assertEqual([row["status"] for row in scored], ["correct", "error", "not_run"])
        self.assertEqual(evaluation.aggregate(scored)["accuracy"], 1 / 3)
        self.assertEqual(evaluation.aggregate(scored)["conditional_accuracy"], 1)
        self.assertEqual(evaluation.agreement(scored, "order")[0],
                         {"total": 1, "matched": 0, "incomplete": 1, "disagreed": 0, "rate": 0})

    def test_different_correct_actions_can_disagree(self):
        rows = [{"case_id": "C03", "order": "original", "repeat": 1, "language": lang,
                 "status": "correct", "request_id": lang, "selected_action_id": action}
                for lang, action in (("en", "narrow"), ("ko", "wide"))]
        self.assertEqual(evaluation.aggregate(rows)["accuracy"], 1)
        self.assertEqual(evaluation.agreement(rows, "language")[0]["disagreed"], 1)

    def test_latency_uses_nearest_rank_and_preserves_max(self):
        result = evaluation.latency([10, 2, 7, 4])
        self.assertEqual(result, {"count": 4, "median": 5.5, "p90": 10, "p95": 10, "max": 10})


if __name__ == "__main__":
    unittest.main()
