# ActorAnimationMotionSet 공용 모션 (Fallback) 시스템 가이드

## 개요

휴머노이드 적 몬스터들이 Idle·Walk·Hit·Die 등 **공통 애니메이션을 하나의 ScriptableObject에서 공유**하고, 개별 몬스터 SO는 고유 공격 클립만 정의하도록 하는 데이터 상속 시스템입니다.

### 핵심 특징

- **Fallback 체인**: `ActorAnimationMotionSet`에 `fallbackMotionSet` 필드를 추가. AnimKey 탐색 시 자신 → fallback → fallback의 fallback 순으로 최대 8단계 탐색
- **순환 참조 방지**: depth > 8이면 null 반환
- **완전 하위 호환**: fallback = null이면 기존 동작과 동일
- **커스텀 인스펙터**: 체인 전체 키를 카테고리별로 표시. 자체 키(녹색)와 상속 키(회색)를 구분하여 표시
- **Override 생성 워크플로**: 상속 키 옆 버튼 한 번으로 새 MotionSetAsset 생성 후 에디터 창에서 즉시 편집 가능

---

## 아키텍처

```
Skeleton_Sword_MotionAsset  (ActorAnimationMotionSet)
  │  [Attack_1, Attack_2 ...]     ← 자체 정의 (검 공격)
  └─ fallbackMotionSet ──────────► Humanoid_Common_MotionAsset
                                     [Idle, Walk, Run, Hit_F/B/L/R, Die ...]
                                     └─ fallbackMotionSet: null

Skeleton_Bow_MotionAsset
  │  [Attack_1 (활 공격) ...]
  └─ fallbackMotionSet ──────────► Humanoid_Common_MotionAsset (동일 에셋 공유)

Lich_MotionAsset
  │  [Skill_1, Skill_2 ...]
  └─ fallbackMotionSet ──────────► Humanoid_Common_MotionAsset
```

**GetMotionSet 탐색 순서:**

```
1. 자신의 motionSets 딕셔너리에서 키 탐색
2. null 또는 미등록이면 fallbackMotionSet.GetMotionSet(key, depth+1)
3. depth > 8이면 null 반환 (순환 참조 차단)
```

### 파일 구조

```
Assets/02.Scripts/Data/Actor/Animation/
├── ActorAnimationMotionSet.cs          # fallback 필드 + 체인 탐색
├── PlayerActorAnimationMotionSet.cs    # 플레이어 전용 (WeaponType → ActorAnimationMotionSet)
├── MotionSetAsset.cs                   # 단일 MotionSet 래퍼 SO
└── Editor/
    ├── ActorAnimationMotionSetEditor.cs  # 커스텀 인스펙터 (신규)
    ├── MotionSetAssetEditor.cs
    ├── MotionSetWindow.cs              # 애니메이션 에디터 창
    ├── LocoMotionSetupWindow.cs        # 클립 일괄 등록 창
    └── MotionSetDrawer.cs

Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/
├── Humanoid/
│   └── Humanoid_Common_MotionAsset.asset   # 공용 모션 (신규 생성 필요)
├── Skeleton/
│   ├── Skeleton_Sword_MotionAsset.asset    # fallback → Humanoid_Common
│   └── Skeleton_Bow_MotionAsset.asset
└── Lich/
    └── Lich_MotionAsset.asset
```

---

## 핵심 클래스

### `ActorAnimationMotionSet`

`AnimKey → MotionSetAsset` 딕셔너리를 보유하는 ScriptableObject.

| 필드 | 타입 | 설명 |
|------|------|------|
| `fallbackMotionSet` | `ActorAnimationMotionSet` | 이 SO에 없는 키를 여기서 탐색 |
| `motionSets` | `SerializedDictionary<AnimKey, MotionSetAsset>` | AnimKey → 클립 매핑 |

```csharp
// 핵심 API
public MotionSet GetMotionSet(AnimKey key, int depth = 0)
// depth > 8이면 null 반환 (순환 참조 방지)
// 자신에 없으면 fallbackMotionSet?.GetMotionSet(key, depth + 1)
```

**커스텀 인스펙터 (`ActorAnimationMotionSetEditor`) 기능:**

| 표시 | 색상 | 제공 버튼 |
|------|------|-----------|
| 자체 정의 키 | 녹색 배경 | ObjectField + `선택`(Ping) + `열기`(에디터 창) + `×`(삭제) |
| Fallback 상속 키 | 회색 배경 | `↑ 출처SO명` 표시 + `Override 생성` |

카테고리 Foldout: 이동 / 공격 / 강공격 / 대시 공격 / 점프 공격 / 스킬 / 차지·피니시 / 피격·사망 / 기타

---

## 셋업 방법

### 1. Humanoid 공용 에셋 생성

1. `Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/` 폴더 생성
2. 우클릭 → `Create > UPlayGround/ActorData/Motion/Actor` → `Humanoid_Common_MotionAsset` 생성
3. 인스펙터에서 `+ 모션 키 추가` 버튼 → Idle, Walk, Run, Hit_F, Hit_B, Hit_L, Hit_R, Die, Knockback, Knockdown 등 선택

### 2. LocoMotionSetupWindow로 기본 클립 일괄 등록

1. 메뉴: `UPlayGround > Util > Locomotion Motion Setup`
2. 스캔 폴더: FBX가 있는 `Base Move` 폴더 경로 입력
3. 등록 대상: `Humanoid_Common_MotionAsset` SO 드래그
4. `폴더 스캔` → 결과 확인 → `MotionSetAsset 생성 / 업데이트`
5. Walk_Slow / Walk / Run 8방향 + Stop / TurnInPlace 클립이 자동 등록됨

### 3. 각 몬스터 SO에 Fallback 연결

1. 적 몬스터의 `ActorAnimationMotionSet` 선택 (예: `Skeleton_Sword_MotionAsset`)
2. 인스펙터 상단 `Fallback MotionSet` 필드에 `Humanoid_Common_MotionAsset` 드래그
3. 이제 Idle·Walk·Hit 등은 Humanoid_Common에서 자동으로 가져옴
4. 중복 키가 있다면 해당 항목 옆 `×` 버튼으로 제거

### 4. 고유 공격 모션 등록

1. 몬스터 SO에서 `+ 모션 키 추가` → `Attack_1` 선택
2. ObjectField에 전용 MotionSetAsset 드래그 (또는 빈칸으로 두고 나중에 연결)
3. `열기` 버튼으로 MotionSetEditorWindow에서 클립·이벤트 편집

---

## 사용 예시

### 상속 키 Override 생성

```
인스펙터에서 상속된 "Idle" 항목 우측 [Override 생성] 클릭
→ 저장 경로 다이얼로그 → Skeleton_Sword_MotionAsset_Idle.asset 생성
→ MotionSetEditorWindow에서 자동으로 열림
→ 다른 Idle 클립 설정 완료
→ 이후 Skeleton_Sword는 자체 Idle, 나머지는 Humanoid_Common 사용
```

### 런타임 코드 변경 없음

```csharp
// 기존 코드 그대로 동작
gameActor.Animator.PlayMotion(AnimKey.Idle, 0.25f);
// → 내부적으로 ActorAnimationMotionSet.GetMotionSet(AnimKey.Idle)
//   자신에 없으면 → fallback.GetMotionSet(AnimKey.Idle) 탐색
```

### 다단계 체인 예시 (필요 시)

```
Human_Bandit_MotionAsset
  └─ fallback → Human_Common_MotionAsset
                  └─ fallback → Humanoid_Universal_MotionAsset
                                  └─ fallback: null  (최대 8단계)
```

---

## 에디터 도구

### LocoMotionSetupWindow

메뉴: `UPlayGround > Util > Locomotion Motion Setup`

| 기능 | 설명 |
|------|------|
| 스캔 폴더 | FBX Model 에셋을 재귀 탐색 |
| InPlace 버전 우선 | 동명 일반/InPlace 클립 중 선택 |
| 지원 패턴 | Walk_Slow/Walk/Run 8방향 Base + Stop + TurnInPlace |
| 일괄 생성 | MotionSetAsset 파일 생성 후 대상 SO에 자동 등록 |

**인식 파일명 패턴 (Base 클립):**

```
Walk_Slow_F, Walk_Slow_B, Walk_Slow_B_L45, Walk_Slow_B_R45
Walk_Slow_F_L45, Walk_Slow_F_R45, Walk_Slow_F_L90_A, Walk_Slow_F_R90_A
Walk_F, Walk_B, Walk_B_L45, Walk_B_R45, Walk_F_L45, Walk_F_R45
Walk_F_L90_A, Walk_F_R90_A
Run_F, Run_B, Run_B_L45, Run_B_R45, Run_F_L45, Run_F_R45
Run_F_L90_A, Run_F_R90_A
```

### ActorAnimationMotionSetEditor (커스텀 인스펙터)

`ActorAnimationMotionSet` SO를 선택하면 자동 적용.

| UI 요소 | 기능 |
|---------|------|
| Fallback MotionSet 필드 | 체인 참조 연결 |
| 카테고리 Foldout | 이동/공격 등 8개 카테고리로 분류 |
| `선택` 버튼 | Project 창에서 해당 에셋 Ping |
| `열기` 버튼 | MotionSetEditorWindow에서 바로 열기 |
| `Override 생성` | Fallback에서 상속된 키를 이 SO에서 직접 정의 |
| `×` 버튼 | 해당 키 항목 삭제 (확인 다이얼로그 포함) |
| `+ 모션 키 추가` | 미사용 AnimKey를 카테고리별 GenericMenu로 선택 추가 |

---

## 주의 사항

- **순환 참조 금지**: A.fallback = B, B.fallback = A 설정 시 depth 한도(8)까지 탐색 후 null 반환. 실제로 설정하지 않도록 주의.
- **null MotionSetAsset**: 딕셔너리에 키가 있어도 값이 null이면 탐색 실패로 간주하고 fallback으로 넘어감. 아직 클립을 연결하지 않은 상태에서도 fallback이 정상 동작.
- **fallback 변경 후 Inspector Repaint**: fallback SO를 변경하면 인스펙터가 즉시 갱신되어 상속 키 목록이 바뀜.

---

## 확장 포인트

- **3단계 이상 체인**: `족속 공통 → 휴머노이드 공통 → 범용` 순으로 3단계 체인도 depth 범위 내에서 지원
- **플레이어 확장 불필요**: `PlayerActorAnimationMotionSet`은 `WeaponType → ActorAnimationMotionSet`을 이미 관리하므로 별도 fallback 설정 불필요
- **신규 AnimKey 추가**: `AnimKey.cs`에 값 추가 후 커스텀 인스펙터의 `KEY_RANGES` 배열에 그룹을 추가하면 새 카테고리로 분류됨
