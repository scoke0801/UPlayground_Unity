# BT Repoint 목록 (2026-06-19)

스코어러 표준화 마이그레이션의 마지막 갭. 아래 적들은 **intentWeights는 설정돼 있으나 raw BT 트리에 묶여 스코어링 시스템을 우회**하거나(휴머노이드), **원거리인데 멜리 스코어러 트리**를 쓰고 있다. 각 항목은 `BehaviorData_*.asset`의 `behaviorTree` 필드를 타겟 트리로 교체하면 된다.

> ⚠️ **Unity Inspector에서 교체 권장.** 텍스트 GUID 직접 편집은 behavior-changing이라 프로젝트 정책상 회피. Inspector에서 BehaviorData 에셋을 열고 `Behavior Tree` 슬롯에 타겟 `.asset`을 드래그하면 참조가 안전하게 갱신된다.
> 권장 절차: **1개 먼저 적용 → 플레이 모드에서 정상 로드/거동 확인 → 나머지 일괄 적용.**

## 타겟 트리 GUID

| 트리 | 경로 | GUID |
|---|---|---|
| **Balanced** (휴머노이드용 스코어러 멜리) | `Assets/10.Datas/AI/BehaviorTree/Generated/BT_EnemyBehavior_GroundMelee_Balanced.asset` | `812a888314740a84e84d3af763621df4` |
| **RangedKiter** (원거리 스코어러) | `Assets/10.Datas/AI/BehaviorTree/Generated/BT_EnemyBehavior_RangedKiter.asset` | `479484640d77450429b54eca3cecb03f` |

---

## A. 휴머노이드 12개 → Balanced

현재: `547eec6e48eb3d949afb7715414b42dd` (raw `Assets/BT_EnemyGroundMelee_Organic.asset`, 스코어러 미적용)
타겟: **Balanced** (`812a888314740a84e84d3af763621df4`)

`Assets/10.Datas/Actor/Enemy/BehaviorData/Humanoid/` 아래:
- BehaviorData_Bokusei
- BehaviorData_Hichi
- BehaviorData_Honoka
- BehaviorData_humanoid
- BehaviorData_Inori
- BehaviorData_Komoe
- BehaviorData_Lian
- BehaviorData_Lili
- BehaviorData_Nenmir
- BehaviorData_Reien
- BehaviorData_Sera
- BehaviorData_Siuha

> 이들은 `intentWeights`가 이미 연결돼 있어 Balanced로 옮기면 즉시 스코어링이 동작한다(현재는 raw 트리라 가중치가 사장됨).

---

## B. 원거리 적 → RangedKiter

| BehaviorData | 현재 트리 | 비고 |
|---|---|---|
| BehaviorData_skeleton_bow | `a04027...` (raw `Generated/BT_EnemyRangedKiter`) | raw → 스코어러 RangedKiter로 |
| Enemy_Random_F_Bow_001_Behavior | `8b0e12...` (Test_Aggressive **멜리** 스코어러) | 원거리인데 멜리 트리 |
| Enemy_Random_F_Bow_002_Behavior | `8b0e12...` (Test_Aggressive **멜리** 스코어러) | 원거리인데 멜리 트리 |
| **BehaviorData_lich** | `8b0e12...` (Test_Aggressive **멜리** 스코어러) | ⚠️ **판단 필요** — 카이터형이 아니라 캐스터/소환형이면 RangedKiter 부적합. 거동 확인 후 결정 |

타겟: **RangedKiter** (`479484640d77450429b54eca3cecb03f`)

---

## C. 참고 — 손대지 말 것

- `Test_Aggressive` (`8b0e12...`)에 묶인 나머지 ~26개 멜리 적은 **정상**(스코어러 멜리). 단 트리 이름이 `..._Test_...`라 정식 트리로 rename/승격 권장(별도 작업, 이번 범위 아님).
- Dummy / Monster_Bokusei 류는 intentWeights null = 의도적, 그대로 둔다.

---

## D. ⚠️ Tick LOD 상호작용 (B 원거리 적과 함께 검토)

같은 작업에서 `BehaviorTreeRunner`에 거리 기반 Tick LOD를 추가했다(원거리/화면 밖 적의 BT 평가 빈도 감쇠). 기본값: near 20m / far 45m / scale 2.

- **일반 멜리 적:** `lostTargetRadius≈15m` 밖에서 타겟을 놓으므로 LOD가 거의 발동 안 함 → 무해.
- **B의 RangedKiter 적:** 15~40m에서 교전하는 아키타입이라 LOD 밴드와 겹친다. near=20m로 교전거리는 풀 레이트로 덮었지만, **20m 밖으로 리포지셔닝하면 평가 간격이 최대 0.2s로 늘어** 거리 반응이 둔해질 수 있다.
- **권장:** RangedKiter로 repoint한 적은 플레이 확인 후, 둔하면 해당 프리팹의 `BehaviorTreeRunner._lodNearDistance`를 카이터 사거리 위로 올리거나 `_useDistanceLod`를 끈다.
