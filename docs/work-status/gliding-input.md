# 공중 비행 W/S 고도 조작

- 요청: 기본 자동 이동, W 상승·S 하강, 카메라 방향의 상승·하강 제거.
- 정본: [공중 비행 입력 계약](../../Assets/docs/design/ui-player/GLIDING_INPUT_DESIGN.md).
- 조사 완료: PlayerMovementController.Gliding → PlayerGlidingMovement → PlayerSuperJumpGlidingState 경로를 확인했다. 기존 자동 순항은 있으나 S가 최저 속도로 감속했고 고도는 카메라 피치에서 읽었다. 기존 SO는 `Assets/10.Datas/Player/Traversal/PlayerGlidingSettings.asset` 하나이며 상승4·하강1.6·순항10m/s다.
- 판정: 구현 반영·정상 컴파일·관련 EditMode55/55·Unity SO 로드 검사 통과. 실제 조작감은 GI01~GI04 사용자 테스트 대기.
- 컴파일 경로: 정규 LakeVillageWorld Edit Mode, Hot Reload 세션 없음. 시그니처·SO 필드 변경과 allowlist 밖 대상이므로 정상 컴파일로 검증했다. 컴파일 시작·완료와 Domain Reload 후 MCP 복귀, Console 오류0을 확인했다. 앞선 수영 수정과 기존 아트·씬·다른 문서 변경은 보존했다.
- 구현 범위: `PlayerMovementController.Gliding`가 원본 MoveInput.y를 고도 요청으로 전달하고 `PlayerGlidingMovement`는 세로축±1의 크기에 비례해 상승/하강한다. 상태는 입력과 독립적인 `IntegrateCruiseSpeed`를 사용한다. 사용처 없는 기존 스로틀/입력 크기 속도 계산과 카메라 피치 설정3개·최저 전진 속도를 제거했다. 기존 SO의 상승4·하강1.6·순항10m/s·전환0.22초는 유지한다. 앞선 수영의 카메라 조작은 변경하지 않았다.

## 자동 검증·보존

- PlayerGlidingMovementTests·PlayerCharacterInputsTests의 Editor EditMode **55/55 통과**, 실패/건너뜀0([MCP 원본](../../output/GlidingInput/editmode.json)). 실제 상태 `UpdateVelocity` 경로에 W/S/무입력을 전달하고 카메라 하향각-30/18/70도에서 같은 자동 전진·상하 방향을 확인했다. 입력 제한·약한 아날로그 값, 가속 해제 후 순항 복귀, 프레임률별 수직 변위, 기존 접촉/선회/스태미나/충돌 검사도 포함한다. 실제 사용자 입력이나 Play Mode 실행으로 표현하지 않는다.
- [Unity SO 조회](../../output/GlidingInput/settings-audit.json): 저장된 SO 로드·유효값 검사 통과, 상승4·하강1.6·순항10·수렴0.22 확인, 카메라 피치 필드3개 제거 확인. 컴파일 실패=false·진행 중=false·씬/SO dirty=false. 씬을 저장하거나 다른 SO/프리팹을 검증 과정에서 재직렬화하지 않았다. 실제 SO diff는 최저 전진속도 제거·기존 상승 기본값4의 명시뿐이다.
- [최종 준비표](../../output/harness/20261114-115314-13133612/review.md): 코드5개·실행 SO1개와 meta 해시 확인. `git diff --check` 통과. 최초 준비표는 조사 단계 기록으로 보존한다.
- `output/GlidingInput/` JSON2개27,413바이트는 검증 근거로 보존한다. 삭제할 임시 스크립트·빌드·캐시·캡처는 생성하지 않았다. 새 설계 문서와 Unity 생성 meta를 보존하고 기존 미추적 아트·씬·다른 문서 변경은 수정하지 않았다. Player/Addressables 빌드·커밋 미실행.

## 사용자 직접 확인 — 모두 사용자 테스트 대기

대상: 정규 LakeVillageWorld의 기존 비행 가능한 Raon. 원본 저장을 덮지 않는 슬롯·충분한 스태미나, 키보드/마우스 또는 실제 패드를 준비한다. 기존 슈퍼 점프로 탑승한 뒤 수행한다. 일반 낙하와 탑승 전 도약 이동의 변경 작업은 아니다.

| ID | 조작 | 기대 결과·실패 기준 |
|---|---|---|
| GI01 | 비행 탑승 뒤 이동 입력을 놓고 카메라를 위/아래/뒤로 돌림 | 현재 진행 방향으로 자동 전진하고 짧은 수렴 뒤 고도 유지. 시선만으로 상승·하강·선회하거나 전진이 멎으면 실패 |
| GI02 | W 유지→해제→S 유지→해제, W+S 동시 입력 | W 상승·S 하강, 놓거나 양쪽 입력이 상쇄되면 부드럽게 고도 유지. S가 전진을 감속하거나 카메라 방향에 따라 상하가 뒤집히면 실패 |
| GI03 | W+A/D와 S+A/D, 패드의 약한 세로/대각선 입력 | 상승/하강과 좌우 선회 동시 가능. 패드 입력 크기에 비례해 고도 조절. 수평 자동 순항 유지 |
| GI04 | 기존 가속 유지/해제·공중 재도약·놓기·천장/벽·착지 확인 | 기존 가속·복귀와 자원 소모, 재도약 제한, 정리·낙하·충돌·착지 유지. 진행 불능·천장 관통·오류면 실패 |

회신: `GI ID / 통과·실패·미수행 / 입력 장치·비행 시작 상태 / 실제 반응 / Console 오류 또는 화면`. 실제 감각과 Console 오류 유무를 구분한다.
