# 스탯/레벨/장비 밸런스 가이드

이 문서는 현재 코드 기준으로 플레이어/파티 캐릭터의 스탯이 어디서 계산되고, 장비 옵션이 어떤 방식으로 합산되며, 실제 전투에 어떤 항목이 반영되는지 정리한다.

## 핵심 결론

- 플레이어/파티 캐릭터의 기본 스탯은 `PartyMemberGrowthSO`의 레벨 성장 계산으로 만든다.
- 장비 스탯은 `EquipmentSO`의 `Stat Modifiers`를 `ActorStatContainer`에 `StatModifier`로 추가해서 적용한다.
- 최종 스탯 계산 순서는 모든 경로에서 다음 순서를 따른다.

```text
최종값 = (기본값 + Flat 합계) * (1 + Percent 합계) * Multiply 곱
```

- 공격력과 방어력은 실제 피해 계산에 반영된다.
- 치명타 확률 `CritRate`와 치명타 배율 `CritMultiplier`는 실제 피해 계산에 반영된다.
- `AttackData.criticalMultiplier`가 1보다 큰 공격은 스킬/특수 공격이 강제한 치명타로 보고, 확률 굴림 없이 해당 배율을 우선 사용한다.

## 데이터 입력 위치

### 레벨 성장 데이터

파티 캐릭터 레벨 스탯은 다음 데이터에서 시작한다.

- `PartyMemberGrowthSO`
- `baseStat`: 레벨 1 기준 `ActorStatSO`
- `growthRules`: 레벨별 증가 규칙
- `levelCap`: 최대 레벨

계산 코드는 `PartyPowerCalculator.CalculateGrowthStats()`가 담당한다.

### 장비 스탯 데이터

장비 옵션은 `EquipmentSO`의 `Stat Modifiers`에 입력한다.

각 옵션은 다음 세 값으로 구성된다.

```text
StatType + ModifierType + value
```

예시:

```text
무기: AttackPower / Percent / 0.15   => 공격력 +15%
무기: CritRate    / Flat    / 0.08   => 치명타 확률 +8%
갑옷: Defense     / Flat    / 0.05   => 방어력 +5%
갑옷: MaxHealth   / Flat    / 40     => 최대 체력 +40
신발: MoveSpeed   / Percent / 0.08   => 이동 속도 +8%
```

기존 `attackPower`, `critChance`, `critDamage` 필드는 legacy 호환용이다. `Stat Modifiers`가 비어 있을 때만 fallback으로 변환된다. 신규 장비는 `Stat Modifiers`만 사용하는 것을 권장한다.

## ModifierType 의미

### Flat

기본값에 직접 더한다.

```text
기본 체력 100 + MaxHealth Flat 40 = 140
기본 방어 0.00 + Defense Flat 0.05 = 0.05
기본 치명타 확률 0.05 + CritRate Flat 0.08 = 0.13
```

방어력과 치명타 확률은 내부 값이 0~1 비율이다.

```text
0.05 = 5%
0.13 = 13%
```

### Percent

Flat까지 더한 값에 비율로 곱한다.

```text
기본 공격력 1.0 + AttackPower Percent 0.15 = 1.15
기본 체력 100 + MaxHealth Percent 0.20 = 120
```

여러 Percent는 더한 뒤 한 번 곱한다.

```text
+10%, +15% => * 1.25
```

### Multiply

마지막에 직접 배율로 곱한다. 여러 Multiply는 서로 곱한다.

```text
MoveSpeed Multiply 1.1 => x1.1
Multiply 1.1, Multiply 1.2 => x1.32
```

일반 장비 옵션은 대부분 `Flat` 또는 `Percent`를 쓰고, 특수 상태/세트 효과처럼 강한 배율 효과에만 `Multiply`를 쓰는 편이 안전하다.

## 레벨별 스탯 계산 방식

`PartyPowerCalculator.CalculateGrowthStats()`는 모든 `StatType`을 순회하며 레벨 스탯을 만든다.

기본값은 다음 우선순위로 결정된다.

```text
PartyMemberGrowthSO.baseStat에 값이 있으면 그 값
없으면 ActorStatSO 기본값
```

성장 규칙이 없으면 기본값을 그대로 쓴다.

### Flat 성장

레벨마다 고정값을 더한다.

```text
최종값 = baseValue + flatPerLevel * (level - 1)
```

예:

```text
MaxHealth base 100
flatPerLevel 10
Lv.1 = 100
Lv.5 = 100 + 10 * 4 = 140
```

### Percent 성장

레벨마다 기본값 기준 비율로 증가한다.

```text
최종값 = baseValue * (1 + percentPerLevel * (level - 1))
```

예:

```text
AttackPower base 1.0
percentPerLevel 0.03
Lv.1 = 1.00
Lv.5 = 1.00 * (1 + 0.03 * 4) = 1.12
```

### Curve 성장

레벨을 1~levelCap 사이의 0~1 값으로 정규화한 뒤, 커브 값을 기본값에 곱한다.

```text
normalized = InverseLerp(1, levelCap, level)
최종값 = baseValue * curve.Evaluate(normalized)
```

커브가 비어 있으면 기본값을 그대로 쓴다.

## 장비 포함 최종 스탯 계산

UI와 파티 전투력은 `CharacterEffectiveStatCalculator`를 통해 성장 스탯에 장비 modifier를 합산한다.

런타임 활성 캐릭터는 `PlayerActor.ApplyEquipmentStatsForActiveCharacter()`가 실제 `ActorStatContainer`에 장비 modifier를 추가한다.

장착/해제 흐름:

```text
InventoryManager.TryEquipItem / TryUnequipItem
=> 캐릭터별 장착 레지스트리 변경
=> PlayerActor.RefreshEquipmentStatsForCharacter
=> 활성 캐릭터면 ActorStatContainer modifier 재적용
=> 벤치 캐릭터면 저장 HP를 새 MaxHealth에 맞춰 보정
=> Inventory/Party UI 갱신
```

HP 보정 규칙:

```text
풀피였으면 새 최대 체력 기준 풀피 유지
풀피가 아니면 기존 체력 비율 유지
```

예:

```text
100/100에서 MaxHealth +40 장비 장착 => 140/140
50/100에서 MaxHealth +40 장비 장착 => 70/140
140/140에서 장비 해제 => 100/100
70/140에서 장비 해제 => 50/100
```

## 실제 전투 적용 여부

### AttackPower

적용됨.

피해 계산에서 공격자의 `Stats.AttackPower`를 곱한다.

```text
finalDamage = baseDamage * attackerPower * ...
```

`AttackPower`는 현재 주석상 공격력 배율이다.

```text
1.0 = 기본 피해
1.15 = 피해 +15%
0.8 = 피해 -20%
```

장비에서 공격력을 올릴 때는 보통 다음을 권장한다.

```text
AttackPower / Percent / 0.10  => 현재 공격력 +10%
AttackPower / Flat / 0.10     => 공격 배율 +0.10
```

둘 다 결과가 비슷해 보일 수 있지만 의미가 다르다. 일반 장비 옵션은 `Percent`가 밸런싱 의도가 명확하다.

### Defense

적용됨.

피해 계산에서 피격자의 `Stats.Defense`를 0~1로 clamp한 뒤 피해를 줄인다.

```text
finalDamage = ... * (1 - defenseRate)
```

예:

```text
Defense 0.05 = 피해 5% 감소
Defense 0.30 = 피해 30% 감소
Defense 1.00 이상 = 피해 100% 감소로 clamp
```

단, 기본 피해가 0보다 큰 공격은 최소 1 피해가 보장된다.

### MaxHealth

적용됨.

플레이어/파티 캐릭터의 최대 체력으로 쓰이며, 장비 변경 시 현재 체력도 위 HP 보정 규칙에 따라 조정된다.

### CritMultiplier

적용됨.

피해 계산식은 `HitRequest`에 확정된 `criticalMultiplier`를 실제로 곱한다.

```text
finalDamage = ... * criticalMultiplier
```

확률 치명타가 성공하면 공격자의 `Stats.CritMultiplier`가 `HitRequest.CriticalMultiplier`로 들어간다.

예:

```text
CritRate 0.20, CritMultiplier 1.7
=> 20% 확률로 최종 피해 x1.7
```

`AttackData.criticalMultiplier`가 1보다 크면 스킬/특수 공격의 강제 치명타로 본다.

```text
AttackData.criticalMultiplier 2.0
=> CritRate를 굴리지 않고 최종 피해 x2.0
```

### CritRate

적용됨.

표준 히트 입력이 `HitRequest.FromAttackData()`를 통과할 때 공격자의 `Stats.CritRate`를 굴린다.

성공하면 공격자의 `Stats.CritMultiplier`가 이번 히트의 치명타 배율이 된다.

```text
Random.value <= attacker.Stats.CritRate
=> criticalMultiplier = attacker.Stats.CritMultiplier
```

실패하면 치명타 배율은 1.0이다.

```text
Random.value > attacker.Stats.CritRate
=> criticalMultiplier = 1.0
```

표준 근접 공격, 투사체, AOE, 잔상 공격처럼 `HitRequest.FromAttackData()`를 사용하는 경로에 공통 적용된다.

### MoveSpeed, DashDistance, SkillGaugeRate, InvincibleDuration

장비 modifier로 최종 스탯에는 계산된다. UI/전투력에도 일부 반영된다.

다만 실제 이동, 대시, 스킬 게이지, 무적 시간 로직이 각각 해당 `Stats` 값을 읽는지는 별도 연결 여부에 따라 달라진다. 밸런싱 전에 사용처를 확인해야 한다.

현재 전투력 계산에는 다음이 반영된다.

```text
MoveSpeed
SkillGaugeRate
MaxPoise
```

## 전투력 계산

전투력은 실제 전투 피해와 1:1로 같은 값은 아니며, 비교용 점수다.

계산식:

```text
effectiveAttack = AttackPower * (1 + CritRate * max(0, CritMultiplier - 1)) * SkillGaugeRate
effectiveHealth = MaxHealth / max(0.1, 1 - Defense)
utility = MaxPoise * 0.25 + max(0, MoveSpeed - 1) * 100

combatPower = effectiveHealth * 0.35
            + effectiveAttack * 100 * 0.55
            + utility * 0.10
```

장비 포함 전투력은 `PartyManager.GetEffectiveCombatPower()`를 사용한다.

## 밸런싱 입력 권장 규칙

### 기본 스탯

레벨 1 기준 `ActorStatSO`에는 캐릭터의 정체성을 넣는다.

예:

```text
탱커: MaxHealth 높음, Defense 높음, AttackPower 낮음
딜러: AttackPower 높음, CritRate 높음, Defense 낮음
기동형: MoveSpeed 높음, DashDistance 높음
```

### 레벨 성장

레벨 성장은 캐릭터의 장기 성장 방향을 넣는다.

권장:

```text
MaxHealth: Flat 또는 Percent
AttackPower: Percent
Defense: Flat을 낮은 수치로 제한
CritRate: Flat을 낮은 수치로 제한
CritMultiplier: Flat 또는 성장 없음
```

방어력은 0~1 감소율이므로 작은 값도 영향이 크다.

```text
Defense +0.05 = 받는 피해 5% 감소
Defense +0.20 = 받는 피해 20% 감소
```

### 장비 옵션

무기:

```text
AttackPower / Percent / 0.05 ~ 0.20
CritRate / Flat / 0.03 ~ 0.10
CritMultiplier / Flat / 0.10 ~ 0.30
```

방어구:

```text
MaxHealth / Flat / 20 ~ 100
Defense / Flat / 0.02 ~ 0.10
MaxPoise / Flat / 10 ~ 50
```

신발/장갑:

```text
MoveSpeed / Percent / 0.03 ~ 0.10
DashDistance / Percent / 0.05 ~ 0.15
SkillGaugeRate / Percent / 0.05 ~ 0.15
```

## 주의할 점

- `Defense`, `CritRate`는 내부 값이 퍼센트가 아니라 0~1 비율이다.
- `CritMultiplier`는 기본값이 1.5다. `Flat +0.2`를 넣으면 1.7, 즉 170%가 된다.
- `AttackPower`는 배율 스탯이다. `Flat +10` 같은 값은 피해를 11배로 만들 수 있으므로 피해야 한다.
- 신규 장비는 legacy 필드 대신 `Stat Modifiers`만 사용한다.
- `AttackData.criticalMultiplier`가 1보다 큰 공격은 확률 치명타가 아니라 강제 치명타로 처리된다.
