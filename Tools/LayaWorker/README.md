# Mac Laya 추론 처리자

이 폴더만 Mac에 내려받아 실행한다. Windows 측에서 선별한 관측 상태와 합법 행동 후보를 받아 Laya가 선택한 행동 ID를 반환한다. 전투 실행기·Unity 연결·수치 시뮬레이터는 포함하지 않는다.

실제 추론은 Apple Silicon, macOS 14 이상, Python 3.11 이상을 대상으로 한다. 기본 모델은 한국어 관측도 받도록 multilingual 체크포인트로 고정했다. `laya-mlx==0.6.0`과 모델 revision을 설정에 명시했다. 모델 자체의 전투 판단 정확도는 별도 확인 대상이다.

Mac에서 한 번에 평가·결과 커밋까지 진행할 작업 명세는 [MAC_WORK_ORDER.md](MAC_WORK_ORDER.md)다. 평가 사례 40개와 고정 비교 설정은 `evaluation/`에 있고, 반환 결과는 `output/mac-evaluation-01/<run-id>/`에 모은다.

훈련장 연결을 위한 **Mac 서버 준비 작업**은 [MAC_SERVER_WORK_ORDER.md](MAC_SERVER_WORK_ORDER.md)를 전달한다. 기존 고정 평가 작업과 구분하며, 환경 준비·실제 추론·HTTP 검증·서버 실행·Windows 연결 안내를 포함한다.

**현재 연결 방식은 [무료·무계정 HTTPS 터널 전환 지침](MAC_HTTPS_TUNNEL_WORK_ORDER.md)**이다. Mac 사설 주소에 직접 접근하지 않고 인증된 `127.0.0.1:8765` 서버를 Quick Tunnel에 연결한다. VPN·공유기·SSH 설정과 Windows 로컬 포트는 변경하지 않는다. 계정 생성·로그인·결제수단 등록·유료 전환은 하지 않는다.

실제 몬스터 판단 자료는 [enemy-data/README.md](enemy-data/README.md)를 사용한다. 골렘·엔트의 실제 데이터 의존성·판단 코드·필수 관측 계약과 몬스터 Intent 요청 변환기가 포함되어 있다. 기존 40개 합성 문제와 구분한다.

훈련장 Unity 연동은 [훈련장 Mac Laya 전투 연동 설계](TRAINING_GROUND_INTEGRATION_DESIGN.md)를 따른다. 관측·비동기 의도 선택·BT 적용·장애 복구·비교 테스트와 UI Toolkit 기반 인게임/에디터 디버거를 정의한 설계안이다. Development 빌드 실행도 필수 범위다. [한글 디버거 화면 시안](TRAINING_GROUND_DEBUGGER_PREVIEW.html)은 가상 데이터로 탐색할 수 있으며 실제 Unity 연동 구현은 아직 포함하지 않는다.

## 1. 폴더만 내려받기

이미 저장소가 있으면 커밋을 받은 뒤 `Tools/LayaWorker`로 이동한다. 새로 받는 경우 sparse checkout으로 프로젝트 원본 다운로드를 줄일 수 있다.

```bash
git clone --filter=blob:none --sparse <저장소_URL> UPlayground-Laya
cd UPlayground-Laya
git sparse-checkout set Tools/LayaWorker
cd Tools/LayaWorker
```

로컬 커밋은 원격에 push된 뒤 다른 Mac에서 받을 수 있다. 저장소의 다른 `Assets`·`Library` 폴더는 처리자 실행에 필요하지 않다.

## 2. 설치와 파일 추론

```bash
python3 -m venv .venv
source .venv/bin/activate
python -m pip install -r requirements-mac.txt
python laya_worker.py verify-data
python laya_worker.py inspect
python laya_worker.py infer --output results/first-run.jsonl
```

첫 `infer` 또는 `serve`는 고정 revision의 모델을 Hugging Face에서 준비한다. 이후 네트워크 없이 캐시만 사용하려면 `--offline`을 붙인다. 모델 가중치는 Git에 넣지 않는다. 결과 파일은 기존 파일을 덮어쓰지 않는다. 재실행할 때 새 출력 경로를 지정한다.

`inspect`는 모델을 로드하지 않고 실제 모델 입력을 출력한다. `infer`는 모델을 한 번 로드하고 JSONL의 각 행을 처리한다. 어느 행이 실패해도 해당 행의 오류를 결과에 남기고 나머지를 진행하며, 실패가 있으면 종료 코드 1을 반환한다. 잘못된 입력 형식은 모델 로드 전에 검사한다.

## 3. Mac 서버와 Windows 요청

현재는 [HTTPS 터널 전환 지침](MAC_HTTPS_TUNNEL_WORK_ORDER.md)의 토큰 설정·401 검사·터널 실행·HTTPS 요청 순서를 따른다. 아래는 로컬 서버 실행과 직접 접근이 가능한 환경의 SSH 대체 예제다.

```bash
source .venv/bin/activate
python laya_worker.py serve --host 127.0.0.1 --port 8765
```

`Laya 준비 완료`가 출력된 뒤 `/health`가 응답한다. 모델 로딩 중에는 서버가 아직 열리지 않는다. 종료는 Ctrl+C다.

Windows에서 SSH 터널을 열면 Mac 처리자를 로컬 주소로 사용할 수 있다.

```powershell
ssh -N -o ExitOnForwardFailure=yes -L 18765:127.0.0.1:8765 <사용자>@<Mac주소>
```

다른 Windows 터미널에서 모델 설치 없이 표준 Python으로 요청을 보낸다.

```powershell
python Tools/LayaWorker/laya_worker.py send --url http://127.0.0.1:18765 --output Tools/LayaWorker/results/mac-run-01.jsonl
```

SSH를 쓰지 않고 신뢰한 LAN에서 직접 연결하려면 Mac의 `LAYA_WORKER_TOKEN` 환경변수를 설정하고 `serve --host 0.0.0.0`으로 실행할 수 있으나 현재 선택한 연결 경로는 아니다. 현재 HTTPS 터널 방식은 loopback 바인딩을 유지하고 서버가 Bearer 인증을 적용한다. 이 도구 자체는 TLS를 제공하지 않는다. 토큰은 설정 파일이나 Git에 넣지 않는다.

## 4. 요청과 응답 계약

`POST /v1/decide`, `Content-Type: application/json`으로 보낸다. 한 요청의 전체 크기는 64KiB 이하, observation은 4096 bytes 이하, 후보는 1~8개다. 이 제한보다 작아도 모델 토큰 한도를 넘을 수 있다. 고정 SDK의 실제 토크나이저·후보 렌더러·체크포인트 예산으로 원문 토큰과 입력에 남는 후보 구간을 비교한다. 후보가 서로 구분되더라도 설명 일부가 잘리면 추론 전에 `options_truncated`로 거절한다. 관측 잘림은 추론 결과의 사용 내역으로 검사한다.

```json
{
  "protocol_version": 1,
  "request_id": "fight1-step17",
  "session_id": "experiment-001",
  "state_seq": 17,
  "observation": {
    "objective": "Resume attacking after avoiding the visible strike.",
    "player": {"hp_ratio": 0.6},
    "visible_threat": "The enemy is recovering from its strike."
  },
  "candidates": [
    {"id": "counter_attack", "description": "Use the available counterattack against the recovering enemy."},
    {"id": "wait", "description": "Stay still and wait."}
  ]
}
```

응답에는 `selected_action_id`, 후보별 `probabilities`, `selected_probability`, `inference_ms`, 요청 식별자·상태 번호, 모델·런타임·설정 해시, 토큰 사용 정보가 들어간다. 후보 하나면 모델을 호출하지 않고 `decision_source=single_candidate`로 반환한다. 모델 선택은 `decision_source=laya`다. 확률은 새 전투 과제의 정확도 보장이 아니다.

후보의 합법성·쿨다운·피해·명중은 호출자가 계산한다. 처리자는 후보를 추가하거나 임의 행동으로 폴백하지 않는다. 적의 다음 난수 결과나 화면 밖 숨은 상태는 관측에 넣지 않는다. 원본 참고 에셋 전체를 observation으로 보내지 않는다.

오래된 응답을 사용할지는 호출자가 현재 상태 번호와 다시 비교한다. 동봉 `send`는 원래 요청과 응답의 번호 일치까지 확인하지만 실제 게임 상태의 변화를 알지는 못한다.

| HTTP | 오류 | 처리 |
|---|---|---|
| 400/413/415 | 형식·크기·Content-Type 오류 | 요청 수정 |
| 401 | 인증 실패 | 양쪽 환경변수 확인 |
| 409 | 같은 요청 ID의 내용 변경 | 새 판단이면 새 ID 사용 |
| 422 | 후보 설명 잘림·관측 잘림·후보 구분 유실 | 요약을 줄여 새 요청으로 전송 |
| 502 | 추론 실패·모델 출력 오류 | 모델·버전 확인. 성공한 선택처럼 사용하지 않음 |
| 503 | 다른 추론 진행 중 | 같은 ID·같은 내용으로 재시도 |

성공 결과는 세션+요청 ID 기준 최근 256개만 메모리에 보관한다. 캐시가 남아 있으면 재전송 시 같은 결과를 반환한다. 프로세스 재시작·캐시 퇴출 뒤의 영속 exactly-once 처리는 제공하지 않는다. 송신기는 자동 재시도하지 않아 중복·폴백 실험을 숨기지 않는다.

HTTP 오류 본문을 받는 중 연결이 끊겨도 원래 상태를 `error.http_status`에 남기고 다음 행을 처리한다. 응답의 행동 ID가 문자열이 아니거나 후보에 없으면 해당 행은 `invalid_response`다. 실패한 행의 응답을 유효한 행동으로 사용하지 않는다.

## 5. 함께 배치한 데이터

- `worker-config.json`: 모델·revision·추론 지시문.
- `data/examples/requests.jsonl`: 합성 관측 3개. 실제 전투 로그가 아니다.
- `data/examples/expectations.json`: 합성 문제에서 기대하는 행동 후보. 처리자가 읽어 정답을 강제하지 않는다.
- `data/reference/`: 선택 정책 SO, 카타나·쌍도끼 공격/Set 일부, 종별 BT Rules와 설계 문서의 원본 바이트 복사.
- `data/manifest.json`: 복사 경로·원본 경로/GUID·원본 Git 식별 정보·SHA256·크기.

참고 에셋은 **해결된 전투 데이터가 아니다**. GUID 의존 전체·실제 클립·히트박스·KCC 환경이 없으며, 처리자가 자동으로 해석하지 않는다. 동봉 자료는 모델 입력을 작성하고 선택 근거를 비교하는 참고용이다. 이 폴더만으로 처치 시간·손맛·물리 접촉을 검증할 수 있다고 해석하지 않는다.

원본이 있는 현재 PC에서만 참고 자료를 갱신한다.

```powershell
python Tools/LayaWorker/export_reference_data.py
python Tools/LayaWorker/laya_worker.py verify-data
```

갱신기는 코드의 `SOURCES` 허용 목록만 복사한다. 원본 씬·모델·텍스처·음원·저장 데이터·계정·Git 이력은 복사하지 않는다. 수정 중인 원본도 현재 바이트로 복사하고, HEAD와 다른지 manifest에 남긴다. 복사본을 원본 에셋에 재적용하지 않는다. 모델 입력이나 기대 행동을 바꿨다면 실행 결과의 요청 파일도 함께 보관한다.

## 6. 확인 범위

표준 라이브러리 검사에는 모델 설치가 필요 없다.

```bash
python -m unittest discover -s . -p 'test_*.py' -v
```

이 검사는 대체 추론 응답으로 라벨 매핑·잘림 거절·중복 요청·오류 보존·HTTP 인증/왕복·상태 번호 검사를 수행한다. 실제 Laya 추론이나 전투 검증 통과를 의미하지 않는다. Mac에서 `infer`를 실행한 뒤 원시 응답과 `expectations.json`을 비교해야 해당 체크포인트의 합성 문제 적합성을 확인할 수 있다.

실제 모델의 토큰 사용 계측이 계약과 다르면 조용히 선택을 반환하지 않고 오류를 낸다. 모델·전처리·프롬프트를 바꿀 때 평가 자료와 출력도 새로 구분한다.

## 출처

SDK 호출·응답 필드는 [Laya MLX 사용 안내](https://github.com/mizorewww/laya-mlx/blob/main/docs/USAGE.md), 모델 revision은 [배포 체크섬 기록](https://github.com/mizorewww/laya-mlx/blob/main/benchmarks/results/hub-publication.json)을 기준으로 했다. 이 독립 MLX 이식판은 Laya 공식 구현과 별개이며, 모델의 과제별 한계는 [공식 Laya 문서](https://github.com/NandhaKishorM/laya)를 따른다.

## Windows 클라이언트 인계

2026-11-08 Mac 서버 검증과 연결·운영 방법은 [Windows 클라이언트 연결 안내](WINDOWS_CLIENT_HANDOFF.md)를 참조한다. Unity 없이 열어 볼 수 있는 HTML 결과와 원시 HTTP 기록을 포함한다.
