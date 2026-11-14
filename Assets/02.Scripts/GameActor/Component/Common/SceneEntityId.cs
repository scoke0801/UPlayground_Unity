using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>
    /// 씬에 배치된 오브젝트에 세션 간 안정적인 고유 식별자(GUID)를 부여한다.
    /// 몬스터 처치 영속화처럼 "이 특정 배치 인스턴스가 어떤 상태인가"를 추적할 때 사용한다.
    ///
    /// - instanceID는 실행마다 재생성되고, ActorId는 타입 단위라 개별 식별에 쓸 수 없다.
    /// - GUID는 에디터에서 부여(비어 있으면 OnValidate가 자동 생성, 일괄 보정은 SceneEntityIdAssigner).
    /// - 프리팹 에셋 자체에는 부여하지 않는다(씬 인스턴스에서만 유효).
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneEntityId : MonoBehaviour
    {
        [SerializeField, Tooltip("씬 배치 인스턴스의 고유 GUID. 비우면 에디터에서 자동 생성된다.")]
        private string _guid;

        // 복제(Ctrl+D / 복사-붙여넣기) 감지용. Unity는 복제 시 _guid를 그대로 복사하므로,
        // 소유 인스턴스 식별자가 바뀌면 복제로 판단하고 GUID를 재발급한다.
        [SerializeField, HideInInspector]
        private string _ownerKey;

        public string Guid => _guid;

        public bool HasGuid => !string.IsNullOrEmpty(_guid);

        public void RuntimeSetGuid(string guid)
        {
            if (!string.IsNullOrEmpty(guid))
                _guid = guid;
        }

#if UNITY_EDITOR
        private static System.Collections.Generic.HashSet<SceneEntityId> _pendingNewIds;

        /// <summary>새 컴포넌트의 소유 키만 일괄 조회한다. 기존 ID와 일반 복제 검사는 유지한다.</summary>
        public static void EditorInitializeNewIds(System.Action author)
        {
            if (_pendingNewIds != null) { author(); return; }
            _pendingNewIds = new System.Collections.Generic.HashSet<SceneEntityId>();
            try { author(); }
            finally
            {
                try
                {
                    var targets = new System.Collections.Generic.List<SceneEntityId>();
                    foreach (var target in _pendingNewIds) if (target != null) targets.Add(target);
                    var keys = new UnityEditor.GlobalObjectId[targets.Count];
                    UnityEditor.GlobalObjectId.GetGlobalObjectIdsSlow(targets.ToArray(), keys);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        targets[i]._ownerKey = keys[i].ToString();
                        UnityEditor.EditorUtility.SetDirty(targets[i]);
                        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(targets[i]);
                    }
                }
                finally { _pendingNewIds = null; }
            }
        }

        /// <summary> 에디터 전용: GUID를 강제로 설정한다(일괄 보정 툴에서 사용). </summary>
        public void EditorSetGuid(string guid)
        {
            _guid = guid;
            _ownerKey = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this).ToString();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // 프리팹 에셋 편집 모드(씬에 속하지 않은 오브젝트)에는 부여하지 않는다.
            if (!gameObject.scene.IsValid()) return;

            if (_pendingNewIds != null)
            {
                if (_pendingNewIds.Contains(this)) return;
                if (string.IsNullOrEmpty(_guid))
                {
                    _guid = System.Guid.NewGuid().ToString("N");
                    _pendingNewIds.Add(this);
                    return;
                }
            }

            // 소유 인스턴스 식별자. 복제본은 원본과 _guid는 같지만 이 키가 달라진다.
            string myKey = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this).ToString();

            // GUID가 비었거나(신규) 복제본이면(소유 키 불일치) 새 GUID 발급.
            if (string.IsNullOrEmpty(_guid) || _ownerKey != myKey)
            {
                _guid = System.Guid.NewGuid().ToString("N");
                _ownerKey = myKey;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
