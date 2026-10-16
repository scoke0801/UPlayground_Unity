using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// speakerId → 대화 초상화 매핑 테이블.
    /// NPC 초상화의 단일 소스이며, 노드마다 스프라이트를 꽂지 않아도 화자 이름만으로 해석된다.
    /// 노드의 portrait 필드는 "이 줄만 다른 표정"을 위한 오버라이드로만 쓰고, 기본 초상화는 여기 등록한다.
    /// DialogueManager가 Addressables로 로드해 관리한다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/Speaker Portrait Table", fileName = "SpeakerPortraitTable")]
    public class SpeakerPortraitTableSO : ScriptableObject
    {
        // Addressables 등록 키 — SO 에셋의 Address 값과 일치해야 한다.
        public const string AddressableKey = "SpeakerPortraitTable";

        [System.Serializable]
        public class SpeakerPortraitEntry
        {
            [Tooltip("DialogueNodeSO.speakerId와 정확히 일치해야 한다.")]
            public string speakerId;
            public Sprite portrait;
        }

        [SerializeField] private List<SpeakerPortraitEntry> entries = new();

        public DialoguePortraitSetSO[] presentationSets = System.Array.Empty<DialoguePortraitSetSO>();

        /// <summary>캐릭터 식별과 명시 화자 바인딩으로 이관된 표정 세트만 찾는다.</summary>
        public DialoguePortraitSetSO FindPresentation(string speakerId,
            UPlayGround.Data.EnumType.CharacterActorType active,
            UPlayGround.Data.EnumType.CharacterActorType protagonist)
        {
            bool reserved = DialogueSpeakerResolver.IsActivePlayerSpeaker(speakerId)
                || DialogueSpeakerResolver.IsProtagonistSpeaker(speakerId);
            var type = DialogueSpeakerResolver.IsActivePlayerSpeaker(speakerId) ? active : protagonist;
            foreach (var set in presentationSets)
            {
                if (set == null) continue;
                if (reserved)
                {
                    if (type != UPlayGround.Data.EnumType.CharacterActorType.None && set.characterType == type) return set;
                    continue;
                }
                foreach (string id in set.speakerIds)
                    if (!string.IsNullOrEmpty(id) && id == speakerId) return set;
            }
            return null;
        }

        private Dictionary<string, Sprite> _portraitMap;

        /// <summary>등록된 화자 목록. 에디터 검증 도구가 미등록 speakerId를 찾는 데 쓴다.</summary>
        public IReadOnlyList<SpeakerPortraitEntry> Entries => entries;

        private void OnEnable() => BuildMap();

        /// <summary>등록된 초상화를 반환한다. 미등록이면 null.</summary>
        public Sprite GetPortrait(string speakerId)
        {
            if (_portraitMap == null) BuildMap();

            if (!string.IsNullOrEmpty(speakerId) && _portraitMap.TryGetValue(speakerId, out var portrait))
                return portrait;

            return null;
        }

        private void BuildMap()
        {
            _portraitMap = new Dictionary<string, Sprite>();
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.speakerId) && entry.portrait != null)
                    _portraitMap[entry.speakerId] = entry.portrait;
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => BuildMap();
#endif
    }
}
