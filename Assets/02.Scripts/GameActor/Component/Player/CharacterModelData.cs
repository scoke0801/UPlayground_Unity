using Animancer;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.Ability;

namespace UPlayGround.Components
{
    /// <summary>
    /// Model 서브루트에 붙는 캐릭터 식별·전투 데이터 컨테이너.
    ///
    /// 애니메이션(motionSet·avatar)은 같은 Model에 있는 PlayerActorAnimator에서 직접 관리.
    /// 장비(weaponType·Constraint)는 같은 Model에 있는 PlayerEquipment에서 직접 관리.
    /// AnimancerComponent는 같은 GameObject에서 자동 탐색.
    /// </summary>
    public class CharacterModelData : MonoBehaviour
    {
        [Header("Identity")]
        public CharacterActorType characterType;

        [Header("Equipment")]
        public WeaponType defaultWeaponType = WeaponType.NoWeapon;

        [Header("Combat")]
        [Tooltip("캐릭터의 일반 공격, 스킬, 차지, 연계 라우트를 포함하는 단일 전투 데이터입니다.")]
        public AbilitySetSO abilitySet;

        [Header("Cycle Weight")]
        public CharacterWeightProfileSO weightProfile;

        [Header("Entry Attack")]
        [Tooltip("교체 등장 시 자동 발동될 공격의 검출 반경. 0 이하이면 PartyConfigSO.defaultEntryAttackRange 사용.")]
        public float entryAttackRange = 0f;

        [Tooltip("벽 너머의 적은 무시. true 면 LOS(시야선) 검사를 통과한 적만 카운트.")]
        public bool requireLineOfSight = false;

        [HideInInspector]
        public float maxHealth = 100f;

        [Header("Sockets — Model 내부 본")]
        [SerializeField] private SerializedDictionary<ActorSocketType, Transform> _socketDict = new();

        [Header("Foot IK Bones")]
        public Transform HipBone;
        public Transform LeftFootBone;
        public Transform RightFootBone;

        public SerializedDictionary<ActorSocketType, Transform> SocketDict => _socketDict;

        public AnimancerComponent AnimancerComponent { get; private set; }

        private void Awake()
        {
            _socketDict ??= new SerializedDictionary<ActorSocketType, Transform>();
            AnimancerComponent = GetComponent<AnimancerComponent>();
        }
    }
}
