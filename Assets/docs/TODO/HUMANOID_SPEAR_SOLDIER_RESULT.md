# 신규 일반 창병 제작 기록

기존 Reien을 복구하는 대신 신규 일반 창병을 제작하라는 사용자 결정에 따른다. 기존 캐릭터·스토리·씬 배치는 바꾸지 않는다. 생성·컴파일·검사 결과는 아래 검증 절에 실제 결과를 기록하며, 체감 품질은 사용자 테스트 대기다.

## 소비 경로

- 안정 ID `Enemy_Field_Spear_001`, 표시명 `창병`, Normal Lv3.
- Definition `Assets/10.Datas/Actor/HumanoidSpear/Enemy_Field_Spear_001.asset`.
- 독립 Profile·Behavior·Attributes·Poise·BreakGauge·Codex는 같은 폴더에 둔다.
- 프리팹 `Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Field_Spear_001.prefab`.
- Definition/Profile → `Assets/10.Datas/Ability/Actor/GradeVariation/Spear/AbilitySet_Spear_Normal.asset` → 독립 Ability/Payload → `Humanoid.Spear.<공격 ID>` → `FieldSpearAnimationSet.asset` → 독립 Motion/클립 → 실제 3본 창 `MainWeapon` 판정.
- Set은 기존 Speat Set을 baseSet으로 유지하며 기존 AI 공격을 Remove하고 새 8개를 Add한다. 기본 요청 라우터는 상속한다. 레벨별 Set·미사용 Elite/Boss Set은 만들지 않는다.
- 원형은 일반 갑옷 병사 `Enemy_Random_M_GreatSword_001`이다. 외형만 분리 복제하며 기존 대검 소비자는 바꾸지 않는다. 기존 지상 근접 역할·리듬 BT를 사용하고 Behavior 거리만 독립시킨다. 원본 BT·공유 Profile·P09 생성 데이터는 수정하지 않는다.
- ActorDatabase와 MonsterCodexDatabase에 추가한다. 월드 스폰·진행 배치에는 넣지 않는다. 시험장 일반 적 Definition 슬롯에서 직접 지정한다.

## 모델·클립 근거

Frank Slash Pack의 `Meshes/Frank_RPG_Spear_Unity.FBX`에 포함된 실제 `Weapon_Spear` 스킨 메시와 `R_Hand_Weapon/Spear_Root`, `Spear_Bone01`, `Spear_Bone02`를 사용한다. 단일 창 소품을 손에 임의 회전시켜 붙이지 않는다. 원본/대상 휴머노이드를 같은 근육 자세로 맞춰 손 좌표계를 구하고, humanScale 비율로 추가 본 첫 노드만 환산한다.

정식 휴머노이드 클립의 Animator 바디·루트 곡선은 그대로 두고 원본 FBX preview 서브클립의 창 추가 본 곡선만 결합한다. 결과는 독립 `.anim`이며 런타임에 preview 서브클립을 참조하지 않는다. 재생 속도를 늦추지 않는다. 지상 적 상태 클립에도 추가 본 곡선 또는 정지 자세를 넣어 공격 중단 뒤 무기 변형이 남지 않게 한다. 기존 P09 머리 추종 Constraint는 유지한다.

지상 상태와 8개 공격을 합쳐 독립 클립 49개·Motion 48개를 사용한다. 같은 원본 클립/타임라인은 메모리 캐시로 공유하고 미사용 낚시·채집·글라이더 상태는 복제하지 않았다. 실제 스킨 리그에 없는 `R_Slash_FX_Bone` 클립 경로는 제외한다. 오른쪽 회피의 정식 클립은 .5초인데 원본 preview 추가 본 곡선은 .667초까지 있어, Hermite 끝점 분할로 사용 구간을 보존하며 .5초에 자르고 ClipSettings도 원본으로 유지한다. 이 두 문제는 신규 경로/바디 곡선 검사에서 발견해 교정했다. 기존 공유 클립은 바꾸지 않았다.

각 스킨 본의 주요 가중치 정점들을 bindpose로 환산한 경계에 BoxCollider를 둔다. 손잡이·중간 관절·창날의 3개 판정이 같은 MainWeapon 그룹을 공유한다. 원본 메시의 각 부위 범위는 대략 0.98m/0.24m/0.84m이며 크기를 키워 빗나감을 덮지 않는다.

원본/대상 각각 21개 지상 Spear 모션을 60Hz, 1,831표본으로 비교했다. 선택 공격에서 원본이 손을 붙이는 구간의 추가 접촉 오차는 오른손 약 0.5~2.5cm, 왼손 약 1.7~5.3cm다. 원본 자체의 손 놓기 구간은 그립 유지 통과로 계산하지 않았다. 스킨 삼각형 표면과 손가락 관절의 거리를 비교했으며 Constraint 추종 오차로 대신하지 않았다. 렌더 표본과 CSV는 `Logs/HumanoidSpear`에 둔다. 전체 동작의 자연스러움·옷 관통·중단 복구의 화면 품질은 사용자 검수 대기다.

## 공격 제작표

원본은 모두 `Humanoid/Spear/Humanoid_Spear_<모션>.asset`의 실제 Frank Spear 클립이다. 기존 Speat의 잘못 연결된 Katana/DualBlade MotionKey를 사용하지 않는다. 같은 동작의 피해판을 중복 후보에 넣지 않는다. 모든 피해는 투사체 없이 부착 창 판정이다.

| ID / 원본 모션 | 해금 / 역할 | Phase 피해 / 합계 | 판정 초 | 전체 / 마지막 판정 뒤 초 | 쿨다운 그룹 / 초 | 시작 거리 m |
|---|---|---|---|---|---|---|
| CrossCut / Attack_1 | Lv1 Opener Basic | 4+4 / 8 | .23–.31, .48–.57 | 1.167 / .597 | Basic / .8 | .8–2.3 |
| ShortThrust / Attack_3 | Lv1 Finisher Basic | 10 / 10 | .17–.33 | .917 / .587 | Basic / 1.2 | .75–2 |
| AdvancingCuts / HeavyAttack_2 | Lv1 Punish Heavy | 4+5+6 / 15 | .53–.61, .73–.81, .95–1.07 | 1.833 / .763 | Heavy / 6 | 1–2.7 |
| CloseSweep / Attack_9 | Lv1 Counter Skill | 10 / 10 | .26–.38 | 1 / .62 | Heavy / 5 | .6–1.5 |
| LungingThrust / Skill_1 | Lv1 GapCloser Skill | 14 / 14 | 1–1.18 | 2 / .82 | Skill / 8 | 1.8–4 |
| RisingCuts / Attack_2 | Lv10 Opener Basic | 4+5 / 9 | .17–.25, .55–.64 | 1.4 / .76 | Basic / .8 | .8–2.4 |
| ExtendedThrust / Attack_5 | Lv20 Finisher Basic | 12 / 12 | .26–.39 | 1.217 / .827 | Basic / 1.2 | 1–2.8 |
| OverheadThrust / HeavyAttack_8 | Lv30 Punish Heavy | 20 / 20 | .49–.61 | 1.25 / .64 | Heavy / 6 | 1.1–2.8 |

쿨다운 그룹 접두사는 `Humanoid.Spear.`다. 추가 Punish가 기본 Punish의 6초 쿨다운을 우회하지 않는다. 근접 Counter도 Heavy 그룹을 소비한다. 방어 중첩으로 강공격 연타를 허용하지 않는 의도이며 실제 압박·공백은 사용자 검수한다. Lv1/9=5개, Lv10/19=6개, Lv20/29=7개, Lv30/40/50=8개다. Normal Lv50에도 Signature는 없다.

강공격 첫 판정은 .53/.49초, GapCloser는 1초다. 기존 공격 상태의 조준 종료는 첫 판정 .15초 전이므로 각각 .38/.34/.85초다. 별도 워프 없이 원본 루트 이동을 사용한다. Heavy2 순 전진 약 1.71m, Heavy8 .76m, Skill1 1.46m이며 판정 시점의 최대 전진과 순 전진을 혼동하지 않는다. 강타 예비동작을 보고 옆으로 피한 뒤 마지막 회수 동작에 반격한다. 다단 Punish는 첫 베기 뒤 남은 두 타까지 확인한다. 위험 링을 첫 판정보다 과도하게 늘리지 않는다.

판정 창은 창끝의 실제 경로와 속도를 조사해 선정했다. ExtendedThrust의 뒤쪽 회수 베기에는 추가 피해를 넣지 않았다. 표의 총피해는 모든 Phase가 한 번씩 적중하는 경우의 예산이며 자동 표본의 실측 적중 수나 DPS와 다르다.

## 실제 스탯을 반영한 피해 비교

`MonsterStatCalculator.CalculateAtLevel`과 `DamageExecution.Calculate`를 Unity에서 호출했다. 이 액터는 Normal이며 별도 등급 피해 배율을 중복 적용하지 않는다. 기본 Lv3 AttackPower=1.06, Lv10=1.27, Lv20=1.57, Lv30=1.87, Lv50=2.47이다. 방어율 20%, 난이도·피격·속성·치명 배율 1, 난수 편차를 적용하지 않은 전 Phase 합계 비교다.

| 공격 | 기본 Lv3 최종 피해 | Lv30 최종 피해 |
|---|---:|---:|
| CrossCut | 6.784 | 11.968 |
| ShortThrust / CloseSweep | 각 8.480 | 각 14.960 |
| AdvancingCuts | 12.720 | 22.440 |
| LungingThrust | 11.872 | 20.944 |
| RisingCuts | 잠김, Lv10 해금 시 9.144 | 13.464 |
| ExtendedThrust | 잠김, Lv20 해금 시 15.072 | 17.952 |
| OverheadThrust | 잠김 | 29.920 |

전체 비교 CSV는 `Logs/HumanoidSpear/final-damage.csv`다. 실제 `EnemyCombat`은 Phase 피해에 ±20% 난수 편차를 적용해 HitRequest에 전달하고 피격자 전투 경계가 이후 AttackPower·방어 등을 계산한다. 시험용 수신 대상의 `play-hits.csv`는 이 **스탯 계산 전 입력 피해**를 기록하며 플레이어 HP 감소 실측값이 아니다. 실제 플레이어 장비·속성·회피·방어를 반영한 손실 HP는 사용자 검수에서 기록한다. 미사용 Elite/Boss 창병과 등급 Set은 제작하지 않았다.

## 검증과 보존

최종 Unity 스크립트 컴파일 실패 상태는 해제됐다. 보조 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -p:BuildProjectReferences=false`도 오류 0·기존 obsolete 경고 3개로 통과했다(`editor-compile.final.log`). Player/Addressables 빌드는 수행하지 않았다. 별도 주민 대표 배치 검사의 Console 예외는 이번 창병 검사 결과에 합치지 않았으며 해당 작업을 임의 수정하지 않았다.

변경 전 관련 EditMode 27/27 통과(`f25d5524f1c74248b86ea13d1e59057b`). 신규 창병 EditMode 13/13 통과(`b22e901f194740fbb39af68e233b8d4b`). 실제 Definition/Profile/프리팹/Normal Set 연결, 1·9/10·19/20·29/30·40·50 해금, 다섯 필수 역할·Signature 격리, 8개 고유 모션·Phase·부착 판정, 공용 Heavy 쿨다운, 49개 클립의 원본 바디 곡선·길이·추가 본 경로를 검사했다.

기존 수직 슬라이스 4개+등급 프리팹/혼성 슬롯 2개+신규 창병 1개의 Editor Play Mode 7/7 통과(`b775bde5ed3c4fb08a0e0a7e777f1dfc`). 신규 시험은 1.6m 높이/반경 .35m의 정지 캡슐을 1.4m, 돌진은 2.8m 앞에 두고 8개 공격 전체 타임라인을 재생했다. 모든 Phase가 순서대로 한 번씩 적중했고 동일 Phase 중복 적중은 없었다. Lv30 Punish의 Idle·Hit·Stun·Death 상태 주입 이탈에서 실행 핸들·Collision·워프 잔류가 없었다. 실제 피격·브레이크 판정과 조작은 사용자 검수다. 화면 밖 10,000m 격리 시험이므로 fixture만 Animator AlwaysAnimate를 사용한다. 기본 컬링 상태에서 본이 정지해 미적중했던 최초 시험은 통과 결과에 포함하지 않는다.

최종 Ability EditMode 전수 실행은 **300/301 통과**(`1f8e6a848d774b5ab539b0db6839382d`). 실패 1개는 기존 `CharacterSkillProgressionServiceTests`의 `SeolA/Nenmir.Attack.TautString` 연결이다. 창병 검사와 기존 몬스터 통합 검사는 모두 통과했다. 초기에 발견한 클립 FX 경로·회피 길이 오류는 수정 후 신규 검사 13개와 전수 검사에서 통과했으며 신규 실패로 남아 있지 않다. 시작 전 전수 Validator/Ability 실행은 이 창병 단계에서 별도로 하지 않았고, 위 27개 선택 검사와 변경 전 파일 해시를 기준선으로 남겼다. 과거 전체 통과 수를 이번 실행으로 재사용하지 않는다.

공유 쿨다운·런타임 해금 검사를 보강한 마지막 Play Mode 묶음은 **6/7 통과**(`f61fa0dbffb44c7bb46d08e82f3e6ea6`). 신규 창병, 혼성 슬롯, 등급 프리팹 검사는 통과했다. 창병은 Lv9/10·19/20·29/30의 실제 후보 선택, Punish 실행 직후 다른 Lv30 Punish 선택 거절, 기본 Opener fallback, Normal Lv50 Signature 부재까지 확인했다. 실패는 변경하지 않은 기존 `Boss_Lian_간격압박_공격트리거는_상태와_Ability를_시작한다`의 한 프레임 뒤 실행 핸들 유지 조건(395행)이다. 앞선 7/7 실행에서는 통과했으나 이 실패의 원인을 확정하지 않았다. 범위 밖 보스 데이터를 고쳐 없애지 않았다.

Lian 단독 재시도(`f3347a974a754461a5edb1cd6a6cdef5`)는 테스트 0개 실행, 초기화 시간 초과였다. 당시 동시 NPC 작업의 `NpcRoutine.cs`가 존재하지 않는 KCC Capsule 속성 3개를 참조해 Play 진입이 막혔다. 이 오류는 이후 해당 작업에서 해소되어 재개 시 `scriptCompilationFailed=false`를 확인했다. 현재 월드 씬의 다른 작업 미저장 변경을 보존하며 추가 Play 재실행은 하지 않았다. **Lian 재현 검사는 미완료**다. 이 컴파일 차단을 앞선 Lian 실행 유지 실패의 원인으로 단정하지 않는다.

최종 `AbilityDataValidator.ValidateAll`은 **Error 7 / Warning 599 / Info 656**(`validator.final.txt`). 기존 자산의 Error 7·Warning 599를 유지하며 창병 신규 Error/Warning은 0이다. 새 공격 8개는 기존 창 아이콘·한국어 표시명을 연결했다. 이름 로컬라이즈 키 미등록 Info 8개는 남아 있다. 기존 Error는 플레이어 기본/Ultimate HitPhase 2개와 패시브 DB 5개이며 수정하지 않았다. P09 전체 검증은 이 창병 단계에서 미수행이다. 기존 Armor06 Bow HP bake 실패를 해결하려고 수동 스탯을 재생성하지 않았다.

혼성 계약 표본은 **대검1+신규 창병1+궁수1, 근접 슬롯1/원거리 슬롯1**이다. 실제 기본 시험장 조건인 **6명/근접2·원거리1**과 다르다. 이 검사는 슬롯 요청·거절·양보·반환만 확인한다. 자율 BT로 90초 혼성전을 플레이한 결과나 AI 정지·동시 압박·연속 히트스톱 품질 통과로 해석하지 않는다. fixture의 서비스 대체 구현에서 미등록 서비스 경고가 있으므로 전체 GameManager 부팅의 무경고 검증도 아니다.

기존 `MonsterMeleeRangeBakeTool`을 **이 신규 Definition 하나에만 읽기 전용 호출**했다. 높이1.6m/반경.5m 캡슐, 120Hz 판정 표본에서 8개 모두 Measured, Blocked 0이다. 공격 순 최대 측정 거리 2.8/2.55/3.95/2/5/3.65/3.9/3.5m로 현재 선택 최대거리보다 길다(`ranges.json`). 특정 크기 정지 캡슐 표본이므로 모든 실제 캐릭터·이동 경로의 명중 보장으로 사용하지 않는다.

생성기는 `HumanoidSpearAuthoring.Apply`이며 제작표는 같은 폴더의 `SpearRecipe.json`이다. 신규 경로가 이미 있으면 덮어쓰지 않는다. DB 및 meta를 백업하고 신규 데이터 저장 후·프리팹 저장 후·DB 공개 후의 중간 실패를 주입한다. 실패하면 이 배치의 신규 에셋만 삭제하고 DB/meta 바이트를 복구한다. 최종 공개 후 재생성은 실행하지 않는다. GUID가 생긴 결과를 삭제·교체하는 재빌드는 제공하지 않는다.

사용자 기존 변경과 Reien·Sera·Nenmir·스토리/NPC·플레이어·월드 배치·P09 수동 조율 수치는 보존 대상이다. Player/Addressables 빌드·커밋은 실행하지 않는다.

최종 참조 목록은 `Logs/HumanoidSpear/assets.final.txt`의 124개 에셋 경로/GUID다. 신규 프리팹 Missing Script 0, 독립 Motion 48개 managed reference 타입 누락 0을 확인했다. 기준 해시 14,227개 중 `Assets/10.Datas`·`Assets/03.Prefabs`의 기존 파일 변경은 ActorDatabase/MonsterCodexDatabase의 신규 참조 각 1행뿐이다. 기존 파일 meta와 공유 공격·Payload·MotionSet은 이 창병 단계에서 바꾸지 않았다. 다른 NPC·월드·문서의 동시 사용자 변경은 보존했다(`preservation.json`). 이번 임시 InitTestScene은 남지 않았고 기존 사용자 InitTestScene은 지우지 않았다. 재개 시 열린 `LakeVillageWorld`의 미저장 변경도 저장·폐기하지 않았다.

기존 ignore 규칙에 가려지는 신규 데이터·독립 클립·프리팹 및 meta에만 `.gitignore` 예외를 추가했다. 다른 Actor/Animation 폴더는 계속 제외한다. 신규 리소스와 GUID를 함께 리뷰할 수 있으며 스테이징·커밋은 하지 않았다. 49개 독립 `.anim`의 저작 파일 크기는 약 153MiB다. 런타임 메모리·프레임 프로파일링은 미수행이며 에셋 텍스트 크기를 런타임 사용량으로 해석하지 않는다.

체감 검수 절차는 `HUMANOID_GRADE_PLAYTEST.md`의 G18~G22를 따른다. 사용자 결과가 오기 전에는 전투 품질 완료로 판정하지 않는다.
