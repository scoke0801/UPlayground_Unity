using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 맵별 월드 상태(처치된 몬스터 등)를 추적·영속화하는 매니저.
    ///
    /// 처치 기록: MonsterActor.OnDeath에서 SceneEntityId가 있으면 RecordKill 호출.
    /// 복원 적용: 씬 전환(OnSceneChanged) 시 현재 맵의 처치 GUID에 해당하는
    ///            배치 몬스터를 제거한다(다시 살아나지 않음).
    ///
    /// 세이브 데이터는 순수 데이터라 비동기 DB 의존이 없으므로,
    /// ImportSaveData에서 즉시 _killedMonsters를 교체하고 다음 씬 로드 시 적용한다.
    /// </summary>
    public class WorldStateManager : BaseManager<WorldStateManager>, IManager, ISaveable
    {
        // mapId → 처치된 SceneEntityId GUID 집합
        private readonly Dictionary<string, HashSet<string>> _killedMonsters = new();

        // 씬 스캔 시 재사용해 GC 할당 방지
        private static readonly List<MonsterActor> _sceneMonsterBuffer = new();

        #region IManager

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit() { }
        public void Dispose() { }
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            // 씬 컨텍스트가 확정된 시점(CurrentMapID 유효). 처치된 배치 몬스터 제거.
            ApplyKilledToScene(SceneManager.Instance?.CurrentMapID);
        }

        #endregion

        // ── 처치 기록 / 조회 ─────────────────────────────────────────

        /// <summary> 지정 맵에서 GUID 몬스터를 처치 상태로 기록한다. </summary>
        public void RecordKill(string mapId, string guid)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid)) return;

            if (!_killedMonsters.TryGetValue(mapId, out var set))
            {
                set = new HashSet<string>();
                _killedMonsters[mapId] = set;
            }
            set.Add(guid);
        }

        /// <summary> 지정 맵에서 GUID 몬스터가 이미 처치됐는지 여부. </summary>
        public bool IsKilled(string mapId, string guid)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid)) return false;
            return _killedMonsters.TryGetValue(mapId, out var set) && set.Contains(guid);
        }

        // ── 씬 적용 ──────────────────────────────────────────────────

        /// <summary>
        /// 현재 씬의 배치 몬스터 중, 해당 맵에서 이미 처치된 GUID를 가진 것을 제거한다.
        /// SceneEntityId가 없는(동적 스폰 등) 몬스터는 추적 대상이 아니므로 건드리지 않는다.
        /// </summary>
        private void ApplyKilledToScene(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return;
            if (!_killedMonsters.TryGetValue(mapId, out var killedSet) || killedSet.Count == 0) return;

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
                Debug.Log($"[WorldStateManager] 맵 '{mapId}' 처치 몬스터 {_sceneMonsterBuffer.Count}개 제거");
            _sceneMonsterBuffer.Clear();
        }

        // ── 저장 / 복원 (ISaveable) ─────────────────────────────────

        public void ExportSaveData(GameSaveData saveData)
        {
            if (saveData == null) return;
            var world = saveData.world ??= new WorldStateSaveData();

            world.killedMonsters = new Dictionary<string, List<string>>(_killedMonsters.Count);
            foreach (var kv in _killedMonsters)
                world.killedMonsters[kv.Key] = new List<string>(kv.Value);
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _killedMonsters.Clear();

            var world = saveData?.world;
            if (world?.killedMonsters == null) return;

            foreach (var kv in world.killedMonsters)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                _killedMonsters[kv.Key] = new HashSet<string>(kv.Value ?? new List<string>());
            }

            // 현재 씬이 이미 로드돼 있다면(인게임에서 즉시 로드 등) 바로 적용.
            ApplyKilledToScene(SceneManager.Instance?.CurrentMapID);
        }

        public void ResetForNewGame()
        {
            // 처치 기록을 모두 비운다. 이후 씬 전환 시 ApplyKilledToScene이 제거할 대상이
            // 없으므로 배치 몬스터가 전부 정상 스폰된다(새 게임 = 전 맵 몬스터 부활).
            _killedMonsters.Clear();
        }
    }
}
