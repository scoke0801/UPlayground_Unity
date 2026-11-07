# Mac Laya 실제 추론 테스트

2026-11-07에 실행한 합성 전투 관측 3건의 결과다. 두 실행 모두 응답 오류 없이 완료했고 기대 행동은 3건 중 2건이 일치했다.

- 코드 기준 커밋 `795beaf2`, Worker `1.0.1`
- 실행 환경 Apple Silicon, macOS 14.0, Python 3.12.16, Laya MLX 0.6.0, MLX 0.32.4
- 모델 `aac6fef/laya-multilingual-mlx`, revision `ba40c87fcb357f1643d04d71323af9cdc3b9e591`, FP16
- [입력 요청](../../data/examples/requests.jsonl)과 [기대 행동](../../data/examples/expectations.json)은 기준 커밋에 포함된 파일을 사용했다.
- [첫 실행 원본](mac-smoke-01.jsonl)은 모델을 내려받은 뒤 실행했다. [오프라인 재실행 원본](mac-smoke-02-offline.jsonl)은 같은 모델 캐시를 사용해 `infer --offline`으로 실행했다.

| 요청 | 기대 행동 | 실제 선택 | 첫 실행 (ms) | 오프라인 (ms) | 판정 |
|---|---|---|---:|---:|---|
| `sweep-0001` | `light_combo` 또는 `wide_finisher` | `wide_finisher` | 2127.276 | 71.884 | 일치 |
| `sweep-0002` | `dodge_side` | `dodge_side` | 53.902 | 39.106 | 일치 |
| `sweep-0003` | `approach_attack` | `attack_in_place` | 28.202 | 43.658 | 불일치 |

두 실행의 선택과 확률은 모두 같았다. 세 번째 요청에서 모델은 사거리 밖 적에게 제자리 공격(0.4848)을 접근 공격(0.3465)보다 높게 평가했다. 시간은 Worker가 기록한 `inference_ms`이며 모델 다운로드·로드와 네트워크 왕복을 포함하지 않는다.

같은 Python 환경에서 자동 테스트 23개가 통과했다. Windows에서 Mac으로 보내는 실제 요청과 Unity 전투 연동은 미검증이다.
