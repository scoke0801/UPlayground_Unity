# Animancer 기반 애니메이션 시스템

언리얼 엔진의 애니메이션 몽타쥬 시스템을 Unity의 Animancer로 구현한 시스템입니다.
**AnimationMontage**와 **MotionSet** 두 가지 시스템을 제공합니다.

---

## 📚 시스템 개요

### 🎬 AnimationMontage (섹션 기반 애니메이션)
복잡한 애니메이션을 섹션으로 나누어 제어하는 시스템
- **용도**: 콤보 공격, 재장전, 스킬 등 구간별 제어가 필요한 애니메이션
- **특징**: 섹션 점프, 노티파이 이벤트, 조건부 분기

### 🎯 MotionSet (그룹 기반 애니메이션)
여러 애니메이션을 그룹으로 관리하는 시스템
- **용도**: 이동(Idle/Walk/Run), 8방향 이동, 콤보, Idle 배리에이션
- **특징**: 자동 블렌딩, 방향성, 순차/랜덤 재생

---

## 🔧 주요 컴포넌트

### 1. AnimationSlot (슬롯 정의)
**ScriptableObject 기반**으로 애니메이션 레이어와 본 마스크를 정의
- Avatar Mask를 사용한 상체/하체 분리
- Animancer 레이어 인덱스 및 가중치 설정
- Override/Additive 블렌딩 모드

### 2. AnimationMontage (몽타쥬)
여러 섹션으로 구성된 애니메이션 에셋
- 섹션별 페이드 인/아웃, 재생 속도, 루프 설정
- 다음 섹션 지정 (순차/점프)
- 타임라인 노티파이 이벤트

### 3. MontagePlayer (몽타쥬 재생)
AnimationMontage를 재생하는 컴포넌트
- 슬롯 등록 및 레이어 관리
- 섹션 재생, 점프, 정지
- 이벤트 시스템 (시작, 종료, 섹션 변경, 노티파이)

### 4. MotionSet (모션 세트)
여러 애니메이션을 그룹으로 관리하는 에셋
- 5가지 재생 모드: Sequential, Blend, Directional, Random, Single
- 3가지 블렌딩 타입: Linear, Cartesian, Directional
- AnimationClip 또는 AnimationMontage 사용 가능

### 5. MotionSetPlayer (모션 세트 재생)
MotionSet을 재생하는 컴포넌트
- 블렌딩 파라미터 업데이트
- 방향 기반 재생
- 순차 재생 제어

### 6. MontageSlotManager (슬롯 그룹 관리)
슬롯 그룹별 몽타쥬 인터럽트 관리
- 같은 그룹 내 몽타쥬 자동 중단
- 런타임 슬롯 그룹 관리

---

## 📖 Part 1: AnimationMontage 시스템

### 사전 준비: AnimationSlot 생성

AnimationSlot은 **ScriptableObject**로 레이어와 본 마스크를 정의합니다.

```
1. Project 창 우클릭
2. Create > Animation > Slot
3. Inspector에서 설정:
   - Slot Name: "UpperBody"
   - Layer Index: 1 (0은 기본 레이어)
   - Layer Weight: 1.0
   - Avatar Mask: (상체 본만 선택한 마스크)
   - Blending Mode: Override 또는 Additive
```

**주요 슬롯 예시:**
- **FullBody**: Layer 0, 전신 애니메이션
- **UpperBody**: Layer 1, 상체만 (사격, 재장전)
- **LowerBody**: Layer 2, 하체만 (이동)

### 몽타쥬 에셋 생성

```
1. Project 창 우클릭
2. Create > Animation > Montage
3. 이름: "AttackMontage"
```

### 몽타쥬 설정

```
Inspector에서:
- Montage Name: "Attack Combo"
- Slot Name: "FullBody" (또는 생성한 AnimationSlot 이름)

Sections:
  [0] Section: "Attack1"
      - Clip: (공격1 애니메이션)
      - Fade In: 0.2
      - Play Rate: 1.0
      - Next Section: "Attack2"
      - Notifies:
        * "EnableCollision" at 0.3
        * "DealDamage" at 0.5
      
  [1] Section: "Attack2"
      - Clip: (공격2 애니메이션)
      - Next Section: "Attack3"
      
  [2] Section: "Attack3"
      - Clip: (피니셔 애니메이션)
      - Play Rate: 1.2
      - Next Section: (비워둠 - 종료)
```

### 캐릭터에 적용

```csharp
// 필요한 컴포넌트:
1. AnimancerComponent
2. MontagePlayer
   - Registered Slots: (생성한 AnimationSlot들을 등록)
```

### 코드 사용

```csharp
[SerializeField] private MontagePlayer montagePlayer;
[SerializeField] private AnimationMontage attackMontage;

// 몽타쥬 재생
montagePlayer.PlayMontage(attackMontage);

// 특정 섹션부터 재생
montagePlayer.PlayMontage(attackMontage, "Attack2");

// 섹션 점프
montagePlayer.JumpToSection("Attack3");

// 재생 속도 변경
montagePlayer.SetPlayRate(1.5f);

// 슬롯 가중치 변경 (런타임)
montagePlayer.SetSlotWeight("UpperBody", 0.5f);

// 정지
montagePlayer.StopMontage();

// 이벤트 구독
montagePlayer.OnMontageStarted += (montage) => 
    Debug.Log($"Started: {montage.MontageName}");
    
montagePlayer.OnNotifyTriggered += (notifyName) =>
{
    if (notifyName == "DealDamage")
        DealDamageToEnemy();
};
```

### 예제: 재장전 시스템

```
Montage: "Reload"
Slot: "UpperBody"

Sections:
  [0] "Start"
      - Clip: 탄창 빼기
      - Next: "Loop"
      
  [1] "Loop"
      - Clip: 탄 장전 (반복 애니메이션)
      - Loop: ✓
      
  [2] "End"
      - Clip: 탄창 넣기
```

사용:
```csharp
// 재장전 시작
montagePlayer.PlayMontage(reloadMontage);

// 필요한 만큼 Loop 섹션이 반복됨

// 완료 시 End로 점프
montagePlayer.JumpToSection("End");
```

---

## 📖 Part 2: MotionSet 시스템

### MotionSet이란?

여러 애니메이션을 **하나의 그룹**으로 관리하는 시스템입니다.

**AnimationMontage와의 차이:**
- **Montage**: 하나의 복잡한 애니메이션을 섹션으로 분할
- **MotionSet**: 여러 개의 관련 애니메이션을 그룹화

### MotionSet 생성

```
1. Project 창 우클릭
2. Create > Animation > Motion Set
3. 이름: "Locomotion"
```

### 재생 모드 (Play Mode)

#### 1️⃣ Sequential (순차 재생)
```
용도: 콤보 공격
동작: 순서대로 재생
예시: Attack1 → Attack2 → Attack3 → Finisher
```

#### 2️⃣ Blend (블렌딩)
```
용도: 속도에 따른 이동
동작: 파라미터에 따라 자동 블렌딩
타입:
  - Linear: 1D (속도)
  - Cartesian: 2D (X, Y)
  - Directional: 2D 방향
```

#### 3️⃣ Directional (방향성)
```
용도: 8방향 이동
동작: 입력 방향에 가장 가까운 애니메이션 선택
```

#### 4️⃣ Random (랜덤)
```
용도: Idle 배리에이션
동작: 랜덤 선택
```

#### 5️⃣ Single (단일)
```
용도: 일반 애니메이션
동작: 첫 번째 모션만 재생
```

### 예시 1: Locomotion (Linear Blend)

**MotionSet 설정:**
```
이름: "Locomotion"
Play Mode: Blend
Blend Type: Linear
Blend Parameter Range: (0, 10)

Motions:
  [0] Idle
      - Source Type: Clip
      - Clip: Idle.anim
      - Threshold: 0
      
  [1] Walk
      - Clip: Walk.anim
      - Threshold: 3
      
  [2] Run
      - Clip: Run.anim
      - Threshold: 6
      
  [3] Sprint
      - Clip: Sprint.anim
      - Threshold: 10
```

**코드:**
```csharp
[SerializeField] private MotionSet locomotionSet;
[SerializeField] private MotionSetPlayer player;

void Start()
{
    player.Play(locomotionSet);
}

void Update()
{
    float speed = GetMovementSpeed(); // 0~10
    player.UpdateBlendParameter(speed);
    // 속도에 따라 Idle → Walk → Run → Sprint 자동 블렌딩
}
```

### 예시 2: Combat Combo (Sequential)

**MotionSet 설정:**
```
이름: "Light Attack Combo"
Play Mode: Sequential
Target Slot Asset: (UpperBody 슬롯)

Motions:
  [0] LightAttack1
  [1] LightAttack2
  [2] LightAttack3
  [3] LightAttack4_Finisher
```

**코드:**
```csharp
void Start()
{
    player.Play(combatComboSet);
}

void OnAttackInput()
{
    player.PlayNextSequential(); // 다음 콤보 재생
}
```

### 예시 3: 8방향 이동 (Directional)

**MotionSet 설정:**
```
이름: "8 Direction Movement"
Play Mode: Directional

Motions:
  [0] Forward      - Direction Angle: 90°
  [1] Right        - Direction Angle: 0°
  [2] Back         - Direction Angle: 270°
  [3] Left         - Direction Angle: 180°
  [4] ForwardRight - Direction Angle: 45°
  [5] ForwardLeft  - Direction Angle: 135°
  [6] BackLeft     - Direction Angle: 225°
  [7] BackRight    - Direction Angle: 315°
```

**코드:**
```csharp
void Update()
{
    Vector2 input = new Vector2(
        Input.GetAxis("Horizontal"),
        Input.GetAxis("Vertical")
    );
    
    if (input.magnitude > 0.1f)
    {
        // 입력 방향에 맞는 애니메이션 자동 선택
        player.PlayByDirection(input.normalized);
    }
}
```

### 예시 4: Idle Variations (Random)

**MotionSet 설정:**
```
이름: "Idle Variations"
Play Mode: Random

Motions:
  [0] Idle_LookAround
  [1] Idle_Stretch
  [2] Idle_CheckWeapon
```

**코드:**
```csharp
void Start()
{
    InvokeRepeating(nameof(PlayRandomIdle), 0f, 5f);
}

void PlayRandomIdle()
{
    player.Play(idleVariationsSet);
    // Random 모드로 설정되어 자동으로 랜덤 선택
}
```

### 예시 5: Strafe (Cartesian Blend)

**MotionSet 설정:**
```
이름: "Strafe Movement"
Play Mode: Blend
Blend Type: Cartesian

Motions:
  [0] Idle         - Threshold: (0, 0)
  [1] Forward      - Threshold: (0, 5)
  [2] Back         - Threshold: (0, -5)
  [3] Right        - Threshold: (5, 0)
  [4] Left         - Threshold: (-5, 0)
  [5] ForwardRight - Threshold: (5, 5)
  ...
```

**코드:**
```csharp
void Update()
{
    Vector2 input = new Vector2(
        Input.GetAxis("Horizontal"),
        Input.GetAxis("Vertical")
    ) * maxSpeed;
    
    player.UpdateBlendParameter(input); // 2D 블렌딩
}
```

### MotionSet에서 Montage 사용하기

MotionSet의 각 모션은 **Clip** 또는 **Montage**를 사용할 수 있습니다:

```
Motion Data:
  [0] Heavy Attack
      - Source Type: Montage
      - Montage: (HeavyAttackMontage 에셋)
      - Threshold: 0
      
  [1] Light Attack
      - Source Type: Clip
      - Clip: (LightAttack.anim)
      - Threshold: 1
```

---

## 🎮 슬롯 그룹 시스템

### MontageSlotManager 설정

Hierarchy에서 `MontageSlotManager` 찾기 (없으면 자동 생성):

```
Inspector:
Slot Groups:
  [0] Group Name: "CombatGroup"
      Slots:
        - FullBody
        - UpperBody
        
  [1] Group Name: "MovementGroup"
      Slots:
        - LowerBody
```

### 동작 방식

```
시나리오:
1. UpperBody 슬롯에서 재장전 몽타쥬 재생 중
2. UpperBody 슬롯에서 근접 공격 몽타쥬 재생 요청
3. 같은 CombatGroup이므로 재장전 자동 중단
4. 근접 공격 몽타쥬 재생
```

---

## 🆚 시스템 선택 가이드

| 상황 | 사용할 시스템 | 이유 |
|------|---------------|------|
| 단순 재생 | AnimationClip | 가장 간단 |
| 섹션 분할 필요 | **AnimationMontage** | 특정 구간 재생, 조건부 점프 |
| 타임라인 이벤트 | **AnimationMontage** | 노티파이 활용 |
| 콤보 공격 | **MotionSet** (Sequential) | 순차 재생 자동화 |
| 이동 블렌딩 | **MotionSet** (Blend) | 속도 기반 자동 블렌딩 |
| 방향별 이동 | **MotionSet** (Directional) | 방향 자동 선택 |
| Idle 배리에이션 | **MotionSet** (Random) | 랜덤 선택 |

---

## 📁 파일 구조

```
Assets/Scripts/Animation/
├── AnimationClip                    # Unity 기본
├── AnimationMontage.cs              # 몽타쥬 시스템
├── AnimationSlot.cs                 # 슬롯 ScriptableObject
├── MontagePlayer.cs                 # 몽타쥬 재생
├── MontageSlotManager.cs            # 슬롯 그룹 관리
├── MotionSet.cs                     # 모션 세트 시스템
├── MotionSetPlayer.cs               # 모션 세트 재생
└── Editor/
    ├── MotionSetEditor.cs           # Inspector 에디터
    └── MotionSetWindow.cs           # 독립 윈도우
```

---

## 🎯 언리얼 엔진과의 비교

| 기능 | 언리얼 | 이 구현 |
|------|--------|---------|
| Montage 섹션 | ✅ | ✅ |
| Slot 시스템 | ✅ | ✅ (ScriptableObject) |
| Slot 그룹 | ✅ | ✅ |
| 노티파이 | ✅ | ✅ |
| Blend Space | ✅ | ✅ (MotionSet Blend) |
| 자식 몽타쥬 | ✅ | ❌ (추가 구현 필요) |

---

## ⚙️ 고급 기능

### 1. Additive 애니메이션

```
AnimationSlot 설정:
- Blending Mode: Additive
- Layer Weight: 0.5

용도: 기본 자세 + 호흡 애니메이션 등
```

### 2. 멀티 슬롯 동시 재생

```csharp
// 하체: 이동 애니메이션
lowerBodyPlayer.PlayMontage(runMontage); // LowerBody 슬롯

// 상체: 사격 애니메이션 (동시 재생)
upperBodyPlayer.PlayMontage(shootMontage); // UpperBody 슬롯
```

### 3. 런타임 슬롯 그룹 추가

```csharp
MontageSlotManager.Instance.AddSlotGroup("CustomGroup");
MontageSlotManager.Instance.AddSlotToGroup("CustomGroup", "CustomSlot");
```

---

## ⚠️ 주의사항

1. **Animancer 플러그인 필수**: Animancer v8.x 이상 필요
2. **AnimationSlot은 ScriptableObject**: 프리팹에 직접 할당 불가, 에셋으로 생성 필요
3. **레이어 인덱스**: 0은 기본 레이어, 1번부터 사용 권장
4. **Avatar Mask**: Humanoid 리그에서만 작동
5. **성능**: 많은 노티파이 사용 시 성능 영향 고려

---

## 🔧 트러블슈팅

### Q: 슬롯 애니메이션이 재생되지 않아요
```
A: AnimationSlot의 Layer Weight가 0이 아닌지 확인
   MontagePlayer의 Registered Slots에 슬롯이 등록되었는지 확인
```

### Q: 블렌딩이 부자연스러워요
```
A: MotionSet의 Threshold 값 조정
   Blend Parameter Range 확인
```

### Q: 같은 그룹 몽타쥬가 중단되지 않아요
```
A: MontageSlotManager에서 슬롯 그룹이 올바르게 설정되었는지 확인
   몽타쥬의 Slot Name이 그룹에 포함되어 있는지 확인
```

---

## 📚 더 알아보기

- **QUICKSTART.md**: 5분 안에 빠르게 시작하기
- **MontagePlayerExample.cs**: 몽타쥬 사용 예제 코드
- **MotionSetPlayerExample.cs**: 모션 세트 사용 예제 코드
- **애니메이션_관련 문서**: 상세한 시스템 설명
