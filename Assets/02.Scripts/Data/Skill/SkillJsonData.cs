using System;
using UnityEngine;

namespace Game.Data
{
    public enum SkillType
    {
        Instant,    // 즉시 발동
        Charged,    // 차징 필요
        Toggle,     // 토글 (ON/OFF)
        Channeling  // 지속 시전
    }
    /// <summary>
    /// Json에서 로드할 스킬 데이터 클래스 (ILoader 구현)
    /// ScriptableObject 없이 순수 Json 기반으로 동작합니다.
    /// </summary>
    [Serializable]
    public class SkillJsonData : ILoader<int, SkillJsonData>
    {
        public int skillID;
        public string skillName;
        public string description;
        public string iconPath;
        public string executionStatePath;
        public string skillType;  // "Instant", "Charged", "Toggle", "Channeling"
        public float cooldownTime;
        public string actionAnimKey;
        public float chargeTime;
        public int manaCost;
        public int energyCost;
        public string effectPrefabPath;
        public string soundPath;

        // 런타임 캐시 (Resources.Load로 로드된 에셋)
        [NonSerialized] private Sprite _cachedIcon;
        [NonSerialized] private GameObject _cachedEffectPrefab;
        [NonSerialized] private AudioClip _cachedSound;
        
        public int GetKey() => skillID;
        
        // 프로퍼티
        public string SkillName => skillName;
        public int SkillID => skillID;
        public string Description => description;
        public string ActionAnimKey => actionAnimKey;
        public float CooldownTime => cooldownTime;
        public float ChargeTime => chargeTime;
        public int ManaCost => manaCost;
        public int EnergyCost => energyCost;
        
        // SkillType enum 변환
        public SkillType Type
        {
            get
            {
                if (System.Enum.TryParse<SkillType>(skillType, out var parsedType))
                    return parsedType;
                return SkillType.Instant;
            }
        }
        
        // 에셋 로드 (지연 로딩)
        public Sprite Icon
        {
            get
            {
                if (_cachedIcon == null && !string.IsNullOrEmpty(iconPath))
                {
                    _cachedIcon = Resources.Load<Sprite>(iconPath);
                }
                return _cachedIcon;
            }
        }
        
        public GameObject SkillEffectPrefab
        {
            get
            {
                if (_cachedEffectPrefab == null && !string.IsNullOrEmpty(effectPrefabPath))
                {
                    _cachedEffectPrefab = Resources.Load<GameObject>(effectPrefabPath);
                }
                return _cachedEffectPrefab;
            }
        }
        
        public AudioClip SkillSound
        {
            get
            {
                if (_cachedSound == null && !string.IsNullOrEmpty(soundPath))
                {
                    _cachedSound = Resources.Load<AudioClip>(soundPath);
                }
                return _cachedSound;
            }
        }
    }

    /// <summary>
    /// Json 파싱용 래퍼 (Unity JsonUtility는 배열을 직접 파싱 못함)
    /// </summary>
    [Serializable]
    public class SkillDataWrapper
    {
        public SkillJsonData[] skills;
    }
}
