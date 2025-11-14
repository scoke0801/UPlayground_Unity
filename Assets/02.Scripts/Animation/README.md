# Animancer 기반 애니메이션 몽타쥬 시스템

언리얼 엔진의 애니메이션 몽타쥬 시스템을 Unity의 Animancer로 구현한 시스템입니다.

## 주요 기능

### 1. 애니메이션 몽타쥬 (AnimationMontage)
- 여러 애니메이션 클립을 하나의 에셋으로 결합
- 섹션 단위로 애니메이션 분할 및 관리
- 슬롯 이름을 통한 애니메이션 그룹화

### 2. 몽타쥬 섹션 (MontageSection)
- 개별 애니메이션 클립과 재생 설정
- 페이드 인/아웃 지속 시간 설정
- 재생 속도 조절
- 루프 설정
- 다음 섹션 지정 (순차 또는 특정 섹션으로 점프)
- 노티파이 이벤트

### 3. 몽타쥬 플레이어 (MontagePlayer)
- 몽타쥬 재생, 정지, 섹션 점프
- 재생 속도 제어
- 이벤트 시스템 (시작, 종료, 섹션 변경, 노티파이)

### 4. 슬롯 그룹 시스템 (MontageSlotManager)
- 슬롯 그룹 관리
- 같은 그룹 내 몽타쥬 자동 중단 (인터럽트 기능)
- 여러 슬롯 정의 가능 (UpperBody, LowerBody, FullBody 등)

## 사용 방법

### 1. 몽타쥬 에셋 생성
```
1. Project 창에서 우클릭
2. Create > Animation > Montage 선택
3. 몽타쥬 이름 설정
```

### 2. 몽타쥬 설정
```
1. 생성한 몽타쥬 에셋을 선택
2. Inspector에서 다음을 설정:
   - Montage Name: 몽타쥬 이름
   - Slot Name: 슬롯 이름 (예: "FullBody", "UpperBody")
   - Sections: 섹션 추가 및 설정
     * Section Name: 섹션 이름
     * Clip: 애니메이션 클립
     * Fade In/Out Duration: 페이드 시간
     * Play Rate: 재생 속도
     * Loop: 루프 여부
     * Next Section: 다음 섹션 이름
     * Notifies: 노티파이 추가
```

### 3. 캐릭터에 컴포넌트 추가
```csharp
// 필요한 컴포넌트
- AnimancerComponent (Animancer 플러그인)
- MontagePlayer
```

### 4. 코드에서 사용
```csharp
// 몽타쥬 재생
montagePlayer.PlayMontage(attackMontage);

// 특정 섹션부터 재생
montagePlayer.PlayMontage(reloadMontage, "Start");

// 섹션 점프
montagePlayer.JumpToSection("Loop");

// 재생 속도 변경
montagePlayer.SetPlayRate(1.5f);

// 정지
montagePlayer.StopMontage();

// 이벤트 구독
montagePlayer.OnMontageStarted += OnMontageStarted;
montagePlayer.OnNotifyTriggered += OnNotifyTriggered;
```

## 예제 시나리오

### 공격 콤보 시스템
```
몽타쥬: AttackCombo
섹션:
- Attack1 -> Attack2로 자동 전환
- Attack2 -> Attack3으로 자동 전환
- Attack3 -> 종료

노티파이:
- "EnableCollision" (0.3): 무기 콜라이더 활성화
- "DealDamage" (0.5): 데미지 처리
- "DisableCollision" (0.7): 무기 콜라이더 비활성화
```

### 재장전 시스템
```
몽타쥬: Reload
섹션:
- Start (재장전 시작)
- Loop (탄창 장전 - 루프 가능)
- End (재장전 완료)

사용:
1. Start 섹션 재생
2. 필요한 만큼 Loop 섹션 반복
3. End 섹션으로 점프하여 종료
```

### 슬롯 그룹 인터럽트
```
슬롯 그룹: CombatGroup
슬롯: FullBody, UpperBody

시나리오:
1. FullBody 슬롯에서 재장전 몽타쥬 재생 중
2. UpperBody 슬롯에서 근접 공격 몽타쥬 재생 요청
3. 같은 CombatGroup이므로 재장전 자동 중단
4. 근접 공격 몽타쥬 재생
```

## 언리얼과의 비교

| 기능 | 언리얼 | 이 구현 |
|------|--------|---------|
| 섹션 시스템 | ✅ | ✅ |
| 슬롯 시스템 | ✅ | ✅ |
| 슬롯 그룹 | ✅ | ✅ |
| 노티파이 | ✅ | ✅ |
| 자식 몽타쥬 | ✅ | ❌ (추가 구현 필요) |
| 블렌드 스페이스 | ✅ | ❌ (Animancer 기본 기능 사용) |

## 확장 가능성

### 자식 몽타쥬 구현
```csharp
public class ChildMontage : AnimationMontage
{
    [SerializeField] private AnimationMontage parentMontage;
    // 부모 설정을 상속받아 특정 섹션만 오버라이드
}
```

### 몽타쥬 블렌딩
```csharp
// 여러 몽타쥬를 동시에 재생하여 블렌딩
public class MultiSlotMontagePlayer : MonoBehaviour
{
    private Dictionary<string, MontagePlayer> slotPlayers;
    
    public void PlayMontageOnSlot(AnimationMontage montage, string slot)
    {
        // 슬롯별로 독립적인 몽타쥬 재생
    }
}
```

## 주의사항

1. **Animancer 플러그인 필요**: 이 시스템은 Animancer 플러그인을 기반으로 합니다.
2. **섹션 타이밍**: 여러 슬롯을 사용할 경우 애니메이션 길이를 맞춰야 합니다.
3. **성능**: 많은 노티파이를 사용할 경우 성능에 영향을 줄 수 있습니다.

## 키 바인딩 (MontagePlayerExample)

- `1`: 공격 몽타쥬 재생
- `2`: 특정 섹션부터 재생
- `3`: 재장전 몽타쥬 재생
- `J`: 섹션 점프
- `S`: 몽타쥬 정지
- `+`: 재생 속도 증가 (2x)
- `-`: 재생 속도 감소 (0.5x)
