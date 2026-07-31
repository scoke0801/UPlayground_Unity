using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Animation
{
    public partial class PlayerActorAnimator : ActorAnimator
    {
        // 무기는 오른손에 장착된 무기를 기준으로 한다?
        [Header("PlayerActor Only")]
        [SerializeField] private PlayerActorAnimationMotionSet _playerActorAnimationMotionSet;

        private PlayerActor _playerActor;
        private PlayerEquipment _playerEquipment;
        private PlayerCombat _playerCombat;

        public bool IsOpenedComboWindow { get; set; } = false;
        public PlayerActorAnimationMotionSet PlayerMotionSet => _playerActorAnimationMotionSet;

        public override void Init(GameActor actor)
        {
            base.Init(actor);
            
            _playerActor = actor as PlayerActor;
            if (_playerActor != null)
            {
                _playerEquipment = _playerActor.GetPlayerEquipment();
                _playerCombat = _playerActor.GetCombat();
            }
        }

        /// <summary>
        /// 플레이어는 무기별 세트(_playerActorAnimationMotionSet)에서 모션 에셋을 해석한다.
        /// 상체 오버레이(PlayUpperBodyOverlay)가 올바른 소스를 쓰도록 base의 _motionSet 대신 이걸 사용한다.
        /// </summary>
        protected override MotionSetAsset ResolveMotionSetAsset(GameplayTag slot)
            => _playerActorAnimationMotionSet != null
                ? _playerActorAnimationMotionSet.GetMotionSetAsset(
                    GetActiveWeaponTypeForMotion(slot), slot)
                : null;

        protected override MotionSetAsset ResolveAbilityMotionAsset(
            AbilityMotionKey key) =>
            _playerActorAnimationMotionSet != null
                ? _playerActorAnimationMotionSet.GetAbilityMotionAsset(
                    _playerEquipment != null
                        ? _playerEquipment.GetMainWeaponType()
                        : WeaponType.NoWeapon,
                    key)
                : null;

        private WeaponType GetActiveWeaponTypeForMotion(GameplayTag slot)
        {
            if (_playerEquipment == null)
                return WeaponType.NoWeapon;

            if (slot.IsChildOf(MotionTags.Equipment))
                return _playerEquipment.GetMainWeaponType();

            return _playerEquipment.IsMainWeaponEquipped
                ? _playerEquipment.GetMainWeaponType()
                : WeaponType.NoWeapon;
        }

    }

    public partial class PlayerActorAnimator : ActorAnimator
    {
        /// <summary>
        /// 애니메이션 이벤트: 히트 판정 실행
        /// 각 공격 애니메이션의 무기가 적에게 닿는 프레임에 호출됨
        /// </summary>
        private void OnAnimationEvent_HitCheck()
        {
            if (_playerCombat == null)
            {
                Debug.LogWarning("[PlayerActorAnimator] PlayerCombat이 없습니다!");
                return;
            }
            
            _playerCombat.PerformHitDetection();
            
            Debug.Log($"[PlayerActorAnimator] 히트 판정 실행: {Time.frameCount} 프레임");
        }
    }
}
