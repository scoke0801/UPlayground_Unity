using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 맵별 월드 상태(처치된 몬스터, 소모된 채집 오브젝트 등)를 추적·영속화하는 매니저.
    ///
    /// 처치 기록은 두 갈래로 나뉜다:
    ///   - 영구 처치(permanent kill): 보스/합류 몬스터 등 재스폰 제외 대상.
    ///     씬 전환 시 해당 배치 몬스터를 제거한다(다시 살아나지 않음).
    ///   - 재스폰 상태(respawn state): 일반 필드 몬스터. 사망 시각/다음 재스폰 시각/횟수를 보관하며,
    ///     실제 재스폰 판정·스폰은 MonsterRespawnManager가 담당한다(이 매니저는 데이터 저장소).
    ///
    /// 분기 판정은 MonsterActor.NotifyWorldStateKill → MonsterRespawnManager.TryScheduleRespawn에서 한다.
    /// 구버전 세이브의 killedMonsters는 전부 영구 처치로 읽는다.
    /// </summary>
    public class WorldStateManager : BaseManager<WorldStateManager>, IManager, ISaveable,
        IElementRandomSeedService
    {
        public int NewGameElementSeed { get; private set; }

        // mapId → 영구 처치된 SceneEntityId GUID 집합
        private readonly Dictionary<string, HashSet<string>> _permanentKilled = new();

        // mapId → (guid → 재스폰 상태)
        private readonly Dictionary<string, Dictionary<string, MonsterRespawnState>> _respawnStates = new();

        // mapId → 소모된 채집/파괴형 인터랙션 오브젝트 SceneEntityId GUID 집합
        private readonly Dictionary<string, HashSet<string>> _consumedInteractables = new();

        // 씬 스캔 시 재사용해 GC 할당 방지
        private static readonly List<MonsterActor> _sceneMonsterBuffer = new();

        #region IManager

        public void Init()
        {
            EnsureElementRandomSeed();
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit() { }
        public void Dispose() { }
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            // 씬 컨텍스트가 확정된 시점(CurrentMapID 유효). 영구 처치된 배치 몬스터 제거.
            // 재스폰 상태 몬스터의 제거/복원은 MonsterRespawnManager.OnSceneChanged가 처리한다.
            ApplyPermanentKilledToScene(SceneManager.Instance?.CurrentMapID);
        }

        #endregion

        // ── 영구 처치 기록 / 조회 ─────────────────────────────────────

        /// <summary> 지정 맵에서 GUID 몬스터를 영구 처치 상태로 기록한다(재스폰 없음). </summary>
        public void RecordPermanentKill(string mapId, string guid)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid)) return;

            if (!_permanentKilled.TryGetValue(mapId, out var set))
            {
                set = new HashSet<string>();
                _permanentKilled[mapId] = set;
            }
            set.Add(guid);
        }

        /// <summary> 호환용 별칭. 내부적으로 RecordPermanentKill로 위임한다. </summary>
        public void RecordKill(string mapId, string guid) => RecordPermanentKill(mapId, guid);

        /// <summary> 지정 맵에서 GUID 몬스터가 영구 처치됐는지 여부. </summary>
        public bool IsPermanentlyKilled(string mapId, string guid)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid)) return false;
            return _permanentKilled.TryGetValue(mapId, out var set) && set.Contains(guid);
        }

        /// <summary> 호환용 별칭. </summary>
        public bool IsKilled(string mapId, string guid) => IsPermanentlyKilled(mapId, guid);

        // ── 재스폰 상태 저장소 ────────────────────────────────────────

        /// <summary> 재스폰 상태를 기록/갱신한다(mapId+guid 키로 교체). </summary>
        public void SetRespawnState(MonsterRespawnState state)
        {
            if (state == null || string.IsNullOrEmpty(state.mapId) || string.IsNullOrEmpty(state.guid)) return;

            if (!_respawnStates.TryGetValue(state.mapId, out var map))
            {
                map = new Dictionary<string, MonsterRespawnState>();
                _respawnStates[state.mapId] = map;
            }
            map[state.guid] = state;
        }

        public bool TryGetRespawnState(string mapId, string guid, out MonsterRespawnState state)
        {
            state = null;
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid)) return false;
            return _respawnStates.TryGetValue(mapId, out var map) && map.TryGetValue(guid, out state);
        }

        /// <summary> 지정 맵의 재스폰 상태 목록(없으면 null). 순회 중 수정 금지. </summary>
        public IReadOnlyCollection<MonsterRespawnState> GetRespawnStates(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return null;
            return _respawnStates.TryGetValue(mapId, out var map) ? map.Values : null;
        }

        public void RemoveRespawnState(string mapId, string guid)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid)) return;
            if (_respawnStates.TryGetValue(mapId, out var map))
                map.Remove(guid);
        }

        // ── 인터랙션 오브젝트 소모 상태 ───────────────────────────────

        public void RecordConsumedInteractable(string mapId, string guid)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid)) return;

            if (!_consumedInteractables.TryGetValue(mapId, out var set))
            {
                set = new HashSet<string>();
                _consumedInteractables[mapId] = set;
            }
            set.Add(guid);
        }

        public bool IsInteractableConsumed(string mapId, string guid)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid)) return false;
            return _consumedInteractables.TryGetValue(mapId, out var set) && set.Contains(guid);
        }

        /// <summary> 지정 맵의 소모된 인터랙션 GUID 목록(없으면 null). 순회 중 수정 금지. </summary>
        public IReadOnlyCollection<string> GetConsumedInteractables(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return null;
            return _consumedInteractables.TryGetValue(mapId, out var set) ? set : null;
        }

        public void ClearConsumedInteractables(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return;
            _consumedInteractables.Remove(mapId);
        }

        // ── 씬 적용 ──────────────────────────────────────────────────

        /// <summary>
        /// 현재 씬의 배치 몬스터 중, 해당 맵에서 영구 처치된 GUID를 가진 것을 제거한다.
        /// SceneEntityId가 없는(동적 스폰 등) 몬스터는 추적 대상이 아니므로 건드리지 않는다.
        /// </summary>
        private void ApplyPermanentKilledToScene(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return;
            if (!_permanentKilled.TryGetValue(mapId, out var killedSet) || killedSet.Count == 0) return;

            _sceneMonsterBuffer.Clear();
            var monsters = UnityEngine.Object.FindObjectsByType<MonsterActor>(FindObjectsSortMode.None);
            foreach (var monster in monsters)
            {
                if (monster == null) continue;
                var entityId = monster.GetComponent<SceneEntityId>();
                if (entityId != null && entityId.HasGuid && killedSet.Contains(entityId.Guid))
                    _sceneMonsterBuffer.Add(monster);
            }

            foreach (var monster in _sceneMonsterBuffer)
                UnityEngine.Object.Destroy(monster.gameObject);

            if (_sceneMonsterBuffer.Count > 0)
                Debug.Log($"[WorldStateManager] 맵 '{mapId}' 영구 처치 몬스터 {_sceneMonsterBuffer.Count}개 제거");
            _sceneMonsterBuffer.Clear();
        }

        // ── 저장 / 복원 (ISaveable) ─────────────────────────────────

        public void ExportSaveData(GameSaveData saveData)
        {
            if (saveData == null) return;
            var world = saveData.world ??= new WorldStateSaveData();
            world.elementRandomSeed = NewGameElementSeed;

            world.killedMonsters = new Dictionary<string, List<string>>(_permanentKilled.Count);
            foreach (var kv in _permanentKilled)
                world.killedMonsters[kv.Key] = new List<string>(kv.Value);

            world.respawnStates = new List<MonsterRespawnState>();
            foreach (var mapStates in _respawnStates.Values)
                world.respawnStates.AddRange(mapStates.Values);

            world.consumedInteractables = new Dictionary<string, List<string>>(_consumedInteractables.Count);
            foreach (var kv in _consumedInteractables)
                world.consumedInteractables[kv.Key] = new List<string>(kv.Value);
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _permanentKilled.Clear();
            _respawnStates.Clear();
            _consumedInteractables.Clear();

            var world = saveData?.world;
            NewGameElementSeed = world?.elementRandomSeed ?? 0;
            EnsureElementRandomSeed();
            if (world == null) return;

            if (world.killedMonsters != null)
            {
                foreach (var kv in world.killedMonsters)
                {
                    if (string.IsNullOrEmpty(kv.Key)) continue;
                    _permanentKilled[kv.Key] = new HashSet<string>(kv.Value ?? new List<string>());
                }
            }

            if (world.respawnStates != null)
            {
                foreach (var state in world.respawnStates)
                    SetRespawnState(state);
            }

            if (world.consumedInteractables != null)
            {
                foreach (var kv in world.consumedInteractables)
                {
                    if (string.IsNullOrEmpty(kv.Key)) continue;
                    _consumedInteractables[kv.Key] = new HashSet<string>(kv.Value ?? new List<string>());
                }
            }

            // 현재 씬이 이미 로드돼 있다면(인게임에서 즉시 로드 등) 바로 적용.
            ApplyPermanentKilledToScene(SceneManager.Instance?.CurrentMapID);
            MonsterRespawnManager.Instance?.ApplyRespawnStatesToScene();
            InteractionRespawnManager.Instance?.ApplyConsumedStatesToScene();
        }

        public void ResetForNewGame()
        {
            // 처치/재스폰 기록을 모두 비운다. 이후 씬 전환 시 제거할 대상이 없으므로
            // 배치 몬스터가 전부 정상 스폰된다(새 게임 = 전 맵 몬스터 부활).
            _permanentKilled.Clear();
            _respawnStates.Clear();
            _consumedInteractables.Clear();
            NewGameElementSeed = CreateElementRandomSeed();
        }

        private void EnsureElementRandomSeed()
        {
            if (NewGameElementSeed == 0)
                NewGameElementSeed = CreateElementRandomSeed();
        }

        private static int CreateElementRandomSeed()
        {
            int seed;
            do
            {
                seed = System.Guid.NewGuid().GetHashCode();
            } while (seed == 0);
            return seed;
        }
    }
}
