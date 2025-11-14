# 빠른 시작 가이드

## 5분 안에 애니메이션 몽타쥬 설정하기

### 1단계: 몽타쥬 생성 (30초)
1. Project 창에서 우클릭
2. `Create > Animation > Montage`
3. 이름: "AttackMontage"

### 2단계: 섹션 설정 (2분)
몽타쥬를 선택하고 Inspector에서:

```
Montage Name: Attack Combo
Slot Name: FullBody

Sections:
  [0] Section: Attack1
      - Clip: (공격1 애니메이션)
      - Fade In: 0.2
      - Play Rate: 1.0
      - Next Section: Attack2
      
  [1] Section: Attack2
      - Clip: (공격2 애니메이션)
      - Fade In: 0.2
      - Play Rate: 1.0
      - Next Section: Attack3
      
  [2] Section: Attack3
      - Clip: (공격3 애니메이션)
      - Fade In: 0.2
      - Play Rate: 1.2
      - Next Section: (비워둠 - 종료)
```

### 3단계: 캐릭터에 적용 (1분)
캐릭터 GameObject 선택:
1. `Add Component > Animancer Component`
2. `Add Component > Montage Player`
3. `Add Component > Montage Player Example`

### 4단계: 테스트 (1분)
1. Play 버튼 클릭
2. `1` 키를 눌러 몽타쥬 재생
3. `S` 키를 눌러 정지

## 노티파이 추가하기

공격 타이밍에 이벤트를 추가하려면:

```
Section: Attack1
Notifies:
  [0] Notify Name: EnableCollision
      Trigger Time: 0.3 (애니메이션의 30% 지점)
      
  [1] Notify Name: DealDamage
      Trigger Time: 0.5 (애니메이션의 50% 지점)
```

코드에서 처리:
```csharp
void OnNotifyTriggered(string notifyName)
{
    if (notifyName == "EnableCollision")
    {
        weaponCollider.enabled = true;
    }
    else if (notifyName == "DealDamage")
    {
        // 데미지 처리
    }
}
```

## 재장전 몽타쥬 예제

```
Montage Name: Reload
Slot Name: UpperBody

Sections:
  [0] Section: Start
      - Clip: (탄창 빼기)
      - Next Section: Loop
      
  [1] Section: Loop
      - Clip: (탄 장전)
      - Loop: ✓ (체크)
      - Next Section: (비워둠)
      
  [2] Section: End
      - Clip: (탄창 넣기)
```

사용:
```csharp
// 재장전 시작
montagePlayer.PlayMontage(reloadMontage);

// 필요한 만큼 자동 루프됨...

// 완료 시 End 섹션으로
montagePlayer.JumpToSection("End");
```

## 슬롯 그룹 설정

Hierarchy에서 `MontageSlotManager` 오브젝트 찾기:
- 없으면 자동 생성됨

Inspector에서 슬롯 그룹 설정:
```
Slot Groups:
  [0] Group Name: CombatGroup
      Slots:
        - FullBody
        - UpperBody
        
  [1] Group Name: MovementGroup
      Slots:
        - LowerBody
```

이제 같은 그룹의 몽타쥬는 자동으로 이전 것을 중단시킵니다!

## 다음 단계

- README.md에서 전체 기능 확인
- MontagePlayerExample.cs에서 더 많은 예제 코드 참고
- 커스텀 슬롯 그룹 만들기
- 복잡한 콤보 시스템 구현하기
