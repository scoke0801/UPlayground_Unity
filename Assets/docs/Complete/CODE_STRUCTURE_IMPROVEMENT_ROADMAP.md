# 코드 구조 개선 로드맵

> 작성일: 2026-07-11
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: 정비 계획(미실행). 코드 품질이 아니라 **구조 관리(정리·경계·문서 동기화)** 축의 개선 항목을 우선순위화한다.
> 관련 문서: [PROJECT_SYSTEM_IMPROVEMENT_EXECUTION_PLAN.md](PROJECT_SYSTEM_IMPROVEMENT_EXECUTION_PLAN.md) — 비동기 초기화·씬 전환·asmdef 등 시스템 축 개선은 해당 문서가 담당. 본 문서와 중복 항목(asmdef)은 §7에서 관계만 정리한다.

---

## 구현 진행 현황

### 2026-07-11 §1~§5 일괄 실행 (asmdef 제외)

| 항목 | 상태 | 비고 |
|------|------|------|
| §1 폴더 표류 정리 | 완료 | 빈 폴더 4개 삭제(Debug/Gizmo, BehaviorTree, Diagnostics 포함), KnockbackTestBed→Debugging/, SceneManager partial 합류, NpcActor→Object/Npc/, md 3개→Assets/docs/ (저장소 루트가 Assets라 루트 docs/는 버전관리 밖 → Assets/docs 선택). AI/Debugging은 AI 도메인 전용으로 유지 |
| §2 CLAUDE.md 재동기화 | 완료 | 매니저 28개 등록순 반영, 핸들러 재편 명시, EnemyStatsSO→ActorStatSO, 플레이어 상태 24개, 네임스페이스·UI 네이밍·partial 규약 명문화 |
| §3 네임스페이스 | 완료 | 무네임스페이스 97개(BOM 오탐 제외 실측치) 전부 부여. 개명: UPlayGround.Object/FX→UPlayGround.Particle, Game.Input→UPlayGround.Input, Game.Editor.P09Builder→UPlayGround.Editor.P09Builder, **UPlayGround.Component→UPlayGround.Components**(UnityEngine.Component 충돌 해소, 181파일), Interaction.Enum·UPlayGround.UREnum→UPlayGround.Data.EnumType, Tool.Editor→UPlayGround.Tool.Editor. 파급 using 보충 및 Input/Editor/Path/Event 무자격 참조 자격화. MSBuild(Assembly-CSharp/-Editor)로 컴파일 검증 완료 |
| §4 싱글톤 핫스팟 캐싱 | 완료 | 7개 파일 184건을 lazy 캐시 프로퍼티(XxxMgr)로 대체. BaseManager.Instance의 매 호출 lock 회피. fake-null 재조회로 파괴 안전 |
| §5 대형 파일 분할 | 완료 | PlayerActor 6파일(최대 504줄), PlayerCombat 6파일(base 872줄), ActorMovementController→3파일: 동거하던 **MotionWarpController(별도 MonoBehaviour)를 자체 파일로 독립**(1,203줄 — 단일 클래스라 추가 분할 보류, delta-warp 미검증 상태라 내부 재구성 회피) + MotionWarpTypes.cs |
| §7 asmdef | 제외 | 사용자 지시로 이번 실행에서 제외 (기존 실행 계획 Phase 7 담당) |

**Unity 수작업 잔여:** 에디터 포커스 시 신규 파일 .meta 생성 확인, 콘솔 컴파일 에러 없음 확인, 스모크 테스트(이동/공격/스왑/가드/제작·인벤토리·파티 UI).

---

## 0. 개요

2026-07-11 기준 `Assets/02.Scripts` 전수 조사 결과를 바탕으로 한 코드 구조 개선 로드맵이다.

핵심 결론:

- 런타임 코드 자체(GameManager의 타입 캐시·업데이트 리스트 분리 등)는 잘 다듬어져 있다. 개선 여지는 코드 품질이 아니라 **구조 관리** 쪽에 집중된다.
- 가장 비용이 낮고 효과가 즉각적인 것은 **폴더 구조 표류 정리**(빈 폴더, 유사 폴더 4갈래, partial 분산)와 **CLAUDE.md 재동기화**다.
- 싱글톤 결합(`.Instance` 1,208회)은 전면 DI 전환이 아니라 **핫스팟 국소 개선**으로 접근한다.

### 현황 수치 (2026-07-11 조사)

| 항목 | 수치 |
|------|------|
| C# 파일 총계 | 976개 (에디터 툴 257개 포함) |
| 상태 클래스 (`GameActor/State`) | 63개 |
| UI 파일 | 111개 |
| `.Instance` 싱글톤 직접 접근 | 1,208회 / 186개 파일 |
| 네임스페이스 없는 파일 | 110개 |
| 자체 코드 asmdef | `UPlayGround.Core.asmdef` 1개 (내부 파일 1개 — 실행 계획 Phase 7의 1차 구현물) |

---

## 1. 폴더 구조 표류 정리 — 우선순위 1

비용이 낮고 효과가 즉각적이다. 유사 폴더가 여러 갈래로 갈라져 탐색 혼란을 만들고 있다.

| 항목 | 현황 | 조치 |
|------|------|------|
| `Debug/` vs `Debugging/` vs `Diagnostics/` vs `AI/Debugging/` | 디버그성 폴더 4곳. `Debug/Gizmo`는 **빈 폴더**, 실사용은 `Debugging/Gizmo` | `Debug/Gizmo` 빈 폴더 삭제. `Debug/KnockbackTestBed.cs`를 `Debugging/`으로 이동 후 `Debug/` 제거. `Diagnostics/`·`AI/Debugging/`은 역할 확인 후 통합 여부 판단 |
| 루트 `BehaviorTree/` | **빈 폴더**. 실체는 `AI/BehaviorTree/` | 빈 폴더 + .meta 삭제 |
| `Manager/SceneManager.cs` + `Manager/Scene/SceneManager.Load.cs` | 같은 클래스의 partial이 서로 다른 폴더에 분산 | `SceneManager.cs`를 `Manager/Scene/`으로 이동해 합류 (GUID 유지를 위해 .meta 함께 이동) |
| `02.Scripts` 루트의 `.md` 3개 | `review.md`, `런타임_성능_오버헤드_조사.md`, `PlayerActor_접근캐싱_조사.md` — Unity가 임포트하는 Assets 안에 조사 문서 방치 | `Assets/docs/` 또는 프로젝트 루트 `docs/`로 이동 |
| `GameActor/NpcActor.cs` | 다른 액터는 전부 `GameActor/Object/` 하위인데 혼자 루트에 위치 | `GameActor/Object/Npc/`로 이동 |

**주의:** 파일 이동은 반드시 `.meta` 파일을 함께 이동해 GUID를 보존한다(씬/프리팹 참조 유지). Unity 에디터 내 이동 또는 .meta 동반 이동만 허용.

### 완료 기준

- 빈 폴더 0개, 디버그성 폴더 계열이 1~2곳으로 수렴.
- partial 클래스의 파일들이 같은 폴더에 존재.
- `Assets/02.Scripts` 아래 `.md` 조사 문서 0개.

---

## 2. CLAUDE.md 재동기화 — 우선순위 2

CLAUDE.md가 현재 코드와 어긋나 있다. 1인 개발 + Claude Code 협업 구조에서는 CLAUDE.md가 틀리면 매 세션 잘못된 전제로 시작하므로 주기적 재동기화 가치가 크다.

확인된 불일치:

| CLAUDE.md 기술 | 실제 |
|----------------|------|
| "PlayerActor — partial class로 5개 파일 분리 (base, lifecycle, input, combat, equipment)" | `PlayerActor.cs` **단일 1,566줄 파일**. partial 관계는 `PlayerActorAnimator.cs`뿐 |
| 매니저 목록 17개 | QuestManager, SoundManager, SaveManager, WorldStateManager, RecipeManager, MonsterRespawnManager, InteractionRespawnManager, CheatManager, ActorSpawnManager, WorldLightingManager 등 누락 |
| 데이터 아키텍처에 `EnemyStatsSO` | 이미 제거됨 (`ActorStatSO` 단일 소스화 완료) |

### 완료 기준

- 위 3개 불일치 수정 + 폴더 정리(§1) 반영.
- 이후 대형 구조 변경 시 CLAUDE.md 갱신을 작업 완료 조건에 포함하는 습관 정착.

---

## 3. 네임스페이스 일관성 — 우선순위 3

| 문제 | 현황 | 조치 |
|------|------|------|
| 네임스페이스 없는 파일 | 110개 | 폴더 위치 기준으로 `UPlayGround.*` 부여. IDE 일괄 리팩터링 활용 |
| `Game.Editor.P09Builder` | 45개 파일이 `UPlayGround` 루트를 따르지 않음 | `UPlayGround.Editor.P09Builder`로 개명 |
| `UPlayGround.Object` ↔ `UnityEngine.Object` 충돌 | 무자격 `Object.Destroy` 등이 CS0234 유발. 현재는 매번 `UnityEngine.Object` 명시로 회피 중 | 네임스페이스를 `UPlayGround.Actors`(또는 `UPlayGround.Objects`)로 개명해 근본 해결. IDE 리네임으로 일괄 처리 |
| 폴더↔네임스페이스 매핑 불규칙 | `GameActor/` 폴더가 `UPlayGround.State`, `UPlayGround.Component`, `UPlayGround.Combat` 등으로 갈라짐 | 전면 재정렬은 과비용. **신규 파일부터 "폴더 경로 = 네임스페이스" 규칙 적용**하고 기존은 충돌 유발 건만 수정 |

### 완료 기준

- `UPlayGround.Object` 네임스페이스 소멸(개명 완료), 관련 `UnityEngine.Object` 명시 우회 코드 정리.
- 네임스페이스 없는 파일 0개.
- 신규 파일 네임스페이스 규칙이 CLAUDE.md 코드 컨벤션 섹션에 명문화.

---

## 4. 싱글톤 결합 완화 — 핫스팟 국소 개선

`.Instance` 직접 접근 1,208회를 DI로 전면 전환하는 것은 1인 개발에 과하다. **핫스팟만** 정리한다.

### 4.1 접근 횟수 상위 파일 (조사 기준)

| 파일 | `.Instance` 접근 |
|------|------|
| `Manager/GameManager.cs` | 44회 (자기 서브매니저 관리 — 정상 범주) |
| `UI/Scene/Crafting/UI_CraftMenu.cs` | 38회 |
| `GameActor/Object/Player/PlayerActor.cs` | 34회 |
| `GameActor/Combat/Feedback/CombatFeedbackDispatcher.cs` | 29회 |
| `UI/HUD/UI_GamePlay.cs` | 28회 |
| `Manager/World/MonsterRespawnManager.cs` | 27회 |
| `UI/Scene/Inventory/UI_Inventory.cs` | 25회 |
| `UI/Scene/Party/UI_PartyMenu.cs` | 24회 |

### 4.2 접근 방식

1. **필드 캐싱**: 상위 파일에서 매니저 참조를 초기화 시 1회 캐싱. 기존 조사 문서(`PlayerActor_접근캐싱_조사.md`)의 결론을 실행으로 옮기는 것이 첫 단계.
2. **UI→매니저 직접 호출을 이벤트 구독으로**: UI 계층은 "매니저를 직접 부르지 않고 이벤트/뷰모델만 본다"는 규칙을 **신규 UI부터** 적용해 점진 전환.
3. 상태 클래스·컴포넌트의 산발적 1~5회 접근은 손대지 않는다 (비용 대비 효과 낮음).

### 완료 기준

- 상위 8개 핫스팟 파일의 반복 `.Instance` 조회가 캐싱 필드로 대체.
- 신규 UI의 매니저 직접 호출 금지 규칙 명문화.

---

## 5. 대형 런타임 파일 분할

| 파일 | 줄 수 | 분할 방향 |
|------|------|----------|
| `GameActor/Component/Player/PlayerCombat.cs` | 2,083 | partial 분리: 기본공격 / 차지 / 스킬 / 궁극기 / 방어반응 등 기능 축. 궁극기 일부가 이미 `UltimateSequencePlayer` 등으로 분리된 방향을 따른다 |
| `GameActor/MovementController/ActorMovementController.cs` | 1,650 | KCC 콜백 위임 / 상태 머신 호스팅 / 워프·보정 축으로 partial 분리 |
| `GameActor/Object/Player/PlayerActor.cs` | 1,566 | CLAUDE.md에 기술된 5-partial 구조(base, lifecycle, input, combat, equipment)를 실제로 복원 |

에디터 툴 대형 파일(`MotionSetWindow.cs` 3,200줄 등)은 런타임 안정성과 무관하므로 **후순위**.

### 완료 기준

- 위 3개 파일이 기능 축 partial로 분리되고, 각 파일이 800줄 이하.
- 분할 후 컴파일 통과 + 플레이어 조작 스모크 테스트(이동/공격/스왑/가드) 통과.

---

## 6. 우선순위 종합

| 순위 | 항목 | 예상 규모 | 리스크 |
|------|------|----------|--------|
| 1 | §1 폴더 표류 정리 | 반나절 | 낮음 (.meta 동반 이동만 준수) |
| 2 | §2 CLAUDE.md 재동기화 | 1~2시간 | 없음 |
| 3 | §3 네임스페이스 (`UPlayGround.Object` 개명 + 무네임스페이스 110개) | 1일 | 낮음~중간 (IDE 리네임, 컴파일 검증 필수) |
| 4 | §4 싱글톤 핫스팟 캐싱 | 1~2일 | 낮음 (동작 동일, 조회만 캐싱) |
| 5 | §5 대형 파일 분할 | 2~3일 | 중간 (partial 분리 자체는 안전하나 스모크 테스트 필요) |
| 6 | §7 asmdef | — | 기존 실행 계획 Phase 7로 위임 |

①~③은 서로 독립적이라 순서 무관하게 착수 가능. ④~⑤는 ③ 이후가 깔끔하다(네임스페이스 확정 후 파일 이동/분할).

---

## 7. asmdef — 기존 실행 계획과의 관계

asmdef 분리는 [PROJECT_SYSTEM_IMPROVEMENT_EXECUTION_PLAN.md](PROJECT_SYSTEM_IMPROVEMENT_EXECUTION_PLAN.md) **Phase 7**이 담당하며, `UPlayGround.Core.asmdef`(현재 `Core/Party/PartyRosterService.cs` 1개)가 그 1차 구현물이다. 본 문서에서는 다음만 보충한다:

- 976개 파일 전체가 Assembly-CSharp 하나에 있어 스크립트 한 줄 수정마다 전체 재컴파일된다. 다만 Manager↔GameActor↔UI 상호 의존 때문에 전면 분리는 순환 참조 위험이 크다 — 실행 계획의 "순환 참조 먼저 정리, 큰 Runtime/Editor 경계 먼저" 원칙을 따른다.
- 의존이 없는 leaf(`Util`, `Data/Enum`, 순수 데이터 SO)부터 `UPlayGround.Core`로 편입하는 것이 안전한 확장 경로다.
- 본 문서 §3(네임스페이스)·§1(폴더 정리)이 선행되면 asmdef 경계 설계가 쉬워진다.
