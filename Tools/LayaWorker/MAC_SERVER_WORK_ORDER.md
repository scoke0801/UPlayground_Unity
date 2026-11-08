# Mac Laya 전투 서버 준비 작업 요청

이 문서를 Mac의 작업 에이전트에게 전달한다. 환경 조사나 설치 안내만 작성하고 끝내지 말고, 가능한 환경 준비·실제 추론·HTTP 검증·서버 실행까지 진행한다.

## 1. 범위와 우선순위

이 Mac에서 Windows Unity 훈련장이 사용할 Laya 추론 서버를 준비한다. Unity 전투 실행기·인게임 디버거는 Windows 담당 범위다. 기존 `laya_worker.py` 서버를 재사용하고 새 서버·중계 서버를 불필요하게 만들지 않는다.

기존 사용자 변경을 보존한다. 이번 서버 준비에서는 별도 요청 없이 커밋·push하지 않는다. [MAC_WORK_ORDER.md](MAC_WORK_ORDER.md)는 과거 고정 평가 작업 명세다. 그 문서의 결과 커밋·대규모 평가 지시를 이번 작업에 자동 적용하지 않는다.

## 2. 먼저 확인할 자료

현재 저장소의 `AGENTS.md`가 있으면 읽고, 다음 파일을 확인한다. sparse checkout에서는 `Tools/LayaWorker` 전체만으로 서버 작업을 진행할 수 있다. 저장소 전체의 Unity 원본이나 Library를 다운로드할 필요는 없다.

- [README.md](README.md)
- [훈련장 연동 설계](TRAINING_GROUND_INTEGRATION_DESIGN.md)
- [한글 디버거 화면 시안](TRAINING_GROUND_DEBUGGER_PREVIEW.html): Windows 측 UI 참고이며 실제 실행 결과가 아니다.
- [기존 Mac 평가 명세](MAC_WORK_ORDER.md)
- [몬스터 데이터 안내](enemy-data/README.md)
- [서버 구현](laya_worker.py)
- [몬스터 요청 변환기](enemy_decision_request.py)
- [Mac 의존성](requirements-mac.txt)
- [몬스터용 서버 설정](enemy-data/worker-config.json)
- [실제 몬스터 데이터 Mac 보고서](output/enemy-data-review/2026-11-07-mac-02/REPORT.md) 및 같은 폴더의 원시 결과
- [기존 고정 평가 보고서](output/mac-evaluation-01/2026-11-07-mac-01/REPORT.md) 및 같은 폴더의 원시 결과

경로가 없으면 현재 체크아웃·받은 커밋부터 확인한다. 로컬 커밋은 원격에 push된 뒤 다른 Mac에서 받을 수 있다. 없는 자료를 읽었다고 가정하지 않는다.

## 3. 서버 역할과 호환 계약

Windows Unity가 실제 몬스터 관측과 허용된 의도 후보를 보낸다. Mac은 Laya로 후보 하나를 선택한다. 이동·피해·명중·애니메이션·공격 실행은 Unity가 소유하며 실제 실행 조건도 Unity가 다시 검사한다.

기존 HTTP v1을 유지한다.

- `GET /health`, `POST /v1/decide`.
- `protocol_version=1`, `session_id`·`request_id`·`state_seq` 일치.
- `selected_action_id`는 요청 후보 안에서만 선택.
- 후보별 `probabilities`, `selected_probability`, `inference_ms`, `decision_source`.
- 기존 모델·revision·설정 hash·usage metadata.
- 요청 크기·관측 크기·후보 수·토큰 잘림 검사.
- 동일 요청 캐시, 같은 ID의 다른 본문 충돌, 동시 추론 503, 기존 인증 동작.

후보·관측을 조용히 자르거나 추론 실패를 임의 행동으로 바꾸지 않는다. 캐시를 영속 exactly-once 실행 보장으로 설명하지 않는다. Windows와 독립적으로 작업하므로 필드·타입·오류 의미를 임의 변경하지 않는다. 호환성을 깨는 수정이 필요하면 이유와 대안을 정리하고 기존 v1 경로를 보존한다.

## 4. 환경 준비

CPU/Apple Silicon 여부, macOS, Python, 기존 가상환경·모델 캐시를 조사한다. 기존 정상 실행 환경과 캐시를 우선 재사용한다. 필요하면 이 폴더 전용 가상환경에 `requirements-mac.txt`를 설치하고 시스템 Python·전역 패키지는 불필요하게 변경하지 않는다.

몬스터용 `enemy-data/worker-config.json`의 모델·revision·dtype·지시문을 사용한다. 합성 플레이어 예제용 기본 설정과 혼동하지 않는다. 임의로 최신 패키지나 다른 모델로 교체하지 않는다. 캐시가 준비됐다면 offline도 확인하고, 없으면 지정 revision을 준비해 실제 다운로드·로드 결과를 기록한다.

## 5. 실행과 Windows 연결

기본은 Mac loopback 서버와 Windows SSH 터널이다. `Tools/LayaWorker`에서 가상환경을 활성화한 뒤 현재 CLI 옵션을 확인하고 실행한다.

```bash
python laya_worker.py serve \
  --config enemy-data/worker-config.json \
  --host 127.0.0.1 \
  --port 8765
```

`--offline`은 캐시가 준비됐을 때만 추가한다. 모델 준비 완료 출력과 실제 health 응답을 확인한 뒤 준비 완료로 판정한다.

Windows 안내 명령:

```powershell
ssh -N -L 8765:127.0.0.1:8765 <Mac사용자>@<Mac주소>
```

SSH 접속에 필요한 조건을 조사하되 SSH·방화벽·공유 설정을 광범위하게 변경하지 않는다. 필요한 설정이 있으면 정확히 안내한다. 기본으로 `0.0.0.0` 바인딩·외부 공개를 하지 않는다. LAN 직접 연결이 필요한 명확한 이유가 있으면 기존 `LAYA_WORKER_TOKEN` 계약을 사용하고 토큰은 파일·Git·출력에 노출하지 않는다. HTTPS·WebSocket 추가는 필요 없다.

## 6. 검증

기존 자동 테스트와 패키지 무결성 검사부터 실행한다. 실제 명령과 옵션은 현재 구현을 확인한다.

```bash
python -m unittest discover -s . -p 'test_*.py' -v
python laya_worker.py verify-data
python enemy_decision_request.py --verify
```

실제 모델·HTTP 서버로 health, 몬스터 설정 metadata, 골렘·엔트 예제 추론, ID·상태 순번 일치, 후보 안 선택, 확률·시간·usage 유효성을 확인한다. 동일 ID/본문 캐시, 다른 본문 충돌, 잘못된 요청·잘림·추론 오류, 동시 추론 제한은 기존 테스트를 우선 활용하고 실제 HTTP로 확인한 범위를 구분한다. 의미 없는 중복 테스트 체계는 만들지 않는다.

예제는 합성 데이터다. `runtime_capture`·실제 전투 결과로 표시하거나 정답 후보를 강제하지 않는다. 모델 로드·첫 추론·후속 추론·HTTP 왕복을 분리한다. Mac loopback 왕복을 Windows↔Mac 지연으로 보고하지 않는다. Unity 전투 품질과 Windows 연결 성공은 Mac 단독 검사로 주장하지 않는다.

검사 실패가 있으면 원인을 조사하고 범위 안에서 수정한 뒤 관련 검사를 재실행한다. 환경 때문에 불가능한 항목은 증거와 함께 미수행으로 남기고 독립적으로 가능한 작업은 끝낸다.

## 7. 서버 유지와 결과 보관

검증 후 가능하면 사용자 세션에서 서버를 사용할 수 있게 실행 상태로 남기고 PID·로그 위치·종료 방법을 기록한다. 에이전트의 일시적인 프로세스 수명에만 의존하지 않는다. 불필요한 전역 daemon·자동 시작 서비스는 설치하지 않는다. 같은 포트의 기존 프로세스는 정체를 확인하고 다른 사용자 프로세스를 임의 종료하지 않는다.

실행을 유지할 수 없는 환경이면 사실대로 알리고 한 번에 시작할 정확한 명령을 제공한다. 실행 중이지 않은 서버를 실행 중이라고 보고하지 않는다.

새 결과는 `results/server-readiness/<run-id>/`에 보존하고 과거 결과를 덮지 않는다. 환경·설정 hash·테스트 결과·원시 요청/응답·서버 로그를 저장하되 비밀 토큰·개인 키는 제외한다.

## 8. Windows 담당자에게 전달할 보고

한국어로 다음을 정리한다.

- 준비 완료 여부와 현재 실행 여부.
- 실제 Python·패키지·모델·revision, 설정 파일·hash.
- 바인딩 주소·포트, Windows SSH 터널 명령·endpoint.
- 인증 필요 여부. 토큰 값은 출력하지 않는다.
- 시작·상태 확인·종료 방법, PID·로그·결과 위치.
- 검사별 통과·실패·미수행, 실측 지연과 측정 범위.
- 코드 수정과 Windows 호환성 영향.
- Windows/Unity에서 추가 확인할 항목.

계획·설치 안내만으로 완료하지 말고, 이 Mac에서 가능한 실제 추론·HTTP 확인·서버 실행까지 수행한다.
