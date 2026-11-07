"""고정 합성 사례를 오프라인으로 실행하고 별도 단계에서 채점한다."""

import argparse
from collections import Counter, defaultdict
from copy import deepcopy
import csv
from datetime import datetime, timezone
import hashlib
import importlib.metadata
from itertools import product
import math
import os
from pathlib import Path
import platform
import random
import re
import secrets
import statistics
import subprocess
import sys
import threading
import time

import laya_worker as worker

ROOT = worker.ROOT
PYTHON = [sys.executable, "-B"]
SCRIPTS = ("evaluate_mac.py", "test_evaluate_mac.py")
GIT_BASE = "beda570b"
AGREEMENTS = {
    "order": ("case_id", "language", "repeat"),
    "language": ("case_id", "order", "repeat"),
    "repeat": ("case_id", "language", "order"),
}


def utc_now():
    return datetime.now(timezone.utc).isoformat()


def read(path):
    return worker.decode_json(path.read_bytes())


def dump(path, value):
    path.write_text(worker.encode_json(value) + "\n", encoding="utf-8")


def append(stream, value):
    stream.write(worker.encode_json(value) + "\n")
    stream.flush()


def rows(path):
    return [worker.decode_json(line) for line in path.read_bytes().splitlines()]


def jsonl(path, values):
    with path.open("w", encoding="utf-8") as stream:
        for value in values:
            append(stream, value)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True)


def verify_fixed_inputs():
    manifest = read(ROOT / "evaluation/manifest.json")
    hashes = {}
    for entry in manifest["files"]:
        path = ROOT / entry["path"]
        digest = sha(path)
        if path.stat().st_size != entry["bytes"] or digest != entry["sha256"]:
            raise ValueError(f"고정 입력 해시 불일치: {entry['path']}")
        hashes[entry["path"]] = digest
    for name in ("evaluation/manifest.json", "worker-config.json", "data/examples/requests.jsonl",
                 "data/examples/expectations.json", "data/manifest.json",
                 "test-results/2026-11-07/mac-smoke-01.jsonl",
                 "test-results/2026-11-07/mac-smoke-02-offline.jsonl"):
        hashes[name] = sha(ROOT / name)
    return hashes


def candidate_order(candidates, order):
    if order == "original":
        return candidates[:]
    if order == "reverse":
        return candidates[::-1]
    if order == "rotate_left_one":
        return candidates[1:] + candidates[:1]
    raise ValueError(f"알 수 없는 후보 순서: {order}")


def request_for(case, language, order):
    observation = {**case["shared_observation"], "objective": case["objective"][language]}
    candidates = [{"id": item["id"], "description": item["description"][language]}
                  for item in candidate_order(case["candidates"], order)]
    return {"observation": observation, "candidates": candidates}


def prepare(plan, cases, run_id):
    rng = random.Random(plan["random_seed"])
    profiles = list(plan["profiles"])
    rng.shuffle(profiles)
    if len({case["case_id"] for case in cases}) != plan["expected_base_cases"]:
        raise ValueError("논리 사례 수 또는 ID가 계약과 다릅니다.")
    if Counter(case["split"] for case in cases) != {"development": 20, "validation": 20}:
        raise ValueError("평가 분할이 계약과 다릅니다.")
    jobs = []
    for profile in profiles:
        group = []
        for case, language, order, repeat in product(
                cases, plan["languages"], plan["candidate_orders"], range(1, plan["repetitions"] + 1)):
            req = request_for(case, language, order)
            index = {key: case[key] for key in ("case_id", "family", "split")}
            signature = [item["id"] for item in req["candidates"]]
            aliases = [name for name in plan["candidate_orders"]
                       if [item["id"] for item in candidate_order(case["candidates"], name)] == signature]
            index.update(profile=profile, language=language, order=order, repeat=repeat,
                         order_aliases=aliases, phase="evaluation")
            group.append((index, req))
        if len(group) != plan["expected_rows_per_profile"]:
            raise ValueError("프로필별 요청 수가 계약과 다릅니다.")
        rng.shuffle(group)
        jobs.extend(group)
    if len(jobs) != plan["expected_inference_rows"]:
        raise ValueError("전체 요청 수가 계약과 다릅니다.")
    return profiles, [identify(index, req, run_id, seq) for seq, (index, req) in enumerate(jobs, 1)]


def identify(index, request, run_id, seq):
    identifier = f"req-{seq:05d}"
    request = {**request, "protocol_version": 1, "session_id": f"eval-{run_id}-{seq:05d}",
               "request_id": identifier, "state_seq": seq}
    return {**index, "request_id": identifier, "state_seq": seq}, worker.validate_request(request)


def rule_choices(rule, state, candidates):
    """정답표 없이 관측 사실로 허용 행동 집합을 계산한다."""
    if rule == "range":
        return ["strike" if state["target"]["distance_m"] <= state["strike"]["reach_m"] else "approach"]
    if rule == "threat":
        threat = state["threat"]
        if threat["imminent"] and threat["intersects_player"]:
            return ["guard" if threat["guardable"] else "dodge"]
        return ["strike"]
    if rule == "opening":
        target = state["target"]
        for name in ("heavy", "light"):
            attack = state[name]
            if target["distance_m"] <= attack["reach_m"] and attack["first_hit_s"] <= target["opening_remaining_s"]:
                return [name]
        return ["wait"]
    if rule == "crowd":
        scores = {name: state[name]["targets"] * state[name]["damage_per_target"]
                  for name in ("narrow", "wide")}
        return [name for name, score in scores.items() if score == max(scores.values())]
    if rule == "resource":
        target = state["target"]
        for name in ("free", "skill"):
            attack = state[name]
            if (name in candidates and attack.get("available", True)
                    and state["player"]["energy"] >= attack["cost"]
                    and target["distance_m"] <= attack["reach_m"] and attack["damage"] >= target["hp"]):
                return [name]
        return ["free"]
    if rule == "retarget":
        distances = {f"target_{name}": target["distance_m"] for name, target in state["targets"].items()
                     if target["visible"] and target["alive"] and target["in_front"]}
        return [name for name, distance in distances.items() if distance == min(distances.values())]
    raise ValueError(f"알 수 없는 규칙: {rule}")


def predict_one(processor, request):
    try:
        return processor.decide(request)
    except worker.WorkerError as error:
        return {**worker.error_response(error), "request_id": request["request_id"],
                "session_id": request["session_id"], "state_seq": request["state_seq"]}


def timed(action):
    start = time.perf_counter()
    result = action()
    return result, round(1000 * (time.perf_counter() - start), 3)


def token_probes(processor, requests):
    results = []
    for request in requests:
        response = predict_one(processor, request)
        expected = "context_truncated" if len(request["observation"]["objective"]) > 160 else "options_truncated"
        error = response.get("error", {})
        results.append({"request": request, "expected_error": expected, "response": response,
                        "passed": error.get("code") == expected and error.get("http_status") == 422})
    return {"status": "completed", "checks": results,
            "budgets": {key: processor.backend.agent.cfg.get(key, default)
                        for key, default in (("max_len", 512), ("head_max_len", 192))}}


def loopback(processor, requests, destination, attempt):
    from urllib.error import HTTPError
    from urllib.request import Request, urlopen

    token = secrets.token_urlsafe()
    server = worker.create_server(processor, "127.0.0.1", 0, token)
    thread = threading.Thread(target=server.serve_forever)
    thread.start()
    url = f"http://127.0.0.1:{server.server_port}"

    def exchange(path, request=None, authenticated=True):
        headers = {"Content-Type": "application/json"}
        headers["Authorization"] = "Bearer " + (token if authenticated else "invalid")
        payload = None if request is None else worker.encode_json(request).encode()
        try:
            with urlopen(Request(url + path, data=payload, headers=headers), timeout=30) as response:
                return {"http_status": response.status, "body": worker.decode_json(response.read())}
        except HTTPError as error:
            with error:
                return {"http_status": error.code, "body": worker.decode_json(error.read())}

    checks = []

    def check(name, request, expected_status, predicate, authenticated=True):
        result, elapsed = timed(lambda: exchange("/health" if request is None else "/v1/decide",
                                                request, authenticated))
        body = result["body"]
        checks.append({"name": name, "request": request, "result": result, "round_trip_ms": elapsed,
                       "passed": result["http_status"] == expected_status and predicate(body)})
        return body

    try:
        check("health", None, 200, lambda body: body["ready"] and body["metadata"] == processor.metadata
              and body["protocol_version"] == worker.PROTOCOL_VERSION)
        originals = []
        for request in requests:
            attempt(request)
            originals.append(check("decide", request, 200, lambda body:
                                   all(body.get(key) == request[key] for key in
                                       ("request_id", "session_id", "state_seq", "protocol_version"))
                                   and isinstance(body.get("selected_action_id"), str)
                                   and body["selected_action_id"] in {c["id"] for c in request["candidates"]}))
        check("cached", requests[0], 200, lambda body: body == {**originals[0], "cached": True})
        conflict = deepcopy(requests[0])
        conflict["observation"]["objective"] += " Changed."
        check("conflict", conflict, 409, lambda body: body["error"]["code"] == "request_conflict")
        check("bad_auth", requests[0], 401, lambda body: body["error"]["code"] == "unauthorized", False)
    finally:
        server.shutdown()
        thread.join()
        server.server_close()
        dump(destination, {"status": "completed" if len(checks) == 7 else "incomplete",
                           "server_stopped": True, "checks": checks})


def child(config_path, input_path, directory, mode):
    os.environ["HF_HUB_OFFLINE"] = "1"
    config = worker.load_config(config_path)
    requests = rows(input_path)
    backend, load_ms = timed(lambda: worker.LayaBackend(config, offline=True))
    processor = worker.DecisionWorker(backend, config)
    dump(directory / "load.json", {"model_load_ms": load_ms, "metadata": processor.metadata})
    with (directory / "attempted.jsonl").open("w", encoding="utf-8") as attempted, (
            directory / "responses.jsonl").open("w", encoding="utf-8") as output:
        def attempt(request):
            append(attempted, request["request_id"])

        if mode == "token":
            for request in requests:
                attempt(request)
            result = token_probes(processor, requests)
            for check in result["checks"]:
                append(output, check["response"])
            dump(directory / "checks.json", result)
        elif mode == "http":
            try:
                loopback(processor, requests, directory / "checks.json", attempt)
            finally:
                checks_path = directory / "checks.json"
                if checks_path.exists():
                    for check in read(checks_path)["checks"]:
                        if check["name"] == "decide":
                            append(output, check["result"]["body"])
        else:
            consecutive_errors = 0
            for request in requests:
                attempt(request)
                response = predict_one(processor, request)
                append(output, response)
                code = response.get("error", {}).get("code")
                consecutive_errors = consecutive_errors + 1 if code in {
                    "inference_failed", "invalid_model_output"} else 0
                if consecutive_errors == 5:
                    raise RuntimeError("모델 오류 5회 연속으로 이 프로세스의 추론을 중단했습니다.")


def scrub(value):
    return (value.replace(str(ROOT), "Tools/LayaWorker")
            .replace(str(Path.home()), "<home>"))


def run_process(command, timeout):
    start = time.monotonic()
    result = {"command": [scrub(str(part)) for part in command]}
    try:
        process = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, timeout=max(timeout, 0.01),
                                 env={**os.environ, "HF_HUB_OFFLINE": "1"})
        result.update(status="completed" if process.returncode == 0 else "failed", exit_code=process.returncode,
                      stdout=scrub(process.stdout), stderr=scrub(process.stderr))
    except subprocess.TimeoutExpired as error:
        result.update(status="timeout", exit_code=None,
                      stdout=scrub((error.stdout or b"").decode(errors="replace")),
                      stderr=scrub((error.stderr or b"").decode(errors="replace")))
    result["wall_ms"] = round((time.monotonic() - start) * 1000, 3)
    return result


def collect_batch(scratch, name, config, jobs, timeout, mode, attempted, predictions):
    directory = scratch / name
    directory.mkdir()
    input_path = directory / "requests.jsonl"
    jsonl(input_path, (request for _, request in jobs))
    result = run_process([*PYTHON, __file__, "--child", str(config), str(input_path),
                          str(directory), mode], timeout)
    for file, sink in (("attempted.jsonl", attempted), ("responses.jsonl", predictions)):
        path = directory / file
        if path.exists():
            for line in path.read_bytes().splitlines():
                try:
                    value = worker.decode_json(line)
                except worker.WorkerError:
                    if result["status"] == "completed":
                        raise
                    break  # 강제 종료 시 마지막 행이 일부만 쓰였을 수 있다.
                if isinstance(value, str):
                    sink.add(value)
                else:
                    sink[value["request_id"]] = value
    for file, key in (("load.json", "load"), ("checks.json", "checks")):
        path = directory / file
        if path.exists():
            result[key] = read(path)
    return result


def environment():
    packages = {}
    for name in ("laya-mlx", "mlx", "tokenizers", "numpy", "huggingface-hub"):
        try:
            packages[name] = importlib.metadata.version(name)
        except importlib.metadata.PackageNotFoundError:
            packages[name] = None

    def sysctl(key):
        result = subprocess.run(["sysctl", "-n", key], capture_output=True, text=True)
        return result.stdout.strip() if result.returncode == 0 else None

    status = git("status", "--porcelain")
    return {"chip": sysctl("machdep.cpu.brand_string"), "architecture": platform.machine(),
            "memory_bytes": sysctl("hw.memsize"), "macos": platform.mac_ver()[0],
            "python": platform.python_version(), "packages": packages, "git_head": git("rev-parse", "HEAD").strip(),
            "dirty": bool(status), "status_counts": dict(Counter(line[:2] for line in status.splitlines()))}


def code_patch():
    # stage 상태와 관계없이 기준 이후 추적 변경과 새 실행기 두 파일만 보관한다.
    patch = git("diff", "--no-ext-diff", GIT_BASE, "--", "laya_worker.py", "test_laya_worker.py", *SCRIPTS)
    for name in SCRIPTS:
        if subprocess.run(["git", "ls-files", "--error-unmatch", name], cwd=ROOT,
                          capture_output=True).returncode:
            result = subprocess.run(["git", "diff", "--no-index", "--", "/dev/null", name], cwd=ROOT,
                                    text=True, capture_output=True, check=False)
            if result.returncode != 1:
                raise RuntimeError(f"새 코드 diff를 만들 수 없습니다: {name}")
            patch += result.stdout
    return patch


def latency(samples):
    if not samples:
        return {"count": 0, "median": None, "p90": None, "p95": None, "max": None}
    values = sorted(samples)
    return {"count": len(values), "median": statistics.median(values),
            "p90": values[math.ceil(0.90 * len(values)) - 1],
            "p95": values[math.ceil(0.95 * len(values)) - 1], "max": values[-1]}


def aggregate(items):
    total = len(items)
    counts = Counter(row["status"] for row in items)
    success = counts["correct"] + counts["wrong"]
    return {"total": total, "correct": counts["correct"], "success": success, "wrong": counts["wrong"],
            "errors": counts["error"], "not_run": counts["not_run"],
            "accuracy": counts["correct"] / total if total else None,
            "conditional_accuracy": counts["correct"] / success if success else None}


def agreement(items, dimension):
    groups = defaultdict(list)
    for item in items:
        key = tuple(item[field] for field in AGREEMENTS[dimension])
        groups[key].append(item)
    counts = Counter()
    failures = []
    for key, group in groups.items():
        selections = {item["selected_action_id"] for item in group}
        if any(item["status"] in {"error", "not_run"} for item in group):
            outcome = "incomplete"
        elif len(selections) == 1:
            outcome = "matched"
        else:
            outcome = "disagreed"
        counts[outcome] += 1
        if outcome != "matched":
            failures.append({"kind": "agreement", "dimension": dimension, "outcome": outcome,
                             "group": dict(zip(AGREEMENTS[dimension], key)),
                             "rows": [{"request_id": row["request_id"], "selected_action_id": row["selected_action_id"],
                                       "status": row["status"]} for row in group]})
    total = len(groups)
    return {"total": total, "matched": counts["matched"], "incomplete": counts["incomplete"],
            "disagreed": counts["disagreed"], "rate": counts["matched"] / total if total else None}, failures


def supplementary_jobs(run_id, seq, profiles, examples):
    groups = {}
    payloads = [{key: item[key] for key in ("observation", "candidates")} for item in examples]
    probes = [
        ("candidate_cap", "Choose a visible action.", [(" x" * 70), (" y" * 70)]),
        ("shared_head", "Choose a visible action.",
         [(" x" * 35) + f" {i}" for i in range(8)]),
        ("korean_cap", "현재 보이는 행동을 고르세요.",
         ["가나다라마바사아자차카타파하 " * 9, "하파타카차자아사바마라다나가 " * 9]),
        ("context", " x" * 1750, ["Attack.", "Wait."]),
    ]
    definitions = {
        "token": [({"probe": label}, {"observation": {"objective": objective},
                   "candidates": [{"id": f"action-{i}", "description": description}
                                  for i, description in enumerate(descriptions)]})
                  for label, objective, descriptions in probes],
        **{phase: [({"example_id": item["request_id"]}, payload)
                   for item, payload in zip(examples, payloads)] for phase in ("smoke-1", "smoke-2", "http")},
        "warm-up": [({"profile": profile, "example_id": examples[0]["request_id"]}, payloads[0])
                    for profile in profiles],
    }
    for phase, group in definitions.items():
        groups[phase] = []
        for extra, payload in group:
            seq += 1
            groups[phase].append(identify({"phase": phase, "profile": "baseline", **extra}, payload, run_id, seq))
    return groups


def rules_and_invalid(cases):
    rules, invalid = [], []
    for case in cases:
        candidates = {item["id"] for item in case["candidates"]}
        expected = sorted(case["expected_actions"])
        result = sorted(rule_choices(case["reference_rule"], case["shared_observation"], candidates))
        correct = result == expected and set(expected) <= candidates and bool(expected)
        rules.append({"case_id": case["case_id"], "family": case["family"], "split": case["split"],
                      "selected_actions": result, "expected_actions": expected, "correct": correct})
        if not correct:
            invalid.append({"case_id": case["case_id"], "reason": "rule_expectation_mismatch"})
        localized = [case["objective"]["ko"], *(item["description"]["ko"] for item in case["candidates"])]
        if any(not re.search("[가-힣]", value) and "?" in value for value in localized):
            invalid.append({"case_id": case["case_id"], "language": "ko", "reason": "corrupted_localized_text"})
    return rules, invalid


def score_jobs(jobs, predictions, attempted, expectations, reasons):
    scored = []
    for index, request in jobs:
        identifier = index["request_id"]
        response = predictions.get(identifier)
        expected = expectations[index["case_id"]]
        if response is None:
            status = "error" if identifier in attempted else "not_run"
            reason = reasons.get(identifier, "no_response")
        elif ("error" in response or response.get("cached") or response.get("decision_source") != "laya"
              or any(response.get(key) != request[key] for key in ("request_id", "session_id", "state_seq"))
              or response.get("selected_action_id") not in [item["id"] for item in request["candidates"]]):
            status, reason = "error", response.get("error", {}).get("code", "invalid_evaluation_response")
        else:
            status = "correct" if response["selected_action_id"] in expected else "wrong"
            reason = None if status == "correct" else "unexpected_action"
        scored.append({**index, "expected_actions": expected, "status": status, "reason": reason,
                       "selected_action_id": response.get("selected_action_id") if response else None})
    return scored


def summarize(scored, predictions):
    grouped = defaultdict(list)
    for row in scored:
        profile = row["profile"]
        grouped[(profile, "", "", "")].append(row)
        for split, language, family in product(("", row["split"]), ("", row["language"]), ("", row["family"])):
            if split or language or family:
                grouped[(profile, split, language, family)].append(row)
    matrix = [{**dict(zip(("profile", "split", "language", "family"), key)), **aggregate(group)}
              for key, group in sorted(grouped.items())]
    metrics, failures = {}, []
    for profile in sorted({item["profile"] for item in scored}):
        all_rows = grouped[(profile, "", "", "")]
        validation = grouped[(profile, "validation", "", "")]
        comparisons = {}
        for scope, group in (("all", all_rows), ("validation", validation)):
            comparisons[scope] = {}
            for dimension in AGREEMENTS:
                result, details = agreement(group, dimension)
                comparisons[scope][dimension] = result
                if scope == "all":
                    failures.extend({"profile": profile, **detail} for detail in details)
        metrics[profile] = {
            "all": aggregate(all_rows), "validation": aggregate(validation),
            "validation_families": {family: aggregate(grouped[(profile, "validation", "", family)])
                                    for family in sorted({row["family"] for row in validation})},
            "agreement": comparisons,
            "warmed_ms": latency([predictions[row["request_id"]]["inference_ms"]
                                  for row in all_rows if row["status"] in {"correct", "wrong"}]),
        }
    return metrics, matrix, failures


def recommend(metrics, thresholds, invalid, prerequisite_failures, attempted_count):
    eligible = []
    gates = {}
    for profile, item in metrics.items():
        compare = item["agreement"]["all"]
        gates[profile] = {
            "validation_accuracy": item["validation"]["accuracy"] >= thresholds["validation_row_accuracy_min"],
            "validation_families": all(f["accuracy"] >= thresholds["validation_family_accuracy_min"]
                                       for f in item["validation_families"].values()),
            "order": compare["order"]["rate"] >= thresholds["candidate_order_agreement_min"],
            "language": compare["language"]["rate"] >= thresholds["language_agreement_min"],
            "repeat": compare["repeat"]["rate"] >= thresholds["repetition_agreement_min"],
            "complete": item["all"]["errors"] <= thresholds["unexpected_error_count_max"]
                        and item["all"]["not_run"] == 0,
        }
        if all(gates[profile].values()) and not invalid and not prerequisite_failures:
            eligible.append(profile)

    def rank(profile):
        item = metrics[profile]
        return (item["validation"]["accuracy"], item["agreement"]["all"]["order"]["rate"],
                -item["warmed_ms"]["median"], profile == "baseline")

    recommendation = "model_candidate" if eligible else "rules_first" if attempted_count else "blocked"
    return {"recommendation": recommendation, "profile": max(eligible, key=rank) if eligible else None,
            "threshold_checks": gates, "invalid_case_rows": len(invalid),
            "prerequisite_failures": prerequisite_failures, "unity_combat_verified": False}


def smoke_summary(supplementary, predictions):
    baseline = ROOT / "test-results/2026-11-07"
    references = [rows(baseline / name) for name in ("mac-smoke-01.jsonl", "mac-smoke-02-offline.jsonl")]
    result = {}
    for index, phase in enumerate(("smoke-1", "smoke-2")):
        comparisons = []
        for (meta, _), previous in zip(supplementary[phase], references[index]):
            observed = predictions.get(meta["request_id"], {})
            comparisons.append({"example_id": meta["example_id"], "request_id": meta["request_id"],
                                "expected_previous_action": previous["selected_action_id"],
                                "selected_action_id": observed.get("selected_action_id"),
                                "same_action": observed.get("selected_action_id") == previous["selected_action_id"],
                                "same_probabilities": observed.get("probabilities") == previous["probabilities"],
                                "inference_ms": observed.get("inference_ms")})
        result[phase] = comparisons
    return result


def report(path, summary, recommendation, info, tasks, invalid):
    decision = {"rules_first": "규칙 정책 우선", "blocked": "환경 차단",
                "model_candidate": "Laya 제한적 채택 후보"}[recommendation["recommendation"]]
    lines = ["# Mac Laya 고정 평가", "", f"**결론은 {decision}이다.**", "",
             f"- 환경 {info['chip']} / macOS {info['macos']} / Python {info['python']}.",
             "- 패키지 " + ", ".join(f"{key} {value}" for key, value in info["packages"].items()) + ".",
             f"- 본 평가 요청 {summary['attempted']}/{summary['planned']}건 실행 시도.",
             f"- 규칙 기준선 {summary['rule_baseline']['correct']}/{summary['rule_baseline']['total']}건 일치.",
             f"- 한국어 입력의 물음표 손상 {sum(x['reason'] == 'corrupted_localized_text' for x in invalid)}개 사례. 원본 그대로 실행했으며 한국어 및 언어 일치율은 정상 번역 평가에 사용할 수 없다.",
             "- Unity 전투와 Windows↔Mac 통신은 이번 Mac 단독 작업 범위 밖이다.",
             "", "## 수행 결과", ""]
    lines.extend(f"- {key} {value}" for key, value in tasks.items())
    lines.extend(["", "## 검증 정확도와 일치율", "",
                  "| 프로필 | 정답 / 전체 | 정답 / 정상 응답 | 순서 | 언어 | 반복 |",
                  "|---|---:|---:|---:|---:|---:|"])
    for profile, metrics in summary["profiles"].items():
        v = metrics["validation"]
        compare = metrics["agreement"]["all"]
        ratios = " | ".join(f"{compare[k]['matched']}/{compare[k]['total']}" for k in ("order", "language", "repeat"))
        conditional = f"{v['conditional_accuracy']:.1%}" if v["conditional_accuracy"] is not None else "미수행"
        lines.append(f"| {profile} | {v['correct']}/{v['total']} ({v['accuracy']:.1%}) | "
                     f"{v['correct']}/{v['success']} ({conditional}) | {ratios} |")
    lines.extend(["", "손상된 한국어 문구를 제외한 영어 검증 참고 집계", ""])
    for profile, splits in summary["valid_rows_by_profile_split"].items():
        valid = splits["validation"]
        if valid["total"]:
            lines.append(f"- {profile} {valid['correct']}/{valid['total']} ({valid['accuracy']:.1%}).")
    lines.extend(["", "## 전체 정확도와 warmed 지연", "",
                  "| 프로필 | 정답 / 전체 | 오류 | 미수행 | 중앙값 ms | P90 ms | P95 ms | 최대 ms |",
                  "|---|---:|---:|---:|---:|---:|---:|---:|"])
    for profile, metrics in summary["profiles"].items():
        counts, ms = metrics["all"], metrics["warmed_ms"]
        lines.append(f"| {profile} | {counts['correct']}/{counts['total']} | {counts['errors']} | {counts['not_run']} | "
                     + " | ".join(str(ms[k]) if ms[k] is not None else "—" for k in ("median", "p90", "p95", "max")) + " |")
    lines.extend(["", "첫 추론, 모델 로드, HTTP 왕복은 warmed 지연과 분리해 아래에 기록했다.", "",
                  "| 단계 | 모델 로드 ms | 첫 추론 ms | HTTP 왕복 중앙값 ms |",
                  "|---|---:|---:|---:|"])
    for phase, timings in summary["phase_timings"].items():
        lines.append(f"| {phase} | {timings['model_load_ms']} | {timings['first_inference_ms']} | "
                     f"{timings.get('round_trip_ms', {}).get('median', '—')} |")
    lines.extend(["", "## 오답과 후속 조치", "",
                  "| 프로필 | 영역 | 오답 | 오류 | 미수행 |",
                  "|---|---|---:|---:|---:|"])
    for item in summary["breakdown"]:
        if item["family"] and not item["split"] and not item["language"]:
            lines.append(f"| {item['profile']} | {item['family']} | {item['wrong']} | {item['errors']} | {item['not_run']} |")
    lines.extend(["", "오답 행의 후보·확률과 일치율 위반은 [failures.jsonl](failures.jsonl), "
                  "전체 분할·언어·영역 집계는 [summary.csv](summary.csv)에 남겼다.",
                  "정상 한국어 문구가 포함된 별도의 고정 평가를 마련한 뒤 언어 비교를 다시 판단할 수 있다. "
                  "이번 실행의 고정 입력과 기본 프로필은 그대로 유지했다.", ""])
    path.write_text("\n".join(lines), encoding="utf-8")


def finish(output, plan, jobs, supplemental, attempted, predictions, processes, reasons, info, manifest,
           unit, data, start):
    cases = read(ROOT / plan["suite"])["cases"]
    rules, invalid = rules_and_invalid(cases)
    jsonl(output / "rule-baseline.jsonl", rules)
    dump(output / "invalid_cases.json", invalid)
    expectations = {case["case_id"]: case["expected_actions"] for case in cases}
    scored = score_jobs(jobs, predictions, attempted, expectations, reasons)
    metrics, matrix, failures = summarize(scored, predictions)
    inputs = dict((index["request_id"], request) for index, request in jobs)
    for item in scored:
        identifier = item["request_id"]
        if item["status"] != "correct":
            failures.append({"kind": item["status"], **item, "request": inputs[identifier],
                             "probabilities": predictions.get(identifier, {}).get("probabilities")})
    checks = {}
    for mode, filename in (("token", "token-checks.json"), ("http", "http-checks.json")):
        process = processes.get(mode, {"status": "not_run"})
        checks[mode] = {**process.get("checks", {"status": process["status"], "checks": []}),
                        "exit_code": process.get("exit_code")}
        dump(output / filename, checks[mode])
    all_jobs = jobs + [job for group in supplemental.values() for job in group]
    jsonl(output / "request-index.jsonl", (index for index, _ in all_jobs))
    jsonl(output / "inputs.jsonl", ({"phase": index["phase"], "profile": index["profile"], "request": req}
                                   for index, req in all_jobs if index["request_id"] in attempted))
    jsonl(output / "predictions.jsonl", (
        {"phase": index["phase"], "response": predictions.get(index["request_id"], {
            "request_id": index["request_id"], "session_id": req["session_id"], "state_seq": req["state_seq"],
            "status": "no_response" if index["request_id"] in attempted else "not_run",
            "reason": reasons.get(index["request_id"], "process_did_not_produce_response"),
        })} for index, req in all_jobs))
    jsonl(output / "scored.jsonl", scored)
    jsonl(output / "failures.jsonl", failures)
    with (output / "summary.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(matrix[0]))
        writer.writeheader()
        writer.writerows(matrix)
    timings = {}
    for phase, process in processes.items():
        first_job = (next((job for job in supplemental["warm-up"] if job[0]["profile"] == phase), None)
                     if phase in plan["profiles"] else next(iter(supplemental.get(phase, ())), None))
        first = predictions.get(first_job[0]["request_id"], {}) if first_job else {}
        timings[phase] = {"model_load_ms": process.get("load", {}).get("model_load_ms"),
                          "first_inference_ms": first.get("inference_ms")}
        if phase == "http":
            timings[phase]["round_trip_ms"] = latency([item["round_trip_ms"] for item in checks["http"]["checks"]])
    good_rules = sum(row["correct"] for row in rules)
    attempted_count = len(set(inputs) & attempted)
    gate_failures = [name for name, ok in {
        "unit_tests": unit["exit_code"] == 0, "verify_data": data["exit_code"] == 0,
        "rules": good_rules == len(rules),
        **{key: len(value["checks"]) == count and all(x["passed"] for x in value["checks"])
           for key, value, count in (("token", checks["token"], 4), ("http", checks["http"], 7))},
    }.items() if not ok]
    valid = [row for row in scored if not any(item["case_id"] == row["case_id"]
             and item.get("language", row["language"]) == row["language"] for item in invalid)]
    summary = {"planned": len(jobs), "attempted": attempted_count, "overall": aggregate(scored),
               "profiles": metrics, "breakdown": matrix, "valid_rows_only": aggregate(valid),
               "valid_rows_by_profile_split": {profile: {split: aggregate([
                   row for row in valid if row["profile"] == profile and row["split"] == split])
                   for split in ("development", "validation")} for profile in plan["profiles"]},
               "invalid_cases_by_reason": dict(Counter(item["reason"] for item in invalid)),
               "rule_baseline": {"correct": good_rules, "total": len(rules)},
               "smoke": smoke_summary(supplemental, predictions), "phase_timings": timings}
    result = recommend(metrics, plan["promotion_thresholds"], invalid, gate_failures, attempted_count)
    dump(output / "summary.json", summary)
    dump(output / "recommendation.json", result)
    task_status = {
        "M01": f"자동 검사 {unit['exit_code']}, 참고 데이터 검사 {data['exit_code']}.",
        "M02": f"토큰 검사 {sum(x['passed'] for x in checks['token']['checks'])}/4 통과.",
        "M03": f"기존 행동·확률 동시 재현 {sum(x['same_action'] and x['same_probabilities'] for g in summary['smoke'].values() for x in g)}/6건.",
        "M04": f"{attempted_count}/{len(jobs)}건 시도. 오류 {summary['overall']['errors']}건, 미수행 {summary['overall']['not_run']}건.",
        "M05": f"Mac loopback {sum(x['passed'] for x in checks['http']['checks'])}/7 통과. 서버 정리 {checks['http'].get('server_stopped', False)}.",
        "M06": "고정 기준 채점, 언어 손상 기록 및 권고 산출.",
    }
    report(output / "REPORT.md", summary, result, info, task_status, invalid)
    manifest.update(status="completed" if attempted_count == len(jobs) else "incomplete",
                    finished_at=utc_now(), elapsed_ms=round((time.monotonic() - start) * 1000, 3),
                    phases={name: {key: value for key, value in proc.items() if key != "checks"}
                            for name, proc in processes.items()}, verify_data=data)
    dump(output / "run-manifest.json", manifest)
    print(worker.encode_json({"run_id": manifest["run_id"], "attempted": attempted_count,
                              "validation": {k: v["validation"] for k, v in metrics.items()}, **result}))


def main():
    import tempfile

    if sys.argv[1:2] == ["--child"]:
        config, input_path, directory, mode = sys.argv[2:]
        child(Path(config), Path(input_path), Path(directory), mode)
        return
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--plan", type=Path, required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--prepare-only", action="store_true")
    args = parser.parse_args()
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_-]{0,63}", args.run_id):
        parser.error("run-id는 영문·숫자·_-로 구성한 1~64자 식별자여야 합니다.")
    start = time.monotonic()
    started_at = utc_now()
    hashes = verify_fixed_inputs()
    if args.plan.resolve() != ROOT / "evaluation/plan.json":
        parser.error("고정 evaluation/plan.json을 사용하세요.")
    plan = read(args.plan)
    cases = [{key: case[key] for key in ("case_id", "family", "split", "shared_observation",
              "objective", "candidates")} for case in read(ROOT / plan["suite"])["cases"]]
    profiles, jobs = prepare(plan, cases, args.run_id)
    examples = worker.read_requests(ROOT / "data/examples/requests.jsonl")
    supplemental = supplementary_jobs(args.run_id, len(jobs), profiles, examples)
    configurations = {name: ROOT / path for name, path in plan["profiles"].items()}
    configs = {name: worker.load_config(path) for name, path in configurations.items()}
    if any({k: v for k, v in config.items() if k != "instructions"}
           != {k: v for k, v in configs["baseline"].items() if k != "instructions"} for config in configs.values()):
        raise ValueError("프로필 사이에 고정 체크포인트가 다릅니다.")
    info = environment()
    patch = code_patch()
    output = ROOT / plan["output_root"] / args.run_id
    output.mkdir(parents=True, exist_ok=False)
    (output / "changes.patch").write_text(patch, encoding="utf-8")
    dump(output / "environment.json", info)
    manifest = {"run_id": args.run_id, "status": "prepared_only" if args.prepare_only else "running",
                "started_at": started_at, "input_sha256": hashes,
                "git_head": info["git_head"], "dirty": info["dirty"], "status_counts": info["status_counts"],
                "code_sha256": {name: sha(ROOT / name) for name in (*SCRIPTS, "laya_worker.py")},
                "patch_sha256": sha(output / "changes.patch"),
                "profiles": configs, "profile_execution_order": profiles, "packages": info["packages"],
                "seed": plan["random_seed"], "inference_budget_minutes": plan["inference_budget_minutes"],
                "execution_order": [index["request_id"] for index, _ in jobs],
                "scope": plan["scope"], "network": plan["network"]}
    dump(output / "run-manifest.json", manifest)
    if args.prepare_only:
        jsonl(output / "inputs.jsonl", ({"phase": index["phase"], "profile": index["profile"], "request": req}
                                       for index, req in jobs))
        jsonl(output / "request-index.jsonl", (index for index, _ in jobs))
        jsonl(output / "predictions.jsonl", ())
        manifest["finished_at"] = utc_now()
        dump(output / "run-manifest.json", manifest)
    else:
        unit = run_process([*PYTHON, "-m", "unittest", "discover", "-s", ".", "-p", "test_*.py", "-v"], 90)
        data = run_process([*PYTHON, "laya_worker.py", "verify-data"], 30)
        with (output / "unit-tests.txt").open("w", encoding="utf-8") as stream:
            for test in (unit, data):
                stream.write(f"$ {' '.join(test['command'])}\nexit_code={test['exit_code']}\n"
                             f"stdout\n{test['stdout']}\nstderr\n{test['stderr']}\n")
        attempted, predictions, processes, reasons = set(), {}, {}, {}
        with tempfile.TemporaryDirectory(prefix="laya-eval-") as directory:
            scratch = Path(directory)
            for phase, mode in (("token", "token"), ("smoke-1", "predict"), ("smoke-2", "predict")):
                processes[phase] = collect_batch(scratch, phase, configurations["baseline"], supplemental[phase],
                                                 90, mode, attempted, predictions)
            deadline = time.monotonic() + 60 * plan["inference_budget_minutes"]
            for profile in profiles:
                group = [job for job in supplemental["warm-up"] + jobs if job[0]["profile"] == profile]
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    processes[profile] = {"status": "timeout"}
                else:
                    processes[profile] = collect_batch(scratch, profile, configurations[profile], group,
                                                       remaining, "predict", attempted, predictions)
                for index, _ in group:
                    if index["request_id"] not in predictions:
                        reasons[index["request_id"]] = processes[profile]["status"]
                print(f"{profile}: {processes[profile]['status']} ({len(set(predictions) & {i['request_id'] for i, _ in group})}/{len(group)})",
                      file=sys.stderr, flush=True)
            processes["http"] = collect_batch(scratch, "http", configurations["baseline"], supplemental["http"],
                                              90, "http", attempted, predictions)
            finish(output, plan, jobs, supplemental, attempted, predictions, processes, reasons, info, manifest,
                   unit, data, start)
    (output / "SHA256SUMS").write_text("".join(f"{sha(path)}  {path.name}\n" for path in sorted(output.iterdir())
                                              if path.name != "SHA256SUMS"), encoding="utf-8")


if __name__ == "__main__":
    main()
