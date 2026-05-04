# 자동화 테스트 도입 설계 문서

> UPlayground 프로젝트에 Unity Test Framework 기반 자동화 테스트 시스템을 점진적으로 도입하기 위한 설계.

---

## 1. 도입 목표

| 목표 | 설명 |
|------|------|
| **회귀 방지** | 매니저 / 핸들러 리팩터링 시 기존 동작이 깨지지 않았음을 자동 검증 |
| **데이터 무결성** | ItemDatabase, ActorDatabase 등 ScriptableObject 자산의 ID 중복·누락 자동 검증 |
| **핵심 로직 보호** | 데미지 계산, timeScale 큐, 인벤토리 같은 **눈에 잘 띄지 않는 버그가 치명적인 영역** 우선 보호 |
| **빠른 피드백** | 1인 개발이므로 수동 QA 부담을 줄이고 변경 직후 즉시 피드백 |

**비목표 (당분간 하지 않음):**

- UI 자동 테스트 (수동 QA가 더 효율적)
- 그래픽 / 셰이더 회귀 테스트
- 모든 코드 100% 커버리지 추구

---

## 2. 기술 스택

| 도구 | 용도 | 비고 |
|------|------|------|
| **Unity Test Framework** | 핵심 — `Window > General > Test Runner` 내장 | Unity 6 기본 포함, 별도 설치 불필요 |
| **NUnit** | 테스트 어노테이션 / 어서션 | Unity Test Framework가 NUnit 기반 |
| **NSubstitute** | 모킹(Mocking) — 의존성 가짜 객체 생성 | Unity Asset Store / NuGet 도입 검토 |

> 1인 개발 + 한정 리소스 고려 — **NSubstitute는 필요해질 때 도입**. 초기에는 NUnit 기본 어서션과 직접 작성한 Stub 클래스로 충분.

---

## 3. 폴더 / Assembly Definition 구조

Unity 자동화 테스트는 **반드시 별도 Assembly Definition(asmdef)** 으로 분리해야 한다 (그렇지 않으면 빌드에 테스트 코드가 포함됨).

```
Assets/
├── 02.Scripts/
│   ├── UPlayGround.asmdef            (런타임 코드)
│   └── Editor/
│       └── UPlayGround.Editor.asmdef (에디터 전용)
│
└── 03.Tests/                         ← 신규
    ├── EditMode/
    │   ├── UPlayGround.Tests.EditMode.asmdef
    │   ├── Manager/
    │   │   ├── GameTimeManagerTests.cs
    │   │   └── HitStopHandlerTests.cs
    │   ├── Combat/
    │   │   └── DamageCalculationTests.cs
    │   ├── Data/
    │   │   └── ItemDatabaseIntegrityTests.cs
    │   └── Inventory/
    │       └── InventoryManagerTests.cs
    │
    └── PlayMode/
        ├── UPlayGround.Tests.PlayMode.asmdef
        ├── State/
        │   └── PlayerStateTransitionTests.cs
        └── Spawn/
            └── ActorSpawnTests.cs
```

### asmdef 설정 핵심

**EditMode asmdef:**
- `includePlatforms`: Editor만
- `optionalUnityReferences`: `TestAssemblies` 체크
- `references`: `UPlayGround` (런타임 asmdef)

**PlayMode asmdef:**
- `includePlatforms`: 비워둠 (모든 플랫폼)
- `optionalUnityReferences`: `TestAssemblies` 체크
- `references`: `UPlayGround`

---

## 4. EditMode vs PlayMode 분류 기준

| 구분 | EditMode | PlayMode |
|------|----------|----------|
| **실행 환경** | 에디터 컴파일 직후, 씬 없음 | 실제 플레이 모드 진입 |
| **속도** | 매우 빠름 (밀리초) | 느림 (초 단위) |
| **MonoBehaviour** | `new` 불가, 의존 시 PlayMode 사용 | 정상 사용 가능 |
| **이 프로젝트에서 적합한 대상** | 순수 로직, ScriptableObject 검증, 데미지 공식 | 매니저 초기화 순서, 상태 머신 전이, 액터 스폰 |

**원칙:** EditMode로 가능하면 EditMode로. PlayMode는 진짜로 씬/매니저가 필요한 케이스만.

---

## 5. 우선순위 — 어떤 시스템부터 테스트할 것인가

도입 비용 대비 가치가 높은 순서:

### Phase 1 — 즉시 가치 (EditMode, 의존성 적음)

| 대상 | 무엇을 테스트 | 왜 우선 |
|------|--------------|---------|
| **`GameTimeManager`** | id 기반 timeScale 큐: 다중 요청 시 최저 scale 적용, Release 후 복구, Pause 우선순위 | 큐 모델이 미묘하고 깨지면 전 시스템에 영향 |
| **`HitStopHandler` 강도 비교** | `ShouldReplaceExisting`, `StopWeakerThan` 로직 (Time/GameTimeManager Stub 사용) | 최근 리팩터링한 영역 — 회귀 위험 |
| **`InventoryManager`** | 추가/제거/스택/슬롯 한도 | CRUD 버그는 세이브 데이터 손상으로 직결 |
| **데미지 계산** | `PlayerCombat` / `EnemyCombat`의 데미지 공식, 강인도 차감 | 밸런스 변경 시 의도치 않은 변화 감지 |
| **ScriptableObject 무결성** | `ItemDatabase` ID 중복, `ActorDatabase` 누락된 prefab/statData 참조 | 데이터 자산이 늘어날수록 사람이 잡기 힘들어짐 |

### Phase 2 — 중간 가치 (PlayMode 필요)

| 대상 | 무엇을 테스트 |
|------|--------------|
| **`GameManager` 초기화 순서** | 22개 매니저(현재 21개)가 순서대로 Init되며 의존 매니저 누락 시 명확히 에러 |
| **상태 머신 전이 규칙** | `PlayerActorState.CanTransitionState()` — 허용/금지 전이 매트릭스 |
| **`ActorSpawnManager`** | ActorDatabase에서 정의된 액터가 정상 스폰, 컴포넌트 의존성 충족 |
| **`SaveManager` 라운드트립** | `Save → Load` 후 데이터 불변성 |

### Phase 3 — 추후 가치

- `EnemyBrain` 의사결정 분기 (페이즈 전환, 행동 선택)
- `PartyManager` 캐릭터 해금 / 교체
- `QuestManager` 목표 추적
- 상태별 `UpdateVelocity` 결과 검증 (KCC 의존이라 Stub 필요)

---

## 6. 테스트 작성 패턴

### 패턴 A — 순수 로직 (EditMode, 가장 흔함)

```csharp
using NUnit.Framework;
using UPlayGround.Manager;

namespace UPlayGround.Tests.EditMode
{
    public class GameTimeManagerTests
    {
        [Test]
        public void Request_SingleRequest_AppliesScale()
        {
            var manager = CreateInIsolation();

            int id = manager.Request(0.5f);

            Assert.That(Time.timeScale, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(manager.IsSlowed, Is.True);

            manager.Release(id);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Request_TwoRequests_AppliesLowest()
        {
            var manager = CreateInIsolation();

            int idA = manager.Request(0.5f);
            int idB = manager.Request(0.1f);

            Assert.That(Time.timeScale, Is.EqualTo(0.1f).Within(0.001f));

            manager.Release(idB);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f).Within(0.001f),
                "더 강한 요청 해제 후 남은 약한 요청으로 복구");
        }

        // 매니저는 BaseManager<T> 싱글톤이라 테스트 격리가 까다롭다.
        // 실제 구현 시: 테스트용 진입점 (internal Reset 메서드 등) 추가가 필요할 수 있음.
        private GameTimeManager CreateInIsolation() { /* ... */ }
    }
}
```

### 패턴 B — ScriptableObject 데이터 무결성

```csharp
using NUnit.Framework;
using UnityEditor;

namespace UPlayGround.Tests.EditMode
{
    public class ItemDatabaseIntegrityTests
    {
        [Test]
        public void AllItems_HaveUniqueIds()
        {
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(
                "Assets/10.Datas/Item/ItemDatabase.asset");

            Assert.IsNotNull(db, "ItemDatabase 자산이 존재해야 함");

            var ids = db.AllItems.Select(i => i.id).ToList();
            var duplicates = ids.GroupBy(x => x)
                                .Where(g => g.Count() > 1)
                                .Select(g => g.Key)
                                .ToList();

            Assert.IsEmpty(duplicates, $"중복된 Item ID: {string.Join(", ", duplicates)}");
        }

        [Test]
        public void AllActorDefinitions_HaveStatData()
        {
            var allDefs = AssetDatabase.FindAssets("t:ActorDefinitionSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>);

            foreach (var def in allDefs)
                Assert.IsNotNull(def.statData, $"{def.name}.statData 누락");
        }
    }
}
```

### 패턴 C — PlayMode 매니저 부트스트랩

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UPlayGround.Manager;
using UPlayGround.Manager.Combat;

namespace UPlayGround.Tests.PlayMode
{
    public class GameCombatManagerBootstrapTests
    {
        [UnityTest]
        public IEnumerator GameCombatManager_OnInit_HandlersAreReady()
        {
            var go = new GameObject("TestGameManager");
            var gm = go.AddComponent<GameManager>();

            yield return null; // 1프레임 대기

            Assert.IsNotNull(GameCombatManager.Instance);
            Assert.IsNotNull(GameCombatManager.Instance.HitStop);
            Assert.IsNotNull(GameCombatManager.Instance.VitalOrb);

            Object.Destroy(go);
        }
    }
}
```

---

## 7. 코드 변경이 필요한 영역

기존 코드는 **싱글톤 + Addressables 비동기 로드** 의존이 강해 그대로는 테스트가 어렵다. 다음 항목은 테스트 도입과 함께 점진적으로 개선이 필요하다:

| 영역 | 현재 상태 | 개선 방향 |
|------|----------|----------|
| **싱글톤 격리** | `BaseManager<T>.Instance`가 전역 상태 | `internal Reset()` 같은 테스트 헬퍼, 또는 테스트마다 새 GameObject 생성 |
| **Addressables 의존** | `Init()` 내부에서 비동기 자산 로드 | 자산 로드 부분을 분리 (`LoadAsync()`)하여 테스트에선 미리 주입 |
| **Time / 코루틴 의존** | `Time.timeScale`, `StartCoroutine` 직접 사용 | EditMode에서 검증 가능한 순수 함수 분리 (`ApplyLowest(scales)` 같은 형태) |

> **주의:** 테스트를 위한 과도한 추상화는 1인 개발에서 부담이 되므로, 실제 회귀 비용이 큰 영역에만 적용한다.

---

## 8. CI / 자동 실행

| 옵션 | 설명 |
|------|------|
| **로컬 (Test Runner)** | Phase 1: `Window > General > Test Runner`로 수동 실행 |
| **Pre-commit Hook** | Git hook으로 EditMode 테스트만 자동 실행 (PlayMode는 느려 제외) |
| **GitHub Actions + GameCI** | 1인 개발에서는 과한 비용, Phase 3 이후 검토 |

권장: **Phase 1~2는 로컬 수동 실행**, 테스트가 50개 이상 누적되고 회귀가 자주 잡히는 시점에 CI 도입.

---

## 9. 도입 로드맵 (제안)

| 단계 | 기간 | 작업 |
|------|------|------|
| **Step 1** | 1주 | `03.Tests/` 폴더 + asmdef 2종 생성, 가장 단순한 테스트 1개 통과 (예: `Inventory.Add` 단일 케이스) |
| **Step 2** | 2~3주 | Phase 1 대상 5개 시스템에 대해 핵심 케이스 각 3~5개 작성 |
| **Step 3** | 필요 시 | `BaseManager<T>` Reset 헬퍼 추가, ScriptableObject 무결성 테스트 추가 |
| **Step 4** | 매니저/시스템 추가 시 | 새 매니저 도입 시 테스트 1개 이상 동반 작성 (관례화) |
| **Step 5** | 장기 | PlayMode 테스트 + 상태 머신 전이 매트릭스 자동 검증 |

---

## 10. 위험 요소

| 위험 | 대응 |
|------|------|
| 테스트 작성에 시간이 너무 들어 본 작업이 늦어짐 | Phase 1만으로도 충분한 가치. 무리한 커버리지 목표 금지 |
| 싱글톤 / 비동기 코드의 테스트 격리 어려움 | Reset 헬퍼 도입은 **테스트 작성 중 필요해진 시점에만** 추가 |
| 테스트 자체가 깨져서 유지보수 부담 | 의도가 명확한 테스트만 작성. "왜 이걸 테스트하는가"가 한 줄로 설명되지 않으면 작성하지 않음 |
| 1인 개발이라 테스트 리뷰 동료가 없음 | 테스트 이름을 한국어로 풀어 적어 6개월 후 본인이 봐도 의도가 보이게 작성 |

---

## 11. 다음 단계

이 문서의 동의가 끝나면 진행할 첫 작업:

1. `Assets/03.Tests/EditMode/UPlayGround.Tests.EditMode.asmdef` 생성
2. `InventoryManagerTests` — 가장 의존성이 적은 케이스 1개 작성
3. Test Runner에서 그린 라이트 확인
4. Phase 1 나머지 시스템으로 확장

---

*관련 문서: [GAMEMANAGER_README.md](Complete/GAMEMANAGER_README.md), [TIME_HITSTOP_GUIDE.md](Complete/TIME_HITSTOP_GUIDE.md)*
