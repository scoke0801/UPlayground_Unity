using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Components.LivingWorld;
using UPlayGround.Manager.World;

namespace UPlayGround.Gameplay.Interaction.LivingWorld
{
    /// <summary>정상 생성 완료된 기존 주민 몸을 배치 GUID별 생활 바인딩에 연결한다.</summary>
    public sealed class NpcLifePlacementBinding : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            public string sceneEntityGuid;
            public NpcRoutine routine;
        }

        [SerializeField] private RuntimePlacementLoader _loader;
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        private static readonly HashSet<NpcLifePlacementBinding> ActiveBindings = new();
        private readonly HashSet<NpcRoutine> _boundRoutines = new();
        private bool _isRegistered;
        public string BindingFailure { get; private set; }

        private void OnEnable()
        {
            if (_isRegistered) return;
            if (_loader == null) { BindingFailure = "생활 배치 로더 누락"; return; }
            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.sceneEntityGuid) || entry.routine == null
                    || string.IsNullOrWhiteSpace(entry.routine.MapId))
                { BindingFailure = "생활 배치 GUID 또는 일과 누락"; return; }
                for (int j = 0; j < i; j++)
                    if (_entries[j].sceneEntityGuid == entry.sceneEntityGuid || _entries[j].routine == entry.routine)
                    { BindingFailure = "같은 GUID 또는 일과의 생활 바인딩 중복"; return; }
            }
            ActiveBindings.RemoveWhere(binding => binding == null);
            ActiveBindings.Add(this);
            _isRegistered = true;
            _loader.NpcSpawned += OnNpcSpawned;
            _loader.SpawnedCleared += OnCleared;
            for (int i = 0; i < _entries.Length; i++)
                if (RejectSharedOwnership(_entries[i])) return;
            BindExistingBodies();
        }

        private void BindExistingBodies()
        {
            if (!_isRegistered || _loader == null) return;
            for (int i = 0; i < _loader.Instances.Count; i++)
            {
                var body = _loader.Instances[i];
                if (body == null) continue;
                var npc = body.GetComponent<NpcActor>();
                var id = body.GetComponent<SceneEntityId>();
                if (npc != null && id != null) OnNpcSpawned(id.Guid, npc);
            }
        }

        private void OnNpcSpawned(string guid, NpcActor npc)
        {
            if (!_isRegistered || npc == null) return;
            Entry match = null;
            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry == null || string.IsNullOrEmpty(guid) || entry.sceneEntityGuid != guid) continue;
                if (match != null) { BindingFailure = "같은 GUID의 생활 바인딩 중복"; return; }
                match = entry;
            }
            if (match == null) return;
            if (match.routine == null) { BindingFailure = "생활 일과 소실"; return; }
            if (RejectSharedOwnership(match)) return;
            for (int i = 0; i < _loader.Instances.Count; i++)
            {
                var body = _loader.Instances[i];
                if (body == null || body == npc.gameObject) continue;
                var id = body.GetComponent<SceneEntityId>();
                if (id != null && id.Guid == guid)
                { BindingFailure = "같은 GUID의 주민 몸 중복"; match.routine?.UnbindActor(); _boundRoutines.Remove(match.routine); return; }
            }
            if (match.routine == null || !match.routine.TryBindActor(npc))
            { BindingFailure = "주민 일과 바인딩 실패"; return; }
            _boundRoutines.Add(match.routine);
            BindingFailure = null;
        }

        private bool RejectSharedOwnership(Entry match)
        {
            foreach (var binding in ActiveBindings)
            {
                if (binding == this || binding == null) continue;
                for (int i = 0; i < binding._entries.Length; i++)
                {
                    var other = binding._entries[i];
                    if (other == null || other.routine == null) continue;
                    if (other.routine != match.routine && (other.sceneEntityGuid != match.sceneEntityGuid
                        || other.routine.MapId != match.routine.MapId)) continue;
                    BindingFailure = binding.BindingFailure = "같은 맵/GUID를 소유하는 생활 바인딩 중복";
                    match.routine.UnbindActor(); other.routine.UnbindActor();
                    _boundRoutines.Remove(match.routine); binding._boundRoutines.Remove(other.routine);
                    return true;
                }
            }
            return false;
        }

        private void OnCleared()
        {
            foreach (var routine in _boundRoutines) if (routine != null) routine.UnbindActor();
            _boundRoutines.Clear();
            BindingFailure = null;
            if (_isRegistered)
                for (int i = 0; i < _entries.Length; i++)
                    if (RejectSharedOwnership(_entries[i])) return;
        }

        private void OnDestroy() => OnDisable();

        private void OnDisable()
        {
            bool wasRegistered = _isRegistered;
            _isRegistered = false;
            ActiveBindings.Remove(this);
            if (_loader != null)
            {
                _loader.NpcSpawned -= OnNpcSpawned;
                _loader.SpawnedCleared -= OnCleared;
            }
            OnCleared();
            if (!wasRegistered) return;
            foreach (var binding in ActiveBindings)
                if (binding != null && binding._isRegistered) binding.BindExistingBodies();
        }
    }
}
