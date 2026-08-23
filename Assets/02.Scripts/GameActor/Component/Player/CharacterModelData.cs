using Animancer;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

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
        [Header("Definition")]
        [SerializeField] private PlayerCharacterDefinitionSO _definition;

        [Header("Sockets — Model 내부 본")]
        [SerializeField] private SerializedDictionary<ActorSocketType, Transform> _socketDict = new();

        [Header("Foot IK Bones")]
        public Transform HipBone;
        public Transform LeftFootBone;
        public Transform RightFootBone;

        public SerializedDictionary<ActorSocketType, Transform> SocketDict => _socketDict;
        public PlayerCharacterDefinitionSO Definition => _definition;
        public CharacterActorType characterType =>
            _definition != null
                ? _definition.characterType
                : CharacterActorType.None;
        public WeaponType defaultWeaponType =>
            _definition != null
                ? _definition.defaultWeaponType
                : WeaponType.NoWeapon;
        public UPlayGround.Data.Ability.AbilitySetSO abilitySet =>
            _definition != null ? _definition.abilitySet : null;
        public UPlayGround.Data.Ability.AbilityResourceRuleSO abilityResourceRules =>
            _definition != null ? _definition.abilityResourceRules : null;
        public UPlayGround.Data.Actor.CharacterWeightProfileSO weightProfile =>
            _definition != null ? _definition.weightProfile : null;
        public float entryAttackRange =>
            _definition != null ? _definition.entryAttackRange : 0f;
        public bool requireLineOfSight =>
            _definition != null
            && _definition.requireEntryAttackLineOfSight;

        public AnimancerComponent AnimancerComponent { get; private set; }

        private void Awake()
        {
            _socketDict ??= new SerializedDictionary<ActorSocketType, Transform>();
            AnimancerComponent = GetComponent<AnimancerComponent>();
        }


        /// <summary>분리된 캐릭터 게임플레이 정의를 모델 뷰에 연결한다.</summary>
        public void AssignDefinition(PlayerCharacterDefinitionSO definition)
        {
            _definition = definition;
        }
    }
}
