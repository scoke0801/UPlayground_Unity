# Raon 콤보·스킬 연계와 훈련 HUD 테스트

## 반영한 전투 방향

약공격으로 압박하고 강공격으로 마무리하거나, 고유 스킬을 끼워 넣은 뒤 다시 약·강공격으로 연결한다. Raon(Bokusei)의 파생 AbilitySet만 연결했다. 기존 약공격 10개·강공격 6개와 성장 트리의 기본 해금 타수·추가 해금은 보존했다.

| 입력 | 연계 | 사용하는 기존 공격 |
|---|---|---|
| 약 → 강 | 짧은 마무리 | Katana.Heavy.00 |
| 약 → 약 → 강 | 이어 내려베기 | Katana.Heavy.01 |
| 약 → 약 → 약 → 강 | 강한 내려베기 | 적중·해금 시 Katana.Heavy.02, 아니면 Heavy.00 |
| 고유 스킬 → 약 | 스킬 후 이어베기 | 적중·해금 시 Katana.Light.06, 아니면 Light.03 |
| 고유 스킬 → 강 | 스킬 후 강공격 | 적중·해금 시 Katana.Heavy.04, 아니면 Heavy.01 |

- 고유 스킬은 기존 Katana Ability의 Default Variant를 독립 복제한 `Player.Raon.Ability`다. 피해·투사체·쿨다운은 원본 기준을 유지했다. 여러 무조건 Variant 중 높은 우선순위를 고르는 기존 구성 대신 한 동작으로 고정했다.
- 새 독립 MotionKey `Raon.Ability`는 `Raon_Ability_Combo`를 해석한다. 기존 `Katana.Ability` 매핑은 변경하지 않았다. 현재 Raon 모델과 실행 인스턴스의 무기 서브 Animator 참조는 null임을 확인했다.
- 스킬의 마지막 기존 Collision 종료인 1.5125초부터 2.0333초까지 후속 ComboWindow를 추가했다. 원본 클립·타격·무기 보정·VFX/SFX 이벤트는 유지했다.
- 입력 간격 허용 시간은 스킬 총 길이 2.0833초에 0.3초를 더한 초기 조율값이다. 극단적인 공격속도 변화에서의 연결 감각은 직접 검증해야 한다.
- 성장 노드 ID와 구매 기록은 유지하고, 전용 스킬 해금·강화가 가리키는 Ability ID 네 곳만 새 스킬로 연결했다.
- 스킬 자체는 기존 Prepare/Commit/End 경로로 실행한다. 새 라우트의 마무리는 약·강공격이며 별도의 스킬 비용 우회 경로를 만들지 않았다.

## Katana 확장 분기

기존 5개 연계에 아래 9개를 추가했다. 기본 약·강·점프 체인의 순서와 성장 해금은 보존한다. 달려 베기만 Raon 전용 파생 모션으로 교체해 타격 뒤 후속 입력 창을 추가했다.

| 입력 | 새 연계 | 모션 근거 |
|---|---|---|
| 지상 강→약 | 엇베기 | 미사용 Katana_Combo_Attack_4_1 / Frank S2_Combo01_01 |
| 지상 강→강→약 | 돌려베기 | 미사용 Katana_Combo_Attack_4_2 / Frank S2_Combo01_02 |
| 지상 강→강→강→약 | 마무리 베기 | 미사용 Katana_Combo_Attack_4_3 / Frank S2_Combo01_03 |
| 구르기→약 | 회피 내려베기 | 미연결 Frank S1_Attack02 |
| 구르기→강 | 회피 후 강공격 | Katana_Combo_Attack_4_3 |
| 대시→약 | 대시 후 엇베기 | Katana_Combo_Attack_4_1 |
| 대시→강 | 대시 후 강공격 | Katana_Combo_Attack_4_3 |
| 공중 약→강 | 공중 전환 베기 | Bokuse_Katana_JumpAttack_4 |
| 공중 약→약→강 | 공중 연속 마무리 | Bokuse_Katana_JumpAttack_6 |

- 구르기는 Dodge, 대시는 Dash 입력이다. 구르기 완료 뒤 공격한다. 대시 후 이동 입력을 유지한 강공격도 새 라우트를 검사한다.
- 기본 공중 강공격의 내려찍기는 유지한다. 공중 대시 강공격은 기존 Dash 목록 두 번째 모션을 사용하도록 수정했다. 두 번째 항목이 없는 무기는 첫 항목을 유지한다.
- 라우트 성공 시 입력 기록을 초기화하는 기존 규칙은 유지한다. 새 분기 이후 기본 약공격 또는 스킬로 다음 압박을 시작한다.
- 원본 MotionSet은 수정하지 않았다. 새 Ability 7개, MotionSet 7개, Raon 무기 커브를 연결한 Frank 클립 4개는 `Assets/10.Datas/Ability/Player/Raon/`에 있다. MotionKey는 `Raon.ReturnCut`, `Raon.CrossCut`, `Raon.RisingCut`, `Raon.ClosingCut`, `Raon.AirTurn`, `Raon.AirFinish`, `Raon.DashEntry`다.
- 새 Frank 단타는 원본 Humanoid Avatar를 만들어 17개 포즈로 프리뷰했다. 머리 위 준비 동작 뒤 0.4~0.6초에 내려베는 구간을 첫 저작값으로 정했다. 다른 신규 모션은 기존 Collision 시간을 재사용했다. 후속 창은 마지막 Collision 종료부터 모션 끝 0.05초 전까지다.
- Frank 클립은 기존 `FrankWeaponBasis`/`FrankSheathBasis` 대상 경로를 재사용하고 원본 `__preview__`의 소품 커브를 독립 클립에 복사했다. 애니메이션이 없는 성분은 원본 리그의 기본값을 사용했다. 보조 클립을 런타임에서 참조하지 않는다. 대상 모델의 실제 그립·칼날 접촉·중단 복구는 아직 사용자 검수 대기다.
- 피해·강인도·피격 피드백은 같은 역할의 기존 Raon 카타나 공격에서 복제했다. 공중 강 분기는 기존 점프 마무리 비용을 사용한다. 기력 소비는 마지막 입력 종류가 아니라 Ability 비용을 따른다. 강→강→강→약 마무리도 강공격 비용을 내며, 최적 경로 여부는 직접 조율한다.
- 조사·프리뷰·검증 로그: `Logs/RaonComboExpansion/`. 원본 등록 조사에서 Frank 미연결 공격 파일 12개를 찾았고 이 중 S1_Attack02를 연결했다. 나머지는 모든 분절을 한 파일로 합친 All 클립과 S3 공격·콤보이며 이번 적용 대상이 아니다.

## 약·강 공격 연결 개선 — 최신 적용 기준

이 절은 아래의 이전 입력 패턴 설명보다 우선한다. 입력 이력은 연계 실행 후 비우지만 현재 실행 모션과 적중 결과는 다음 공격을 고르는 동안 유지한다. 공격 종료·피격·이동·회피로 이탈하면 이 모션 조건은 해제한다.

- 기존 Light 10 / Heavy 6 Ability와 성장 해금을 보존한다. 14개 입력 패턴에 현재 모션 기준 연결 42개를 추가했다. 이는 새 버튼 조합 42개가 아니라 각 모션 뒤의 약/강 선택과 미해금·비적중 대체 경로다.
- 약공격 1~6타에서 강공격 1~6타로 각각 전환한다. 3타 이후의 강한 분기는 직전 공격 적중과 해당 Ability 해금이 필요하다. 미충족 시 강공격 1타로 연결한다.
- 강공격 1타 → 약: 교차베기 → 약: Light 4타 → 약: 5타 → 약: 6타. 기본 1타로 되감지 않고 해당 체인 인덱스를 이어받는다.
- 강공격 2타 → 약: 올려베기 → 약: Light 7타(미해금 시 4타). 강공격 3~6타 → 약: Light 7~10타. Light 10타는 기존과 같이 첫 클립의 재사용이다.
- 회피 되받아베기 → 약: Light 5타. 대시 진입 공격 → 약: Light 4타. 기존 공중 분기는 유지한다.
- 고유 스킬 → 약: Light 4타, 적중·해금 시 7타. 고유 스킬 → 강: Heavy 2타, 적중·해금 시 5타.
- Light/Heavy MotionSet 16개를 Raon 폴더로 독립 복제해 Raon의 카타나 매핑에 연결했다. 원본 클립·MotionSet·피해·비용·HitPhase·VFX 참조는 보존했다. 후속 창 시작은 기존 저작값과 마지막 Collision 종료 중 늦은 시점이다. 창이 없던 Heavy 3~6타는 마지막 Collision 종료부터 모션 끝 0.05초 전까지 연결한다.
- 선입력은 타격 이전이나 다단 공격 중간의 Collision 공백에서 다음 동작을 재생하지 않는다. 훈련장에는 현재 모션에서 가능한 후속 입력과 용도를 먼저 표시한다.
- 시작 모션 조건과 적중 조건, 훈련 용도는 Ability Editor의 Set → 활성화 조건 → 콤보 연계에서 편집할 수 있다. 일반 PropertyField가 새 필드도 함께 노출한다.
- 조사·타이밍·데이터 검사 로그: `Logs/RaonComboDepth/`. 실제 접촉 프레임·패드 조작감·최적 피해 경로는 아래 사용자 테스트 대기다. 기존 승인된 타격 타이밍을 재사용했으며 새 시각 프리뷰로 타격을 재조정하지 않았다.

| ID | 절차 | 기대 결과 / 실패 기준 | 상태 |
|---|---|---|---|
| R19 | 강→약→약→약→약 입력 | 강1→교차베기→Light4→5→6. 도중 Light1로 돌아가거나 입력 한 번에 두 동작이 나가면 실패 | 사용자 테스트 대기 |
| R20 | 적에게 약1→강과 약2→강을 각각 사용 | 서로 다른 Heavy1/2 분기, 비용 1회 소비. 훈련 안내와 실제 모션 일치 | 사용자 테스트 대기 |
| R21 | 해금된 상태에서 약3~6→강을 적중/헛치기로 비교 | 적중은 Heavy3~6, 헛치기는 Heavy1. 미해금 시 강한 분기를 우회 획득하지 않음 | 사용자 테스트 대기 |
| R22 | 강2→약→약, 회피→약→약, 대시 공격→약 | 올려베기 뒤 Light7(미해금 시4), 되받아베기 뒤 Light5, 대시 뒤 Light4로 진입 | 사용자 테스트 대기 |
| R23 | 고유 스킬 적중/헛치기 뒤 약 또는 강 | 적중·해금 조건에 따라 Light7/Heavy5, 아니면 Light4/Heavy2. 쿨다운과 비용 중복 없음 | 사용자 테스트 대기 |
| R24 | 다단 Light4/5/7/8 및 Heavy4/5의 첫 타 전부터 다음 공격 선입력 | 마지막 타격까지 재생한 뒤 후속 공격. 중간 타격 유실·무한 공격 상태 없음 | 사용자 테스트 대기 |
| R25 | 분기 중 회피·피격·이동 이탈 후 다시 약/강 입력, 캐릭터 교체 후 복귀 | 이전 모션 전용 후속이 남지 않음. 회피의 새 입력 패턴은 정상 작동 | 사용자 테스트 대기 |
| R26 | 동일 동작에서 적중 전후, 자원 부족, 해금 전후 HUD 확인 | 현재 모션 후속이 우선 표시되고 실행 불가능한 상위 경로가 대체 경로를 가리지 않음. 패드 글리프 유지 | 사용자 테스트 대기 |
| R27 | 강공격 1→2→3타와 약→강 분기 확인 | 1타 S2_Attack01, 2타 S2_Attack02, 3타 S2_Attack03. 1타 타격·잔상·소리가 새 동작과 일치하며 후속 입력이 마지막 타격 이후 연결됨 | 사용자 테스트 대기 |

강공격 1타는 `Motion_Raon_Heavy_00_Flow`의 GUID를 유지한 채 `Katana_Heavy_Attack_1_PlayerGrounded`의 S2_Attack01 클립과 저작 타임라인을 독립 복제해 연결했다. 기존 MotionKey·Ability·비용·피해는 유지한다. 타격 0.3888~0.8507초, 후속 창 0.8507~1.2037초, 전체 1.3333초. 새 시각 프리뷰로 재조정한 값이 아니라 기존 지상 모션의 타이밍을 재사용한 값이며 R27은 사용자 확인 대기다.

## 테스트 준비

- Unity 6000.3.21f1 이상, `Assets/01.Scenes/Test/CombatTestMap.unity`의 기존 훈련장 진입 경로를 사용한다.
- Raon을 활성 캐릭터로 선택하고 카타나를 장착한다. 키보드·마우스와 게임패드를 각각 준비한다. 실제 키는 재바인딩에 따라 HUD의 입력 글리프를 따른다.
- 기존 저장을 초기화하지 않는다. 일반 분기는 현재 해금 상태로 검사하고, 강공격 마무리는 기존 성장 노드 해금 전후를 구분해 기록한다.
- 상대 정지로 연결을 먼저 확인한 뒤, 무적·정지를 끄고 일반 적·강인도 높은 적·소규모 무리에서 확인한다.
- 고정 프리팹: `Assets/03.Prefabs/UI/HUD/UI_HUD_TrainingCombo.prefab`. 화면 왼쪽에 현재 연계, 다음 입력, 스킬 연결 상태를 표시한다. 훈련장 세션이 생성·폐기하며 입력을 차단하지 않는다.

## 직접 실행할 항목

| ID | 절차 | 기대 결과 / 실패 기준 | 상태 |
|---|---|---|---|
| R01 | 훈련장 진입, 준비 화면 닫기 | 연계 HUD 표시. 캐릭터 이동·카메라·공격 입력 유지. 준비 화면의 Cancel과 패드 포커스 유지 | 사용자 테스트 대기 |
| R02 | 약→강, 약→약→강을 각각 입력 | 서로 다른 기존 모션으로 마무리하고 해당 연계 성공 표시. 강공격 비용이 한 번만 소비됨 | 사용자 테스트 대기 |
| R03 | 강공격 마무리 해금 전후 약 3회→강 입력 | 해금 전 전용 마무리로 우회하지 않음. 가능한 짧은 분기는 실제 선택과 HUD 안내가 일치. 해금 후 마무리 사용 가능 | 사용자 테스트 대기 |
| R04 | Idle에서 고유 스킬, 타격 도중 약 선입력 | 스킬 타격을 보존하고 후속창에서 약 연계 발동. 기존 약공 체인이 없어도 연결됨 | 사용자 테스트 대기 |
| R05 | 약→약→고유 스킬→강 | 스킬 재사용 대기 적용, 후속 강공격 단일 소비, 모션·무기 부착·투사체 정상 | 사용자 테스트 대기 |
| R06 | 스킬 대기 중 재입력, 기력 부족 상태에서 강 입력 | 무료 스킬 재발동·이중 비용·유령 연계 성공 표시가 없어야 함 | 사용자 테스트 대기 |
| R07 | 공격 중 회피·가드·피격, 일정 시간 대기 후 다시 공격 | 입력이 뒤늦게 튀어나오거나 공격 상태에 고착되지 않음. 연계 기록 만료와 실제 선택이 일치 | 사용자 테스트 대기 |
| R08 | 패드/키보드 전환 및 기존 키 재바인딩 | 입력 아이콘 갱신, 진행한 키·다음 키·남은 키 구별. 글리프 누락·겹침 없음 | 사용자 테스트 대기 |
| R09 | 캐릭터 교체, 사망/재시작, 일반 필드로 이동 | 교체 대상의 라우트를 표시하거나 라우트 없으면 숨김. 필드에서는 HUD 없음. 구독·트윈 예외 없음 | 사용자 테스트 대기 |
| R10 | 히트스톱·일시정지 및 16:9/울트라와이드 확인 | 패널과 초상화·기존 HUD 겹침 없음. 안내 트윈은 비스케일 시간 사용. 작은 글자 읽기 가능 | 사용자 테스트 대기 |
| R11 | 일반 적·강인도 높은 적·다수 상대 실전 | 짧은 마무리, 긴 마무리, 스킬 연결을 상황별로 선택할 이유가 있어야 함. 무한 경직·무조건 최적 연계 여부 기록 | 사용자 테스트 대기 |
| R12 | 기존 전용 기술 강화 노드 적용 전후 비교 | 기존 구매 상태 유지, 새 스킬에 피해·쿨다운·소모량 보정 연결 | 사용자 테스트 대기 |
| R13 | 훈련장 진입 후 전투 시작, 기력 소비·완전 회복, 캐릭터 교체와 재입장 | 전투 시작 시 스태미나 HUD가 잠깐 표시되고, 이후 소비·회복 중 표시. 완전 회복 후 기존 숨김 지연이 지나면 사라짐. 이동 중 캐릭터를 따라감 | 사용자 테스트 대기 |
| R14 | 새 지상 강→약 3분기를 각각 실행 | 타수별 새 동작과 HUD 성공 표시 일치. 기본 강공격의 해금 타수를 우회하지 않음 | 사용자 테스트 대기 |
| R15 | 구르기/대시 완료 후 약·강 입력, 이동 유지한 대시→강도 실행 | 회피 무적 종료 후 공격. 판정 중복·무료 강공격 없음. 실패 시 기본 공격으로 안전 복귀 | 사용자 테스트 대기 |
| R16 | 공중 약→강, 약→약→강, 단독 강, 공중 대시→강 | 새 공중 분기·기존 내려찍기·공중 대시 모션 구분. 착지와 기력 부족에서 공중 상태 고착 없음 | 사용자 테스트 대기 |
| R17 | 새 Frank 4종을 앞·옆에서 관찰하고 타격·피격·회피·장비 교체로 중단 | 손잡이·칼날·칼집 정렬, 실제 접촉과 타격 시점 일치, 종료 뒤 무기 크기·부착 복구 | 사용자 테스트 대기 |
| R18 | 달려 베기 타격 직후 약/강, 이어서 스킬 입력 | 후속 창에서 기본 공격으로 전환. 활성 타격 도중 공격 캔슬이나 중복 입력 소비 없음 | 사용자 테스트 대기 |

회신 형식: `항목 ID / 통과·실패·미수행 / 실제 결과 / 입력 장치·해금 상태 / 필요 시 영상 또는 Console 로그`.

## 수행한 보조 검증

- 약·강 후속 연결 개선 후 Unity 컴파일 오류 0, 관련 EditMode 20개 통과(Raon 13, Set 합성 5, 데이터 뷰 2). 최종 job: `bb6a03a4d2b342c5946dc1859b06a70f`. 첫 검사에서 Light6의 직접 진입만 요구한 테스트가 실패해, 직전 타격으로 진입한 뒤 이어지는 경로도 검증하도록 수정했다. 최종 검사는 건너뛴 항목 없이 통과했다.
- `Logs/RaonComboDepth/reachability.txt`: 해금 완료·적중 조건의 데이터 경로 검사에서 Light10/Heavy6 모두 도달 가능. 실제 입력을 주입한 결과가 아니며 모션 접촉·조작감 검증과 구분한다. 시작 Motion Key 매핑 누락 0, Raon 데이터 신규 Error/Warning 0, 프로젝트 기존 오류 7개 유지(`validation.txt`).
- 이번 CLI 보조 컴파일은 샌드박스에서 Microsoft SDKs 경로 접근이 거부돼 실행되지 않았다. Unity 컴파일·EditMode 결과를 기준으로 확인했다. Player/Addressables 빌드는 실행하지 않았다.
- 약·강 후속 연결 개선 후 기존 Ability Editor PlayMode 수직 슬라이스 4개 통과. job: `1049508bfc5c4b65a9a39457b56cd4e0`. 실제 Raon 입력·타격·모션 연결의 감각 검증은 R19~R26 사용자 테스트 대기다.
- Katana 확장 후 RaonComboTrainingTests 9개 통과: 기존 5분기 유지, 지상 12/공중 2 안내, 확장 분기 선택·HitPhase·후속 창·공유 기본 체인 보존 확인. Actor CLI 보조 컴파일 오류 0(기존 어셈블리 버전 경고 26개). 새 Frank 클립의 Transform 커브 대상이 Raon 모델에 모두 존재함을 Unity에서 확인했다.
- 확장 후 전체 Ability 검사 오류는 기존과 동일한 7개이며 새 Raon 데이터의 Error/Warning은 0개다. 현지화 키 미설정 Info는 남아 있다. 결과는 `Logs/RaonComboExpansion/all-validation.txt`에 보관했다.
- 확장 후 기존 Ability PlayMode 수직 슬라이스 4개 통과, Unity 콘솔 오류 0. 이는 새 공격의 실제 입력·무기 접촉·시각 품질 검증을 대신하지 않는다. R14~R18은 사용자 테스트 대기다.
- 훈련장 재진입 후 활성 Raon의 PlayerCombat에서 14개 라우트가 실제 로드됨을 API로 확인했다.
- 스태미나 HUD 누락 원인: 최초 표시 시 카메라가 준비되지 않아 null을 계속 유지했다. 주기 갱신에서 카메라를 재조회하도록 수정했다. 수정 후 Unity 컴파일·콘솔 오류 0, 훈련장 Play Mode에서 카메라 연결·패널 활성·알파 1·상시 표시 조건을 API로 확인했다. 전투 시작 이후 실제 화면 확인은 R13에서 진행한다.
- Unity 스크립트 컴파일 오류 0 확인.
- 관련 EditMode 15개 통과: RaonComboTrainingTests 8개, AbilitySetCompositionTests 5개, PlayerCombatAbilityDataViewTests 2개.
- 기존 Editor Play Mode Ability 수직 슬라이스 4개 통과. 이는 실제 패드 조작·전투 감각의 검증을 대신하지 않는다.
- 훈련장 실행 화면에서 HUD 생성과 입력 글리프 표시를 확인하고 파티 초상화와 겹치지 않도록 왼쪽으로 조정했다. 캡처는 `Logs/RaonComboTraining/`에 있다. 입력 조작 테스트는 수행하지 않았다.
- 전체 Ability 검사 결과는 `Logs/RaonComboTraining/ability-validation.txt`에 보관했다. 변경하지 않은 기존 데이터에서 오류 7개: Default Ability/Ultimate의 HitPhase 누락 2개, CharacterPassiveDatabase의 패시브 ID 중복 4개와 Triggered Effect null 1개. 자동 Fix는 하지 않았다.
- 전체 검사 시 새 스킬에 있던 아이콘 경고는 기존 Skill_Ability.png 연결로 보완했다. 현지화 키 미설정 Info는 남긴다.
- 보완 후 Raon 세트·스킬 선택 검사 오류 0, 경고 0. HUD Missing Script 0, Graphic raycastTarget 0, HUD 레이어·훈련장 전용 조건·UI 데이터베이스 연결 확인.
- Player·Addressables 빌드는 지침에 따라 수행하지 않았다. 실제 전투 감각, 입력 타이밍, 카메라와 시각적 품질의 최종 판정은 사용자 테스트 대기다.

## ImageGen 리소스

- 기본 내장 ImageGen 사용. 최종 원본: `Assets/04.Images/UI/Training/TrainingComboPanel.png`.
- 생성 이미지의 바깥 매트 대신 내부 질감 영역을 Unity Sprite로 사용하고 기존 Celadon Focus 프레임을 조합했다. 입력 키는 기존 InputIcon 글리프를 사용한다.
- 생성 프롬프트:

> Create a production game UI sprite: one single wide rectangular training-combo information panel, front-facing orthographic flat 2D, aspect ratio 3:2, transparent outside the panel, no text no letters no icons no mockup no scene. Match an elegant Korean fantasy action RPG interface: very dark charcoal teal lacquer inset with almost flat subtle fine paper texture, slender muted antique champagne gold double-line trim, small celadon jade corner inlays, restrained clipped corners. Interior must be empty, quiet and dark for readable white text. Very thin borders, no ornate flourishes, no glow, no drop shadow. Panel fills 95 percent of canvas with small transparent margin. The border edges must be straight and uniform to permit Unity 9-slicing; decorative detail confined to corners. High quality crisp bitmap UI asset.

- 최종 편집 프롬프트:

> Keep this exact empty dark teal and antique gold game UI panel. Remove ALL background outside the outer thin gold frame and make it fully transparent alpha, including the entire wide top and bottom margins and the corner cutouts. Remove the external brown glow/shadow entirely. Preserve opaque dark inset and crisp frame inside. Do not add text or icons. Crop canvas tightly around the panel with only a tiny transparent margin.

기존 `Katana.Ability` 공유 매핑을 교체하는 시도는 자동 승인 검토가 공유 참조 영향 가능성으로 거절했다. 교체는 적용하지 않았으며, 새 Raon 키 추가만 사용했다.

## 강공격 타수 제한과 중복 모션 개선

- Raon의 기본 강공격 연속 타수를 2에서 5로 확장했다. 6타는 기존 HeavyFinisher 해금을 유지한다. 기존 저장 데이터에도 기본 타수 설정이 적용되며, 성장 초기화 후에도 5타를 유지한다.
- 약공격 10타는 1타 반복 대신 기존 S2_Attack03 마무리 타임라인을 사용한다.
- 일반 교체 진입은 Dash_Attack_ver_B, 그로기 진입은 Skill_H, 공중 대상 진입은 RisingCut, 회피 교체는 ReturnCut으로 분리했다.
- 교체 특수공격은 Damage_Critical_Katana의 기존 4타 타임라인과 Raon 무기 보정 클립을 연결했다. 이전 약공격 1타 매핑에서 누락됐던 HitPhase 1~3도 실행한다. 실제 총 피해가 기존 잘못된 매핑보다 증가하므로 플레이 테스트에서 확인한다.
- 교체 공격 5종은 마지막 타격 뒤 후속 창을 열고 약공격 4타 또는 강공격 1타로 이어진다. 공유 원본 MotionSet은 변경하지 않았다.

| 항목 | 확인 방법 | 상태 |
|---|---|---|
| R28 | 전체 해금 옵션을 끄고 강공격 연타: 1~5타의 서로 다른 모션 확인. HeavyFinisher 해금 후 6타 확인 | 사용자 테스트 대기 |
| R29 | 약공격 10타의 마무리 모션 및 타격·무기 궤적 확인 | 사용자 테스트 대기 |
| R30 | 일반/그로기/공중/회피 교체 동작 구분, 마지막 타격 후 약·강 후속 입력 확인 | 사용자 테스트 대기 |
| R31 | 교체 특수공격 4회 타격과 2.30초 이후 후속 입력, 회피 중단 및 무기 복구 확인 | 사용자 테스트 대기 |

검증: Unity 컴파일 오류 0, 관련 EditMode 30/30 통과(job b392c5bf354041e3a24e99401b7f124b), Ability 수직 슬라이스 PlayMode 4/4 통과(job cc247b41be6640c19dfc026116b51761). 실제 입력 연타·무기 궤적·전투 감각은 미검증이며 위 R28~R31로 확인한다. Player 빌드는 수행하지 않았다.

## 콤보 HUD 가독성 확인

준비: Editor CombatTestMap 훈련장에서 Raon을 선택한다. 키보드·마우스와 게임패드를 각각 사용하고, 기존 저장 데이터를 그대로 쓴다.

- R32: 공격 전 HUD 크기와 글자·입력 아이콘이 읽히는지 확인한다. 다섯 줄을 초과하면 숨겨진 개수가 안내되어야 한다. 화면 밖으로 나가거나 다른 HUD와 겹쳐 읽지 못하면 실패.
- R33: 약·강 공격과 스킬을 사용한다. 현재 공격에서 이어지는 행에 ‘다음’이 보이고, 가장 밝은 아이콘이 다음 입력인지 확인한다. 글자가 잘리거나 입력과 안내가 일치하지 않으면 실패.
- R34: 게임패드와 키보드를 전환해 입력 표기가 갱신되는지 확인하고 일시정지와 훈련장 종료 시 HUD의 동작을 확인한다. 훈련장 밖에 남거나 전환되지 않으면 실패.
- 결과: 항목 ID / 통과·실패·미수행 / 해상도·입력 장치 / 실제 증상. 실패 시 화면과 Console 오류를 함께 전달한다. 상태: 사용자 테스트 대기.
