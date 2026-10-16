# 사망 렉돌 연출 도입 설계

> 작성: 2026-10-16
> 대상: Unity 6 (6000.3.21f1), URP, KCC + Animancer
> 분류: 설계서(미구현 계획). 코드·프리팹 정적 분석 기준이며 **컴파일·Play Mode 검증 전**이다.
> 관련 지침: [전투 시스템 작업 지침](../guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md), [코드 작성 지침](../guide/CODE_AUTHORING_GUIDE.md), [피격 리액션 고도화 설계](HIT_REACTION_ADVANCEMENT_DESIGN.md)

---

## 0. 결론

**적용할 수 있다.** 단, 범위는 **휴머노이드 일반 몬스터의 사망 연출**로 시작한다.

| 판정 | 내용 |
|---|---|
| 적용 | 몬스터 사망 직후의 몸 반응 (공중 처치, 강타 처치, 피니시 처치) |
| 비적용 | 피격·넉다운·에어본·Grab·SpecialBreak 피해자 모션. 계속 MotionSet이 소유한다 |
| 1차 제외 | Generic 리그 몬스터, 플레이어, 네임드 보스 |
| 선결 조건 | ① 전용 `Ragdoll` 레이어와 충돌 매트릭스 ② 액터 로컬 시간 배율 연결 처리 ③ 치명타 정보 보존 |

구조적으로 붙이기 쉬운 상태다. 사망이 종착 상태이고, 시체는 풀링 없이 `Destroy`되며, 사망 후 처리 흐름(잔존 → 디졸브)이 이미 있다. 막히는 지점은 코드 양이 아니라 **레이어 설정과 시간 배율** 두 가지다.

---

## 1. 왜 필요한가 — 플레이어 경험

지금 몬스터는 사망 모션이 **`Motion.Reaction.Death` 하나뿐**이다 (`Data/Gameplay/GameplayTags.cs:152`). 그래서 다음이 어색하다.

1. **공중 콤보로 처치하면** 지상용 사망 모션이 중력을 받으며 그대로 재생된다. 에어본 저글은 이 게임의 핵심 손맛인데, 마무리 순간이 가장 어색하다.
2. **처치 방향과 세기가 쓰러짐에 반영되지 않는다.** 마지막 일격이 Light든 Heavy든 KnockBack이든 같은 모습으로 쓰러진다.
3. **피니시 처치는 이미 우회 처리가 들어가 있다.** KCC로 시체를 수평 30만큼 밀어낸다 (`GameActor/Object/Monster/MonsterActor.cs:378`). 수직 성분을 넣으면 시체가 솟구치는 문제 때문에 수평으로만 민다. 이 코드 자체가 "처치 반동을 몸으로 보여주고 싶다"는 요구가 이미 있다는 증거다.

렉돌은 세 가지를 한 번에 해결한다. 방향별·공중용 사망 모션을 따로 만드는 것보다 비용도 적다.

### 1.1 피격 반응에 쓰지 않는 이유

에어본 저글, 넉다운 후 기상, Grab, SpecialBreak 페어 모션은 KCC 루트 위치와 MotionSet 타이밍에 의존한다. 전투 지침의 원칙도 "타이밍은 MotionSet이 결정한다"이다.

이걸 물리에 넘기면 두 가지가 깨진다.
- 같은 콤보가 매번 같은 결과로 재현되지 않는다.
- 판정은 루트 캡슐 기준인데 몸은 흩어진다. 보이는 몸과 맞는 위치가 어긋난다.

소울류·세키로도 물리는 사망에만 쓴다. 피격 시 신체 일부만 물리로 반응하는 방식(physical hit reaction)은 이 문서 범위 밖이며 별도로 설계한다.

---

## 2. 현재 구조 (검증된 사실)

경로는 `Assets/02.Scripts/` 기준이다.

### 2.1 사망 흐름

| 단계 | 담당 | 위치 |
|---|---|---|
| 사망 진입 | `MonsterActor.OnDeath` | `GameActor/Object/Monster/MonsterActor.cs:926` |
| 진입 경로 ① 일반 피격 | `ReceiveHit` 처리 중 체력 0 | `MonsterActor.cs:300` |
| 진입 경로 ② 피니시 | 수평 밀기 후 사망 | `MonsterActor.cs:378, 381` |
| 진입 경로 ③ 스페셜 브레이크 | `LastDeathWasSpecialBreak = true` | `MonsterActor.cs:418-419` |
| 진입 경로 ④ 직접 체력 설정 | `SetHealth` (치트 등, 히트 정보 없음) | `MonsterActor.cs:515` |
| 사망 상태 | `EnemyDeathState` — `Die` 모션 재생 | `GameActor/State/Enemy/EnemyDeathState.cs:65` |
| 종착 보장 | `BlocksExitTo`가 Death 외 모든 전환 차단 | `EnemyDeathState.cs:24` |
| 모션 종료 → 잔존 | `BeginDeathRemains` | `MonsterActor.cs:1087` |
| 잔존 연장 | `HoldDeathRemains` (사용처: 영입 조우) | `MonsterActor.cs:1072`, `Gameplay/Encounter/Recruitment/RecruitmentEncounterParticipant.cs:286` |
| 정리 | `PlayDissolveAndDestroy` — Motor 끄기 → 디졸브 → `Destroy` | `GameActor/Base/GameActor.cs:404` |
| KCC 캡슐 | `OnDeath` 끝에서 `SetCapsuleCollisionsActivation(false)` → 캡슐이 트리거가 됨 | `KinematicCharacterMotor.cs:592` |

**의미:** 사망은 되돌아오지 않는 종착 상태이고 시체는 파괴된다. 렉돌을 원래 상태로 되돌리는 경로가 **필요 없다.** 사망 시점에 루트 캡슐은 이미 트리거가 되므로 렉돌 몸과 물리 충돌하지 않는다.

### 2.2 판정과 히트 정보

| 항목 | 사실 | 위치 |
|---|---|---|
| 피격 판정 | 루트 캡슐 1개(Enemy 레이어) + 오버랩 쿼리. 본 단위 콜라이더 없음 | `GameActor/Combat/Detection/CombatHitDetector.cs` |
| 대상 해석 | 콜라이더 → `GetComponent` → `GetComponentInParent<IDamageable>` | `CombatHitDetector.cs:262-264` |
| `[HitBox]` 콜라이더 | 비활성 트리거. 판정 형상 정의용 | 프리팹 |
| 히트 정보 | `HitContext`에 `HitPoint`, `AttackDirection`, `ReactionType`, `KnockbackForce`, `AirborneForce` 존재 | `GameActor/Combat/Resolution/HitContext.cs:23, 40-46` |
| 처치 정보 | `CombatKillContext`에는 공격자·귀속만 있고 **방향·지점·리액션 타입이 없음** | `GameActor/Combat/Attribution/CombatContributionLedger.cs:7` |

### 2.3 시간

| 종류 | 방식 | 렉돌과의 관계 |
|---|---|---|
| 전역 히트스톱 | `GameTimeManager` 요청으로 `Time.timeScale` 조절 | PhysX도 같이 느려진다. **문제 없음** |
| 액터 로컬 시간 | `GameActor.LocalTimeScale` → Animancer 속도 + `KCCSimulator`의 액터별 dt | PhysX 강체는 **따르지 않는다** |
| 액터 로컬 슬로모 사용처 | 방어 성공 tail 슬로모 (`Manager/Handler/Combat/DefenseSuccessFeedbackHandler.cs:184`), PlayerGuard 3초 0.05배 (`GameHitStopHandler.cs:133`) | 일괄 적용 함수가 **시체에도** 배율을 건다 (`Manager/Object/GameObjectManager.cs:101`) |

### 2.4 물리 설정과 프리팹

- Fixed Timestep 0.02 (50Hz), 기본 솔버 반복 6 / 속도 반복 1.
- **레이어 충돌 매트릭스가 전부 켜져 있다.** 모든 레이어가 서로 충돌한다.
- 사용 중인 레이어는 0~16. **17~31은 비어 있다.**
- 액터 프리팹 91개에 Rigidbody·Joint가 **0개**다. 렉돌 관련 코드·플러그인(PuppetMaster 등)도 없다.
- MagicaCloth2는 **플레이어 모델 9개에만** 있다. 몬스터 프리팹에는 없다.
- 디졸브는 렌더러 머티리얼 기반이라 본 위치와 무관하다.

---

## 3. 대상 판정 — 리그 조사

몬스터 프리팹 50개의 Animator Avatar를 추적한 결과다. Humanoid는 `Animator.GetBoneTransform(HumanBodyBones)`로 본을 자동으로 찾을 수 있다. Generic은 개별 저작이 필요하다.

| 분류 | 프리팹 | 수 |
|---|---|---|
| **Humanoid — 일반 몬스터** | Skeleton_Common / Sword / Bow, Golem, Golem_Black / Inferno / Normal, Ent_V1 / V2 | 9 |
| **Humanoid — 네임드 캐릭터** | AlternateSelfArin / Raon, Hwarin(+2Handed), Lian(+Whip), Raon(+Katana), Reien_Spear, SeolA_Bow, Sera(+GreatSword), Suiha, YeonHoa(+Staff), Yura_Assasin, lili | 17 |
| 확인 필요 | Myomyo (VRM 프리팹 경유, Humanoid로 추정) | 1 |
| Generic | SpiderMinion ×3, SpiderQueen ×3, Plant ×3, ChildPlant ×3, RootPlant ×3, Griffin ×2, Lich ×2, Dryad, SkySentinel, StoneColossus | 22 |
| Avatar 없음 | Training_Dummy | 1 |

**P1 대상은 일반 몬스터 9개**다. 네임드 캐릭터는 사이클 보스 풀(Honoka/Bokusei/Hichi/Lili)과 영입 조우 대상을 포함한다. 처치 직후 대화·연출로 이어지므로 연출 정책을 따로 정한다(§11 열린 질문 1).

---

## 4. 설계 원칙

1. **렉돌은 사망 후 표현이다.** 판정·수치·타이밍 어느 것도 바꾸지 않는다. 사망 판정, 보상, 퀘스트 통지는 지금처럼 `OnDeath`가 처리한다.
2. **3계층 경계를 지킨다.**
   - 힘의 크기는 GAS가 이미 소유한 `HitContext` 값(`KnockbackForce`, `AirborneForce`, `ReactionType`)에서 파생한다. 새 수치 원본을 만들지 않고 **변환 배율만** 프로필에 둔다.
   - 지상 처치의 렉돌 전환 시점은 **Die MotionSet의 MotionEvent로 저작**한다. 타이밍은 MotionSet의 것이다.
3. **이름 분기 금지.** 어떤 몬스터가 렉돌을 쓰는지는 `ActorDefinitionSO`의 데이터로 정한다.
4. **매 프레임 비용을 만들지 않는다.** 살아 있는 동안 렉돌 부품은 꺼져 있다. 액터별 `Update`를 만들지 않고 `IManagedTick`으로 등록한다.

---

## 5. 연출 설계

### 5.1 사망 연출 정책

`DeathPresentationMode` (Data 모듈 enum)

| 값 | 동작 | 기본 대상 |
|---|---|---|
| `Animation` | 지금과 동일. Die 모션 → 잔존 → 디졸브 | Generic 몬스터, 네임드 보스, 렉돌 미구성 프리팹 |
| `Ragdoll` | 사망 즉시 렉돌 | 실험용, 특수 몬스터 |
| `AnimationThenRagdoll` | 상황에 따라 즉시 렉돌 또는 Die 모션 앞부분 후 렉돌 | **일반 휴머노이드 몬스터 (권장 기본값)** |

### 5.2 처치 상황별 전환 규칙 (`AnimationThenRagdoll`)

| 처치 상황 | 판정 기준 | 전환 | 힘 |
|---|---|---|---|
| 공중 처치 | 접지 안 됨 (`GroundingStatus.IsStableOnGround == false`) | 즉시 | 루트 속도 상속 + 치명타 힘 |
| 강타 처치 | `ReactionType` ∈ Heavy, KnockBack, Airborne, Knockdown | 즉시 | 치명타 힘 (반응 타입별 배율) |
| 일반 처치 | Light, Hit 등 | Die 모션의 `RagdollActivate` MotionEvent 시점 | 약한 힘 |
| 피니시 처치 | 경로 ② | 즉시 | 기존 수평 30 밀기를 **렉돌 힘으로 대체** |
| 스페셜 브레이크 처치 | 경로 ③ | 페어 모션 종료 후, 또는 `Animation` 유지 | 없음 |
| 히트 정보 없음 | 경로 ④ (`SetHealth`) | Die 모션 이벤트 시점 | 없음 |

Die MotionSet에 `RagdollActivate` 이벤트가 없으면 프로필의 기본 지연 시간으로 대신한다. 이벤트가 없다고 렉돌이 영구히 안 켜지는 일이 없어야 한다.

### 5.3 무게감

- **속도 상속:** 렉돌을 켜는 순간 모든 본에 KCC 루트 속도(`Motor.BaseVelocity`)를 넣는다. 공중 저글의 운동량이 그대로 이어진다. 본별 포즈 차분으로 속도를 구하는 방식은 P0 결과를 보고 필요할 때만 추가한다.
- **힘 적용 지점:** `HitPoint`에서 가장 가까운 본에 `AddForceAtPosition`. `HitPoint`가 비어 있으면 가슴 본에 준다.
- **방향:** `AttackDirection`의 수평 성분에, 반응 타입별 상향 각도를 더한다.
- **히트스톱 결합:** 치명타 힘은 히트스톱 중에 적용된다. 전역 `timeScale`이 돌아오는 순간 몸이 튕겨 나가므로 "멈췄다 터지는" 감각이 자연히 나온다. 별도 처리가 필요 없다.
- **착지 반응:** 첫 강한 충돌 1회에 충돌음과 먼지 VFX. 이후는 쿨다운으로 반복을 막는다.

### 5.4 안정화 → 잔존 → 디졸브

지금은 Die 모션 종료가 `BeginDeathRemains`를 부른다. 렉돌일 때는 이 신호를 다음으로 바꾼다.

1. 골반 속도가 임계값 아래로 일정 시간 유지되면 **안정화**로 본다.
2. 안정화되면 모든 강체를 kinematic으로 고정한다(시뮬레이션에서 빠짐).
3. 그 다음 `BeginDeathRemains`를 부른다. 이후 잔존·홀드·디졸브는 지금 흐름 그대로다.
4. 안정화 타임아웃을 넘기면 강제로 2~3을 진행한다. `EnemyDeathState`의 기존 타임아웃 안전망과 같은 취지다.

---

## 6. 런타임 구조

### 6.1 모듈 배치

| 모듈 | 추가 | 역할 |
|---|---|---|
| `UPlayGround.Data` | `DeathPresentationMode` enum | 사망 연출 정책 |
| `UPlayGround.Data` | `RagdollProfileSO` | 질량 분배, 관절 각도 템플릿, 반응 타입별 힘 배율, 안정화 임계·타임아웃, 기본 전환 지연, 동시 렉돌 상한 |
| `UPlayGround.Data` | `ActorDefinitionSO`에 정책·프로필 필드 | 몬스터 종류별 단일 소스 |
| `UPlayGround.Actor` | `ActorRagdoll` 컴포넌트 | 본 강체·콜라이더·관절 참조 보관, 전환, 안정화 감지, 시간 배율 연결 |
| `UPlayGround.Actor` | `RagdollImpulse` 값 객체 | `HitContext` → 힘 변환 결과. 순수 함수로 계산해 EditMode 테스트 가능하게 둔다 |
| `UPlayGround.Actor` | `MonsterActor`에 치명타 스냅샷 | 사망 진입 직전 `HitContext`를 보존 |
| `UPlayGround.Actor` | `EnemyDeathState` 분기 | 정책에 따라 모션 / 렉돌 / 이벤트 대기 |
| Actor (MotionEvent) | `RagdollActivate` MotionEvent | 지상 처치 전환 시점 (`add-motion-event` 스킬로 생성) |
| `UPlayGround.GameActor.Editor` | 셋업 창 + 검증기 | §8 |

인스턴스별 값(본 참조)은 프리팹의 `ActorRagdoll`이, 기획이 조정할 값은 `RagdollProfileSO`가 소유한다. 프로필은 여러 몬스터가 공유한다(예: 표준 휴머노이드, 무거운 골렘).

### 6.2 렉돌 전환 순서

1. 모션 워프 해제 (지금도 `EnemyDeathState.OnEnter`가 한다).
2. 속도 상속용 루트 속도를 읽어 둔다.
3. 애니메이션 정지: `Animator.enabled = false`. FootIK 정지.
4. KCC 정지: `Motor.enabled = false`. 지금 `PlayDissolveAndDestroy`가 하는 일을 앞당기는 것이다.
5. 콜라이더 켜기, `isKinematic = false`, 보간 켜기.
6. 상속 속도 주입 → 치명타 힘 적용.
7. `IManagedTick` 등록 (안정화 감지, 시간 배율 감시).

살아 있는 동안에는 콜라이더를 끄고 강체를 kinematic으로 둔다. 애니메이션이 움직이는 kinematic 콜라이더를 매 프레임 물리에 동기화하는 비용을 만들지 않기 위해서다.

### 6.3 액터 로컬 시간 배율 연결

방어 성공 슬로모 중에는 살아 있는 적이 모두 느려지는데, 시체만 정상 속도로 날아가면 연출이 깨진다. PhysX에는 강체별 시간 배율이 없으므로 직접 맞춘다.

액터 시간 배율이 `s`일 때 실제 시간 기준으로 보면 다음과 같다.
- 속도는 `s`배가 된다.
- 중력 가속도는 `s²`배가 된다.

따라서 배율이 `s0 → s1`로 바뀌면:
- 선속도·각속도에 `s1 / s0`를 곱한다.
- 각 강체의 `ConstantForce.force`를 `mass × g × (s1² − 1)`로 둔다. 기본 중력과 합쳐 `g × s1²`가 된다.

`LocalTimeScale`에는 변경 이벤트가 없으므로(`GameActor/Base/GameActor.cs:99`) 틱에서 이전 값과 비교해 감지한다. 전역 `timeScale`은 PhysX가 알아서 반영하므로 이 연결 처리는 로컬 배율만 다룬다.

### 6.4 루트와 몸의 분리

렉돌이 되면 골반 본은 날아가고 KCC 루트는 제자리에 남는다. 루트 위치를 쓰는 곳이 영향을 받는다.

| 소비자 | 영향 | 대응 |
|---|---|---|
| 드롭 아이템, 바이탈 오브 | `OnDeath` 시점 위치를 쓰므로 영향 없음 | 없음 |
| 락온, HP 바 | 사망 시 해제되므로 영향 없음 | 없음 |
| 영입 조우의 잔존 홀드 → 대화 연출 | 대화 카메라·배치가 루트 기준이면 시체와 어긋남 | 해당 액터는 `Animation` 정책. 또는 안정화 후 루트를 골반 바닥 투영 위치로 옮긴다(본 월드 포즈를 저장 → 루트 이동 → 복원) |

---

## 7. 반드시 처리할 충돌 지점

| # | 문제 | 근거 | 대응 |
|---|---|---|---|
| 1 | 충돌 매트릭스 전부 켜짐 | KCC가 Awake에서 매트릭스로 충돌 레이어 계산 (`KinematicCharacterMotor.cs:746-753`) | `Ragdoll` 레이어 신설 + 매트릭스 수정(§7.1). 안 하면 플레이어가 시체에 걸리거나 밀어낸다 |
| 2 | 판정이 부모로 거슬러 올라가 대상을 찾음 | `CombatHitDetector.cs:262-264`, 타깃 탐색 `PlayerTargetingController.cs:56, 125` | 렉돌 콜라이더가 대상 마스크에 들어가면 시체가 오버랩 결과 버퍼를 채운다. 모든 공격·타깃·감지 마스크에서 `Ragdoll` 제외 |
| 3 | 텔레그래프 바닥 레이캐스트가 Everything | 몬스터 프리팹 48개의 `_telegraphGroundLayers`, `EnemyCombat.cs:1876` | 장판이 시체 위에 얹힌다. 마스크에서 `Ragdoll` 제외 (검증기 항목) |
| 4 | 액터 로컬 슬로모 미반영 | §2.3 | §6.3 연결 처리 |
| 5 | 치명타 정보 없음 | §2.2 | 사망 진입 직전 `HitContext` 스냅샷 |
| 6 | 루트와 몸 분리 | §6.4 | 정책 분리 또는 루트 동기화 |
| 7 | 고속 충돌 시 관통 | 피니시 처치는 초속 30 수준 | 골반·가슴에 `ContinuousSpeculative`, `maxDepenetrationVelocity` 제한 |
| 8 | 원거리 액터 시뮬레이션 일시정지 | `AgentTickManager`가 Suspended 그룹의 틱을 멈춤 | 정지 중에도 PhysX는 돈다. 일시정지 진입 시 시체를 즉시 고정하거나 디졸브 시작 |

**영향 없음으로 확인된 것:**
- 카메라 충돌은 레이어 이름 포함 목록 방식이다 (`Data/Config/CameraConfig.cs:39`). 새 레이어는 자동으로 제외된다.
- 사망 시 루트 캡슐은 이미 트리거가 되므로 렉돌 몸과 충돌하지 않는다.
- 파티클 충돌 모듈(`collidesWith` Everything, 68건)은 시체에 튕기게 된다. 해롭지 않고, 오히려 자연스럽다.

### 7.1 레이어 설정안

`Ragdoll` 레이어를 17번에 추가한다.

| 상대 레이어 | 충돌 |
|---|---|
| Default, Ground, Water | 켬 |
| Ragdoll (시체끼리) | 켬 — 시체가 겹쳐 쌓이는 모습. 동시 상한으로 비용을 막는다 |
| Player, Enemy, Npc, HitBox, Projectile, InteractableObject, Trigger | 끔 |
| CharacterPreview, UltimateStage, UltimateActor, UltimateVFX, UI, TransparentFX, Ignore Raycast | 끔 |

KCC는 Awake에서 매트릭스를 읽으므로, 런타임에 바꾸지 말고 프로젝트 설정에서 바꾼다. 한 렉돌 안의 인접 본끼리는 관절의 `enableCollision = false`(기본값)로 충돌하지 않는다.

---

## 8. 에디터 도구

`CombatHitboxSetupWindow`/`CombatHitboxSetupProfileSO`/`CombatHitboxSetupValidator`와 같은 구성으로 만든다.

**셋업 창**
- 프리팹 여러 개를 골라 Humanoid Avatar로 본 11개를 찾는다: 골반, 허벅지·종아리 ×2, 상완·전완 ×2, 가슴, 머리 (Unity 기본 렉돌 구성과 같은 범위).
- 본 길이로 콜라이더 크기를 정한다: 팔다리는 캡슐, 골반·가슴은 박스, 머리는 구.
- 질량은 프로필의 분배 비율로, 관절은 프로필 템플릿 각도로 만든다.
- **여러 번 실행해도 중복으로 만들지 않는다.** 기존 부품을 찾아 갱신한다.
- 하나의 Undo 그룹으로 묶고, 중간에 예외가 나면 그룹 전체를 롤백한다 (CLAUDE.md "Editor 데이터 도구 안전 규칙"과 같은 기준).

**검증기**
- 렉돌 콜라이더가 전부 `Ragdoll` 레이어인가.
- 프리팹 상태에서 콜라이더가 꺼져 있고 강체가 kinematic인가.
- 관절 연결 체인이 골반에서 끊기지 않는가.
- `Ragdoll` 레이어가 공격·타깃·감지·텔레그래프 마스크에 들어 있지 않은가.
- `ActorDefinitionSO` 정책이 `Ragdoll`/`AnimationThenRagdoll`인데 프리팹에 `ActorRagdoll`이 없는 경우를 Error로 보고.

P09 빌더로 다시 생성하는 프리팹이 대상에 포함된다면, 빌더가 렉돌 부품을 지우지 않는지 확인한다. P09 빌더는 기존 에셋을 교체하는 경로가 있다.

---

## 9. 성능 예산

- 휴머노이드 렉돌 1구 = 강체 11개, 관절 10개.
- 살아 있는 동안 비용: 콜라이더 꺼짐 + kinematic → 사실상 0.
- 활성 렉돌만 비용이 든다. 안정화되면 kinematic 고정, 디졸브 시작 시 콜라이더도 끈다.
- **동시 활성 렉돌 상한**을 프로필에 둔다. 넘으면 가장 오래된 시체를 즉시 고정한다.
- 강체별 솔버 반복은 프로필에서 지정한다(전역 6/1은 관절에 부족할 수 있다).

아래 초기값은 **튜닝 출발점일 뿐 측정값이 아니다.** P0에서 Profiler로 `Physics.Simulate` 시간을 보고 정한다.

| 항목 | 초기값 |
|---|---|
| 동시 활성 렉돌 상한 | 8 |
| 안정화 판정 속도 / 유지 시간 | 0.15 m/s / 0.5초 |
| 안정화 타임아웃 | 4초 |
| 강체별 솔버 반복 (위치 / 속도) | 10 / 2 |
| 기본 전환 지연 (이벤트 없을 때) | 0.15초 |

---

## 10. 단계 계획

### P0 — 스파이크

- `MonsterActor_Skeleton_Sword` 1종에 렉돌을 수동으로 붙인다.
- `Ragdoll` 레이어와 매트릭스를 설정한다.
- `EnemyDeathState`에서 즉시 렉돌 전환만 구현한다 (루트 속도 상속 + 치명타 힘).
- **완료 기준:** §12 시나리오 중 1~6을 Play Mode에서 확인하고, 체감이 기존 Die 모션보다 낫다고 판단될 때.

### P1 — 일반 휴머노이드 적용

- `RagdollProfileSO`, `DeathPresentationMode`, `ActorDefinitionSO` 필드.
- `AnimationThenRagdoll` 규칙과 `RagdollActivate` MotionEvent.
- 안정화 → 잔존 연결, 시간 배율 연결 처리, 동시 상한.
- 셋업 창과 검증기.
- 일반 몬스터 9개에 적용.

### P2 — 확장 검토

- 네임드 캐릭터와 보스의 정책 결정 (§11).
- Generic 몬스터 개별 저작 (Spider처럼 다리가 많은 몬스터가 효과가 크다. 식물형은 제외 검토).
- 플레이어 렉돌: 파티 자동 교체·리스폰이 같은 액터를 재사용하므로 해제·포즈 복구가 필요하다. MagicaCloth2가 붙은 모델 9개의 검증 부담이 크다.
- 피격 시 부분 물리 반응: 별도 설계서로 분리.

---

## 11. 열린 질문 (결정 필요)

1. **네임드 캐릭터 사망 연출.** 사이클 보스와 영입 조우 대상은 처치가 곧 이야기 장면이다. `Animation` 유지(구도 통제)와 렉돌(일관성) 중 무엇을 기본으로 할까? 권장: `Animation` 유지.
2. **시체끼리 충돌.** 쌓이는 모습을 원하는가, 비용과 떨림을 피하려고 끌 것인가? 권장: 켬 + 동시 상한.
3. **시체 피격.** 시체를 때리면 반응하게 할 것인가? 권장: 하지 않음. 판정 마스크에서 `Ragdoll`을 빼는 것과 같은 결정이다.
4. **플레이어 렉돌 도입 여부와 시기.**

---

## 12. 검증 계획

### Play Mode 시나리오

| # | 시나리오 | 확인할 것 |
|---|---|---|
| 1 | 지상 처치, 반응 타입별 | 힘 방향·세기가 공격과 맞는가 |
| 2 | 공중 저글 처치 | 운동량이 이어지는가, 땅에 박히지 않는가 |
| 3 | 피니시 처치 | 기존 30 밀기 대비 체감, 벽 관통 없음 |
| 4 | 시체 위로 걷기·대시·회피 | 플레이어가 걸리거나 시체가 밀리지 않는가 |
| 5 | 시체 근처에서 공격·락온·투사체 | 시체가 대상이 되지 않는가 |
| 6 | 다인 전투에서 시체 다수 | 프레임 시간, 동시 상한 동작 |
| 7 | 방어 성공 슬로모 중 처치 | 시체가 살아 있는 적과 같은 속도로 느려지는가 |
| 8 | 처치 직후 텔레그래프 장판 | 장판이 시체 위에 뜨지 않는가 |
| 9 | 경사·계단·물가·낭떠러지 | 관통·폭주 없음 |
| 10 | 영입 조우 처치 → 대화 | 시체와 연출 위치가 맞는가 |
| 11 | 디졸브 (lilToon 포함) | 렉돌 포즈에서 정상 디졸브 |
| 12 | 원거리로 이동해 액터 일시정지 | 시체가 남지 않는가 |
| 13 | 사망 직후 씬 전환·세이브/로드 | 에러·잔존 없음 |

### 자동 테스트 (EditMode)

- `HitContext` → `RagdollImpulse` 변환 (반응 타입별 배율, `HitPoint` 없음 폴백).
- 시간 배율 연결 수학 (속도 `s1/s0`, 중력 보정 `s1² − 1`).
- 정책 선택 규칙 (공중/강타/일반/피니시/정보 없음 → 전환 방식).

이 셋은 순수 함수로 분리해 Unity 물리 없이 테스트한다.
