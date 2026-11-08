# Mac Laya 서버 준비 결과

몬스터용 기존 v1 서버 준비 완료. PID 9919, 127.0.0.1:8765에서 실행 중(보고 시 health 재확인). 기존 CLI를 독립 세션 프로세스로 실행했다. 재부팅·로그아웃 후 자동 시작은 설치하지 않았다.

- Apple Silicon Apple M2, macOS 14.0, Python 3.12.16
- 패키지: {"laya-mlx": "0.6.0", "mlx": "0.32.4", "tokenizers": "0.23.3", "numpy": "2.5.4"}
- 모델: aac6fef/laya-multilingual-mlx, revision ba40c87fcb357f1643d04d71323af9cdc3b9e591, dtype float16
- 설정: enemy-data/worker-config.json
- 응답의 canonical config_sha256: e98b441f42d45aa39c026cb3e7f2a0e0212406a76375370fbe70bbdefe6831d3
- 설정 원본 파일 SHA256: 011ccc49d02dc4faf930f35e6b2e5982a8f93eefb5275fd6ad6588cf0b2ae248
- 기존 가상환경·HF 캐시 재사용. 설치·다운로드·모델·프롬프트·서버 코드 변경 없음.

## 검증

기존 자동 테스트 34개 통과(모의 backend 기반 오류·인증·동시성 검증 포함). verify-data 및 enemy_decision_request.py --verify 통과. 실제 모델/HTTP로 합성 골렘·엔트 12/12 요청 성공. 식별자·상태 순번, 후보 포함, 확률, usage, metadata 검사 통과. 같은 본문 캐시, 다른 본문 409, 잘못된 요청 400, 관측 크기 413, 실제 관측/후보 토큰 초과 422, 병렬 요청 503 확인. 원시 요청·응답은 동봉 JSONL/JSON에 보존.

추론 실패 502 및 잘못된 모델 출력 거절은 기존 모의 테스트로 확인했다. 실행 중인 실제 모델에 고의 장애를 주입하지 않았다. 토큰·개인 키 저장 없음. 인증은 loopback 서버에서 사용하지 않으며 SSH 인증은 별도 필요하다. LAN 공개나 시스템 서비스 변경 없음.

## 시간 측정

- 별도 프로세스의 오프라인 backend 모델 로드: 1884.051 ms (OS 캐시 warm)
- 서버 프로세스 시작→health: 3041.674 ms (로드·프로세스·poll 포함)
- 서버 첫 추론: 219.072 ms
- 이후 11회 추론 중앙값: 229.749 ms
- 12회 HTTP loopback RTT 중앙값: 226.057 ms

Windows↔Mac 지연이 아니다. 실제 Unity 전투·runtime_capture·Windows 접속 성공·전투 판단 품질은 미검증이다. 입력은 synthetic_fixture이며 정답 강제·fallback 없음. 모델은 Intent만 선택하고 Unity가 실행 자격을 다시 검사해야 한다. 메모리 캐시는 영속 exactly-once 보장이 아니다.

## Windows 연결

```powershell
ssh -N -L 8765:127.0.0.1:8765 jonghyun@<Mac주소>
```

endpoint: http://127.0.0.1:8765/v1/decide , health: http://127.0.0.1:8765/health

Mac 조사 결과(SSH loopback와 네트워크 주소):
```
nc: connectx to 127.0.0.1 port 22 (tcp) failed: Connection refused
imjonghyeon-ui-noteubug
192.168.35.223
```

SSH 22 포트가 거절되어 현재 터널 준비는 미완료다. macOS 시스템 설정 → 일반 → 공유 → 원격 로그인을 켜고 해당 Mac 사용자에게 접속을 허용해야 한다. 같은 네트워크/VPN에서 Windows가 Mac 주소의 TCP 22에 도달하는지 확인한다. 방화벽은 SSH에 필요한 범위만 허용한다. 설정은 임의 변경하지 않았다.

## 운영

상태: `curl --fail http://127.0.0.1:8765/health`
종료: `kill 9919` (해당 PID가 이 서버인지 확인 후)
재시작: 이 결과 폴더의 start-server.sh를 `sh`로 실행. 이미 포트가 사용 중이면 중복 실행하지 않는다. 터미널에서 재시작하면 Ctrl+C로 종료한다.

서버 로그: /Users/jonghyun/ProjectRepo/UnityProj/UPlayground_Unity/Tools/LayaWorker/results/server-readiness/2026-11-08-mac-01/server.log
PID: /Users/jonghyun/ProjectRepo/UnityProj/UPlayground_Unity/Tools/LayaWorker/results/server-readiness/2026-11-08-mac-01/server.pid
실행 결과: /Users/jonghyun/ProjectRepo/UnityProj/UPlayground_Unity/Tools/LayaWorker/results/server-readiness/2026-11-08-mac-01

TRAINING_GROUND_INTEGRATION_DESIGN.md는 현재 체크아웃 검색에서 발견되지 않았다. README·두 MAC_WORK_ORDER·Python 구현·기존 Mac 결과를 조사했다. 커밋·push·Unity 프로젝트 변경 없음. 필드·타입·오류 의미·v1 경로 변경 없음. 기존 사용자 변경 보존. Windows 측은 SSH 터널과 실제 Unity 관측 전송, 오래된 state_seq 폐기, 503 동일 요청 재시도, 실행 직전 BT/Ability 검사를 추가 확인해야 한다.
