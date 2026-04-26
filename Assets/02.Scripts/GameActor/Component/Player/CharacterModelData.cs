using Animancer;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Component
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

        [Header("Combat")]
        public PlayerAttackDataSO attackData;

        [Header("Stats")]
        public float maxHealth = 100f;

        [Header("Sockets — Model 내부 본")]
        public Transform RightHandSocket;
        public Transform LeftHandSocket;
        public Transform CenterSocket;
        public Transform HeadSocket;

        [Header("Foot IK Bones")]
        public Transform HipBone;
        public Transform LeftFootBone;
        public Transform RightFootBone;

        public AnimancerComponent AnimancerComponent { get; private set; }

        private void Awake()
        {
            AnimancerComponent = GetComponent<AnimancerComponent>();
        }
    }
}
