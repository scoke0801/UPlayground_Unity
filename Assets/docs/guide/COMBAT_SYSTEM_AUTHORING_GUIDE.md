# 전투 시스템 작업 지침 (BT · GAS · MotionSet)

> 대상: 적 AI(BT), Ability 데이터(GAS), 모션·히트 타이밍(MotionSet)을 만들거나 고치는 모든 작성자(사람과 AI)<br>
> 이 문서는 **어떻게 작업하는가**를 정한다. 각 시스템의 상세 사양은 9절의 권위 문서를 따른다.

---

## 1. 역할과 목표

이 문서를 들고 작업하는 사람은 **AAA 액션 게임의 전투 디자이너**다. 요청받은 공격 하나를 동작시키는 것이 아니라, **손맛이 있고 읽히는 전투**를 만드는 것이 목표다.

전투의 품질은 다음 세 가지로 판정한다.

1. **읽힌다.** 플레이어가 적의 다음 행동을 예측할 수 있다. 죽었을 때 "내가 뭘 잘못했는지" 안다.
2. **반응한다.** 입력한 순간 캐릭터가 반응하고, 때린 순간 맞았다는 것이 몸으로 느껴진다.
3. **선택이 있다.** 같은 상황에 여러 대응이 있고, 더 나은 대응이 더 좋은 결과를 준다.

수치를 채우는 것은 작업이 아니라 작업의 일부다. **텔레그래프·히트스톱·캔슬 창·카메라 반응까지가 전투 데이터의 범위다.**

---

## 2. 3계층 책임 경계 — 가장 중요한 규칙

전투는 세 시스템이 나눠 갖는다. **경계를 넘으면 반드시 나중에 무너진다.**

| 계층 | 답하는 질문 | 소유 | 위치 |
|---|---|---|---|
| **BT** | 지금 **무엇을 할까** | 상황 판단, 행동 선택, 페이싱, 거리 관리 | `10.Datas/AI/BehaviorTree/` |
| **GAS** | 그 행동은 **무엇인가** | 발동 조건, 비용, 쿨다운, Variant 선택, 히트 페이즈 수치 | `10.Datas/Ability/` |
| **MotionSet** | 그 행동은 **어떻게 보이고 언제 판정되는가** | 클립 체이닝, 히트박스 개폐 타이밍, VFX/SFX, 캔슬 창 | `10.Datas/Actor/Animation/ActorMotion/MotionSet/` |

### 2.1 각 계층에서 하지 말아야 할 것

**BT에서 하지 않는다**
- 데미지·범위·경직 수치를 BT 노드에 넣지 않는다. 그것은 Ability의 것이다.
- 모션 클립을 BT에서 직접 재생하지 않는다. Ability를 활성화하면 모션은 따라온다.
- 특정 몬스터 전용 하드코딩 분기를 노드로 만들지 않는다. 세 번째 반복되면 데이터화한다.

**GAS에서 하지 않는다**
- "언제 쓸지"를 Ability에 넣지 않는다. `aiSelectable`로 후보임을 표시할 뿐, 선택은 BT가 한다.
- Payload 바깥에 모션 참조를 중복으로 두지 않는다. 모션의 단일 소스는 `AbilityAttackInfo.motionKey`다.
- 제거된 레거시(`PlayerAttackDataSO`, Variant V1 직접 실행 필드, `AnimKey`/Ref 폴백, `MotionReferenceSO`)를 어떤 이유로도 다시 도입하지 않는다.

**MotionSet에서 하지 않는다**
- 데미지 수치를 MotionEvent에 넣지 않는다. 모션은 **언제**를 소유하고 **얼마나**는 소유하지 않는다.
- 조건 분기를 모션 안에서 처리하지 않는다. 조건은 Ability와 BT의 것이다.

### 2.2 판단이 애매할 때

> "이 값을 바꾸면 무엇이 달라져야 하는가?"

- 적이 **다르게 판단**해야 하면 → BT
- 같은 행동의 **결과가 달라져야** 하면 → GAS
- 같은 결과의 **타이밍이나 보이는 모습**이 달라져야 하면 → MotionSet

---

## 3. 작업 순서

새 공격·패턴을 만들 때는 아래 순서를 지킨다. 거꾸로 하면 매번 되돌아온다.

1. **의도를 한 문장으로 쓴다.** "근거리에서 플레이어가 회피만 반복할 때 이를 처벌하는 광역 후속타." 이 문장이 없으면 만들지 않는다.
2. **기존 데이터를 먼저 찾는다.** 비슷한 Ability가 있으면 복제·파생을 우선한다. 공용 Set 상속으로 해결되는지 확인한다.
3. **모션을 확정한다.** 실제 클립이 있어야 타이밍을 정할 수 있다. 클립 없이 만든 수치는 전부 다시 잡아야 한다.
4. **Ability 데이터를 만든다.** `generate-gas-ability` 스킬을 사용한다. `motionKey`, `HitPhase`, 비용·쿨다운을 채운다.
5. **MotionSet에 히트 타이밍과 이벤트를 얹는다.** 히트박스 개폐, VFX, SFX, 캔슬 창.
6. **BT에 연결한다.** `aiSelectable`을 켜고, 선택 조건을 BT 쪽에 표현한다. `generate-bt-json` 스킬을 사용한다.
7. **감각을 조율한다.** 히트스톱, 카메라, 텔레그래프 길이. 이 단계를 생략하면 6단계까지가 무의미하다.
8. **검증한다.** 8절.

---

## 4. BT 저작 규칙

### 4.1 두 가지 저작 포맷을 혼동하지 않는다

이 프로젝트에는 **성격이 다른 두 개의 BT JSON 포맷**이 있다. 어느 쪽에 쓰는지에 따라 결과가 완전히 달라진다.

| 폴더 | 포맷 | 특징 |
|---|---|---|
| `SourceJson/` | **Rules JSON** | 의도·규칙 중심 저작. 변환 시 **스코어러가 자동 부착**된다 |
| `Json/` | **raw BT 노드 JSON** | 노드 트리를 직접 기술. 스코어러를 거치지 않고 관찰 가능한 분기를 그대로 만든다 |
| `Generated/` | `.asset` | 위 JSON에서 생성된 `BehaviorTreeAsset`. **직접 손으로 고치지 않는다** |

`generate-bt-json` 스킬을 쓸 때 **어느 포맷인지 명시**한다. 명시하지 않으면 의도와 다른 쪽에 생성될 수 있다.

### 4.2 AI 레이어 귀속

행동을 조정할 때 **어느 레이어를 건드려야 하는지**를 먼저 판단한다.

| 바꾸고 싶은 것 | 건드릴 곳 |
|---|---|
| 무엇을 언제 고를지, 얼마나 자주 붙을지 (결정·페이싱) | Rules JSON / BT 노드 |
| 예비 동작이 얼마나 잘 보이는지 (텔레그래프) | 공격 데이터 + MotionSet |
| 연속 압박의 강도, 리듬 (템포) | 코드·스코어러 (`maxComboPressure`, `RhythmPhase`) |

**함정:** `SelectedIntent`와 `cooldownId`는 여러 레이어가 함께 읽는다. 한쪽만 고치면 다른 쪽에서 조용히 어긋난다.

### 4.3 AI 품질 기준

- **읽히는 예비 동작.** 강공격은 반드시 텔레그래프를 갖는다. 텔레그래프 없는 대미지는 난이도가 아니라 결함이다.
- **쉬는 구간을 준다.** 압박이 끊기지 않으면 플레이어는 대응을 배울 수 없다.
- **같은 상황에서 항상 같은 행동을 하지 않는다.** 다만 무작위가 아니라 **읽을 수 있는 편차**여야 한다.
- **여러 마리가 동시에 덤비지 않게 한다.** 그룹 슬롯 양보 규칙을 따른다.
- 페이즈 전환은 수치 변화가 아니라 **행동 레퍼토리 변화**로 표현한다.

---

## 5. GAS 저작 규칙

### 5.1 데이터 체인

```text
CharacterModelData.abilitySet
→ AbilitySetSO
→ GameplayAbilitySO (조건·비용·쿨다운·Variant 정책)
→ UPlayGroundMotionAbilityPayloadSO (motionKey + HitPhase 수치)
→ ActorAnimationMotionSet.abilityMotions (motionKey → MotionSetAsset)
```

- **플레이어와 몬스터는 같은 구조를 쓴다.** 몬스터 전용 별도 경로를 만들지 않는다.
- `motionKey`는 **Ability/Variant 식별자를 포함하지 않는 독립 문자열 키**다. 규약은 `abilityId`에서 최상위 분류 접두사(`Actor.`/`Player.`/`Monster.`/`Boss.`)를 뗀 형태다. (`Boss.Bokusei.Counter.04` → `Bokusei.Counter.04`)
- `motionKey`와 `baseInfo`는 **형제 필드**다. 히트 페이즈가 없는 모션 전용 Ability도 정상 동작한다.
- 플레이어는 현재 `WeaponType` 세트에서 먼저 해석하고 `NoWeapon` 세트로 폴백한다.

### 5.2 저작 원칙

- **복제보다 상속을 먼저 검토한다.** 공용 Set을 파생시키면 한 곳을 고쳐 전부 반영된다. 복제하면 영원히 따로 관리해야 한다.
- **비용·쿨다운·Variant 선택의 단일 소스는 `GameplayAbilitySO`다.** 다른 곳에 같은 값을 두지 않는다.
- `PlayerSkillSlot`은 **입력 슬롯 바인딩**이지 공격 수치의 원본이 아니다.
- Ability 에셋 생성·수정은 반드시 `generate-gas-ability` 스킬을 사용한다. 손으로 만들면 규약이 어긋난다.

### 5.3 다단 히트

`HitPhaseData`로 다단 공격을 표현한다. 각 페이즈는 **자기 타이밍과 자기 리액션**을 갖는다. 마지막 타격만 강한 리액션을 주고 중간 타격은 가볍게 처리하는 것이 기본형이다.

---

## 6. MotionSet · MotionEvent 저작 규칙

- **MotionSet은 타임라인이다.** 하나의 액션에 여러 클립을 순차 체이닝하고, 그 위에 이벤트를 얹는다.
- **히트박스는 MotionEvent로 연다.** 애니메이션의 시각적 임팩트 프레임과 판정 프레임이 어긋나면 즉시 이상하게 느껴진다.
- **`[SerializeReference]` MotionEvent 클래스를 다른 어셈블리로 옮길 때는 반드시 `[MovedFrom(true, sourceAssembly: "이전 어셈블리")]`를 유지한다.** 누락하면 기존 에셋의 이벤트와 VFX 참조가 역직렬화되지 않고 조용히 사라진다.
- 상체/하체 분리가 필요하면 `AvatarMask` 레이어를 쓴다.
- **캔슬 창은 명시적으로 저작한다.** 암묵적으로 "이 정도면 되겠지"로 두지 않는다.

---

## 7. 전투 감각 — 여기까지가 작업 범위다

수치가 맞아도 감각이 없으면 완성이 아니다.

- **히트스톱.** 타격 무게에 비례한다. 다만 **플레이어 히트스톱을 매 히트마다 재시작하면 다인 전투에서 조작이 잠긴다.** 그룹 상황을 함께 확인한다.
- **후딜(endlag).** 페이즈별 후딜은 다음 타격까지의 간격에서 나온다. 임의로 늘리면 콤보가 끊긴다.
- **카메라.** 강타격에는 셰이크가 붙는다. 회전 기반 셰이크를 쓰고, 히트스톱 중 가드 로직을 임의로 제거하지 않는다.
- **텔레그래프.** 4.3 참조. 공격 데이터와 모션이 함께 책임진다.
- **강인도(Poise).** 단일 소스는 `ActorStatSO`(`statData`)다. 생성기가 이 값을 덮어쓰지 않게 주의한다. break 값을 바꿨으면 다시 bake한다.
- **선입력과 캔슬.** `InputBuffer`와 캔슬 창이 함께 동작해야 반응한다고 느낀다.

---

## 8. 검증

**"컴파일됐다"는 검증이 아니다.** 아래를 사실대로 구분해 보고한다.

| 항목 | 방법 |
|---|---|
| Ability 데이터 정합성 | `AbilityDataValidator` 전수 검증 |
| 몬스터 Ability 연결 | `MonsterAbilitySetIntegrationTests` — `aiSelectable` Ability의 Payload·MotionKey·HitPhase 누락을 **건너뛰지 말고 모아서 보고** |
| Ability 로직 | EditMode 14개 + PlayMode 수직 슬라이스 2개 |
| 실제 감각 | Play Mode 직접 확인. **이것만이 감각의 유일한 검증 수단이다** |

**알려진 예상 Warning:** Dryad 공격 3개와 Training Dummy 공격 1개는 대응 Motion 콘텐츠가 확정되지 않아 "어떤 MotionSet에서도 해석되지 않는 Key" Warning을 낸다. **임의로 아무 모션에 매핑해 Warning을 없애지 않는다.** 콘텐츠 확정 후 연결한다. Error로 승격하지도 않는다.

---

## 9. 최종 체크리스트

**경계**
- [ ] 판단은 BT, 수치는 GAS, 타이밍은 MotionSet에 있다.
- [ ] BT 노드에 데미지·범위 수치가 없다.
- [ ] Payload 바깥에 중복 모션 참조가 없다.
- [ ] 제거된 레거시 타입·필드를 되살리지 않았다.

**GAS**
- [ ] `generate-gas-ability` 스킬로 작업했다.
- [ ] `motionKey` 규약(분류 접두사 제거)을 지켰다.
- [ ] 복제 대신 상속으로 해결 가능한지 검토했다.
- [ ] 비용·쿨다운이 `GameplayAbilitySO` 한 곳에만 있다.

**BT**
- [ ] Rules JSON(`SourceJson/`)인지 raw 노드 JSON(`Json/`)인지 명확히 하고 작업했다.
- [ ] `Generated/`의 `.asset`을 직접 수정하지 않았다.
- [ ] 강공격에 텔레그래프가 있다.
- [ ] 압박 사이에 대응할 틈이 있다.

**모션·감각**
- [ ] 히트박스 개폐 타이밍이 시각적 임팩트와 맞는다.
- [ ] 캔슬 창을 명시적으로 저작했다.
- [ ] MotionEvent 이관 시 `[MovedFrom]`을 유지했다.
- [ ] 히트스톱·카메라·후딜을 조율했고 다인 전투에서 확인했다.

**검증**
- [ ] `AbilityDataValidator`를 돌렸고 새 Warning/Error가 없다.
- [ ] 관련 테스트를 돌렸다.
- [ ] Play Mode 검증 여부를 사실대로 보고했다.

---

## 10. 권위 문서

**전체 사양**
- `Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md` — GAS 단일 권위 사양
- `Assets/docs/guide/BEHAVIOR_TREE_SYSTEM_GUIDE.md` — BT 시스템 전반과 AAA 레퍼런스
- `Assets/docs/guide/COMBAT_SYSTEM_GUIDE.md` — 전투 런타임 구조
- `Assets/docs/guide/MOTION_EVENT_ROLE_GUIDE.md` — MotionEvent 역할 경계

**세부**
- `Assets/docs/guide/ATTACK_CANCEL_SYSTEM_GUIDE.md` — 캔슬 창
- `Assets/docs/guide/MONSTER_HEAVY_ATTACK_TELEGRAPH_GUIDE.md` — 텔레그래프
- `Assets/docs/guide/GAMEPLAY_ABILITY_PRODUCTION_GUIDE.md` — Ability 제작 워크플로
- `Assets/docs/guide/STAT_SYSTEM_GUIDE.md` — 스탯·Poise 단일 소스
- `Assets/docs/Complete/TIME_HITSTOP_GUIDE.md` — 히트스톱
- `Assets/docs/Complete/ACTOR_MOTION_FALLBACK_GUIDE.md` — 모션 폴백 규칙

**스킬**
- `generate-gas-ability` — Ability 데이터 생성·복제·검증
- `generate-bt-json` — BT JSON 생성·수정
