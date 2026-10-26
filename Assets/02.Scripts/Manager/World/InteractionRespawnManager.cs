using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 시간 기반 월드 리스폰 시 소모된 채집/파괴형 인터랙션 오브젝트를 복구한다.
    /// 데이터 저장은 WorldStateManager가 담당하고, 이 매니저는 현재 씬 오브젝트 적용만 담당한다.
    /// </summary>
    public class InteractionRespawnManager : BaseManager<InteractionRespawnManager>, IManager,
        IInteractionPersistenceService
    {
        private class PlacementInfo
        {
            public string mapId;
            public string guid;
            public int sceneHandle;
            public GatheringActor actor;
            public UPlayGround.DropItemActor dropItemActor;
        }

        private readonly Dictionary<(string mapId, string guid), PlacementInfo> _placements = new();
        private IGameTimeService _clock;

        public void Init() { }
        public void AfterInit()
        {
            if (Services.TryGet<IGameTimeService>(out _clock))
                _clock.OnGameMinuteChanged += OnGameMinuteChanged;
        }
        public void Dispose()
        {
            if (_clock != null) _clock.OnGameMinuteChanged -= OnGameMinuteChanged;
            _clock = null;
            _placements.Clear();
        }
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
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                RegisterRegion(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));
        }

        /// <summary>지역 로드 시 이웃의 등록과 재생 타이머를 유지한다.</summary>
        public void RegisterRegion(UnityEngine.SceneManagement.Scene scene)
        {

            var actors = UnityEngine.Object.FindObjectsByType<GatheringActor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var actor in actors)
            {
                if (actor == null || actor.gameObject.scene != scene) continue;

                var entityId = actor.GetComponent<SceneEntityId>();
                if (entityId == null || !entityId.HasGuid) continue;

                _placements[(World.RegionOwnership.Resolve(actor), entityId.Guid)] = new PlacementInfo
                {
                    actor = actor, mapId = World.RegionOwnership.Resolve(actor), guid = entityId.Guid, sceneHandle = actor.gameObject.scene.handle,
                };
            }

            var dropItemActors = UnityEngine.Object.FindObjectsByType<UPlayGround.DropItemActor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var actor in dropItemActors)
            {
                if (actor == null || actor.gameObject.scene != scene) continue;

                var entityId = actor.GetComponent<SceneEntityId>();
                if (entityId == null || !entityId.HasGuid) continue;

                _placements[(World.RegionOwnership.Resolve(actor), entityId.Guid)] = new PlacementInfo
                {
                    dropItemActor = actor, mapId = World.RegionOwnership.Resolve(actor), guid = entityId.Guid, sceneHandle = actor.gameObject.scene.handle,
                };
            }
        }

        /// <summary> 저장된 소모 상태를 현재 씬에 적용한다. 세이브 로드 직후에도 호출된다. </summary>
        public void ApplyConsumedStatesToScene()
        {
            var maps = new HashSet<string>();
            foreach (var placement in _placements.Values) maps.Add(placement.mapId);
            foreach (string mapId in maps) ApplyConsumedStatesToRegion(mapId);
        }

        private void ApplyConsumedStatesToRegion(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return;

            var consumed = WorldStateManager.Instance?.GetConsumedInteractables(mapId);

            // 같은 씬에서 더 이른 저장을 불러오면 현재 꺼진 채집물도 저장 상태로 되돌린다.
            foreach (var pair in _placements)
                if (pair.Key.mapId == mapId && pair.Value.actor != null && pair.Value.actor.GetData()?.regrowGameMinutes > 0f
                    && WorldStateManager.Instance?.IsInteractableConsumed(mapId, pair.Key.guid) == false)
                    RestorePlacement(pair.Value);
            if (consumed == null || consumed.Count == 0) return;

            int applied = 0;
            foreach (string guid in consumed)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                if (!_placements.TryGetValue((mapId, guid), out var placement)) continue;
                if (placement.actor != null)
                {
                    placement.actor.ApplyConsumedState();
                    applied++;
                }
                else if (placement.dropItemActor != null)
                {
                    placement.dropItemActor.ApplyConsumedState();
                    applied++;
                }
            }

            if (applied > 0)
                Debug.Log($"[InteractionRespawnManager] 맵 '{mapId}' 소모된 인터랙션 오브젝트 {applied}개 적용");
            RestoreMaturePlacements();
        }

        private void OnGameMinuteChanged(int day, float minuteOfDay) => RestoreMaturePlacements();

        private void RestoreMaturePlacements()
        {
            var world = WorldStateManager.Instance;
            if (world == null || _clock == null) return;
            float now = _clock.CurrentDay * UPlayGround.Data.World.WorldTimeSettingsSO.MinutesPerDay + _clock.MinuteOfDay;
            foreach (var pair in _placements)
            {
                string mapId = pair.Key.mapId;
                var data = pair.Value.actor != null ? pair.Value.actor.GetData() : null;
                if (data == null || data.regrowGameMinutes <= 0f || !world.IsInteractableConsumed(mapId, pair.Key.guid)) continue;
                if (now - world.GetInteractionConsumedMinute(mapId, pair.Key.guid) < data.regrowGameMinutes) continue;
                world.RestoreConsumedInteractable(mapId, pair.Key.guid);
                RestorePlacement(pair.Value);
            }
        }

        /// <summary>지역 실체만 제거하고 소비 기록은 보존한다.</summary>
        public void UnregisterRegion(UnityEngine.SceneManagement.Scene scene)
        {
            var keys = new List<(string mapId, string guid)>();
            foreach (var pair in _placements)
                if (pair.Value.sceneHandle == scene.handle) keys.Add(pair.Key);
            foreach (var key in keys) _placements.Remove(key);
        }

        private static void RestorePlacement(PlacementInfo placement)
        {
            if (placement.actor != null)
            {
                placement.actor.ResetForRespawn();
                placement.actor.gameObject.SetActive(true);
            }
            else if (placement.dropItemActor != null)
            {
                placement.dropItemActor.ResetForRespawn();
                placement.dropItemActor.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 채집/파괴형 인터랙션 오브젝트 소모를 기록하고 현재 씬 상태를 소모됨으로 적용한다.
        /// SceneEntityId가 없으면 false를 반환해 호출자가 기존 Destroy 흐름으로 폴백한다.
        /// </summary>
        public bool TryConsume(GatheringActor actor)
        {
            if (actor == null) return false;

            string mapId = World.RegionOwnership.Resolve(actor);
            if (string.IsNullOrEmpty(mapId)) return false;

            var entityId = actor.GetComponent<SceneEntityId>();
            if (entityId == null || !entityId.HasGuid) return false;

            WorldStateManager.Instance?.RecordConsumedInteractable(mapId, entityId.Guid,
                trackRegrowth: actor.GetData()?.regrowGameMinutes > 0f);
            _placements[(World.RegionOwnership.Resolve(actor), entityId.Guid)] = new PlacementInfo { actor = actor, mapId = mapId, guid = entityId.Guid, sceneHandle = actor.gameObject.scene.handle };

            actor.ApplyConsumedState();
            return true;
        }

        public void RegisterRuntimePlacement(GatheringActor actor, string guid)
        {
            if (actor == null || string.IsNullOrEmpty(guid)) return;

            _placements[(World.RegionOwnership.Resolve(actor), guid)] = new PlacementInfo { actor = actor, mapId = World.RegionOwnership.Resolve(actor), guid = guid, sceneHandle = actor.gameObject.scene.handle };

            string mapId = World.RegionOwnership.Resolve(actor);
            if (WorldStateManager.Instance != null
                && WorldStateManager.Instance.IsInteractableConsumed(mapId, guid))
                actor.ApplyConsumedState();
        }

        /// <summary>
        /// 맵 배치 아이템 픽업 소모를 기록하고 현재 씬 상태를 소모됨으로 적용한다.
        /// SceneEntityId가 없으면 false를 반환해 호출자가 Destroy 흐름으로 폴백한다.
        /// </summary>
        public bool TryConsume(UPlayGround.DropItemActor actor)
        {
            if (actor == null) return false;

            string mapId = World.RegionOwnership.Resolve(actor);
            if (string.IsNullOrEmpty(mapId)) return false;

            var entityId = actor.GetComponent<SceneEntityId>();
            if (entityId == null || !entityId.HasGuid) return false;

            WorldStateManager.Instance?.RecordConsumedInteractable(mapId, entityId.Guid);
            _placements[(World.RegionOwnership.Resolve(actor), entityId.Guid)] = new PlacementInfo { dropItemActor = actor, mapId = mapId, guid = entityId.Guid, sceneHandle = actor.gameObject.scene.handle };

            actor.ApplyConsumedState();
            return true;
        }

        public void RegisterRuntimePlacement(UPlayGround.DropItemActor actor, string guid)
        {
            if (actor == null || string.IsNullOrEmpty(guid)) return;

            _placements[(World.RegionOwnership.Resolve(actor), guid)] = new PlacementInfo { dropItemActor = actor, mapId = World.RegionOwnership.Resolve(actor), guid = guid, sceneHandle = actor.gameObject.scene.handle };

            string mapId = World.RegionOwnership.Resolve(actor);
            if (WorldStateManager.Instance != null
                && WorldStateManager.Instance.IsInteractableConsumed(mapId, guid))
                actor.ApplyConsumedState();
        }

        /// <summary>
        /// 현재 맵의 소모된 인터랙션 오브젝트를 모두 복구한다.
        /// 현재 씬에 없는 GUID도 저장 상태에서는 제거되어 다음 진입 시 원본 배치가 살아난다.
        /// </summary>
        public int RespawnConsumedInteractables() => RespawnConsumedInteractables(SceneManager.Instance.CurrentMapID);

        /// <summary>재생을 요청한 지역의 기록만 적용한다.</summary>
        public int RespawnConsumedInteractables(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return 0;

            var consumed = WorldStateManager.Instance?.GetConsumedInteractables(mapId);
            if (consumed == null || consumed.Count == 0) return 0;

            var guids = new List<string>(consumed);

            int restored = 0;
            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                if (!_placements.TryGetValue((mapId, guid), out var placement))
                {
                    if (!WorldStateManager.Instance.HasInteractionRegrowth(mapId, guid))
                        WorldStateManager.Instance.RestoreConsumedInteractable(mapId, guid);
                    continue;
                }
                // 농작물은 몬스터 재생성으로 조기 복구하지 않는다.
                if (placement.actor != null && placement.actor.GetData()?.regrowGameMinutes > 0f) continue;
                WorldStateManager.Instance.RestoreConsumedInteractable(mapId, guid);
                if (placement.actor != null)
                {
                    placement.actor.ResetForRespawn();
                    placement.actor.gameObject.SetActive(true);
                    restored++;
                }
                else if (placement.dropItemActor != null)
                {
                    placement.dropItemActor.ResetForRespawn();
                    placement.dropItemActor.gameObject.SetActive(true);
                    restored++;
                }
            }

            if (restored > 0)
                Debug.Log($"[InteractionRespawnManager] 맵 '{mapId}' 인터랙션 오브젝트 {restored}개 리스폰");

            return restored;
        }
    }
}
