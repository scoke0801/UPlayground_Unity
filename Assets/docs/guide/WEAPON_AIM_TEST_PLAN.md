# 무기 끝 조준 보정 — 구현 및 사용자 테스트

상태: 코드·모델 설정 반영. Unity 에셋 로드, EditMode 실행, 전투 감각은 **사용자 테스트 대기**.

AI 보조 검사: `UPlayGround.Combat.Tests.csproj`와 종속 Actor 모듈 컴파일 오류 0개, 경고 26개. 경고는 기존 참조 DLL의 버전 충돌 등이며 결과는 `Temp/WeaponAimCompile.log`에 있다. 새 테스트 16개는 컴파일만 확인했고 Test Runner에서는 실행하지 않았다. 변경된 추적 파일의 `git diff --check`도 통과했다.

## 구현과 근거

- H09는 사용자 확인에 따라 Staff와 기존 `AbilitySet_PlayerStaff`를 연결했다. Reine은 기존 Spear와 AbilitySet을 유지한다. 공유 Ability/Payload의 수치와 기존 모션 매핑은 수정하지 않았다.
- `PlayerActorAnimator`의 `Weapon Aim` 설정으로 지정한 모델·무기만 활성화한다. 두 모델의 ParentConstraint 손측 source와 로컬 +Y 축을 사용한다. 모델 이름으로 분기하지 않는다.
- 기존 Animancer IK 패스에서 가슴 → 주손 상완 → 보조손 IK 순서로 보정한다. 무기의 이전 프레임 Transform 대신 손측 source와 Constraint offset으로 조준선을 구성한다. 팔 회전으로 조준 원점이 이동하는 것도 계산한다.
- 일반 공격·콤보·스킬과 대시 공격의 기존 타겟 선택을 재사용한다. 확정된 GAS 타겟이 있으면 그 위치를 우선한다. 그 외에는 공격 타겟의 Capsule 중심을 사용한다. 범위 밖 타겟을 새로 탐색하지 않는다.
- `BeginCollisionEvent`/`SpawnProjectileEvent`에 `weaponAimBlendIn`, `weaponAimBlendOut`, `suppressWeaponAim`을 추가했다. 기존 MotionSet 에디터의 반사 기반 필드 표시로 편집할 수 있다. 타격 이벤트의 실제 시간 링크·클립 누적 시간을 사용하며 별도 레이어 이벤트와 명시적 광역 판정은 제외한다.
- 근접은 판정 구간에, 투사체는 발사 시점에 강도를 높인다. Forward/LockOnTarget 투사체는 같은 조준점과 타겟을 사용한다. 지면 지정·텔레그래프 투사체의 동작은 유지한다.
- 보정 기본값은 가슴 최대 10도, 팔 최대 30도, 35도부터 감쇠하여 75도에서 비활성화한다. 플레이 확인 전의 초기 조율값이다. 원본이 이미 옆이나 뒤를 향하는 휘두르기를 강제로 정면 찌르기로 바꾸지 않는다.

설계 참고: [Animancer IK 패스와 가중치](https://kybernetik.com.au/animancer/docs/manual/ik/), [Unity Multi-Aim의 조준축·가중치·제한](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.3/manual/constraints/MultiAimConstraint.html). Animation Rigging 패키지는 추가하지 않았다.

## 준비

1. Unity 6000.3.21f1에서 스크립트 재컴파일 후 Console 오류가 없는지 확인한다.
2. `UPlayGround > 툴 런처 > 월드 / 맵 > 전투 테스트 맵 열기`로 `Assets/01.Scenes/Test/CombatTestMap.unity`를 연다.
3. 평소 사용하는 키보드·마우스 또는 게임패드로 시작한다. 기존 저장 데이터 초기화는 필요하지 않다. 테스트 준비 창에서 적을 생성하고 H09 또는 Reine으로 교체한다.
4. H09가 지팡이를 꺼내고 일반 공격이 실행되는지 먼저 확인한다. 두 모델의 PlayerActorAnimator에 `Weapon Aim` 설정과 해당 무기의 Constraint 참조가 있어야 한다.

## 실행 항목

| ID | 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| A01 | Test Runner의 EditMode에서 `UPlayGround.Combat.Tests.WeaponAimTests` 실행 | 16개 모두 통과. 0개 검색 결과는 통과가 아니다. 각도 제한·피벗 이동·클립 누적 시간·광역 제외·발사 후 복귀·두 모델 연결을 검사한다. |
| A02 | H09·Reine 각각 정면의 가까운 적을 락온하고 약공격 연계 | 타격 전 상체·팔이 부드럽게 보정되고 무기 끝이 몸통 쪽을 향한다. 무기가 손에서 분리되거나 팔꿈치가 뒤집히면 실패. 데미지·히트스톱·콤보 입력도 기존처럼 작동해야 한다. |
| A03 | 적을 약 20~30도 옆에 둔 상태와 높낮이가 다른 위치에서 공격 | 몸 전체 회전에 이어 상체가 남은 오차를 줄인다. 발 위치가 IK 때문에 이동하거나 허리가 과도하게 꺾이면 실패. |
| A04 | 크게 휘두르는 공격·회전베기·반동 동작 확인 | 원래 동작과 궤적이 남아야 한다. 해당 모션이 꺾이면 Collision 이벤트의 `suppressWeaponAim`으로 제외하고 모션명을 기록한다. |
| A05 | 공격 중 회피·피격·타겟 사망·모델 교체, 이어서 타겟 없는 공격 | 이전 타겟을 계속 보거나 새 모델의 팔이 돌아간 채 남으면 실패. 중단 직후 짧은 복귀 블렌드는 정상이다. |
| A06 | 대시 공격과 사용 가능한 투사체 스킬을 락온/비락온 각각 확인 | 대시 공격도 같은 타겟을 사용한다. 투사체 사용 시 무기 조준과 발사 방향이 같은 목표를 향한다. 투사체가 없는 공격은 해당 없음으로 기록한다. |
| A07 | 히트스톱·일시정지·재개와 빠른 연속 공격 확인 | 정지 중 보정이 누적되지 않고 재개 시 떨림·한 프레임 지연·급회전이 없어야 한다. Console 예외는 별도 실패다. |

결과는 `ID / 통과·실패·미수행·해당 없음 / 캐릭터·공격·입력 장치 / 실제 증상 / 필요한 영상·Console 로그`로 전달한다. A02~A07은 화면과 조작감에 대한 사용자 판정이 필요하다.

## 범위와 남은 확인

- 현재 기본 공격 타임라인만 지원한다. 별도 병렬 레이어 공격, 독립적인 양손 무기 조준, 근본적으로 방향이 다른 애니메이션의 재저작은 포함하지 않는다.
- 양손 관계는 원본 애니메이션을 기준으로 보존한다. 원본 그립 오차 자체를 없애지는 않으며, 보조손 도달 한계는 A02/A03에서 확인해야 한다.
- +Y 조준축은 두 무기의 로컬 구조를 근거로 설정했지만 실제 클립 재생에서 끝 방향을 확인해야 한다. 반대 끝을 향하면 모델 설정의 축을 수정한다.
- 두 모델 프리팹은 기존 `.gitignore` 대상이다. 설정은 로컬 파일에 반영되어 있으며 일반 Git diff/커밋에 자동 포함되지 않는다. 배포 시 기존 모델 에셋 전달 경로에 포함해야 한다.
- 새 `.cs`는 생성 프로젝트의 목록 갱신 전에도 포함되도록 `Temp/WeaponAimCompile.targets`를 사용해 보조 컴파일했다. 이는 Unity 컴파일·에셋 임포트·Test Runner 실행을 대체하지 않는다.
- 모듈 경계·직렬화 타입 이동·Player 전용 설정 변경이 없어 Windows Player 빌드는 수행하지 않는다. 실제 플레이 검증은 `TEST_VALIDATION_GUIDE.md`의 사용자 직접 테스트 운영에 따른다.
