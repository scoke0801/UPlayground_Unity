using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Actor
{
    /// <summary>
    /// 프로젝트 전체 ActorDefinitionSO를 관리하는 데이터베이스 ScriptableObject.
    /// ActorSpawnManager가 참조하며, actorId를 키로 빠른 조회를 지원한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ActorDatabase", menuName = "UPlayGround/액터/Database")]
    public class ActorDatabase : ScriptableObject
    {
        [SerializeField] private List<ActorDefinitionSO> _actors = new();

        private Dictionary<string, ActorDefinitionSO> _lookup;

        /// <summary>등록된 모든 Actor 정의 목록 (읽기 전용)</summary>
        public IReadOnlyList<ActorDefinitionSO> All => _actors;

        /// <summary>
        /// 딕셔너리 초기화. ActorSpawnManager.Init()에서 호출한다.
        /// </summary>
        public void Initialize()
        {
            _lookup = new Dictionary<string, ActorDefinitionSO>();

            foreach (var def in _actors)
            {
                if (def == null) continue;

                if (string.IsNullOrEmpty(def.actorId))
                {
                    Debug.LogWarning($"[ActorDatabase] actorId가 비어있는 항목: {def.name}");
                    continue;
                }

                if (_lookup.ContainsKey(def.actorId))
                {
                    Debug.LogWarning($"[ActorDatabase] 중복된 actorId: '{def.actorId}' ({def.name})");
                    continue;
                }

                _lookup[def.actorId] = def;
            }

            Debug.Log($"[ActorDatabase] {_lookup.Count}개 Actor 정의 로드 완료");
        }

        public ActorDefinitionSO GetDefinition(string actorId)
        {
            EnsureInitialized();
            return _lookup.TryGetValue(actorId, out var def) ? def : null;
        }

        public bool TryGetDefinition(string actorId, out ActorDefinitionSO definition)
        {
            EnsureInitialized();
            return _lookup.TryGetValue(actorId, out definition);
        }

        public bool Contains(string actorId)
        {
            EnsureInitialized();
            return _lookup.ContainsKey(actorId);
        }

        private void EnsureInitialized()
        {
            if (_lookup == null) Initialize();
        }

#if UNITY_EDITOR
        public void AddDefinition(ActorDefinitionSO definition)
        {
            if (definition == null || _actors.Contains(definition)) return;
            _actors.Add(definition);
            _lookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void RemoveDefinition(ActorDefinitionSO definition)
        {
            if (definition == null) return;
            _actors.Remove(definition);
            _lookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>fromIndex 항목을 toIndex 위치(삽입 전 기준)로 이동한다. 실제 이동이 발생하면 true 반환.</summary>
        public bool MoveDefinition(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _actors.Count) return false;
            if (toIndex < 0 || toIndex > _actors.Count) return false;
            if (fromIndex == toIndex) return false;

            int insertAt = toIndex > fromIndex ? toIndex - 1 : toIndex;
            if (fromIndex == insertAt) return false;

            var item = _actors[fromIndex];
            _actors.RemoveAt(fromIndex);
            _actors.Insert(insertAt, item);
            _lookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
            return true;
        }

        /// <summary>에디터에서 순서 변경 후 호출</summary>
        public void InvalidateLookup() => _lookup = null;
#endif
    }
}
