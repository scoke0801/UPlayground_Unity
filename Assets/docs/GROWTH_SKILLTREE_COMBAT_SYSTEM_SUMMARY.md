# UPlayground 성장 & 스킬트리 & 전투 시스템 구조 정리 (2026-08-27 기준)

> 이 문서는 개선안이 아니라 **현재 구현 상태를 사실대로 정리한 스냅샷**이다. 코드/문서 조사 기반이며, 조사에서 발견한 문서-코드 불일치도 그대로 명시한다.

---

## 0. 큰 그림

- 캐릭터별 **개별 레벨/EXP** → 레벨업 시 **스킬 포인트 지급** → 포인트로 **스킬트리 노드** 취득(스탯 증가/스킬 해금/패시브 부여/쿨다운 감소 등) → 캐릭터 고유 **패시브 세트** + 스킬트리로 얻은 패시브가 함께 상시 적용 → 실제 전투는 **BT(판단)/GAS(수치)/MotionSet(타이밍)** 3계층이 실행.
- **레벨업 시 자동 스탯 상승은 폐기됨.** 레벨의 유일한 효과는 (a) 스킬트리 노드의 레벨 게이트, (b) 스킬 포인트 지급. 스탯 상승은 오직 스킬트리 노드(`StatDeltaEffect`) 취득으로만 이루어진다. 수동 스탯 배분 UI는 애초에 없다.

---

## 1. 스탯(Attribute) 시스템

### 1-1. 문서-코드 불일치 (중요)
`Assets/docs/guide/STAT_SYSTEM_GUIDE.md`는 `ActorStatSO`/`ActorStatContainer`/`StatModifier`/`StatType` 구조를 설명하지만, **이 클래스들은 현재 저장소에 존재하지 않는다.** 이미 GAS(Gameplay Ability System) 스타일의 Attribute 체계로 전환 완료된 상태이며, 가이드 문서만 갱신되지 않았다.

### 1-2. 실제 구조 (Attribute/GAS 체계)
- `Assets/02.Scripts/Data/Stat/AttributeRegistrySO.cs` — attributeId·stableId·카테고리·기본값·클램프 정책 등을 담는 등록 SO. `Resources/AttributeRegistry.asset` 단일 인스턴스를 정적 클래스 `AttributeRegistry`가 로드.
- `Assets/02.Scripts/Ability/Core/AttributeSetRuntime.cs` — Attribute의 런타임 단일 권위 저장소. Base/Current, Modifier(Flat/Percent 등), Priority, Source 기반 트랜잭션 재계산. 과거 `ActorStatContainer.GetFinalStat`에 대응.
- `Assets/02.Scripts/Ability/Core/AttributeProfileSO.cs` — 캐릭터 기본 Attribute 프로필(과거 `ActorStatSO`에 대응). `PartyMemberGrowthSO.baseProfile`이 참조.
- `Data/Party/GrowthAttributeCatalog`(`PartyMemberGrowthSO.cs` 내) — 레거시 정수 인덱스 → `AttributeId` 매핑 상수(`Vital.MaxHealth`, `Combat.Defense`, `Combat.CritRate`, `Combat.AttackSpeed`, `Combat.AttackPower`, `Resource.MaxStamina`).

### 1-3. Poise(강인도)/Break 게이지
- Poise 단일 소스는 여전히 `PoiseStat` 컴포넌트(전투 가이드 기준). `MonsterBreakGauge`(`ActorDefinitionSO.breakGaugeData` → `MonsterBreakGaugeSO`)가 브레이크 게이지를 담당. 노출 중 피해배율(`damageTakenMultiplierWhileExposed`), 등급별 배율(`gradePolicy`) 등 존재.

---

## 2. 레벨/EXP 시스템

**구현 상태: 완성 (몬스터 처치 → EXP 지급 → 레벨업까지 end-to-end 동작).**

### 2-1. 데이터/런타임
- `Assets/02.Scripts/Manager/Party/PartyManager.cs`
  - `_levels: Dictionary<CharacterActorType, int>` — 캐릭터별 개별 레벨.
  - `_exp: Dictionary<...>` — 캐릭터별 개별 누적 EXP.
  - `AwardBattleExp(long amount)` — 출전(BattleOrder) 전원에게 패시브 경험치 배율(`PassiveModifierCalculator.CalculateExperience`) 적용 후 100% 동일 분배(공유 풀 아님, 각자 개별 누적).
  - `AddExp(type, amount)` — EXP 누적 + `while` 루프 다중 레벨업 지원, 레벨캡 도달 시 잉여 EXP 버림, 레벨업마다 `_skillProgression.ReconcileLevel(type)` 호출(스킬 포인트 정산), `OnLevelUp`/`OnExpChanged`/`OnPartyProgressionChanged` 이벤트 발행.
  - `SetLevelForDebug` — 디버그 전용 즉시 레벨 세팅.
- `Assets/02.Scripts/Data/Party/LevelCurveSO.cs` — 레벨→필요 EXP 곡선(공식 또는 explicitTable). 없으면 PartyManager 기본 폴백 곡선.
- `Assets/02.Scripts/Data/Party/PartyMemberGrowthSO.cs` — `characterType`, `baseProfile`(AttributeProfileSO), `levelCurve`, `initialLevel`, `levelCap`. 주석에 "능력치와 스킬 성장은 CharacterSkillTreeSO가 단독 소유"라고 명시 — 자동 레벨별 스탯 곡선(growthRules) 필드는 삭제됨.

### 2-2. EXP 획득 (몬스터 처치)
- `Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs` `GrantGuaranteedRewards()` — `_expReward`/`_runtimeExpReward`에서 exp 산출, 몬스터 코덱스 배율(`Svc.MonsterCodexReader.GetExpMultiplier`) 적용 후 `RewardData` 생성 → `Svc.Reward.TryGrant(reward, RewardGrantTarget.BattleParty)`.
- `Assets/02.Scripts/Manager/Reward/RewardManager.cs` `TryGrant` — `reward.exp > 0`이면 특정 캐릭터 지정(`RewardExperienceRecipient.Character`) 또는 기본값(`party.AwardBattleExp`)으로 분기.

### 2-3. 저장/복원
- `PartyManager` 내부에 레벨/EXP를 세이브 데이터에 담는 코드(`PartyMemberSaveEntry` 등) 존재 확인. 과거 메모의 "저장 미구현" 기록은 현재는 해소된 것으로 보임(정확한 `ISaveable` 구현 여부는 추가 확인 필요).

### 2-4. 문서 상태
- `Assets/docs/Complete/PLAYER_GROWTH_LEVELING_DESIGN.md` — 원 설계 문서. 상단에 "2026-08-16 성장 파트 대체: `../cycle/08_CHARACTER_SKILL_GROWTH_SPEC.md`가 현재 권위 문서... 자동 스탯 곡선·레벨업 풀 회복·벤치 성장 갱신·수동 성장 제외 결정은 모두 폐기"라고 명시. 단, 참조된 `Assets/docs/cycle/08_CHARACTER_SKILL_GROWTH_SPEC.md`는 **저장소에 실제로 존재하지 않음**(누락/이동). 해당 내용에 대응하는 실제 구현은 스킬트리 시스템(3, 4절)으로 확인됨.

---

## 3. 스킬/Ability(GAS) 시스템

- `Assets/02.Scripts/Data/Ability/GameplayAbilitySO.cs` — `abilityId`, `presentation`, `abilityTagIds`, `triggers`, `cancelAbilitiesWithTag`/`blockAbilitiesWithTag`, `activation`, `targeting`, `cost`, `cooldown`, `concurrency`, `taskGraph`, `variants`, `commitEffects`/`endEffects`, `persistence`, `balance`. 2026-08-08 스냅샷 기준 AbilitySet 39개 / GameplayAbility 559개 / Motion Payload 547개.
- `Assets/02.Scripts/Data/Ability/AbilitySetSO.cs` — 캐릭터/몬스터별 스킬 슬롯 바인딩. `baseSet` 상속·오버라이드(Replace/Remove), `playerSlots`(PlayerSkillSlot↔ability), `combatBindings`(콤보 슬롯), `charge`(차지 공격 스테이지), `comboRoutes`(콤보 분기). 플레이어/몬스터 동일 구조 공유.
- 데이터 체인: `CharacterModelData.abilitySet → AbilitySetSO → GameplayAbilitySO(Variant) → UPlayGroundMotionAbilityPayloadSO(motionKey+HitPhase) → ActorAnimationMotionSet.abilityMotions → MotionSetAsset`.
- `motionKey`는 Ability/Variant 식별자를 포함하지 않는 독립 문자열 키(`abilityId`에서 최상위 분류 접두사 제거). `motionKey`와 `baseInfo`(히트 페이즈 수치)는 형제 필드 — 모션 전용 Ability(히트 페이즈 없음)도 정상 동작.
- 스킬 해금: 기본 콤보 타수는 `CharacterSkillTreeSO.initiallyUnlockedLightComboCount`/`initiallyUnlockedHeavyComboCount`로 즉시 해금. 그 외는 스킬트리 노드의 `AbilityUnlockEffect`(포인트 소비)로 해금. `grantedByDefault=true` 노드는 무료 지급.
- `Assets/docs/Complete/SKILL_SYSTEM_ADVANCEMENT_SPEC.md` — **설계 단계(미구현)**. GAS V1 이후 남은 상위 이슈(권위 이중화, Task 표현력 부족, Forte/Concerto/SkillCharge 자원 모델 미완, 동시 실행 제약)를 다루는 차기 문서. 스킬트리와는 별개 레이어.

---

## 4. 스킬트리 (실존, 완성도 높음)

### 4-1. 데이터: `CharacterSkillTreeSO`
`Assets/02.Scripts/Data/Party/CharacterSkillTreeSO.cs`

- 필드: `characterType`, `initiallyUnlockedLightComboCount`/`initiallyUnlockedHeavyComboCount`, `nodes: List<SkillNodeDefinition>`.
- `SkillNodeDefinition`: `nodeId`, `displayNameKey`, `descriptionKey`, `icon`, `cost`, `maxRank`, `requiredNodeIds`(선행 노드), `requiredLevel`, `layoutPosition`(그래프 UI 좌표), `effects: List<SkillNodeEffect>`([SerializeReference] 다형 리스트).
- 노드 효과 타입(`SkillNodeEffect` 서브클래스):
  - `StatDeltaEffect` — Attribute 증가.
  - `AbilityScalarEffect` — abilityId의 Damage/BreakDamage/Cooldown/Cost 배율 조정.
  - `AbilityUnlockEffect` — 스킬/기술 해금(`grantedByDefault`로 무료 노드 지정 가능).
  - `DodgeCooldownEffect` — 회피 쿨다운 감소.
  - `PassiveGrantEffect` — `PassiveAbilitySO` 참조해 패시브 활성화(스킬트리↔패시브 연결점).
- `SkillPointRule`: `perLevel`(레벨당 지급 포인트), `TotalPointsAtLevel(level)`.
- 진행 상태: `CharacterSkillProgressState`(characterType, grantedUpToLevel, totalPoints, spentPoints, takenNodes: `List<SkillNodeRankEntry>`).

### 4-2. 런타임: `CharacterSkillProgressionService`
`Assets/02.Scripts/Data/Party/CharacterSkillProgressionService.cs` (601줄, 순수 C# 서비스, PartyManager가 소유)

- `Configure(trees, pointRule, levelProvider)` — `PartyConfigSO.characterSkillTrees`/`skillPointRule` 주입.
- `CanTakeNode` — `MaxRank`/`LevelTooLow`/`MissingPrerequisite`/`InsufficientPoints`/`MissingTree`/`MissingNode` 6가지 차단 사유 검증.
- `TryTakeNode` — 포인트 소비 + 랭크 증가 + `OnSkillProgressChanged` 이벤트.
- `TryRespec` — 전체 초기화(포인트 환불, 리스펙 기능 존재).
- `ReconcileLevel` — 레벨업 시 `SkillPointRule.TotalPointsAtLevel` 차분만큼 포인트 지급(과거 레벨 소급 정산 포함).
- `GetStatModifiers`, `GetAbilityScalar` — 취득 노드 효과를 실제 Attribute/Ability 수치에 반영하는 조회 API. EditMode 테스트 존재(`Assets/Tests/EditMode/Ability/CharacterSkillProgressionServiceTests.cs`).

### 4-3. UI: `UI_Scene_SkillTree` (실동작, 스텁 아님)
`Assets/02.Scripts/UI/Scene/Growth/UI_Scene_SkillTree.cs` (1202줄)

- 캐릭터 탭 리스트, 노드 그래프(연결선/노드 렌더링, `NodeSize=(154,176)`), 상세 패널(이름/상태/효과/프리뷰), 포인트 게이지, 취득 버튼(`TakeSelectedNode`), 리스펙 버튼(`Respec`).
- `UISvc.Party.OnSkillProgressChanged` 구독, 노드 상태별 색상(획득/사용가능/잠김/레벨부족) 시각화.
- 팝업 열림 시 게임 일시정지(`Svc.GameTime.SetPause(true)`), 게임패드 포커스 내비게이션(`UIFocusNavigation`) 등 프로덕션 수준 UX.
- 프리팹: `Assets/03.Prefabs/UI/Scene/Growth/UI_Scene_SkillTree.prefab`.

### 4-4. 데이터 에셋
`Assets/10.Datas/Party/SkillTree/CharacterSkillTree_*.asset` — 11개 존재(Hwarin, Lian, Lili, Myomyo, MyoRyeong, Raon, Reine, SeolA, Sera, YeonHoa, Yura). 캐릭터당 노드 13~14개(Raon만 14개) — 전 캐릭터 실데이터 채워짐(플레이스홀더 아님).

### 4-5. 에디터 도구
- `Assets/02.Scripts/Data/Editor/Party/CharacterSkillTreeValidator.cs` — 데이터 검증기.
- `Assets/02.Scripts/UI/Editor/UISkillTreePrefabBuilder.cs` — UI 프리팹 빌드 툴.

---

## 5. 패시브 시스템

권위 문서: `Assets/docs/Complete/PASSIVE_ABILITY_SYSTEM_SPEC.md`(818줄). 자체 체크포인트(2026-07-18): "런타임·UI·검증 기반 및 플레이어블 11종 샘플 데이터 구현 완료 / 최종 밸런스·아이콘 확정 대기".

### 5-1. 데이터
- `Assets/02.Scripts/Data/Ability/Passive/PassiveAbilitySO.cs` — `passiveId`, `presentation`, `characterSelectDescription`, `activationType`, `scope`(`PassiveScope.ActiveCharacter` 등), `stackPolicy`(`PassiveStackPolicy.Additive` 등), `modifiers: List<PassiveModifierDefinition>`, `triggeredEffects: List<GameplayEffectSO>`.
- `Assets/02.Scripts/Data/Ability/Passive/CharacterPassiveSetSO.cs` — 캐릭터당 `passives`(전체) + `characterSelectRepresentatives`(캐릭터 선택 화면 노출용, 최대 2개).
- `Assets/02.Scripts/Data/Ability/Passive/CharacterPassiveDatabaseSO.cs` — `CharacterActorType ↔ CharacterPassiveSetSO` 단일 매핑 DB. `PartyConfigSO.characterPassiveDatabase`로 연결.
- `Assets/02.Scripts/Data/Ability/Passive/PassiveModifierCalculator.cs` — 계산 로직(예: `CalculateExperience(amount, multiplier)` — 경험치 패시브 배율에 실제 사용).

### 5-2. 런타임 적용
- `Assets/02.Scripts/GameActor/Gameplay/Passive/PassiveAbilityController.cs` — 활성 캐릭터 세트 갱신, 방어 성공(퍼펙트 회피/가드/패리) 트리거 처리, `GameplayEffectSO` 적용.
- `IPassiveModifierReader` 인터페이스로 문맥형 보정(약공격/강공격/스킬 피해 구분, Break 피해, 쿨다운, 소비 아이템 회복량, 제작 재료, 장비 옵션 행운, 경험치 배율) 조회. `StatType`을 늘리지 않는 설계 결정.
- 스킬트리의 `PassiveGrantEffect`로 노드 취득 시 패시브 추가 활성화 가능 — 스킬트리↔패시브 공식 연결점.

### 5-3. 장비 → 패시브 연결 여부
- `InventoryManager.RollGrowthAttributes` 등 장비 옵션 롤링은 언급되나, 이는 **Attribute(스탯) 옵션 롤링**으로 보이며 "장비 → 패시브 어빌리티 직접 부여" 경로는 이번 조사에서 확인되지 않음. 패시브는 캐릭터 고유 소유, 장비는 Attribute 수정자 중심으로 추정(추가 확인 필요).

### 5-4. Buff/Effect 스택
- `GameplayEffectSO` + `GameplayEffectController`(퍼펙트 방어 발동 버프 등 시간제 효과, 지속시간·중첩 정책).
- `Assets/02.Scripts/Ability/Core/ActiveGameplayEffectContainer.cs`, `AbilityEffectStackRuntime.cs` — Effect 스택 런타임.

---

## 6. 파티/캐릭터별 성장 모델

- **개별 EXP + 개별 레벨** (Dictionary 기반, `_levels`/`_exp`). 전투 중에는 출전(BattleOrder) 슬롯 전원에게 **개별적으로 100% 동일 지급**(공유 풀 아님, 각자 독립 누적). 벤치(대기) 캐릭터는 EXP 미획득.
- `Assets/02.Scripts/Data/Party/PartyConfigSO.cs` `Header("Growth")`: `growthData: List<PartyMemberGrowthSO>`(캐릭터별), `characterSkillTrees: List<CharacterSkillTreeSO>`(캐릭터별), `skillPointRule: SkillPointRule`(전역 규칙).
- `maxBattleSize`(기본 4) — 출전 슬롯 상한.
- 스왑(교체) 시 성장 스탯을 즉시 주입하는 경로: `PlayerActor.ApplyCharacterStats`(코드 미직접열람, 문서/PartyManager 참조로 존재 확인).
- 선행 설계 문서: `Assets/docs/Complete/PARTY_LEVEL_POWER_DESIGN.md` — "1차 범위 Phase A~C 구현 완료" 표기.

---

## 7. 전투 시스템 (BT / GAS / MotionSet 3계층)

`Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md`, `Assets/docs/guide/COMBAT_SYSTEM_GUIDE.md` 기준.

### 7-1. 3계층 책임 경계
| 계층 | 답하는 질문 | 소유 | 위치 |
|---|---|---|---|
| BT | 지금 무엇을 할까 | 상황 판단, 행동 선택, 페이싱, 거리 관리 | `10.Datas/AI/BehaviorTree/` |
| GAS | 그 행동은 무엇인가 | 발동 조건, 비용, 쿨다운, Variant 선택, 히트 페이즈 수치 | `10.Datas/Ability/` |
| MotionSet | 그 행동은 어떻게 보이고 언제 판정되는가 | 클립 체이닝, 히트박스 개폐, VFX/SFX, 캔슬 창 | `10.Datas/Actor/Animation/ActorMotion/MotionSet/` |

- BT는 데미지/범위/경직 수치를 갖지 않는다(그건 GAS 소유). MotionSet은 조건 분기를 갖지 않는다(그건 BT/GAS 소유).
- BT 저작 포맷은 두 가지: `SourceJson/`(Rules JSON, 변환 시 스코어러 자동 부착) vs `Json/`(raw BT 노드 JSON, 스코어러 우회). `Generated/*.asset`은 직접 수정 금지.

### 7-2. 실행 아키텍처
```
Input / AI / BT
 ├── PlayerMovementController → PlayerAttackState/Guard/Dodge/Hit... → PlayerCombat
 │     (PlayerCombatAbilityDataView, AttackData 생성, 콤보/캔슬, PlayerCombatStateTracker,
 │      CombatActionRunner, CombatHitDetector)
 └── EnemyMovementController → EnemyAttackState/Guard/Hit/Death... → EnemyCombat
       (AbilitySetSO, 스킬 선택/쿨다운/타겟 캐시, 텔레그래프/Danger Ring,
        CombatActionRunner, CombatHitDetector)

MotionSetAsset → MotionEventExecutor
 (BeginCollisionEvent 판정ON, TelegraphEvent, SpawnProjectileEvent,
  ComboWindowEvent, MotionEvent_MotionWarp, TimeScaleEvent)

IDamageable.TakeDamage(AttackData)
 ├── PlayerActor.TakeDamage → DefenseResolver → DamageResolver → ReactionResolver → HP/상태/사망/피드백
 └── MonsterActor.TakeDamage → 가드 → DamageResolver → PoiseStat → MonsterBreakGauge → ReactionResolver → HP/상태/사망/드랍
```

- 피해 계산: `DamageResolver`/`DamageResult`. 방어 판정: `DefenseResolver`/`DefenseResult`. 피격 리액션: `ReactionResolver`/`ReactionDecision`. 근접 히트 탐색: `CombatHitDetector`/`MeleeHitShape`/`CombatHit`. 전투 피드백: `CombatFeedbackDispatcher`(HitStop/VitalOrb/카메라/UI 중앙 배분).

### 7-3. 가드 / 패리 / 회피
- **가드**: `PlayerCombat`이 `_guardHitCount`/`_maxGuardCount`/`_guardResetDelay`로 내구도 관리. 한계 도달 시 `PlayerGuardBreakState`. 퍼펙트 가드 성공 시 반격 입력 창(`OpenPerfectGuardCounterWindow`) → 다음 입력은 `counterAttack` 또는 강공격 폴백.
- **패리**: `DefenseResolver.ResolvePlayerDefense()`가 조건(공격 상태·`IsPossibleCollide`·`NormalAttack`·`CanParry`·비투사체)을 만족하면 패리 판정. 성공 시 반격 창, 히트 판정 OFF, `HitStopIntensity.PlayerGuard`, 카메라/VFX/바이탈오브, 공격자가 몬스터면 `OnParried()`로 스턴.
- **퍼펙트 도지**: `PlayerCombat.OpenPerfectDodgeWindow()`(`PlayerDodgeState.OnEnter`에서 호출) → 도지 중 피격 시도 시 `TryPerfectDodge()`가 바이탈오브/히트스톱/카메라 피드백 발동.

### 7-4. Poise / Break
- **Poise**: `PoiseStat`(몬스터 강인도). Poise 소진 시 `IsPoiseBroken=true` → `EnemyStunState`/`EnemyKnockdownState`. `EnemyAttackState`는 진입 시 `SetHyperArmor(true)`.
- **Break Gauge**: `MonsterBreakGauge`(`ActorDefinitionSO.breakGaugeData`). 필드: `useBreakGauge`, `allowRepeatBreak`, `maxGauge`, `breakResist`, `exposedDuration`, `damageTakenMultiplierWhileExposed`, `resetGaugeRatioOnExpire`, `resetGaugeRatioOnSpecialAttack`, `gradePolicy`(등급별 배율). 게이지 0 → `ForceExpose()` → `MonsterActor.ExposedMonsters` 등록 → 강공 입력 시 `PlayerSpecialBreakAttackState`로 라우팅, 피해는 일반 무적/가드/피격 흐름 우회.

### 7-5. MotionEvent 연동
| 이벤트 | 역할 |
|---|---|
| `BeginCollisionEvent` | 히트 타겟 초기화, hitPhaseIndex 설정, 판정 ON |
| `DisableCollisionEvent` | 판정 OFF |
| `ComboWindowEvent` | 플레이어 콤보 입력 창 |
| `TelegraphEvent` | 몬스터 텔레그래프/Danger Ring |
| `SpawnProjectileEvent` | 투사체 생성 |
| `SpawnSkillEvent` | 소환/스킬 프리팹 생성 |
| `FinishAttackEvent` | 피니시 처형 타격 |
| `SpecialBreakAttackEvent` | 브레이크 특수공격 타이밍 |
| `InvincibilityEvent` | 무적 구간 |
| `MotionEvent_MotionWarp` | 접근/회전 보정 |
| `TimeScaleEvent` | 구간 기반 슬로우/히트스톱 |

### 7-6. 피드백 시스템
- `GameCombatManager.GameHitStop` — 전역 timeScale + 액터별 Animator 속도 조작. 다인 전투에서는 플레이어 히트스톱을 매 히트 재시작하면 조작이 잠기므로 그룹 상황 확인 필요.
- 히트스톱/바이탈오브/방어성공/레벨업 피드백은 별도 매니저가 아니라 `GameCombatManager` 산하 핸들러(`Manager/Handler/Combat/`)로 구성.
- 카메라: 강타격 시 회전 기반 셰이크, 히트스톱 중 가드 로직 유지.

### 7-7. 검증 수단
- `AbilityDataValidator` 전수 검증(정합성).
- `MonsterAbilitySetIntegrationTests` — `aiSelectable` Ability의 Payload/MotionKey/HitPhase 누락을 모아서 보고(스킵 금지).
- EditMode 14개 + PlayMode 수직 슬라이스 2개(Ability).
- 알려진 예상 Warning: Dryad 공격 3개, Training Dummy 공격 1개는 대응 모션 미확정으로 "해석 불가 Key" 경고 — 콘텐츠 확정 전까지 정상.

---

## 8. 관련 문서 목록

| 문서 | 상태 | 핵심 내용 |
|---|---|---|
| `Assets/docs/guide/STAT_SYSTEM_GUIDE.md` | **구식(stale)** | `ActorStatSO` 등 현재 코드에 없는 레거시 구조 설명. Attribute/GAS 전환 미반영 |
| `Assets/docs/Complete/PLAYER_GROWTH_LEVELING_DESIGN.md` | 코드 구현 완료(EXP 루프) / 일부(자동 스탯 곡선 등) 2026-08-16 폐기 명시 | 개별 EXP 루프 원 설계. 레벨=스킬포인트 트리거로 모델 전환 |
| `Assets/docs/cycle/08_CHARACTER_SKILL_GROWTH_SPEC.md` | **참조되지만 파일 없음(누락)** | 위 문서가 "현재 권위 문서"로 지칭하나 저장소에 실존하지 않음 |
| `Assets/docs/Complete/PARTY_LEVEL_POWER_DESIGN.md` | Phase A~C 구현 완료 | PartyMemberGrowthSO/PartyPowerCalculator/캐릭터별 레벨-전투력 API |
| `Assets/docs/Complete/PASSIVE_ABILITY_SYSTEM_SPEC.md` | 런타임/UI/검증 완료, 밸런스·아이콘 미확정 | 패시브 시스템 전체 아키텍처(P-01~P-13) |
| `Assets/docs/Complete/SKILL_SYSTEM_ADVANCEMENT_SPEC.md` | **설계(미구현)** | GAS V1 이후 상위 이슈(권위 이중화, Task 표현력, Forte/Concerto 자원 모델) |
| `Assets/docs/design/PLAYER_SKILL_SYSTEM_REDESIGN_PLAN.md` | Phase 2 구현 완료 / 플레이 검증 대기 | 레거시 스킬 시스템 재설계 선행 문서 |
| `Assets/docs/Complete/PLAYER_COMBAT_SKILL_LINK_SYSTEM_DESIGN.md` | Phase 1 구현 / 검증 대기 | 전투-스킬 연결 |
| `Assets/docs/Complete/WEAPON_COMBO_ROUTE_AND_SKILL_PROMPT.md` | 설계+HUD 1차 작성 / 검증 대기 | 무기 콤보 루트, 스킬 프롬프트 UI |
| `Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md` | GAS 단일 권위 사양 | Ability 전체 구조 |
| `Assets/docs/guide/BEHAVIOR_TREE_SYSTEM_GUIDE.md` | 가이드 | BT 시스템 전반 |
| `Assets/docs/guide/COMBAT_SYSTEM_GUIDE.md` | 가이드(2026-06-03 작성, 07-18 갱신) | 전투 런타임 구조 |
| `Assets/docs/guide/MOTION_EVENT_ROLE_GUIDE.md` | 가이드 | MotionEvent 역할 경계 |
| `Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md` | 작업 지침 | BT/GAS/MotionSet 3계층 저작 규칙 |

---

## 9. UI

- `UI_Scene_SkillTree`(`Assets/02.Scripts/UI/Scene/Growth/UI_Scene_SkillTree.cs`) — 유일한 성장 관련 화면, **실동작 완성 UI**. 별도의 "스탯 배분 전용 팝업"은 존재하지 않는다(수동 스탯 배분 자체가 설계에서 제외됨).

---

## 10. 추가 확인이 필요한 부분 (본 정리에서 미확정)

- `PlayerActor.ApplyCharacterStats` 실제 코드 상세(스왑 시 성장 스탯 주입 로직).
- `InventoryManager.RollGrowthAttributes`가 패시브까지 부여하는지, 순수 Attribute 옵션 롤링인지.
- `PartyManager`의 `ISaveable` 구현 여부 및 세이브/로드 흐름 최종 상태.
- `UPlayGroundMotionAbilityPayloadSO` 계열 Payload 파일 전체 목록/구조(스킬 슬롯-모션 연결 세부).
