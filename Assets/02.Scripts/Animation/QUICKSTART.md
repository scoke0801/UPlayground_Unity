# 🚀 빠른 시작 가이드

두 가지 시스템을 5분 안에 설정하는 방법을 알아봅니다.

---

## 📑 목차

1. [AnimationMontage 빠른 시작](#part-1-animationmontage-5분-시작)
2. [MotionSet 빠른 시작](#part-2-motionset-5분-시작)

---

## Part 1: AnimationMontage (5분 시작)

섹션 기반 애니메이션 시스템 - 콤보, 재장전, 스킬 등에 사용

### 1단계: AnimationSlot 생성 (1분)

**슬롯은 ScriptableObject입니다!**

```
1. Project 창 우클릭
2. Create > Animation > Slot
3. 이름: "UpperBodySlot"
4. Inspector 설정:
   - Slot Name: "UpperBody"
   - Layer Index: 1
   - Layer Weight: 1.0
   - Blending Mode: Override
   - Avatar Mask: (상체 본만 체크한 마스크 할당)
```

**필수 슬롯 3개 만들기:**
- **FullBodySlot**: Layer 0, 전신
- **UpperBodySlot**: Layer 1, 상체
- **LowerBodySlot**: Layer 2, 하체

### 2단계: 몽타쥬 생성 (30초)

```
1. Project 창 우클릭
2. Create > Animation > Montage
3. 이름: "AttackMontage"
```

### 3단계: 섹션 설정 (2분)

Inspector에서 몽타쥬 설정:

```
Montage Name: Attack Combo
Slot Name: FullBody

Sections:
  ➕ 섹션 추가 (총 3개)
  
  [0] Attack1
      - Clip: (공격1 애니메이션 드래그)
      - Fade In: 0.2
      - Play Rate: 1.0
      - Next Section: Attack2
      
      Notifies ➕:
        [0] Name: "EnableCollision", Time: 0.3
        [1] Name: "DealDamage", Time: 0.5
      
  [1] Attack2
      - Clip: (공격2 애니메이션)
      - Next Section: Attack3
      
  [2] Attack3
      - Clip: (피니셔 애니메이션)
      - Play Rate: 1.2
      - Next Section: (비워둠 - 종료)
```

### 4단계: 캐릭터 설정 (1분)

캐릭터 GameObject 선택:

```
1. Add Component > Animancer Component
2. Add Component > Montage Player
3. Montage Player Inspector:
   - Registered Slots:
     ➕ FullBodySlot (드래그)
     ➕ UpperBodySlot (드래그)
     ➕ LowerBodySlot (드래그)
```

### 5단계: 테스트 스크립트 (30초)

```csharp
using UnityEngine;
using Animation;

public class QuickTest : MonoBehaviour
{
    [SerializeField] private MontagePlayer montagePlayer;
    [SerializeField] private AnimationMontage attackMontage;
    
    void Start()
    {
        // 이벤트 구독
        montagePlayer.OnNotifyTriggered += (name) =>
        {
            if (name == "DealDamage")
                Debug.Log("💥 데미지 발생!");
        };
    }
    
    void Update()
    {
        // 1키: 몽타쥬 재생
        if (Input.GetKeyDown(KeyCode.Alpha1))
            montagePlayer.PlayMontage(attackMontage);
        
        // S키: 정지
        if (Input.GetKeyDown(KeyCode.S))
            montagePlayer.StopMontage();
    }
}
```

### ✅ 테스트

1. Play 버튼 클릭
2. `1` 키 → 공격 콤보 재생
3. `S` 키 → 정지

---

## Part 2: MotionSet (5분 시작)

그룹 기반 애니메이션 시스템 - 이동, 블렌딩, 방향성 등에 사용

### 1단계: MotionSet 생성 (30초)

```
1. Project 창 우클릭
2. Create > Animation > Motion Set
3. 이름: "Locomotion"
```

### 2단계: Locomotion 설정 (2분)

Inspector에서:

```
Motion Set Name: Locomotion
Play Mode: Blend
Blend Type: Linear
Blend Parameter Range: (0, 10)

Motions ➕ (총 4개):
  [0] Idle
      - Source Type: Clip
      - Clip: (Idle 애니메이션)
      - Threshold: 0
      - Motion Name: "Idle"
      
  [1] Walk
      - Clip: (Walk 애니메이션)
      - Threshold: 3
      - Motion Name: "Walk"
      
  [2] Run
      - Clip: (Run 애니메이션)
      - Threshold: 6
      - Motion Name: "Run"
      
  [3] Sprint
      - Clip: (Sprint 애니메이션)
      - Threshold: 10
      - Motion Name: "Sprint"
```

### 3단계: 캐릭터 설정 (1분)

```
캐릭터 GameObject:
1. Add Component > Animancer Component (이미 있으면 패스)
2. Add Component > Motion Set Player
3. Motion Set Player Inspector:
   - Animancer: (AnimancerComponent 드래그)
   - Montage Player: (있다면 드래그, 없어도 됨)
```

### 4단계: 테스트 스크립트 (1분)

```csharp
using UnityEngine;

public class LocomotionTest : MonoBehaviour
{
    [SerializeField] private MotionSetPlayer player;
    [SerializeField] private MotionSet locomotionSet;
    [SerializeField] private float maxSpeed = 10f;
    
    void Start()
    {
        // Locomotion 재생 시작
        player.Play(locomotionSet);
    }
    
    void Update()
    {
        // WASD 입력
        Vector2 input = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        
        // 속도 계산 (0~10)
        float speed = input.magnitude * maxSpeed;
        
        // 블렌딩 업데이트
        player.UpdateBlendParameter(speed);
        // 속도에 따라 Idle → Walk → Run → Sprint 자동 블렌딩!
    }
}
```

### ✅ 테스트

1. Play 버튼 클릭
2. WASD로 이동
3. 속도에 따라 애니메이션 자동 블렌딩 확인

---

## 🎯 추가 MotionSet 예제

### 예제 1: 전투 콤보 (Sequential)

```
Motion Set Name: Combat Combo
Play Mode: Sequential

Motions:
  [0] Light Attack 1
  [1] Light Attack 2
  [2] Light Attack 3
  [3] Finisher

사용:
  player.Play(combatCombo);
  
  // Space 키로 다음 콤보
  if (Input.GetKeyDown(KeyCode.Space))
      player.PlayNextSequential();
```

### 예제 2: 8방향 이동 (Directional)

```
Motion Set Name: Directional Movement
Play Mode: Directional

Motions:
  [0] Forward      - Direction Angle: 90
  [1] Right        - Direction Angle: 0
  [2] Back         - Direction Angle: 270
  [3] Left         - Direction Angle: 180
  [4] ForwardRight - Direction Angle: 45
  [5] ForwardLeft  - Direction Angle: 135
  [6] BackLeft     - Direction Angle: 225
  [7] BackRight    - Direction Angle: 315

사용:
  Vector2 input = new Vector2(
      Input.GetAxis("Horizontal"),
      Input.GetAxis("Vertical")
  );
  
  if (input.magnitude > 0.1f)
      player.PlayByDirection(input.normalized);
```

### 예제 3: Idle 배리에이션 (Random)

```
Motion Set Name: Idle Variations
Play Mode: Random

Motions:
  [0] Idle Look Around
  [1] Idle Stretch
  [2] Idle Check Weapon

사용:
  // 5초마다 랜덤 Idle 재생
  InvokeRepeating(nameof(PlayRandomIdle), 0f, 5f);
  
  void PlayRandomIdle()
  {
      player.Play(idleVariationsSet);
  }
```

---

## 🔧 슬롯 그룹 설정 (선택)

같은 그룹의 몽타쥬가 서로 중단되도록 설정

### 설정 방법

```
1. Hierarchy에서 MontageSlotManager 찾기 (없으면 자동 생성)
2. Inspector:

Slot Groups:
  [0] Group: "CombatGroup"
      Slots:
        - FullBody
        - UpperBody
        
  [1] Group: "MovementGroup"
      Slots:
        - LowerBody
```

### 동작 예시

```
시나리오:
1. UpperBody에서 재장전 재생 중
2. UpperBody에서 공격 재생 요청
3. 같은 CombatGroup → 재장전 자동 중단
4. 공격 재생 시작
```

---

## 📊 시스템 선택 가이드

| 상황 | 사용 시스템 |
|------|-------------|
| 콤보 공격 (섹션별 제어) | **AnimationMontage** |
| 재장전 (Start→Loop→End) | **AnimationMontage** |
| 타임라인 이벤트 필요 | **AnimationMontage** (노티파이) |
| 이동 (속도 블렌딩) | **MotionSet** (Blend) |
| 8방향 이동 | **MotionSet** (Directional) |
| Idle 배리에이션 | **MotionSet** (Random) |
| 단순 콤보 | **MotionSet** (Sequential) |

---

## 🎮 키 바인딩 정리

### MontagePlayerExample
- `1`: 공격 몽타쥬 재생
- `2`: 특정 섹션부터 재생
- `3`: 재장전 몽타쥬 재생
- `J`: 섹션 점프
- `S`: 정지
- `+`: 속도 2배
- `-`: 속도 0.5배

### MotionSetPlayerExample
- `1`: Locomotion (Linear Blend)
- `2`: Combat Combo (Sequential)
- `3`: Directional (8방향)
- `4`: Idle Variations (Random)
- `5`: Strafe (Cartesian Blend)
- `WASD`: 이동/방향
- `Space`: 다음 콤보 (Combat 모드)

---

## ⚡ 자주 묻는 질문

### Q: AnimationSlot을 못 찾겠어요
```
A: Project 창 우클릭 → Create → Animation → Slot
   (ScriptableObject 에셋으로 생성됩니다)
```

### Q: 슬롯 애니메이션이 안 나와요
```
A: MontagePlayer의 Registered Slots에 슬롯 등록했는지 확인
   AnimationSlot의 Layer Weight가 0이 아닌지 확인
   Avatar Mask가 올바르게 설정되었는지 확인
```

### Q: MotionSet 블렌딩이 이상해요
```
A: Threshold 값이 올바른지 확인
   Blend Parameter Range 확인
   UpdateBlendParameter()를 매 프레임 호출하는지 확인
```

### Q: Montage와 MotionSet 중 어떤 걸 써야 하나요?
```
A: 
- 섹션별 제어 필요 → Montage
- 타임라인 이벤트 필요 → Montage (노티파이)
- 속도/방향 블렌딩 → MotionSet (Blend/Directional)
- 단순 순차/랜덤 → MotionSet (Sequential/Random)
```

---

## 📚 다음 단계

- **README.md**: 전체 기능 상세 설명
- **애니메이션_관련 문서**: 시스템 설계 문서
- **MontagePlayerExample.cs**: 코드 예제
- **MotionSetPlayerExample.cs**: 코드 예제

---

## 💡 팁

### 1. 에디터 프리셋 활용
MotionSet Editor에서 프리셋 버튼 클릭:
- 🏃 Locomotion
- ⚔️ Combat Combo
- 🧭 8방향 이동
- 😴 Idle 배리에이션

### 2. MotionSet에서 Montage 사용
```
Motion Data:
  - Source Type: Montage (Clip 대신)
  - Montage: (몽타쥬 에셋 할당)
  
→ 섹션 기능과 블렌딩을 함께 사용 가능!
```

### 3. 멀티 슬롯 활용
```
// 하체: 이동
lowerBodyPlayer.PlayMontage(runMontage);

// 상체: 사격 (동시 재생!)
upperBodyPlayer.PlayMontage(shootMontage);
```

### 4. 런타임 파라미터 조절
```csharp
// 블렌딩 파라미터 동적 변경
player.UpdateBlendParameter(currentSpeed);

// 재생 속도 변경
player.SetSpeed(1.5f);

// 슬롯 가중치 조절
montagePlayer.SetSlotWeight("UpperBody", 0.5f);
```

---

이제 시작할 준비가 되었습니다! 🎉
