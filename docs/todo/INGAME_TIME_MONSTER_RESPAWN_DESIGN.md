# 인게임 시간/몬스터 재스폰/낮밤 조명 설계

작성일: 2026-07-06

## 목표

- 실제 플레이 시간에 비례해 인게임 시간이 흐른다.
- 시간이 흐르면 필드 몬스터가 재스폰된다.
- 재스폰 시 화면 페이드인과 안내 문구를 표시한다.
- `MonsterActorGrade.Boss` 몬스터는 재스폰하지 않는다.
- 재스폰된 몬스터는 시간이 지날수록 레벨이 점진적으로 올라가고, 스탯/경험치/재화 보상도 함께 증가한다.
- 낮/밤 시간대에 따라 방향광 위치, 광량, 주변광, URP 후처리 어둡기 처리를 변경한다.

## 웹 조사 요약

Unity 공식 문서 기준으로 구현 축을 정리했다.

- 시간 누적은 프레임 간격인 `Time.deltaTime` 또는 `Time.unscaledDeltaTime`을 누적하는 방식이 기본이다. `deltaTime`은 `Time.timeScale`의 영향을 받고, `unscaledDeltaTime`은 영향을 받지 않는다. 현재 프로젝트의 `GameTimeManager`는 일시정지 중이 아닐 때 `Time.unscaledDeltaTime`으로 플레이 시간을 누적하고 있으므로, 인게임 시간도 이 축을 확장하는 것이 자연스럽다.  
  참고: https://docs.unity3d.com/ScriptReference/Time-deltaTime.html, https://docs.unity3d.com/ScriptReference/Time-unscaledDeltaTime.html
- 코루틴 대기는 `WaitForSeconds`를 사용할 수 있으나 scaled time 기반이다. UI 페이드/시간 시스템은 일시정지와 히트스톱에 흔들리지 않아야 하므로 타이머 누적은 `unscaledDeltaTime` 기반 업데이트가 더 안전하다.  
  참고: https://docs.unity3d.com/ScriptReference/WaitForSeconds.html
- 페이드 UI는 `CanvasGroup.alpha`를 조절하는 방식이 단순하고 기존 UI 구조와 잘 맞는다.  
  참고: https://docs.unity3d.com/ScriptReference/CanvasGroup-alpha.html
- 낮밤 광원은 `Light` 컴포넌트의 회전, 색상, 세기, 그림자 강도를 시간에 따라 보간한다. `RenderSettings.ambientLight` 또는 URP Global Volume을 함께 조정하면 밤의 체감 어둡기를 안정적으로 만들 수 있다.  
  참고: https://docs.unity3d.com/ScriptReference/Light.html, https://docs.unity3d.com/ScriptReference/RenderSettings-ambientLight.html
- URP는 Volume으로 후처리와 씬별/전역 시각 설정을 적용한다. 밤에는 Global Volume의 노출/색보정/Vignette 계열 값을 별도 Profile 또는 런타임 override로 보간하는 구성이 적합하다.  
  참고: https://docs.unity3d.com/6000.0/Documentation/Manual/urp/Volumes.html

## 현재 프로젝트 접점

- `Assets/02.Scripts/Manager/GameTimeManager.cs`
  - 이미 `IUpdatableManager`로 등록되어 있고 `TotalPlaySeconds`를 누적한다.
  - `SetPause`, timeScale 요청 API가 있어 히트스톱/일시정지와 충돌하지 않는 중앙 시간 소유자 역할을 한다.
- `Assets/02.Scripts/Manager/Actor/ActorSpawnManager.cs`
  - `SpawnActor(actorId, position, rotation, group, parent)`로 ActorDatabase 기반 스폰이 가능하다.
  - 씬 배치 Actor도 `AfterInit`에서 자동 등록한다.
- `Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs`
  - 사망 시 `WorldStateManager.RecordKill`, 드랍, 경험치 지급, 파티 합류를 처리한다.
  - `ActorDefinitionSO`에서 `grade`, `level`, `expReward`, `dropTable`, `monsterScaling`을 주입한다.
- `Assets/02.Scripts/Data/Actor/Enemy/MonsterScalingSO.cs`
  - 등급/레벨/난이도 기반 스탯 성장 규칙이 이미 있다.
  - 현재 주석상 런타임 스케일링이 아니라 에디터 bake 용도다. 재스폰 레벨링은 이 구조를 런타임에 재사용하도록 확장해야 한다.
- `Assets/02.Scripts/Manager/Save/WorldStateManager.cs`
  - 현재는 씬 배치 몬스터를 영구 처치로 저장하고 씬 전환 시 제거한다.
  - 필드 재스폰을 도입하면 일반/엘리트/약몹은 영구 처치가 아니라 재스폰 예약 상태로 저장해야 한다.
- `Assets/02.Scripts/Data/Actor/ActorDefinitionSO.cs`
  - `MonsterActorGrade.Boss`를 기준으로 보스 재스폰 제외가 가능하다.
  - 경험치는 `expReward`만 있고 재화 보상 필드는 몬스터 정의에 없다. 재화는 드랍 테이블 또는 신규 `goldReward` 필드 중 하나를 결정해야 한다.

## 핵심 설계

### 1. GameTimeManager 확장

`GameTimeManager`에 인게임 시계 개념을 추가한다.

```text
실제 1초 * gameMinutesPerRealSecond = 인게임 분 증가
인게임 분 누적값 % 1440 = 하루 중 시각
floor(누적 분 / 1440) = 경과 일수
```

권장 기본값:

- `gameMinutesPerRealSecond = 1.0`: 실제 24분이 인게임 1일
- `startMinuteOfDay = 8 * 60`: 새 게임 시작 08:00
- `UseUnscaledTime = true`: 히트스톱에는 낮밤/리스폰 타이머가 멈추지 않는다.
- `PauseStopsWorldTime = true`: 메뉴/부활 팝업으로 `IsPaused`일 때는 인게임 시간이 멈춘다.

추가 API:

```csharp
public int CurrentDay { get; }
public float TotalGameMinutes { get; }
public float MinuteOfDay { get; }
public float DayProgress01 { get; }
public bool IsNight { get; }
public event Action<int, float> OnGameMinuteChanged;
public event Action<DayPeriod> OnDayPeriodChanged;
```

`OnGameMinuteChanged`는 매 프레임 발행하지 않고, 정수 인게임 분이 바뀔 때만 발행한다. 몬스터 재스폰/조명 시스템은 이 이벤트를 구독한다.

### 2. 낮/밤 구간 모델

새 enum:

```csharp
public enum DayPeriod
{
    Dawn,
    Day,
    Dusk,
    Night,
}
```

권장 구간:

- Dawn: 05:00-07:00
- Day: 07:00-18:00
- Dusk: 18:00-20:00
- Night: 20:00-05:00

구간은 하드코딩하지 않고 `WorldTimeSettingsSO`에 둔다.

### 3. WorldLightingController

씬에 배치하는 MonoBehaviour로 설계한다. 전역 매니저가 아닌 씬 오브젝트가 적합하다. 이유는 씬마다 방향광, Volume, 하늘색, 안개 설정이 다를 수 있기 때문이다.

필드:

- `Light _sunLight`
- `Light _moonLight` 선택
- `Volume _globalVolume` 선택
- `Gradient sunColorByTime`
- `AnimationCurve sunIntensityByTime`
- `Gradient ambientColorByTime`
- `AnimationCurve exposureByTime`
- `AnimationCurve fogDensityByTime`
- `Vector3 sunEulerAtSunrise`, `sunEulerAtNoon`, `sunEulerAtSunset`, `sunEulerAtMidnight`

매 분 또는 매 프레임 보간:

```text
dayProgress01 = MinuteOfDay / 1440
sunLight.transform.rotation = Quaternion.Euler(dayProgress01 * 360 - 90, sunAzimuth, 0)
sunLight.color = sunColorByTime.Evaluate(dayProgress01)
sunLight.intensity = sunIntensityByTime.Evaluate(dayProgress01)
RenderSettings.ambientLight = ambientColorByTime.Evaluate(dayProgress01)
```

URP Volume은 프로파일을 직접 수정하면 에셋 오염 가능성이 있으므로 런타임에는 인스턴스화된 Profile 또는 전용 override 컴포넌트를 사용한다. 구현 단계에서는 노출/색보정만 최소 적용하고, 이후 안개/하늘/달빛을 확장한다.

### 4. 몬스터 재스폰 데이터

새 ScriptableObject:

```csharp
[CreateAssetMenu(fileName = "MonsterRespawnSettings_", menuName = "UPlayGround/월드/Monster Respawn Settings")]
public class MonsterRespawnSettingsSO : ScriptableObject
{
    public float respawnIntervalGameMinutes = 360f;
    public int maxRespawnLevelBonus = 20;
    public float levelUpPerGameDay = 0.5f;
    public int minRespawnLevel = 1;
    public bool respawnWeak = true;
    public bool respawnNormal = true;
    public bool respawnElite = true;
    public bool respawnBoss = false;
    public MonsterRewardScaling rewardScaling;
}
```

새 컴포넌트:

```csharp
public class MonsterRespawnPoint : MonoBehaviour
{
    public string actorId;
    public MonsterGroupController group;
    public MonsterRespawnSettingsSO overrideSettings;
    public bool useInitialSceneMonsterDefinition = true;
}
```

씬 배치 몬스터를 모두 수동으로 스폰 포인트화하기 어렵기 때문에 마이그레이션 단계는 두 갈래로 둔다.

- 1차: `SceneEntityId`가 있는 씬 배치 몬스터를 런타임에 `MonsterRespawnManager`가 자동 등록한다.
- 2차: 중요한 인카운터는 `MonsterRespawnPoint` 프리팹으로 명시 배치한다.

자동 등록 시 저장할 값:

- `mapId`
- `sceneEntityGuid`
- `actorId`
- `spawnPosition`
- `spawnRotation`
- `group`
- `baseLevel`
- `grade`
- `killedGameMinute`
- `nextRespawnGameMinute`
- `respawnCount`

### 5. MonsterRespawnManager

새 매니저를 `GameManager`에 등록한다. 초기화 순서는 `ActorSpawnManager`, `WorldStateManager`, `SceneManager` 이후가 안전하다.

역할:

- 현재 맵의 재스폰 포인트 등록
- 몬스터 사망 이벤트 수신
- 보스 제외 여부 판정
- 재스폰 예약 저장
- 인게임 분 변경 시 재스폰 due 목록 처리
- 재스폰 안내 UI 표시 요청

현재 `MonsterActor.OnDeath`에는 공개 사망 이벤트가 없다. 다음 중 하나가 필요하다.

권장:

```csharp
public static event Action<MonsterActor> OnAnyMonsterDied;
```

`OnDeath()` 마지막이 아니라, 드랍/경험치 처리 전후 정책을 명확히 정한 위치에서 발행한다. 재스폰 예약만 필요하므로 `WorldStateManager.RecordKill` 호출 전후 어디든 가능하지만, 보스 영구 처치와 일반 재스폰 예약 분기가 필요하므로 `NotifyWorldStateKill()` 내부를 개편하는 편이 낫다.

재스폰 제외 규칙:

```text
definition.grade == Boss
monster.Grade == Boss
definition.recruitableAs != None인 특별 합류 몬스터
퀘스트 전용 몬스터 플래그가 추가될 경우 respawnPolicy = Never
```

`recruitableAs != None`은 보스가 아니더라도 반복 합류/반복 보상을 막기 위해 기본 제외로 두는 편이 안전하다.

### 6. WorldStateManager 개편

현재 구조는 `killedMonsters`만 저장한다. 재스폰 도입 후에는 다음처럼 분리한다.

```text
permanentKilledMonsters: 보스/퀘스트/영구 제거 대상
respawnStates: 일반 필드 몬스터의 재스폰 예약 상태
```

저장 데이터 예시:

```csharp
[Serializable]
public class MonsterRespawnSaveData
{
    public string mapId;
    public string sceneEntityGuid;
    public string actorId;
    public float nextRespawnGameMinute;
    public int respawnCount;
    public int currentLevel;
}
```

씬 전환 시 처리:

- `permanentKilledMonsters`에 있으면 기존처럼 제거한다.
- `respawnStates`에 있고 아직 시간이 안 됐으면 제거한다.
- `respawnStates`에 있고 시간이 지났으면 원본 배치 몬스터 제거 후 `ActorSpawnManager.SpawnActor`로 현재 레벨 몬스터를 스폰한다.

이렇게 해야 저장/로드 후에도 “죽었지만 아직 재스폰 시간이 안 된 몬스터”와 “재스폰 시간이 지난 몬스터”가 일관된다.

### 7. 재스폰 레벨 스케일링

기본 공식:

```text
elapsedDays = floor((currentGameMinute - firstSeenGameMinute) / 1440)
respawnBonusByDay = floor(elapsedDays * levelUpPerGameDay)
respawnBonusByCount = floor(respawnCount / respawnCountPerLevel)
targetLevel = baseLevel + respawnBonusByDay + respawnBonusByCount
targetLevel = clamp(targetLevel, minRespawnLevel, baseLevel + maxRespawnLevelBonus)
```

권장 초기값:

- `levelUpPerGameDay = 0.5`: 인게임 2일마다 +1레벨
- `respawnCountPerLevel = 3`: 같은 포인트에서 3회 재스폰마다 +1레벨
- `maxRespawnLevelBonus = 20`

스탯 적용 방법은 두 단계로 나눈다.

1차 구현:

- `MonsterActor`에 런타임 레벨 오버라이드 API 추가

```csharp
public void ApplyRuntimeLevel(int runtimeLevel, float difficultyMultiplier = 1f)
```

- 내부에서 `MonsterStatCalculator.Calculate(definition.monsterScaling, definition.grade, runtimeLevel, difficultyMultiplier)` 호출
- 계산 결과를 `ActorStatContainer`에 적용할 수 있는 API가 없으면 `ActorStatSO` 런타임 인스턴스를 만들어 `Stats.Init(runtimeStat)`로 주입한다.

2차 구현:

- `ActorDefinitionSO`를 복제하지 않고 `RuntimeMonsterLevel`만 저장한다.
- UI HP Bar, AI Phase, BreakGauge, EnemyCombat이 레벨 변경 후 재초기화되는지 검증한다.

### 8. 경험치/재화 보상 스케일링

경험치는 이미 `ActorDefinitionSO.expReward`와 `PartyManager.AwardBattleExp`가 있다. 런타임 레벨을 적용할 때 보상도 함께 계산한다.

경험치 공식:

```text
levelDelta = runtimeLevel - baseLevel
expMultiplier = pow(1 + expPerLevelRate, levelDelta)
runtimeExpReward = round(baseExpReward * gradeMultiplier * expMultiplier)
```

권장값:

- `expPerLevelRate = 0.08`
- Weak 0.8, Normal 1.0, Elite 1.35
- Boss는 재스폰 대상이 아니므로 런타임 재스폰 보상 공식에서 제외

재화 보상은 현재 몬스터 직접 골드 필드가 없다. 선택지는 두 가지다.

권장 1안: `ActorDefinitionSO`에 `goldReward` 추가

- 장점: 경험치와 동일한 경로로 스케일링 가능
- 단점: 기존 데이터/에디터/검증기 수정 필요

2안: 골드 아이템을 `EnemyDropTableSO`에 추가하고 드랍 수량을 런타임 보정

- 장점: 드랍 시스템 재사용
- 단점: “재화”와 “아이템 드랍”의 의미가 섞임

프로젝트에는 `InventoryManager.Gold`가 이미 있으므로 1안을 권장한다.

추가 API:

```csharp
public long BaseExpReward => _expReward;
public int BaseGoldReward => _goldReward;
public void SetRuntimeRewards(long expReward, int goldReward);
```

사망 시:

```csharp
PartyManager.Instance?.AwardBattleExp(_runtimeExpReward);
InventoryManager.Instance.Gold += _runtimeGoldReward;
```

### 9. 재스폰 UI 연출

기존 `UI_RespawnPopup`은 플레이어 사망 부활용이며 게임을 pause한다. 몬스터 재스폰 안내에는 사용하지 않는다.

새 UI:

```text
UI_WorldRespawnNotice
```

구성:

- `CanvasGroup`
- 전체 화면 얕은 암전 이미지
- 중앙 또는 상단 안내 텍스트
- 선택: 작은 파티클/라인 장식

문구 후보:

```text
밤의 기척이 짙어집니다.
쓰러졌던 마물이 다시 움직이기 시작했습니다.
필드 몬스터가 재출현했습니다.
```

연출:

```text
fade in 0.35s
hold 1.2s
fade out 0.45s
```

주의:

- 입력을 막지 않는다.
- `GameTimeManager.SetPause(true)`를 호출하지 않는다.
- 페이드는 `Time.unscaledDeltaTime`으로 구동한다.
- 한 프레임에 여러 몬스터가 재스폰되면 UI는 1회만 표시하고, 문구에 지역/수량을 선택적으로 반영한다.

예:

```text
그레이우드 평원의 마물 5체가 다시 출현했습니다.
```

### 10. 재스폰 타이밍 정책

권장 기본 정책:

- 일반 필드 몬스터: 사망 후 인게임 6시간
- 약몹: 사망 후 인게임 4시간
- 엘리트: 사망 후 인게임 12시간
- 보스: 재스폰 없음

밤에만 재스폰하는 정책은 초기 구현에서 제외한다. 낮밤 시스템이 안정화된 후 특정 몬스터/지역의 `respawnPeriodMask`로 확장한다.

재스폰 처리 흐름:

```text
MonsterActor.OnDeath
→ MonsterRespawnManager.ScheduleRespawn(monster)
→ WorldStateManager.RecordRespawnState(...)
→ 기존 씬 배치 몬스터는 죽음 연출 후 Destroy
→ GameTimeManager.OnGameMinuteChanged
→ due respawn state 조회
→ ActorSpawnManager.SpawnActor(actorId, position, rotation, group)
→ MonsterActor.ApplyRuntimeLevel(targetLevel)
→ MonsterActor.SetRuntimeRewards(exp, gold)
→ UI_WorldRespawnNotice.Show(...)
```

### 11. 기존 영구 처치와의 충돌 해결

`MonsterActor.NotifyWorldStateKill()`은 현재 모든 `SceneEntityId` 보유 몬스터를 영구 처치로 기록한다. 이 부분을 정책 기반으로 바꾼다.

```csharp
private void NotifyWorldStateKill()
{
    var entityId = GetComponent<SceneEntityId>();
    if (entityId == null || !entityId.HasGuid) return;

    string mapId = SceneManager.Instance?.CurrentMapID;
    if (MonsterRespawnManager.Instance != null
        && MonsterRespawnManager.Instance.TryScheduleRespawn(this, mapId, entityId.Guid))
    {
        return;
    }

    WorldStateManager.Instance?.RecordPermanentKill(mapId, entityId.Guid);
}
```

`RecordKill`은 호환을 위해 남기되 내부적으로 `RecordPermanentKill`로 위임한다.

## 구현 순서

1. `GameTimeManager`에 인게임 시간 필드/API/이벤트 추가
2. `WorldTimeSettingsSO` 추가
3. `WorldLightingController` 추가 및 테스트 씬에 연결
4. `MonsterRespawnSettingsSO`, `MonsterRespawnPoint`, 저장 데이터 추가
5. `WorldStateManager`를 permanent kill/respawn state로 분리
6. `MonsterActor.OnAnyMonsterDied` 또는 `NotifyWorldStateKill` 정책 분기 추가
7. `MonsterRespawnManager` 추가 및 `GameManager` 등록
8. `MonsterActor.ApplyRuntimeLevel`, `SetRuntimeRewards` 추가
9. `ActorDefinitionSO.goldReward` 추가 및 보상 지급 처리
10. `UI_WorldRespawnNotice` 프리팹/스크립트 추가
11. 에디터 검증기 추가
    - 보스가 respawn point에 들어가면 warning
    - respawn 대상 ActorDefinitionSO에 `monsterScaling` 누락 시 error
    - `goldReward`, `expReward` 음수 검사
12. 저장/로드/씬 전환 수동 QA

## QA 체크리스트

- 실제 10초 플레이 후 인게임 시간이 설정 비율대로 증가한다.
- 메뉴 pause 중 인게임 시간이 멈춘다.
- 히트스톱 중 인게임 시간이 의도대로 흐르거나 멈춘다. 기본 설계는 흐름이다.
- 일반 몬스터 사망 후 즉시 씬 전환해도 재스폰 예약 상태가 유지된다.
- 재스폰 시간이 지나기 전에는 몬스터가 복원되지 않는다.
- 재스폰 시간이 지난 뒤 같은 맵에 들어오면 몬스터가 다시 생성된다.
- Boss 등급은 사망 후 영구 처치로 남고 재스폰되지 않는다.
- `recruitableAs != None` 몬스터는 반복 재스폰/반복 합류하지 않는다.
- 재스폰 횟수/경과 일수에 따라 레벨이 증가한다.
- 레벨 증가분에 따라 MaxHealth/AttackPower/Poise가 증가한다.
- 레벨 증가분에 따라 경험치/골드 보상이 증가한다.
- 재스폰 안내 UI는 게임을 멈추지 않고 1회만 표시된다.
- 낮/밤 전환 시 방향광 회전, 광량, 주변광, Volume 어둡기가 부드럽게 변한다.
- 저장 후 로드해도 `TotalGameMinutes`, `CurrentDay`, 재스폰 예약 시간이 복원된다.

## 리스크와 결정 필요 사항

- 재화 보상 필드 위치: `ActorDefinitionSO.goldReward` 추가를 권장한다.
- 런타임 스탯 적용 방식: `ActorStatContainer`에 딕셔너리 주입 API가 있는지 확인 후, 없으면 런타임 `ActorStatSO` 인스턴스를 생성한다.
- 씬 배치 몬스터 자동 등록만으로는 그룹/인카운터 복원이 완벽하지 않을 수 있다. 중요한 전투는 `MonsterRespawnPoint` 명시 배치가 필요하다.
- 기존 `WorldStateManager.killedMonsters` 세이브 호환이 필요하다. 기존 저장 데이터는 모두 permanent kill로 읽는 것이 안전하다.
- 조명은 씬마다 레퍼런스가 다르므로 매니저보다 씬 컴포넌트가 적합하다. 단, 공통 설정은 `WorldTimeSettingsSO`로 공유한다.
