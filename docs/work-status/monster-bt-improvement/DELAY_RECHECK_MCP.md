# 지연 행동 개선 MCP 전투 관찰

2026-11-14. 사용자 요청으로 단일 Editor의 LakeVillageWorld에서 실행했다. [구현](DELAY_RECHECK.md) · [전투 위키](../../wiki/combat.md).

## 판정

**부분 확인. 전체 체감 합격은 보류한다.** 정상 사격과 근접 교전이 유지되며 임시 엄폐 뒤 공격 정지는 관찰했다. 일반 등급 궁수에는 지연 사격이 요구하는 Signature가 없어 D02 및 지연 사격 자체의 D03을 재현하지 못했다. Console Assert도 남아 있어 오류 없는 전투로 판정하지 않는다.

| 항목 | 실제 실행·관찰 | 판정 |
|---|---|---|
| D03 정상 교전 | 정상 소환한 Enemy_Random_F_Bow_001, 원본 BT, 약 7m에서 20초. 168개 표본 중 Attack 74, Circle 48, Flank 17, JumpBack 17, Dash 6, Idle 5, Chase 1 | 일반 공격 유지 확인. 표본은 공격 횟수가 아니다. 지연 사격 성공은 미확인 |
| 회피 입력 | 가상 게임패드 양쪽 Shoulder 조합 3회, 14초 기록. 실제 Player Dodge와 frequent=true 관찰 | 실제 입력 경로 반응 확인. 지연 분기 실행은 기록에 없음 |
| D01 엄폐 | Default 레이어의 임시 12×4×0.5m Cube를 두 액터 사이에 생성. 최초 사선 false 확인 후 12초 관찰 | Attack 표본 0, Circle 20→Idle 2→Patrol 73. 엄폐 동안 새 공격 없는 것은 확인. **Wait 도중 엄폐 재현은 미확인** |
| D02 타깃 상실 | 원본 BaitFrequentDodge 분기를 직접 구동하려 했으나 Can Activate Skill [Signature]가 시작부터 Failure | 미수행. Wait에 진입하지 않았으므로 중간 취소 검증으로 인정하지 않음 |
| D04 근접 회귀 | 정상 소환한 Enemy_Random_M_GreatSword_001, 원본 BT, 약 4m에서 15초 | Attack 31/Circle 68/Flank 12/Chase 6/Dash 2 표본. 접근·공격 유지 확인. 좁은 문은 미수행 |

MCP로 찍은 실제 [궁수 화면](mcp/delay-combat.png) · [근접 화면](mcp/delay-melee.png). 플레이어 체력/사망으로 관찰이 끊기지 않도록 기존 `PlayerActor.IsCombatTestInvincible`만 일시 사용했다. 따라서 피격 경직·생존 난이도·맞는 손맛은 평가하지 않았다. 원본 SO·BT 수치·게임 코드는 수정하지 않았다.

## 원본 근거

- [20초 자연 교전](mcp/D03-natural.txt), [회피 입력](mcp/D03-dodge-input.txt), [엄폐](mcp/D01-cover.txt), [근접](mcp/D04-melee.txt).
- [Signature 거리별 실행 가능 검사](mcp/D02-signature-unavailable.txt): 0~12m 모두 false. [시작 실패 기록](mcp/D02-controlled.txt)은 실패한 통제 시도이며 통과 증거가 아니다.
- `GA_Humanoid_BowAttackData_08_Skill_3.asset`은 aiRoles=16(Signature), GUID `12a2675485e9ef84a8c35f05fea60ba7`. `AbilitySet_Bow_Normal.asset`이 이를 operation=1(Remove)로 제외한다. BT에 규칙이 존재하는 것만으로 일반 궁수가 그 행동을 실행한다고 볼 수 없다. 일반 등급에서도 지연 공격을 허용할지, 상위 등급 전용으로 유지할지 후속 데이터 설계에서 정해야 한다.
- [Console 원본](mcp/DELAY_CONSOLE.json): `Assertion failed on expression: 'dt > 0'` 다수. 상세 조회에서도 stackTrace=null로 발생 소유자를 특정하지 못했다. 전투 가이드 팝업 중 timeScale=0을 관찰했으나 이것이 Assert 원인이라는 인과는 미확인이다.
- 검사용 C# 실행 요청 중 읽기 전용 DebugMode 대입과 없는 GetString 조회가 컴파일 단계에서 거절됐다. 수정된 관찰 코드로 재실행했다. 이 실패는 제품 소스 컴파일 실패가 아니며 제품 동작 근거에서 제외한다.

## 관찰 의견과 후속

거리 유지·측면 이동·사격이 섞였고 대검도 접근 후 공격을 이어갔다. 엄폐 중 정면 사격이 멈추는 방향은 의도와 맞는다. 다만 궁수는 타깃 상실 후 순찰로 돌아갔으므로 엄폐를 적극 우회하는 지능까지 확인한 것은 아니다. 근접 플레이어를 실제로 공격하지 않았고 테스트 무적을 사용했으므로 공략 기회·리스크/리워드·전투 난이도의 최종 체감 판정은 남는다.

다음 확인은 Signature를 실제 보유한 등급/액터를 먼저 확인하고 원본 BT의 Wait 진입 후 엄폐·거리 이탈을 각각 유도하는 것이다. 일반 궁수에 스킬을 임의 추가해 통과시키지 않았다. D04 좁은 문과 Console Assert 원인 조사도 별도 미완료로 남긴다.

## 종료·보존

검사용 궁수·대검·임시 엄폐와 가상 게임패드를 제거했다. 플레이어를 시작 좌표 `(1118.37,51.81,391.17)`로 복원하고 테스트 무적 false로 복구했다. 최종 조회에서 테스트 액터 0, 입력 장치 없음, timeScale=1, sceneDirty=false. Play는 유지했다. 씬·SO·저장 데이터 저장, 빌드·커밋은 실행하지 않았다. 이번 원본 로그·화면은 검증 근거로 보존한다.
