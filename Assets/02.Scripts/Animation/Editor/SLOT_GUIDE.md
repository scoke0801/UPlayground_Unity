# Animation Slot 시스템 가이드

## 개요
Animation Slot은 캐릭터의 특정 부위(상체/하체/전신)만 독립적으로 제어할 수 있게 해주는 시스템입니다.

## 슬롯 구분 원리

### 1. **Avatar Mask로 본 선택**
- Avatar Mask는 "어떤 본(bone)을 제어할지" 정의합니다
- 예시:
  - UpperBody Mask: 척추, 팔, 머리 본만 활성화
  - LowerBody Mask: 골반, 다리 본만 활성화
  - FullBody: Avatar Mask 없음 (모든 본 제어)

### 2. **Animancer Layer로 분리**
- 각 슬롯은 독립적인 Animancer Layer를 사용합니다
- 예시:
  - Layer 0: FullBody (기본 레이어, 전신)
  - Layer 1: UpperBody (상체만)
  - Layer 2: LowerBody (하체만)

### 3. **Slot Group으로 충돌 방지**
- 같은 그룹의 슬롯에서 새 몽타쥬가 재생되면 이전 몽타쥬를 자동 중단
- FullBody와 UpperBody가 같은 그룹이면 UpperBody 재생 시 FullBody가 자동 정지

---

## 설정 방법

### Step 1: Avatar Mask 생성

#### UpperBody Mask 만들기
1. Project 창 우클릭 → Create → Avatar Mask
2. 이름: `UpperBodyMask`
3. Inspector에서 설정:
   ```
   ✅ Body (척추)
   ✅ Head (머리)
   ✅ Left Arm (왼팔)
   ✅ Right Arm (오른팔)
   ✅ Left Hand (왼손)
   ✅ Right Hand (오른손)
   ❌ Left Leg (왼다리)
   ❌ Right Leg (오른다리)
   ❌ Left Foot (왼발)
   ❌ Right Foot (오른발)
   ```

#### LowerBody Mask 만들기
1. Project 창 우클릭 → Create → Avatar Mask
2. 이름: `LowerBodyMask`
3. Inspector에서 설정:
   ```
   ❌ Body
   ❌ Head
   ❌ Left Arm
   ❌ Right Arm
   ✅ Left Leg
   ✅ Right Leg
   ✅ Left Foot
   ✅ Right Foot
   ```

### Step 2: AnimationSlot 생성

#### FullBody Slot
1. Project 창 우클릭 → Create → Animation → Slot
2. 이름: `FullBodySlot`
3. Inspector 설정:
   ```
   Slot Name: FullBody
   Group Name: DefaultGroup
   Layer Index: 0
   Layer Weight: 1.0
   Avatar Mask: (비워둠)
   Blending Mode: Override
   ```

#### UpperBody Slot
1. Project 창 우클릭 → Create → Animation → Slot
2. 이름: `UpperBodySlot`
3. Inspector 설정:
   ```
   Slot Name: UpperBody
   Group Name: DefaultGroup
   Layer Index: 1
   Layer Weight: 1.0
   Avatar Mask: UpperBodyMask (드래그)
   Blending Mode: Override
   ```

#### LowerBody Slot
1. Project 창 우클릭 → Create → Animation → Slot
2. 이름: `LowerBodySlot`
3. Inspector 설정:
   ```
   Slot Name: LowerBody
   Group Name: DefaultGroup
   Layer Index: 2
   Layer Weight: 1.0
   Avatar Mask: LowerBodyMask (드래그)
   Blending Mode: Override
   ```

### Step 3: MontagePlayer 설정

캐릭터의 MontagePlayer 컴포넌트:
1. Inspector에서 `Registered Slots` 배열 크기를 3으로 설정
2. 생성한 슬롯들을 드래그:
   - Element 0: FullBodySlot
   - Element 1: UpperBodySlot
   - Element 2: LowerBodySlot

### Step 4: Montage에 슬롯 이름 지정

AnimationMontage를 만들 때:
```
Slot Name: "UpperBody"  ← 상체만 재생
Slot Name: "FullBody"   ← 전신 재생
```

---

## 사용 예시

### 예시 1: 달리면서 상체 공격
```csharp
// 하체: 달리기 애니메이션 (FullBody 슬롯)
montagePlayer.PlayMontage(runMontage); // slotName = "FullBody"

// 상체: 공격 애니메이션 (UpperBody 슬롯)
montagePlayer.PlayMontage(attackMontage); // slotName = "UpperBody"

// 결과: 하체는 달리고, 상체는 공격
```

### 예시 2: 서있는데 총 발사
```csharp
// 전신: 대기 애니메이션 (FullBody)
montagePlayer.PlayMontage(idleMontage); // slotName = "FullBody"

// 상체: 발사 애니메이션 (UpperBody)
montagePlayer.PlayMontage(shootMontage); // slotName = "UpperBody"

// 결과: 하체는 가만히 있고, 상체만 총 발사
```

### 예시 3: 슬롯 가중치 조절
```csharp
// 상체 애니메이션의 영향력을 50%로 줄임
montagePlayer.SetSlotWeight("UpperBody", 0.5f);

// 결과: 상체 애니메이션이 절반만 적용됨 (부드러운 블렌딩)
```

---

## 문제 해결

### Q: UpperBody를 재생해도 전신이 움직여요
**A:** Avatar Mask가 제대로 설정되지 않았습니다.
1. UpperBodySlot의 Avatar Mask 확인
2. Mask에서 상체 본만 체크되어 있는지 확인
3. MontagePlayer의 Registered Slots에 UpperBodySlot이 등록되어 있는지 확인

### Q: 두 애니메이션이 동시에 재생되지 않아요
**A:** 같은 Slot Group에 속해있습니다.
1. MontageSlotManager에서 그룹 설정 확인
2. UpperBody와 FullBody를 다른 그룹으로 분리하거나
3. 레이어 인덱스를 다르게 설정

### Q: 애니메이션이 끊겨요
**A:** Layer Weight를 확인하세요.
1. UpperBodySlot의 Layer Weight가 1.0인지 확인
2. 런타임에 `SetSlotWeight()`로 조절 가능

---

## 고급 팁

### 1. Additive Blending 사용
```
Blending Mode: Additive
```
- 이전 레이어에 애니메이션을 더합니다
- 예: 기본 자세 + 호흡 애니메이션

### 2. 동적 슬롯 가중치
```csharp
// 부드럽게 상체 애니메이션 fade in/out
StartCoroutine(FadeSlotWeight("UpperBody", 1.0f, 0.5f));

IEnumerator FadeSlotWeight(string slotName, float targetWeight, float duration)
{
    float elapsed = 0f;
    float startWeight = montagePlayer.GetSlotWeight(slotName);
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float weight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
        montagePlayer.SetSlotWeight(slotName, weight);
        yield return null;
    }
}
```

### 3. 커스텀 슬롯 그룹
```csharp
// 얼굴 표정 전용 그룹 만들기
MontageSlotManager.Instance.AddSlotGroup("FacialGroup");
MontageSlotManager.Instance.AddSlotToGroup("FacialGroup", "FacialSlot");
```
