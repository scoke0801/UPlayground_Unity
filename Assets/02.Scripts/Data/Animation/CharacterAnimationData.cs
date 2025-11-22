// CharacterAnimationData.cs (수정된 부분)
using UnityEngine;
using System.Collections.Generic;
using Animancer;

namespace Game.FSM
{
    // ... (ClipEntry, MixerEntry, Fields 부분은 동일) ...
    public class CharacterAnimationData : MonoBehaviour
    {
        [System.Serializable]
        public class ClipEntry
        {
            public string key;
            public ClipTransition transition;
        }

        [System.Serializable]
        public class MixerEntry
        {
            public string key;
            public LinearMixerTransition mixer;
        }

        [Header("단일 애니메이션 클립 목록")]
        [SerializeField] private List<ClipEntry> clipAnimations = new List<ClipEntry>();

        [Header("믹서 애니메이션 클립 목록")]
        [SerializeField] private List<MixerEntry> mixerAnimations = new List<MixerEntry>();

        private Dictionary<string, ClipTransition> clipDictionary;
        private Dictionary<string, LinearMixerTransition> mixerDictionary;

        /// <summary>
        /// Dictionary 초기화 (CharacterBrain에서 호출)
        /// </summary>
        public void Initialize()
        {
            if (clipDictionary != null) return; 

            clipDictionary = new Dictionary<string, ClipTransition>();
            foreach (var entry in clipAnimations)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                
                // [참고] ClipTransition의 유효성 검사는 유지
                // if (entry.transition.Clip == null) continue; // 필요하다면 유지
                
                clipDictionary[entry.key] = entry.transition;
            }

            mixerDictionary = new Dictionary<string, LinearMixerTransition>();
            foreach (var entry in mixerAnimations)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                
                // [수정] LinearMixerTransition은 Clip 속성을 가지지 않으므로, 유효성 검사 코드를 제거합니다.
                // 믹서 내부 클립이 null인 것은 런타임에 Animancer가 경고합니다.
                
                mixerDictionary[entry.key] = entry.mixer;
            }
        }

        public ClipTransition GetClipTransition(string key)
        {
            if (clipDictionary == null) Initialize();

            if (clipDictionary.TryGetValue(key, out ClipTransition transition))
            {
                if (transition.Clip == null)
                {
                    Debug.LogWarning($"[CharacterAnimationData] '{key}' 클립이 null입니다: {gameObject.name}");
                }
                return transition;
            }

            Debug.LogWarning($"[CharacterAnimationData] '{key}' ClipTransition을 찾을 수 없습니다: {gameObject.name}");
            return default;
        }

        public LinearMixerTransition GetMixerTransition(string key)
        {
            if (mixerDictionary == null) Initialize();

            if (mixerDictionary.TryGetValue(key, out LinearMixerTransition mixer))
            {
                // [수정] 컴파일 에러를 일으키는 유효성 검사 코드를 제거합니다.
                // if (mixer.Clip == null) { Debug.LogWarning(...); } 
                
                return mixer;
            }

            Debug.LogWarning($"[CharacterAnimationData] '{key}' MixerTransition을 찾을 수 없습니다: {gameObject.name}");
            return default;
        }
    }
}