using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Save;
using UPlayGround.Data.World;
using UPlayGround.Group;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 인게임 시간 기반 필드 몬스터 재스폰 매니저.
    ///
    /// 역할:
    ///   - 씬 배치 몬스터(SceneEntityId 보유)의 배치 정보를 씬 로드 시 자동 등록
    ///   - 몬스터 사망 시 재스폰 예약 판정(보스/합류 몬스터 제외 → 영구 처치로 폴백)
    ///   - 인게임 분 경과 시 due 재스폰 처리(스폰 + 레벨/보상 스케일링 + 안내 UI)
    ///   - 씬 전환/세이브 로드 시 재스폰 상태를 씬에 적용
    ///
    /// 데이터 저장은 WorldStateManager(재스폰 상태 저장소)가, 시간은 GameTimeManager가 소유한다.
    /// GameManager 등록 순서는 WorldStateManager/ActorSpawnManager/SceneManager 이후여야 한다.
    /// </summary>
    public class MonsterRespawnManager : BaseManager<MonsterRespawnManager>, IManager
    {
        private const string SettingsKey = "MonsterRespawnSettings";
        private const string NoticeUIKey = UI_WorldRespawnNotice.UIKey;

        /// <summary> 씬 배치 몬스터 1개의 원본 배치 정보(런타임 전용, 저장 안 함). </summary>
        private class PlacementInfo
        {
            public string actorId;
            public Vector3 position;
            public Quaternion rotation;
            public int baseLevel;
            public MonsterActorGrade grade;
            public MonsterGroupController group;
            public MonsterActor placedInstance; // 씬 로드 시점의 배치 인스턴스(파괴되면 null 평가)
        }

        // 현재 맵의 guid → 배치 정보
        private readonly Dictionary<string, PlacementInfo> _placements = new();

        // 런타임 재스폰된 인스턴스 → guid (SceneEntityId가 없으므로 별도 추적)
        private readonly Dictionary<MonsterActor, string> _runtimeSpawned = new();

        private MonsterRespawnSettingsSO _settings;
        private bool _noticePrefabMissingLogged;

        private MonsterRespawnSettingsSO Settings
        {
            get
            {
                // Addressables 에셋이 없어도 동작하도록 코드 기본값 인스턴스를 폴백으로 사용한다.
                if (_settings == null)
                    _settings = ScriptableObject.CreateInstance<MonsterRespawnSettingsSO>();
                return _settings;
            }
        }

        #region IManager

        public void Init()
        {
            LoadSettingsAsync().Forget();
        }

        public void AfterInit()
        {
            if (GameTimeManager.Instance != null)
                GameTimeManager.Instance.OnGameMinuteChanged += HandleGameMinuteChanged;
        }

        public void Dispose()
        {
            if (GameTimeManager.Instance != null)
                GameTimeManager.Instance.OnGameMinuteChanged -= HandleGameMinuteChanged;

            _placements.Clear();
            _runtimeSpawned.Clear();
        }

        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            _runtimeSpawned.Clear();
            RebuildPlacementRegistry();
            ApplyRespawnStatesToScene();
        }

        #endregion

        // ── 씬 배치 등록 / 씬 적용 ────────────────────────────────────

        /// <summary>
        /// 현재 씬의 SceneEntityId 보유 몬스터를 배치 정보로 등록한다.
        /// WorldStateManager가 영구 처치 몬스터를 Destroy한 직후 호출돼도,
        /// Destroy는 프레임 말에 실행되므로 스캔에는 포함될 수 있다(영구 처치는 재스폰 상태가 없어 무해).
        /// </summary>
        private void RebuildPlacementRegistry()
        {
            _placements.Clear();

            var monsters = UnityEngine.Object.FindObjectsByType<MonsterActor>(FindObjectsSortMode.None);
            foreach (var monster in monsters)
            {
                if (monster == null) continue;
                var entityId = monster.GetComponent<SceneEntityId>();
                if (entityId == null || !entityId.HasGuid) continue;

                _placements[entityId.Guid] = new PlacementInfo
                {
                    actorId = monster.ActorId,
                    position = monster.transform.position,
                    rotation = monster.transform.rotation,
                    baseLevel = monster.Level,
                    grade = monster.Grade,
                    group = monster.AIController?.Group,
                    placedInstance = monster,
                };
            }
        }

        /// <summary>
        /// 현재 맵의 재스폰 상태를 씬에 적용한다.
        /// - 대기 중 + 시간 미도래: 살아있는 인스턴스(배치/런타임) 제거
        /// - 대기 중 + 시간 도래: 살아있는 인스턴스가 있으면 그대로 재스폰 처리, 없으면 새로 스폰
        /// - 생존(재스폰됨): 살아있는 인스턴스에 현재 레벨/보상 재적용
        /// 세이브 로드 직후(WorldStateManager.ImportSaveData)에도 호출된다.
        /// 씬 리로드 없는 인게임 즉시 로드에서는 이번 세션에 재스폰된 런타임 인스턴스가
        /// 씬에 남아 있으므로, 임포트된 상태와 대조해 재사용/제거한다.
        /// </summary>
        public void ApplyRespawnStatesToScene()
        {
            string mapId = SceneManager.Instance?.CurrentMapID;
            if (string.IsNullOrEmpty(mapId)) return;

            // guid → 살아있는 런타임 재스폰 인스턴스. 상태 처리에서 소비되고,
            // 남은 항목(임포트된 상태에 없는 guid)은 마지막에 원본 배치로 되돌린다.
            var runtimeByGuid = new Dictionary<string, MonsterActor>();
            foreach (var kv in _runtimeSpawned)
            {
                if (kv.Key != null)
                    runtimeByGuid[kv.Value] = kv.Key;
            }

            float now = GameTimeManager.Instance != null ? GameTimeManager.Instance.TotalGameMinutes : 0f;
            int removed = 0, revived = 0;

            var states = WorldStateManager.Instance?.GetRespawnStates(mapId);
            if (states != null)
            {
                foreach (var state in states)
                {
                    _placements.TryGetValue(state.guid, out var placement);
                    runtimeByGuid.TryGetValue(state.guid, out var runtimeInstance);
                    runtimeByGuid.Remove(state.guid);

                    MonsterActor placed = placement?.placedInstance != null
                        ? placement.placedInstance
                        : runtimeInstance;

                    if (state.waitingRespawn)
                    {
                        if (now < state.nextRespawnGameMinute)
                        {
                            // 아직 재스폰 시간이 안 됨 → 살아있는 인스턴스 제거
                            if (placed != null)
                            {
                                DestroyInstance(placed, placement);
                                removed++;
                            }
                        }
                        else
                        {
                            // 시간 도래 → 살아있는 인스턴스가 있으면 그대로 재스폰 처리, 없으면 새로 스폰
                            MonsterActor target = placed != null ? placed : SpawnFromState(state);
                            if (target != null)
                            {
                                CompleteRespawn(target, state);
                                revived++;
                            }
                        }
                    }
                    else if (placed != null)
                    {
                        // 재스폰되어 생존 중인 몬스터: 씬 재진입/로드 시 성장한 레벨/보상을 재적용
                        ApplyLevelAndRewards(placed, state);
                    }
                    else
                    {
                        // 생존 상태인데 살아있는 인스턴스가 없음 → 새로 스폰
                        var spawned = SpawnFromState(state);
                        if (spawned != null)
                        {
                            ApplyLevelAndRewards(spawned, state);
                            RegisterRuntimeTracking(spawned, state.guid);
                        }
                    }
                }
            }

            // 임포트된 상태에 없는 런타임 인스턴스: 세이브 기준으로는 원본 배치 몬스터가
            // 손대지 않은 채 살아있는 상태다. 스케일된 인스턴스를 제거하고 원본 배치로 되돌린다.
            foreach (var kv in runtimeByGuid)
            {
                DestroyInstance(kv.Value, null);
                removed++;
                RestoreOriginalPlacement(kv.Key);
            }

            int interactableRestored = revived > 0
                ? InteractionRespawnManager.Instance?.RespawnConsumedInteractables() ?? 0
                : 0;

            if (removed > 0 || revived > 0 || interactableRestored > 0)
                Debug.Log($"[MonsterRespawnManager] 맵 '{mapId}' 적용 — 제거 {removed}, 재스폰 {revived}, 인터랙션 복구 {interactableRestored}");
        }

        /// <summary> 인스턴스를 파괴하고 배치/런타임 추적 참조를 함께 정리한다. </summary>
        private void DestroyInstance(MonsterActor monster, PlacementInfo placement)
        {
            _runtimeSpawned.Remove(monster);
            if (placement != null && placement.placedInstance == monster)
                placement.placedInstance = null;
            UnityEngine.Object.Destroy(monster.gameObject);
        }

        /// <summary>
        /// 원본 배치 정보로 기본 레벨 몬스터를 다시 스폰한다.
        /// 세이브에 재스폰 상태가 없는 guid를 인게임 로드에서 원복할 때 사용한다.
        /// </summary>
        private void RestoreOriginalPlacement(string guid)
        {
            if (!_placements.TryGetValue(guid, out var placement)) return;

            var actor = ActorSpawnManager.Instance?.SpawnActor(
                placement.actorId, placement.position, placement.rotation, placement.group);
            if (actor is not MonsterActor monster) return;

            // 스폰 인스턴스에는 SceneEntityId가 없으므로 사망 시 guid 복원용 추적을 등록한다.
            RegisterRuntimeTracking(monster, guid);
        }

        // ── 사망 → 재스폰 예약 ────────────────────────────────────────

        /// <summary>
        /// 몬스터 사망 시 재스폰 예약을 시도한다. 성공하면 true.
        /// false를 반환하면 호출자(MonsterActor)가 영구 처치로 기록해야 한다.
        /// 제외 대상: 보스 등급, 합류(recruitableAs) 몬스터, 추적 정보가 없는 동적 스폰.
        /// </summary>
        public bool TryScheduleRespawn(MonsterActor monster, string mapId, string guid)
        {
            if (monster == null || string.IsNullOrEmpty(mapId)) return false;

            if (!Settings.IsGradeRespawnable(monster.Grade)) return false;
            if (monster.RecruitableAs != CharacterActorType.None) return false;

            // guid가 없으면(런타임 재스폰 인스턴스) 내부 추적에서 복원한다.
            if (string.IsNullOrEmpty(guid) && !_runtimeSpawned.TryGetValue(monster, out guid))
                return false;
            if (string.IsNullOrEmpty(guid)) return false;

            _runtimeSpawned.Remove(monster);

            float now = GameTimeManager.Instance != null ? GameTimeManager.Instance.TotalGameMinutes : 0f;
            float interval = Settings.GetIntervalMinutes(monster.Grade);

            if (WorldStateManager.Instance != null
                && WorldStateManager.Instance.TryGetRespawnState(mapId, guid, out var state))
            {
                // 기존 상태 갱신(재사망): 누적 카운트/최초 사망 시각은 유지
                state.waitingRespawn = true;
                state.nextRespawnGameMinute = now + interval;
            }
            else
            {
                if (!_placements.TryGetValue(guid, out var placement)) return false;

                state = new MonsterRespawnState
                {
                    mapId = mapId,
                    guid = guid,
                    actorId = placement.actorId,
                    position = new SerializableVector3(placement.position),
                    rotation = new SerializableQuaternion(placement.rotation),
                    grade = placement.grade.ToString(),
                    baseLevel = placement.baseLevel,
                    waitingRespawn = true,
                    respawnCount = 0,
                    firstKilledGameMinute = now,
                    nextRespawnGameMinute = now + interval,
                };
            }

            WorldStateManager.Instance?.SetRespawnState(state);
            return true;
        }

        // ── 인게임 분 경과 → due 재스폰 처리 ──────────────────────────

        private void HandleGameMinuteChanged(int day, float minuteOfDay)
        {
            string mapId = SceneManager.Instance?.CurrentMapID;
            if (string.IsNullOrEmpty(mapId)) return;

            var states = WorldStateManager.Instance?.GetRespawnStates(mapId);
            if (states == null || states.Count == 0) return;

            float now = GameTimeManager.Instance.TotalGameMinutes;
            int respawnedCount = 0;

            foreach (var state in states)
            {
                if (!state.waitingRespawn || now < state.nextRespawnGameMinute) continue;

                var spawned = SpawnFromState(state);
                if (spawned == null) continue;

                CompleteRespawn(spawned, state);
                respawnedCount++;
            }

            if (respawnedCount > 0)
            {
                InteractionRespawnManager.Instance?.RespawnConsumedInteractables();
                ShowRespawnNotice(respawnedCount);
            }
        }

        // ── 스폰 / 레벨·보상 적용 ─────────────────────────────────────

        private MonsterActor SpawnFromState(MonsterRespawnState state)
        {
            MonsterGroupController group = null;
            if (_placements.TryGetValue(state.guid, out var placement))
                group = placement.group;

            var actor = ActorSpawnManager.Instance?.SpawnActor(
                state.actorId,
                state.position.ToVector3(),
                state.rotation.ToQuaternion(),
                group);

            if (actor is not MonsterActor monster)
            {
                Debug.LogError($"[MonsterRespawnManager] '{state.actorId}' 재스폰 실패 (guid: {state.guid})");
                return null;
            }

            return monster;
        }

        /// <summary>
        /// 재스폰 확정 처리: 레벨/보상 적용 + 상태 갱신 + 런타임 추적 등록.
        /// state는 저장소에 이미 들어있는 참조이므로 필드 변경만으로 반영된다
        /// (열거 중 SetRespawnState 재호출로 딕셔너리를 건드리지 않는다).
        /// </summary>
        private void CompleteRespawn(MonsterActor monster, MonsterRespawnState state)
        {
            state.waitingRespawn = false;
            state.respawnCount++;

            ApplyLevelAndRewards(monster, state);
            RegisterRuntimeTracking(monster, state.guid);
        }

        /// <summary>
        /// 재스폰 인스턴스를 사망 시 guid 복원용으로 추적한다.
        /// 씬 배치 원본(컴포넌트 guid == 배치 guid)은 사망 시 컴포넌트에서 직접 읽으므로 추적이 필요 없다.
        /// 그 외에는 무조건 추적 등록한다 — 프리팹 에셋에 guid가 구워진 채 복제된 인스턴스는
        /// 낡은 guid를 갖고 있어(HasGuid true), 방치하면 사망 시 잘못된 guid로 영구 처치가
        /// 기록되고 재스폰 예약이 조용히 실패한다. 이런 컴포넌트는 제거해 guid 복원 경로를 강제한다.
        /// </summary>
        private void RegisterRuntimeTracking(MonsterActor monster, string guid)
        {
            var entityId = monster.GetComponent<SceneEntityId>();
            if (entityId != null && entityId.HasGuid && entityId.Guid == guid)
                return;

            if (entityId != null)
                UnityEngine.Object.Destroy(entityId);
            _runtimeSpawned[monster] = guid;
        }

        /// <summary>
        /// 재스폰 상태 기반 목표 레벨을 계산해 몬스터에 적용하고 경험치/골드 보상을 스케일링한다.
        /// </summary>
        private void ApplyLevelAndRewards(MonsterActor monster, MonsterRespawnState state)
        {
            int targetLevel = ComputeTargetLevel(state);
            monster.ApplyRuntimeLevel(targetLevel);

            MonsterActorGrade grade = ParseGrade(state.grade);
            float gradeMult = Settings.GetRewardMultiplier(grade);
            int levelDelta = Mathf.Max(0, targetLevel - Mathf.Max(1, state.baseLevel));

            float expMult = gradeMult * Mathf.Pow(1f + Settings.expPerLevelRate, levelDelta);
            float goldMult = gradeMult * Mathf.Pow(1f + Settings.goldPerLevelRate, levelDelta);

            long exp = (long)Math.Round(monster.BaseExpReward * expMult);
            int gold = Mathf.RoundToInt(monster.BaseGoldReward * goldMult);
            monster.SetRuntimeRewards(exp, gold);
        }

        /// <summary>
        /// targetLevel = baseLevel + floor(경과일 * levelUpPerGameDay) + floor(respawnCount / respawnCountPerLevel)
        /// (min: minRespawnLevel, max: baseLevel + maxRespawnLevelBonus)
        /// </summary>
        private int ComputeTargetLevel(MonsterRespawnState state)
        {
            float now = GameTimeManager.Instance != null ? GameTimeManager.Instance.TotalGameMinutes : 0f;
            int baseLevel = Mathf.Max(1, state.baseLevel);

            int elapsedDays = Mathf.FloorToInt(
                Mathf.Max(0f, now - state.firstKilledGameMinute) / WorldTimeSettingsSO.MinutesPerDay);
            int bonusByDay = Mathf.FloorToInt(elapsedDays * Settings.levelUpPerGameDay);
            int bonusByCount = state.respawnCount / Mathf.Max(1, Settings.respawnCountPerLevel);

            int target = baseLevel + Mathf.Min(bonusByDay + bonusByCount, Settings.maxRespawnLevelBonus);
            return Mathf.Clamp(target, Settings.minRespawnLevel, baseLevel + Settings.maxRespawnLevelBonus);
        }

        private static MonsterActorGrade ParseGrade(string grade)
        {
            return Enum.TryParse(grade, out MonsterActorGrade parsed) ? parsed : MonsterActorGrade.Normal;
        }

        // ── 안내 UI ──────────────────────────────────────────────────

        private void ShowRespawnNotice(int count)
        {
            var uiManager = UIManager.Instance;
            if (uiManager == null) return;

            // DB 미등록 시 ShowUI 내부 에러 로그가 분 이벤트마다 반복되지 않도록 먼저 확인한다.
            if (uiManager.GetUIPrefabEntry(NoticeUIKey) == null)
            {
                if (!_noticePrefabMissingLogged)
                {
                    _noticePrefabMissingLogged = true;
                    Debug.LogWarning($"[MonsterRespawnManager] '{NoticeUIKey}' UI 프리팹이 UIPrefabDatabase에 없습니다. 안내 연출을 건너뜁니다.");
                }
                return;
            }

            var uiObject = uiManager.ShowUI(NoticeUIKey);
            var notice = uiObject != null ? uiObject.GetComponentInChildren<UI_WorldRespawnNotice>() : null;
            if (notice == null) return;

            string message = count > 1
                ? $"쓰러졌던 마물 {count}체가 다시 출현했습니다."
                : "쓰러졌던 마물들이 다시 움직이기 시작했습니다.";
            notice.ShowNotice(message);
        }

        // ── 설정 로드 ────────────────────────────────────────────────

        private async UniTask LoadSettingsAsync()
        {
            try
            {
                var loaded = await AssetManager.Instance.LoadGlobalAsync<MonsterRespawnSettingsSO>(
                    SettingsKey, nameof(MonsterRespawnManager));
                if (loaded != null) _settings = loaded;
            }
            catch (Exception)
            {
                // 설정 에셋이 Addressables에 없으면 코드 기본값으로 동작한다(에러 아님).
                Debug.Log("[MonsterRespawnManager] MonsterRespawnSettings 에셋이 없어 기본값으로 동작합니다.");
            }
        }
    }
}
