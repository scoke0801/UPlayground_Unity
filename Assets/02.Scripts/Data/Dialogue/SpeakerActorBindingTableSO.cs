using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// Dialogue speakerId를 런타임 ActorId로 매핑한다.
    /// 항목이 없으면 호출부에서 speakerId == actorId 폴백을 사용할 수 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/Speaker Binding Table", fileName = "SpeakerActorBindingTable")]
    public class SpeakerActorBindingTableSO : ScriptableObject
    {
        public const string AddressableKey = "SpeakerActorBindingTable";

        [System.Serializable]
        public class SpeakerActorEntry
        {
            public string speakerId;
            public string actorId;
        }

        [SerializeField] private List<SpeakerActorEntry> entries = new();

        private Dictionary<string, string> _map;

        private void OnEnable() => BuildMap();

        public bool TryGetActorId(string speakerId, out string actorId)
        {
            if (_map == null) BuildMap();

            if (!string.IsNullOrEmpty(speakerId) &&
                _map.TryGetValue(speakerId, out actorId) &&
                !string.IsNullOrEmpty(actorId))
                return true;

            actorId = null;
            return false;
        }

        private void BuildMap()
        {
            _map = new Dictionary<string, string>();
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.speakerId) && !string.IsNullOrEmpty(entry.actorId))
                    _map[entry.speakerId] = entry.actorId;
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => BuildMap();
#endif
    }
}
