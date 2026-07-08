using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 시간 기반 월드 리스폰 시 소모된 채집/파괴형 인터랙션 오브젝트를 복구한다.
    /// 데이터 저장은 WorldStateManager가 담당하고, 이 매니저는 현재 씬 오브젝트 적용만 담당한다.
    /// </summary>
    public class InteractionRespawnManager : BaseManager<InteractionRespawnManager>, IManager
    {
        private class PlacementInfo
        {
            public GatheringActor actor;
        }

        private readonly Dictionary<string, PlacementInfo> _placements = new();

        public void Init() { }
        public void AfterInit() { }
        public void Dispose() => _placements.Clear();
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            RebuildPlacementRegistry();
            ApplyConsumedStatesToScene();
        }

        private void RebuildPlacementRegistry()
        {
            _placements.Clear();

            var actors = UnityEngine.Object.FindObjectsByType<GatheringActor>(FindObjectsSortMode.None);
            foreach (var actor in actors)
            {
                if (actor == null) continue;

                var entityId = actor.GetComponent<SceneEntityId>();
                if (entityId == null || !entityId.HasGuid) continue;

                _placements[entityId.Guid] = new PlacementInfo
                {
                    actor = actor,
                };
            }
        }

        /// <summary> 저장된 소모 상태를 현재 씬에 적용한다. 세이브 로드 직후에도 호출된다. </summary>
        public void ApplyConsumedStatesToScene()
        {
            string mapId = SceneManager.Instance?.CurrentMapID;
            if (string.IsNullOrEmpty(mapId)) return;

            var consumed = WorldStateManager.Instance?.GetConsumedInteractables(mapId);
            if (consumed == null || consumed.Count == 0) return;

            int applied = 0;
            foreach (string guid in consumed)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                if (!_placements.TryGetValue(guid, out var placement)) continue;
                if (placement.actor == null) continue;

                placement.actor.ApplyConsumedState();
                applied++;
            }

            if (applied > 0)
                Debug.Log($"[InteractionRespawnManager] 맵 '{mapId}' 소모된 인터랙션 오브젝트 {applied}개 적용");
        }

        /// <summary>
        /// 채집/파괴형 인터랙션 오브젝트 소모를 기록하고 현재 씬 상태를 소모됨으로 적용한다.
        /// SceneEntityId가 없으면 false를 반환해 호출자가 기존 Destroy 흐름으로 폴백한다.
        /// </summary>
        public bool TryConsume(GatheringActor actor)
        {
            if (actor == null) return false;

            string mapId = SceneManager.Instance?.CurrentMapID;
            if (string.IsNullOrEmpty(mapId)) return false;

            var entityId = actor.GetComponent<SceneEntityId>();
            if (entityId == null || !entityId.HasGuid) return false;

            WorldStateManager.Instance?.RecordConsumedInteractable(mapId, entityId.Guid);
            _placements[entityId.Guid] = new PlacementInfo { actor = actor };

            actor.ApplyConsumedState();
            return true;
        }

        /// <summary>
        /// 현재 맵의 소모된 인터랙션 오브젝트를 모두 복구한다.
        /// 현재 씬에 없는 GUID도 저장 상태에서는 제거되어 다음 진입 시 원본 배치가 살아난다.
        /// </summary>
        public int RespawnConsumedInteractables()
        {
            string mapId = SceneManager.Instance?.CurrentMapID;
            if (string.IsNullOrEmpty(mapId)) return 0;

            var consumed = WorldStateManager.Instance?.GetConsumedInteractables(mapId);
            if (consumed == null || consumed.Count == 0) return 0;

            var guids = new List<string>(consumed);
            WorldStateManager.Instance.ClearConsumedInteractables(mapId);

            int restored = 0;
            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                if (!_placements.TryGetValue(guid, out var placement)) continue;
                if (placement.actor == null) continue;

                placement.actor.ResetForRespawn();
                placement.actor.gameObject.SetActive(true);
                restored++;
            }

            if (restored > 0)
                Debug.Log($"[InteractionRespawnManager] 맵 '{mapId}' 인터랙션 오브젝트 {restored}개 리스폰");

            return restored;
        }
    }
}
