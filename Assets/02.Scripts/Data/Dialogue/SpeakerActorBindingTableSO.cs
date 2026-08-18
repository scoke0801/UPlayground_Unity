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

            [Tooltip("월드에 없을 때 대화 동안만 세울 대역의 ActorDatabase ID. 비우면 actorId를 씁니다.")]
            public string standInActorId;
        }

        [SerializeField] private List<SpeakerActorEntry> entries = new();

        private Dictionary<string, string> _map;
        private Dictionary<string, string> _standInMap;

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

        /// <summary>
        /// 대화 동안만 세울 대역의 ActorId. 전용 지정이 없으면 일반 actorId로 폴백한다.
        /// 씬 배치 액터와 스폰용 정의의 ID가 다른 인물을 데이터로 교정할 수 있게 분리했다.
        /// </summary>
        public bool TryGetStandInActorId(string speakerId, out string actorId)
        {
            if (_standInMap == null) BuildMap();

            if (!string.IsNullOrEmpty(speakerId)
                && _standInMap.TryGetValue(speakerId, out actorId)
                && !string.IsNullOrEmpty(actorId))
            {
                return true;
            }

            return TryGetActorId(speakerId, out actorId);
        }

        private void BuildMap()
        {
            _map = new Dictionary<string, string>();
            _standInMap = new Dictionary<string, string>();
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.speakerId))
                    continue;

                if (!string.IsNullOrEmpty(entry.actorId))
                    _map[entry.speakerId] = entry.actorId;
                if (!string.IsNullOrEmpty(entry.standInActorId))
                    _standInMap[entry.speakerId] = entry.standInActorId;
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => BuildMap();
#endif
    }
}
