using UnityEngine;
using Game.FSM; // StateSO 참조를 위해 추가

namespace Game.Skills
{
    public enum SkillType
    {
        Instant,    // 즉시 발동
        Charged,    // 차징 필요
        Toggle,     // 토글 (ON/OFF)
        Channeling  // 지속 시전
    }
    
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skill Data", order = 0)]
    public class SkillData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string skillName = "새 스킬";
        [SerializeField] private int skillID = 0;
        [SerializeField] [TextArea(3, 5)] private string description = "스킬 설명";
        [SerializeField] private Sprite icon;
        
        [Header("FSM 연결 (핵심 수정)")]
        [Tooltip("스킬 사용 시 전환될 상태(State)입니다.")]
        public StateSO ExecutionState; 

        [Header("스킬 설정")]
        [SerializeField] private SkillType skillType = SkillType.Instant;
        [SerializeField] private float cooldownTime = 5f;
        
        [Header("상태 전환 및 애니메이션")]
        [Tooltip("스킬 발동 시 재생할 애니메이션 키")]
        [SerializeField] private string actionAnimKey = "SkillActionDefault";
        public string ActionAnimKey => actionAnimKey;
        
        [SerializeField] private float chargeTime = 1f; 
        
        [Header("코스트")]
        [SerializeField] private int manaCost = 0;
        [SerializeField] private int energyCost = 0;
        
        // 기존 시각 효과 Prefab은 State의 OnEnter에서 생성하도록 변경 권장하지만, 데이터로 남겨둠
        [SerializeField] private GameObject skillEffectPrefab;
        [SerializeField] private AudioClip skillSound;
        
        // 프로퍼티
        public string SkillName => skillName;
        public int SkillID => skillID;
        public string Description => description;
        public Sprite Icon => icon;
        public SkillType Type => skillType;
        public float CooldownTime => cooldownTime;
        public float ChargeTime => chargeTime;
        public int ManaCost => manaCost;
        public int EnergyCost => energyCost;
        public GameObject SkillEffectPrefab => skillEffectPrefab;
        public AudioClip SkillSound => skillSound;
        
        // State 접근용 프로퍼티
        public StateSO ExecutionStateRef => ExecutionState;
    }
}