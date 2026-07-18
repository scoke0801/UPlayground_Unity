# Character Weight 구현 스펙

## 1. 목표

캐릭터 고유 무게가 이동, 공격 템포, 피해, 브레이크, 회피 무적, 회복 보상에 일관되게 반영되도록 한다. P0는 경량·표준·중량 세 프로필만 구현하고 5단계 확장을 전제로 한 조기 복잡도를 만들지 않는다.

---

## 2. P0 프로필

| 등급 | 캐릭터 | 이동 | 공격 템포 | 타격 피해 | 브레이크 | 회피 무적 | 회복 방향 |
|---|---|---|---|---|---|---|---|
| 경량 | Honoka | 1.15 | 1.25 | 0.70 | 0.55 | 0.45초 | 다단 히트 기반 소량 오브 |
| 표준 | Bokusei | 1.00 | 1.00 | 1.00 | 1.00 | 0.35초 | 혼합 기준점 |
| 중량 | H09 | 0.82 | 0.68 | 1.80 | 2.10 | 0.24초 | 브레이크 특수공격 대량 회복 |

값은 시작점이며 코드 상수가 아니다. 모든 값은 프로필 에셋에서 조정한다.

---

## 3. 기존 코드 접점

| 기존 타입 | 현재 상태 | 적용 방향 |
|---|---|---|
| `CharacterModelData` | `characterType`, 무기, 공격 데이터 보유 | 무게 프로필 참조 추가 권장 |
| `ActorStatSO` | `MoveSpeed`, `InvincibleDuration` 등 기본 스탯 보유 | 영구 기본값 유지. 무게 배율의 원본으로 사용 |
| `ActorStatContainer` | 런타임 스탯 컨테이너 | 캐릭터 전환 시 이동 배율 적용 경로 확인 |
| `PlayerSwapBehaviour.SwapTo` | 모델 활성화 후 `PlayerActor.RefreshForCharacter` 호출 | 프로필 재적용 시점 |
| Ability Payload의 `AbilityAttackInfo` / `HitPhaseData` | 피해·브레이크 데이터 보유 | 실행 시 무게 배율을 한 번 적용 |
| `PoiseStat`, `MonsterBreakGauge` | 포이즈·브레이크 처리 | 최종 브레이크 값에 프로필 배율 적용 |
| 플레이어 회피 상태 | 회피 무적 구간 소유 | 고정값 대신 프로필의 초 단위 값 조회 |
| `VitalOrbManager` | 바이탈 오브 생성 관리 | 히트/브레이크 보상 정책 연결 |

`ActorStatSO`에는 공격속도와 브레이크 피해 전용 `StatType`이 없다. P0에서 enum을 늘리기보다 `CharacterWeightProfileSO`가 전투 파생 배율을 소유한다.

---

## 4. 신규 데이터

### `CharacterWeightProfileSO`

```csharp
public enum CharacterWeightClass
{
    Light,
    Standard,
    Heavy,
}

public sealed class CharacterWeightProfileSO : ScriptableObject
{
    public CharacterWeightClass weightClass;
    public float moveSpeedMultiplier = 1f;
    public float attackTempoMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float breakDamageMultiplier = 1f;
    public float dodgeIFrameSeconds = 0.35f;
    public VitalRecoveryPolicySO recoveryPolicy;
}
```

`CharacterModelData`에 다음 참조를 추가한다.

```csharp
[Header("Cycle Weight")]
public CharacterWeightProfileSO weightProfile;
```

- P0 세 캐릭터는 참조가 필수다.
- 참조가 없으면 표준 프로필로 폴백하고 개발 빌드에서 경고한다.
- `characterType`별 별도 딕셔너리를 새 매니저에 만들지 않는다. 모델 데이터가 자신의 프로필을 직접 참조한다.

---

## 5. 적용 순서

```text
PlayerActor.RefreshForCharacter(model)
  -> 기존 공격/장비/애니메이터 갱신
  -> CharacterWeightRuntime.Apply(model.weightProfile)
  -> 이동 배율 갱신
  -> 공격 실행 컨텍스트의 피해/브레이크 배율 갱신
  -> 회피 i-frame 값 갱신
  -> 회복 정책 갱신
```

프로필은 캐릭터 스왑 때마다 완전히 교체한다. 이전 캐릭터의 런타임 수정자가 남지 않도록 토큰 또는 소유자 ID 기반으로 제거 후 적용한다.

---

## 6. 항목별 구현 규칙

### 이동속도

- 기존 `MoveSpeed` 최종값에 `moveSpeedMultiplier`를 한 번 곱한다.
- KCC 상태별 `UpdateVelocity`마다 중복 곱하지 않는다.
- 걷기·달리기·공중 제어가 동일 프로필을 참조하되 상태 고유 배율은 유지한다.

### 공격 템포

`attackTempoMultiplier`를 Animancer 재생 속도에 무조건 곱하는 방식은 금지한다. MotionEvent, 캔슬 윈도우, 히트박스, 루트모션이 함께 빨라져야 하기 때문이다.

P0 권장 방식:

1. 캐릭터별 MotionSet 저작 속도를 기준으로 둔다.
2. 프로필 값은 밸런스 분석과 검증 메타데이터로 먼저 사용한다.
3. 런타임 배속이 필요하면 MotionSet 전체 시간축과 이벤트가 같이 스케일되는 기존 Animancer 경로를 확인한 뒤 한 지점에서 적용한다.

즉, 공격 템포는 숫자만 넣어 완료되는 항목이 아니다. 실제 10초 DPS와 조작 가능 시간을 검증해야 한다.

### 피해와 브레이크

```text
HitPhaseData 원본 값
  -> 장비/레벨/스킬 수정
  -> weight damageMultiplier 또는 breakDamageMultiplier
  -> 방어/반응 파이프라인
```

- 원본 ScriptableObject를 런타임에 변경하지 않는다.
- 히트마다 프로필을 다시 찾지 말고 현재 캐릭터 런타임 컨텍스트에 캐시한다.
- 잔류 공격은 스냅샷을 만든 outgoing 캐릭터의 프로필을 사용한다. 스왑 후 active 프로필을 읽으면 안 된다.

### 회피 무적

- `dodgeIFrameSeconds`는 회피 상태 진입 시 스냅샷한다.
- 스왑 회피의 무적시간은 `PartyConfigSO.swapEvadeIFrameDuration`을 유지하며 무게 프로필로 바꾸지 않는다.
- 일반 회피와 스왑 회피를 같은 값으로 묶지 않는다.

### 회복 정책

P0는 생성 원인을 두 종류로 기록한다.

| 원인 | 경량 | 표준 | 중량 |
|---|---|---|---|
| 일반 유효 히트 | 높은 빈도·낮은 회복량 | 기준 | 낮거나 없음 |
| 브레이크 특수공격 | 낮은 추가 보상 | 기준 | 높은 단발 보상 |

오브 생성 개수와 회복량을 동시에 크게 올리지 않는다. 정책 SO에서 `spawnChance`, `orbCount`, `healScale`을 분리한다.

### 중량 슈퍼아머

- 공격 전체가 아니라 공격 데이터에 표시된 구간만 적용한다.
- 피해는 받고 경직만 억제한다.
- 잡기, 다운, 강제 브레이크 반응은 무시할 수 있다.
- P0 전용 데이터 연결이 준비되지 않으면 슈퍼아머를 빼고 무게 경제부터 검증한다.
- 현재 P0에는 저작 구간 연결이 없으므로 슈퍼아머 정책 필드를 노출하지 않는다. 실제 연결을 구현할 때 데이터와 검증을 함께 추가한다.

---

## 7. 에디터 검증

- P0 캐릭터의 `weightProfile` 누락
- 배율이 0 이하인 프로필
- 회피 무적 0.1~0.6초 범위를 벗어난 값
- 중량 프로필인데 브레이크 보상 정책이 없는 경우
- 경량 프로필인데 일반 히트 오브 정책이 없는 경우
- 같은 프로필을 의도치 않게 공유하는 서로 다른 무게 캐릭터

Balance Designer 추출 데이터에 무게 클래스와 모든 배율을 포함한다.

---

## 8. 텔레메트리

캐릭터별로 다음을 기록한다.

- 전투 활성 시간
- 가한 피해와 초당 피해
- 브레이크 피해와 브레이크 발생 횟수
- 피격 횟수와 받은 피해
- 일반 히트 오브 생성·회복량
- 브레이크 보상 오브 생성·회복량
- 일반 회피 시도·성공
- 해당 캐릭터로 스왑한 횟수

세 등급의 회복 원인 분포가 유사하면 5단계 확장을 진행하지 않는다.

---

## 9. 완료 조건

1. 세 캐릭터 스왑 시 프로필이 즉시 바뀌고 이전 수정자가 남지 않는다.
2. 동일 공격 데이터라도 현재 캐릭터 프로필에 따라 피해·브레이크 결과가 달라진다.
3. 일반 회피 i-frame이 프로필 값과 일치하고 스왑 회피 값은 변하지 않는다.
4. 잔류 공격이 outgoing 캐릭터 프로필을 유지한다.
5. 경량은 일반 히트, 중량은 브레이크 특수공격에서 회복 비중이 높다.
6. 프로필 누락과 비정상 값이 에디터 검증에서 발견된다.
