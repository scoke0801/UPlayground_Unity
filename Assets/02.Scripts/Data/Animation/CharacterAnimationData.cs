using UnityEngine;
using System.Collections.Generic;
using Animancer;

namespace Game.FSM
{
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
                
                clipDictionary[entry.key] = entry.transition;
            }

            mixerDictionary = new Dictionary<string, LinearMixerTransition>();
            foreach (var entry in mixerAnimations)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                
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
                return mixer;
            }

            Debug.LogWarning($"[CharacterAnimationData] '{key}' MixerTransition을 찾을 수 없습니다: {gameObject.name}");
            return default;
        }
    }
}