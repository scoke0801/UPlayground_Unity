# 프로젝트 시스템 스모크 테스트 체크리스트

> 대상: Unity 6 6000.0.60f1  
> 목적: 부팅, 씬 전환, Addressables, 세이브 변경의 PlayMode 회귀 확인

## 실행 환경 기록

- Unity 버전:
- Domain Reload:
- Enter Play Mode Options:
- Addressables Play Mode Script:
- 실행 일시:
- 실행자:

## 필수 시나리오

| 번호 | 시나리오 | 기대 결과 | 결과 | 비고 |
|---|---|---|---|---|
| 1 | Boot에서 Title 진입 | `GameBootState.Ready`, Console Error 0개, Title UI 표시 | 미실행 | |
| 2 | 새 게임으로 GamePlay 진입 | Loading → 대상 씬 → `SceneContext` 순서로 완료 | 미실행 | |
| 3 | GamePlay에서 다른 맵으로 전환 | 중복 구독·입력 중복 없이 대상 맵 진입 | 미실행 | |
| 4 | 로딩 중 중복 씬 요청 | 첫 요청만 유지되고 후속 요청은 경고 후 무시 | 미실행 | |
| 5 | 파티 캐릭터 교체 | 출전 순서와 활성 캐릭터가 일치하고 HUD 갱신 | 미실행 | |
| 6 | 저장 후 타이틀 이동 | `.sav` 생성, 기존 저장이 있으면 `.bak` 유지 | 미실행 | |
| 7 | 저장 슬롯 로드 | 저장 씬, 위치, 파티, HP, 진행 데이터 복원 | 미실행 | |
| 8 | 본 세이브 손상 후 로드 | `.bak`으로 복구되고 `UsedBackup`이 기록됨 | 미실행 | |
| 9 | 종료 후 재실행 | 저장 데이터가 동일하게 재로드됨 | 미실행 | |
| 10 | 씬 왕복 10회 | 이벤트 중복, Addressables Scene 핸들 잔존, 지속 메모리 증가 없음 | 미실행 | |

## 실패 진단 확인

- `GameManager.BootState`
- `GameManager.InitializationFailure`
- `GameManager.ManagerInitializationMilliseconds`
- `SceneManager.LoadState`
- `SceneManager.LastLoadFailure`
- `AssetManager.LogHandleStatistics()`
- `EventManager.LogEventStatistics()`
- `SaveManager.LastOperationResult`

## 자동 검증

- `dotnet build UPlayground.sln --no-restore`
- Unity Test Runner EditMode
- Console Error 0개
