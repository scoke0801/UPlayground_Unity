using UnityEngine;

namespace Game.Skills
{
    /// <summary>
    /// 스킬 타입
    /// </summary>
    public enum SkillType
    {
        Instant,    // 즉시 발동
        Charged,    // 차징 필요
        Toggle,     // 토글 (ON/OFF)
        Channeling  // 지속 시전
    }
    
    /// <summary>
    /// 스킬 데이터 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skill Data", order = 0)]
    public class SkillData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string skillName = "새 스킬";
        [SerializeField] private int skillID = 0;
        [SerializeField] [TextArea(3, 5)] private string description = "스킬 설명";
        [SerializeField] private Sprite icon;
        
        [Header("스킬 설정")]
        [SerializeField] private SkillType skillType = SkillType.Instant;
        [SerializeField] private float cooldownTime = 5f;
        [SerializeField] private float castTime = 0f;          // 시전 시간
        [SerializeField] private float chargeTime = 1f;        // 차징 시간 (Charged 타입)
        [SerializeField] private float duration = 0f;          // 지속 시간 (Channeling, Toggle)
        
        [Header("코스트")]
        [SerializeField] private int manaCost = 0;
        [SerializeField] private int energyCost = 0;
        
        [Header("시각 효과")]
        [SerializeField] private GameObject skillEffectPrefab;
        [SerializeField] private AudioClip skillSound;
        
        // 프로퍼티
        public string SkillName => skillName;
        public int SkillID => skillID;
        public string Description => description;
        public Sprite Icon => icon;
        public SkillType Type => skillType;
        public float CooldownTime => cooldownTime;
        public float CastTime => castTime;
        public float ChargeTime => chargeTime;
        public float Duration => duration;
        public int ManaCost => manaCost;
        public int EnergyCost => energyCost;
        public GameObject SkillEffectPrefab => skillEffectPrefab;
        public AudioClip SkillSound => skillSound;
    }
}
